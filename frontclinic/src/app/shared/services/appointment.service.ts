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
      const doctors = res.map(d => ({
        id: d.id.toString(),
        specialtyId: d.specialtyId.toString(),
        name: `Dr. ${d.firstName} ${d.lastName}`,
        title: 'Specialist',
        experience: 'N/A',
        rating: 0,
        reviews: 0,
        avatar: d.firstName[0] + d.lastName[0],
        availableDays: []
      }));
      this.doctors.set(doctors);

      // Fetch real ratings in parallel (non-blocking)
      const ratings = await Promise.allSettled(
        doctors.map(d =>
          firstValueFrom(this.http.get<any>(`${API}/DoctorReview/doctor/${d.id}`))
        )
      );
      this.doctors.set(doctors.map((d, i) => {
        const r = ratings[i];
        if (r.status === 'fulfilled') {
          return { ...d, rating: r.value.averageRating ?? 0, reviews: r.value.totalReviews ?? 0 };
        }
        return d;
      }));
    } catch (err) {
      console.error('Failed to load doctors', err);
    }
  }

  getDoctorsBySpecialty(specialtyId: string): Doctor[] {
    return this.doctors().filter(d => d.specialtyId === specialtyId);
  }

  async getAvailableSlots(doctorId: string, date: string): Promise<TimeSlot[]> {
    try {
      const patientId = this.auth.userId;
      const pid = patientId ? `&patientId=${patientId}` : '';
      const res = await firstValueFrom(
        this.http.get<string[]>(`${API}/Appointment/available-slots?doctorId=${doctorId}&date=${date}${pid}`)
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

    const dateTime = this._buildIso(data.date, data.time);

    await firstValueFrom(
      this.http.post(`${API}/Appointment`, {
        patientId,
        doctorId: parseInt(data.doctorId),
        dateTime,
        reason: data.reason
      })
    );
  }

  /** Convertit "2026-05-08" + "08:00 AM" → ISO UTC string fiable dans tous les navigateurs */
  private _buildIso(date: string, time: string): string {
    const [year, month, day] = date.split('-').map(Number);
    const match = time.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
    if (!match) throw new Error(`Invalid time format: ${time}`);

    let hours = parseInt(match[1]);
    const minutes = parseInt(match[2]);
    const period = match[3].toUpperCase();

    if (period === 'AM' && hours === 12) hours = 0;
    if (period === 'PM' && hours !== 12) hours += 12;

    // Crée la date en heure locale (pas UTC) pour respecter le fuseau du navigateur
    const local = new Date(year, month - 1, day, hours, minutes, 0, 0);
    return local.toISOString();
  }

  async cancelAppointment(id: string): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Appointment/${id}/cancel`, {}));
  }

  async deleteAppointment(id: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/Appointment/${id}`));
  }

  // ── Reviews ───────────────────────────────────────────────────────────
  async canReview(appointmentId: number): Promise<{ canReview: boolean; reason: string | null }> {
    return firstValueFrom(
      this.http.get<any>(`${API}/DoctorReview/can-review/${appointmentId}`)
    );
  }

  async submitReview(appointmentId: number, rating: number, comment: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API}/DoctorReview`, { appointmentId, rating, comment: comment || null })
    );
  }

  async getDoctorRating(doctorId: number): Promise<{ averageRating: number; totalReviews: number; reviews: any[] }> {
    return firstValueFrom(
      this.http.get<any>(`${API}/DoctorReview/doctor/${doctorId}`)
    );
  }

  async getMyReviews(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${API}/DoctorReview/my`));
  }

  async rescheduleAppointment(id: string, newDate: string, newTime: string): Promise<void> {
    await firstValueFrom(
      this.http.patch(`${API}/Appointment/${id}/reschedule`, {
        newDateTime: this._buildIso(newDate, newTime),
        reason: 'Patient rescheduled via UI'
      })
    );
  }

  /** Normalise un datetime sans timezone en UTC — corrige l'affichage -1h sur les fuseaux positifs */
  static normalizeUtc(s: string): string {
    if (!s) return s;
    return (s.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(s)) ? s : s + 'Z';
  }
}