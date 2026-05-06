import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

const API = 'http://localhost:5217/api/v1';

export interface Specialty {
  id:          string;
  name:        string;
  icon:        string;
  description: string;
}

export interface Doctor {
  id:            string;
  specialtyId:   string;
  name:          string;
  title:         string;
  experience:    string;
  rating:        number;
  reviews:       number;
  avatar:        string;
  availableDays: string[];
}

export interface TimeSlot {
  time:      string;
  available: boolean;
}

export interface BookingData {
  specialtyId: string;
  doctorId:    string;
  date:        string;
  time:        string;
  reason:      string;
}

@Injectable({ providedIn: 'root' })
export class AppointmentService {

  specialties = signal<Specialty[]>([]);
  doctors = signal<Doctor[]>([]);

  constructor(private http: HttpClient, private auth: AuthService) {}

  private _specialtyIcon(name: string): string {
    const icons: Record<string, string> = {
      'Cardiologie':        '❤️',
      'Pneumologie':        '🫁',
      'Gastro-entérologie': '🫃',
      'Neurologie':         '🧠',
      'Orthopédie':         '🦴',
      'Dermatologie':       '🧴',
      'ORL':                '👂',
      'Médecine générale':  '🩺',
      'Endocrinologie':     '🔬',
      'Psychiatrie':        '🧘',
      'Radiologie':         '🩻',
      'Ophtalmologie':      '👁️',
      'Gynécologie':        '🌸',
      'Pédiatrie':          '👶',
      'Urologie':           '💧',
      'Rhumatologie':       '🦱',
      'Infectiologie':      '🦠',
      'Chirurgie générale': '🔪',
      'Urgences':           '🚨',
    };
    return icons[name] ?? '🩺';
  }

  async loadSpecialties(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Specialty`));
      this.specialties.set(res.map(s => ({
        id: s.id.toString(),
        name: s.name,
        icon: this._specialtyIcon(s.name),
        description: s.description || ''
      })));
    } catch (err) {
      console.error('Failed to load specialties', err);
    }
  }

  async loadDoctors(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Doctor`));
      this.doctors.set(res.map(d => ({
        id: d.id.toString(),
        specialtyId: d.specialtyId.toString(),
        name: `Dr. ${d.firstName} ${d.lastName}`,
        title: 'Specialist', // Add actual title mapping if exists
        experience: 'N/A', // Update with actual data
        rating: 5.0, // Update with actual data
        reviews: 0,
        avatar: d.firstName[0] + d.lastName[0],
        availableDays: [] // Update if this logic exists on backend
      })));
    } catch (err) {
      console.error('Failed to load doctors', err);
    }
  }

  getDoctorsBySpecialty(specialtyId: string): Doctor[] {
    return this.doctors().filter(d => d.specialtyId === specialtyId);
  }

  async getAvailableSlots(doctorId: string, date: string): Promise<TimeSlot[]> {
    try {
      const res = await firstValueFrom(
        this.http.get<string[]>(`${API}/Appointment/available-slots?doctorId=${doctorId}&date=${date}`)
      );
      
      const allSlots = [
        '08:00 AM', '08:30 AM', '09:00 AM', '09:30 AM', '10:00 AM', '10:30 AM', '11:00 AM', '11:30 AM',
        '02:00 PM', '02:30 PM', '03:00 PM', '03:30 PM', '04:00 PM', '04:30 PM'
      ];

      return allSlots.map(time => ({
        time,
        available: res.includes(time)
      }));
    } catch (err) {
      console.error('Failed to fetch available slots', err);
      return [];
    }
  }

  async getPatientAppointments(patientId: number): Promise<any[]> {
    try {
      return await firstValueFrom(
        this.http.get<any[]>(`${API}/Appointment/by-patient/${patientId}`)
      );
    } catch (err) {
      console.error('Failed to load patient appointments', err);
      return [];
    }
  }

  async bookAppointment(data: BookingData): Promise<void> {
    const patientId = this.auth.userId;
    if (!patientId) throw new Error('Not logged in');

    // Parse 'YYYY-MM-DD' and 'HH:mm AM/PM' to DateTime string expected by API
    const dateObj = new Date(`${data.date} ${data.time}`);

    await firstValueFrom(
      this.http.post(`${API}/Appointment`, {
        patientId,
        doctorId: parseInt(data.doctorId),
        dateTime: dateObj.toISOString(),
        reason: data.reason
      })
    );
  }

  async cancelAppointment(id: string): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Appointment/${id}/cancel`, {}));
  }

  async rescheduleAppointment(id: string, newDate: string, newTime: string): Promise<void> {
    const dateObj = new Date(`${newDate} ${newTime}`);
    await firstValueFrom(
      this.http.patch(`${API}/Appointment/${id}/reschedule`, {
        newDateTime: dateObj.toISOString(),
        reason: 'Patient rescheduled via UI'
      })
    );
  }
}