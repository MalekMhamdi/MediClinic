import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentService } from '../../shared/services/appointment.service';
import { AuthService } from '../../shared/services/auth.service';
import { PaginationComponent } from '../../shared/pagination/pagination';
const utc = AppointmentService.normalizeUtc;

type Tab    = 'upcoming' | 'past' | 'cancelled';
type Status = 'confirmed' | 'pending' | 'cancelled' | 'refused' | 'completed';

interface Appointment {
  id:        string;
  doctorId:  number;
  type:      string;
  doctor:    string;
  specialty: string;
  date:      Date;
  time:      string;
  status:    Status;
  hasReview: boolean;
}

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PaginationComponent],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css'
})
export class AppointmentsComponent implements OnInit {

  activeTab    = signal<Tab>('upcoming');
  appointments = signal<Appointment[]>([]);
  loading      = signal(true);
  error        = signal('');
  page         = signal(1);
  readonly PAGE_SIZE = 6;

  // ── Review modal ─────────────────────────────────────────────────────
  reviewTarget  = signal<Appointment | null>(null);
  reviewRating  = signal(0);
  hoveredStar   = signal(0);
  reviewComment = signal('');
  reviewSaving  = signal(false);
  reviewError   = signal('');
  reviewDone    = signal(false);

  pagedFiltered = computed(() => {
    const p  = this.page();
    const ps = this.PAGE_SIZE;
    return this.filtered.slice((p - 1) * ps, p * ps);
  });

  constructor(
    private appointmentService: AppointmentService,
    private authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> { await this.loadAppointments(); }

  async loadAppointments(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const patientId = this.authService.userId;
      if (!patientId) { this.error.set('Not authenticated.'); return; }

      const data = await this.appointmentService.getPatientAppointments(patientId);

      // fetch my reviews once to mark which appointments already have one
      let myReviewedApptIds = new Set<string>();
      try {
        const reviews = await this.appointmentService.getMyReviews();
        myReviewedApptIds = new Set(reviews.map((r: any) => r.appointmentId.toString()));
      } catch { /* ignore — non-critical */ }

      this.appointments.set(data.map(a => {
        const dt = new Date(utc(a.dateTime));
        return {
          id:        a.id.toString(),
          doctorId:  a.doctorId ?? 0,
          type:      a.reason || a.doctorSpecialty || 'Consultation',
          doctor:    a.doctorFullName,
          specialty: a.doctorSpecialty,
          date:      dt,
          time:      dt.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }),
          status:    this._mapStatus(a.status),
          hasReview: myReviewedApptIds.has(a.id.toString()),
        };
      }));
    } catch {
      this.error.set('Failed to load appointments.');
    } finally {
      this.loading.set(false);
    }
  }

  private _mapStatus(s: string): Status {
    switch (s?.toLowerCase()) {
      case 'confirmed':  return 'confirmed';
      case 'pending':    return 'pending';
      case 'refused':    return 'refused';
      case 'completed':  return 'completed';
      default:           return 'cancelled';
    }
  }

  get filtered(): Appointment[] {
    const now = new Date();
    switch (this.activeTab()) {
      case 'upcoming':
        return this.appointments().filter(a =>
          (a.status === 'confirmed' || a.status === 'pending') && a.date >= now);
      case 'past':
        return this.appointments().filter(a =>
          a.status === 'completed' || (a.status !== 'cancelled' && a.status !== 'refused' && a.date < now));
      case 'cancelled':
        return this.appointments().filter(a =>
          a.status === 'cancelled' || a.status === 'refused');
    }
  }

  setTab(tab: Tab): void { this.activeTab.set(tab); this.page.set(1); }

  async cancel(id: string): Promise<void> {
    if (!confirm('Are you sure you want to cancel this appointment?')) return;
    try {
      await this.appointmentService.cancelAppointment(id);
      this.appointments.update(list =>
        list.map(a => a.id === id ? { ...a, status: 'cancelled' as Status } : a));
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to cancel appointment.');
    }
  }

  async deleteAppt(id: string): Promise<void> {
    if (!confirm('Permanently delete this appointment?')) return;
    try {
      await this.appointmentService.deleteAppointment(id);
      this.appointments.update(list => list.filter(a => a.id !== id));
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to delete appointment.');
    }
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      confirmed: 'Confirmed', pending: 'Pending',
      refused: 'Refused', cancelled: 'Cancelled', completed: 'Completed',
    };
    return map[status] ?? status;
  }

  // ── Review actions ────────────────────────────────────────────────────

  openReview(appt: Appointment): void {
    this.reviewTarget.set(appt);
    this.reviewRating.set(0);
    this.hoveredStar.set(0);
    this.reviewComment.set('');
    this.reviewError.set('');
    this.reviewDone.set(false);
  }

  closeReview(): void { this.reviewTarget.set(null); }

  stars = [1, 2, 3, 4, 5];

  starClass(star: number): string {
    const active = this.hoveredStar() || this.reviewRating();
    return star <= active ? 'apt-star filled' : 'apt-star';
  }

  async submitReview(): Promise<void> {
    const appt = this.reviewTarget();
    if (!appt || this.reviewRating() < 1) return;
    this.reviewSaving.set(true);
    this.reviewError.set('');
    try {
      await this.appointmentService.submitReview(
        +appt.id, this.reviewRating(), this.reviewComment());
      this.reviewDone.set(true);
      this.appointments.update(list =>
        list.map(a => a.id === appt.id ? { ...a, hasReview: true } : a));
    } catch (e: any) {
      this.reviewError.set(e?.error?.error ?? e?.error?.message ?? 'Failed to submit review.');
    } finally {
      this.reviewSaving.set(false);
    }
  }
}
