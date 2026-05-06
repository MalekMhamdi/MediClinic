import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AppointmentService } from '../../shared/services/appointment.service';
import { AuthService } from '../../shared/services/auth.service';

type Tab = 'upcoming' | 'past' | 'cancelled';

interface Appointment {
  id: string;
  type: string;
  doctor: string;
  specialty: string;
  date: Date;
  time: string;
  status: 'confirmed' | 'pending' | 'cancelled' | 'refused';
}

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css'
})
export class AppointmentsComponent implements OnInit {

  activeTab = signal<Tab>('upcoming');
  appointments = signal<Appointment[]>([]);
  loading = signal(true);
  error = signal('');

  constructor(
    private appointmentService: AppointmentService,
    private authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadAppointments();
  }

  async loadAppointments(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const patientId = this.authService.userId;
      if (!patientId) { this.error.set('Not authenticated.'); return; }

      const data = await this.appointmentService.getPatientAppointments(patientId);
      this.appointments.set(data.map(a => ({
        id:       a.id.toString(),
        type:     a.reason || a.doctorSpecialty || 'Consultation',
        doctor:   a.doctorFullName,
        specialty: a.doctorSpecialty,
        date:     new Date(a.dateTime),
        time:     new Date(a.dateTime).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }),
        status:   this._mapStatus(a.status),
      })));
    } catch (e: any) {
      this.error.set('Failed to load appointments.');
    } finally {
      this.loading.set(false);
    }
  }

  private _mapStatus(s: string): 'confirmed' | 'pending' | 'cancelled' | 'refused' {
    switch (s?.toLowerCase()) {
      case 'confirmed': return 'confirmed';
      case 'pending':   return 'pending';
      case 'refused':   return 'refused';
      default:          return 'cancelled';
    }
  }

  get filtered(): Appointment[] {
    const now = new Date();
    switch (this.activeTab()) {
      case 'upcoming':
        return this.appointments().filter(a =>
          a.status !== 'cancelled' && a.status !== 'refused' && a.date >= now);
      case 'past':
        return this.appointments().filter(a =>
          a.status !== 'cancelled' && a.status !== 'refused' && a.date < now);
      case 'cancelled':
        return this.appointments().filter(a =>
          a.status === 'cancelled' || a.status === 'refused');
    }
  }

  setTab(tab: Tab): void { this.activeTab.set(tab); }

  async cancel(id: string): Promise<void> {
    if (!confirm('Are you sure you want to cancel this appointment?')) return;
    try {
      await this.appointmentService.cancelAppointment(id);
      this.appointments.update(list =>
        list.map(a => a.id === id ? { ...a, status: 'cancelled' as const } : a)
      );
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to cancel appointment.');
    }
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'confirmed': return 'Confirmed';
      case 'pending':   return 'Pending';
      case 'refused':   return 'Refused';
      default:          return 'Cancelled';
    }
  }
}
