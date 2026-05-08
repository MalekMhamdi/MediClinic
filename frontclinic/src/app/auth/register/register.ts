import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {

  // Step 1 — Personal info
  firstName  = signal('');
  lastName   = signal('');
  email      = signal('');
  phone      = signal('');
  birthDate  = signal('');
  gender     = signal('');
  bloodGroup = signal('');
  cin        = signal('');

  // Step 2 — Address & Emergency contact
  address          = signal('');
  city             = signal('');
  emergencyName    = signal('');
  emergencyRelation= signal('');
  emergencyPhone   = signal('');

  // Step 3 — Security
  password  = signal('');
  confirm   = signal('');
  showPwd   = signal(false);
  showConf  = signal(false);

  // State
  loading = signal(false);
  error   = signal('');
  step    = signal<1 | 2 | 3>(1);

  constructor(private authService: AuthService) {}

  togglePwd():  void { this.showPwd.update(v => !v); }
  toggleConf(): void { this.showConf.update(v => !v); }

  nextStep(): void {
    this.error.set('');

    if (this.step() === 1) {
      if (!this.firstName() || !this.lastName() || !this.email() || !this.phone() ||
          !this.gender() || !this.bloodGroup() || !this.cin() || !this.birthDate()) {
        this.error.set('Please fill in all fields.');
        return;
      }
      const emailRx = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRx.test(this.email())) {
        this.error.set('Please enter a valid email address.');
        return;
      }
      this.step.set(2);
      return;
    }

    if (this.step() === 2) {
      if (!this.address() || !this.city() || !this.emergencyName() || !this.emergencyRelation() || !this.emergencyPhone()) {
        this.error.set('Please fill in all fields.');
        return;
      }
      this.step.set(3);
    }
  }

  prevStep(): void {
    this.error.set('');
    this.step.update(s => (s - 1) as 1 | 2 | 3);
  }

  async submit(): Promise<void> {
    this.error.set('');

    if (!this.password() || !this.confirm()) {
      this.error.set('Please fill in all fields.');
      return;
    }
    if (this.password().length < 8) {
      this.error.set('Password must be at least 8 characters.');
      return;
    }
    if (this.password() !== this.confirm()) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.loading.set(true);
    try {
      await this.authService.registerPatient({
        firstName:   this.firstName(),
        lastName:    this.lastName(),
        email:       this.email(),
        password:    this.password(),
        cin:         this.cin(),
        dateOfBirth: this.birthDate(),
        bloodGroup:  this.bloodGroup()        || undefined,
        phone:       this.phone()             || undefined,
        address:     this.address() ? `${this.address()}, ${this.city()}`.trim() : undefined,
        emergencyContactName:     this.emergencyName()     || undefined,
        emergencyContactRelation: this.emergencyRelation() || undefined,
        emergencyContactPhone:    this.emergencyPhone()    || undefined,
      });
    } catch (e: any) {
      const msg = e?.error?.message ?? e?.message ?? 'Registration failed. Please try again.';
      this.error.set(msg);
    } finally {
      this.loading.set(false);
    }
  }

  get progressWidth(): string {
    const map = { 1: '33%', 2: '66%', 3: '100%' };
    return map[this.step()] ?? '33%';
  }

  get pwdStrength(): number {
    const p = this.password();
    if (!p) return 0;
    let score = 0;
    if (p.length >= 8)           score++;
    if (/[A-Z]/.test(p))         score++;
    if (/[0-9]/.test(p))         score++;
    if (/[^A-Za-z0-9]/.test(p))  score++;
    return score;
  }

  get pwdStrengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Good', 'Strong'][this.pwdStrength] ?? '';
  }

  get pwdStrengthClass(): string {
    return ['', 'weak', 'medium', 'good', 'strong'][this.pwdStrength] ?? '';
  }
}
