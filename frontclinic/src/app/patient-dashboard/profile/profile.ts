import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';

const API = 'http://localhost:5217/api/v1';

interface PatientProfile {
  firstName:   string;
  lastName:    string;
  email:       string;
  cin:         string;
  dateOfBirth: string;
  bloodGroup:  string;
  phone:       string;
  address:     string;
  emergencyContactName:     string;
  emergencyContactRelation: string;
  emergencyContactPhone:    string;
}

const EMPTY: PatientProfile = {
  firstName: '', lastName: '', email: '', cin: '', dateOfBirth: '',
  bloodGroup: '', phone: '', address: '',
  emergencyContactName: '', emergencyContactRelation: '', emergencyContactPhone: '',
};

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent implements OnInit {

  loading  = signal(true);
  editing  = signal(false);
  saving   = signal(false);
  error    = signal('');
  success  = signal('');

  showPwdForm = signal(false);
  currentPwd  = '';
  newPwd      = '';
  confirmPwd  = '';
  pwdError    = signal('');
  pwdSuccess  = signal('');
  savingPwd   = signal(false);

  // Read-only display (signal — triggers re-render on load)
  profile = signal<PatientProfile>({ ...EMPTY });

  // Mutable draft — plain object so ngModel works without signal loops
  draft: PatientProfile = { ...EMPTY };

  constructor(private auth: AuthService, private http: HttpClient) {}

  async ngOnInit(): Promise<void> {
    await this.loadProfile();
  }

  async loadProfile(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const userId = this.auth.userId;
      const [me, patient] = await Promise.all([
        firstValueFrom(this.http.get<any>(`${API}/Auth/me`)),
        firstValueFrom(this.http.get<any>(`${API}/Patient/${userId}`)),
      ]);
      const p: PatientProfile = {
        firstName:   patient.firstName  ?? '',
        lastName:    patient.lastName   ?? '',
        email:       patient.email      ?? me.email ?? '',
        cin:         patient.cin        ?? '',
        dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.split('T')[0] : '',
        bloodGroup:  patient.bloodGroup ?? '',
        phone:       patient.phone      ?? '',
        address:     patient.address    ?? '',
        emergencyContactName:     patient.emergencyContactName     ?? '',
        emergencyContactRelation: patient.emergencyContactRelation ?? '',
        emergencyContactPhone:    patient.emergencyContactPhone    ?? '',
      };
      this.profile.set(p);
    } catch {
      this.error.set('Failed to load profile. Make sure the backend is running.');
    } finally {
      this.loading.set(false);
    }
  }

  get initials(): string {
    const p = this.profile();
    return ((p.firstName?.[0] ?? '') + (p.lastName?.[0] ?? '')).toUpperCase() || 'P';
  }

  get fullName(): string {
    return `${this.profile().firstName} ${this.profile().lastName}`.trim();
  }

  startEdit(): void {
    // Copy signal value into mutable draft
    this.draft = { ...this.profile() };
    this.editing.set(true);
    this.success.set('');
    this.error.set('');
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  async saveEdit(): Promise<void> {
    if (!this.draft.firstName.trim() || !this.draft.lastName.trim()) {
      this.error.set('First name and last name are required.');
      return;
    }
    if (this.draft.email.trim()) {
      const emailRx = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRx.test(this.draft.email.trim())) {
        this.error.set('Please enter a valid email address.');
        return;
      }
    }
    this.saving.set(true);
    this.error.set('');
    try {
      await firstValueFrom(this.http.put(`${API}/Auth/profile`, {
        firstName:  this.draft.firstName.trim(),
        lastName:   this.draft.lastName.trim(),
        email:      this.draft.email.trim()    || null,
        phone:      this.draft.phone           || null,
        bloodGroup: this.draft.bloodGroup      || null,
        address:    this.draft.address         || null,
        emergencyContactName:     this.draft.emergencyContactName     || null,
        emergencyContactRelation: this.draft.emergencyContactRelation || null,
        emergencyContactPhone:    this.draft.emergencyContactPhone    || null,
      }));
      const emailChanged = this.draft.email.trim().toLowerCase() !== this.profile().email.toLowerCase();
      this.profile.set({ ...this.draft });
      this.editing.set(false);

      if (emailChanged) {
        this.success.set('Profile updated. Your email has changed — please log in again with your new address.');
        setTimeout(() => this.auth.logout(), 3000);
      } else {
        this.success.set('Profile updated successfully.');
        setTimeout(() => this.success.set(''), 4000);
      }
    } catch (e: any) {
      this.error.set(e?.error?.message ?? 'Failed to update profile. Please try again.');
    } finally {
      this.saving.set(false);
    }
  }

  togglePwdForm(): void {
    this.showPwdForm.update(v => !v);
    this.pwdError.set('');
    this.pwdSuccess.set('');
    this.currentPwd  = '';
    this.newPwd      = '';
    this.confirmPwd  = '';
  }

  async changePassword(): Promise<void> {
    this.pwdError.set('');
    if (!this.currentPwd || !this.newPwd || !this.confirmPwd) {
      this.pwdError.set('All fields are required.');
      return;
    }
    if (this.newPwd.length < 8) {
      this.pwdError.set('New password must be at least 8 characters.');
      return;
    }
    if (this.newPwd !== this.confirmPwd) {
      this.pwdError.set('Passwords do not match.');
      return;
    }
    this.savingPwd.set(true);
    try {
      await firstValueFrom(this.http.put(`${API}/Auth/change-password`, {
        currentPassword: this.currentPwd,
        newPassword:     this.newPwd,
      }));
      this.pwdSuccess.set('Password changed successfully.');
      this.showPwdForm.set(false);
    } catch (e: any) {
      this.pwdError.set(e?.error?.message ?? 'Current password is incorrect.');
    } finally {
      this.savingPwd.set(false);
    }
  }
}
