import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { firstValueFrom } from 'rxjs';

export interface Patient {
  id:         string;
  firstName:  string;
  lastName:   string;
  birthDate:  string;
  gender:     string;
  bloodGroup: string;
  cin:        string;
  phone:      string;
  email:      string;
  address:    string;
  insurance:  string;
  emergency:  { name: string; relation: string; phone: string };
}

export interface Appointment {
  id:       string;
  type:     string;
  doctor:   string;
  location: string;
  date:     Date;
  time:     string;
  status:   'confirmed' | 'pending' | 'cancelled';
}

export interface Notification {
  id:        string;
  type:      'appointment' | 'result' | 'reminder' | 'message' | 'system';
  title:     string;
  body:      string;
  timestamp: string;
  read:      boolean;
}

export interface MedicalDocument {
  id:       string;
  title:    string;
  type:     'result' | 'prescription' | 'imaging' | 'report';
  doctor:   string;
  date:     string;
  fileName: string;
  fileSize: string;
  fileType: string;
}

export interface Prescription {
  medication:  string;
  dosage:      string;
  frequency:   string;
  prescribedBy:string;
  expires:     string;
}

const API = 'http://localhost:5217/api/v1';

@Injectable({ providedIn: 'root' })
export class PatientService {

  patient = signal<Patient | null>(null);
  appointments = signal<Appointment[]>([]);
  notifications = signal<Notification[]>([]);
  documents = signal<MedicalDocument[]>([]);
  prescriptions = signal<Prescription[]>([]);

  constructor(private http: HttpClient, private auth: AuthService) {}

  // ── Loaders ──

  async loadPatientProfile(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<any>(`${API}/Auth/me`));
      // Map to frontend Patient interface
      this.patient.set({
        id: res.userId?.toString(),
        firstName: res.fullName?.split(' ')[0] || '',
        lastName: res.fullName?.split(' ').slice(1).join(' ') || '',
        birthDate: res.dateOfBirth || '',
        gender: res.gender || 'Unknown',
        bloodGroup: res.bloodGroup || '',
        cin: res.cin || '',
        phone: res.phone || '',
        email: res.email || '',
        address: res.address || '',
        insurance: '',
        emergency: { name: '', relation: '', phone: '' }
      });
    } catch (err) {
      console.error('Failed to load patient profile', err);
    }
  }

  async loadAppointments(): Promise<void> {
    const patientId = this.auth.userId;
    if (!patientId) return;
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Appointment/by-patient/${patientId}`));
      this.appointments.set(res.map(a => ({
        id: a.id.toString(),
        type: 'Consultation', // map from API if available
        doctor: `Dr. ${a.doctorName || 'Unknown'}`,
        location: 'Clinic', // map from API
        date: new Date(a.dateTime),
        time: new Date(a.dateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        status: a.status.toLowerCase() as 'confirmed' | 'pending' | 'cancelled'
      })));
    } catch (err) {
      console.error('Failed to load appointments', err);
    }
  }

  async loadNotifications(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Notification`));
      this.notifications.set(res.map(n => ({
        id: n.id.toString(),
        type: n.type.toLowerCase() as any || 'system',
        title: 'Notification',
        body: n.message,
        timestamp: new Date(n.createdAt).toLocaleString(),
        read: n.isRead
      })));
    } catch (err) {
      console.error('Failed to load notifications', err);
    }
  }

  async loadMedicalRecord(): Promise<void> {
    const patientId = this.auth.userId;
    if (!patientId) return;
    try {
      const res = await firstValueFrom(this.http.get<any>(`${API}/Patient/${patientId}/medical-record`));
      // Assuming res has documents and prescriptions arrays
      if (res.documents) this.documents.set(res.documents);
      if (res.prescriptions) this.prescriptions.set(res.prescriptions);
    } catch (err) {
      console.error('Failed to load medical record', err);
    }
  }

  // ── Actions ──

  async cancelAppointment(id: string): Promise<void> {
    try {
      await firstValueFrom(this.http.patch(`${API}/Appointment/${id}/cancel`, {}));
      this.appointments.update(list =>
        list.map(a => a.id === id ? { ...a, status: 'cancelled' as const } : a)
      );
    } catch (err) {
      console.error('Failed to cancel appointment', err);
    }
  }

  async markNotificationRead(id: string): Promise<void> {
    try {
      await firstValueFrom(this.http.patch(`${API}/Notification/${id}/read`, {}));
      this.notifications.update(list =>
        list.map(n => n.id === id ? { ...n, read: true } : n)
      );
    } catch (err) {
      console.error('Failed to mark notification read', err);
    }
  }

  async markAllNotificationsRead(): Promise<void> {
    try {
      await firstValueFrom(this.http.patch(`${API}/Notification/read-all`, {}));
      this.notifications.update(list => list.map(n => ({ ...n, read: true })));
    } catch (err) {
      console.error('Failed to mark all read', err);
    }
  }

  async updateProfile(data: Partial<Patient>): Promise<void> {
    try {
      await firstValueFrom(this.http.put(`${API}/Auth/profile`, data));
      this.patient.update(p => p ? { ...p, ...data } : null);
    } catch (err) {
      console.error('Failed to update profile', err);
    }
  }

  get unreadCount(): number {
    return this.notifications().filter(n => !n.read).length;
  }
}