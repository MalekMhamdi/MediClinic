import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AppointmentService, Specialty, Doctor, TimeSlot } from '../../shared/services/appointment.service';

@Component({
  selector: 'app-book-appointment',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './book-appointment.html',
  styleUrl: './book-appointment.css'
})
export class BookAppointmentComponent implements OnInit {

  step = signal<1 | 2 | 3>(1);

  selectedSpecialty = signal<Specialty | null>(null);
  selectedDoctor    = signal<Doctor | null>(null);
  selectedDate      = signal<string>('');
  selectedTime      = signal<string>('');
  reason            = signal<string>('');

  timeSlots = signal<TimeSlot[]>([]);
  loadingSlots = signal(false);
  loading  = signal(false);
  error    = signal('');
  success  = signal(false);

  constructor(
    private appointmentService: AppointmentService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.appointmentService.loadSpecialties(),
      this.appointmentService.loadDoctors(),
    ]);

    const { specialty, doctorId } = this.route.snapshot.queryParams;
    if (specialty || doctorId) {
      this._preSelectFromAI(specialty, doctorId);
    }
  }

  private _preSelectFromAI(specialtyName: string, doctorId: string): void {
    if (specialtyName) {
      const sp = this.appointmentService.specialties().find(
        s => s.name.toLowerCase() === specialtyName.toLowerCase()
              || s.name.toLowerCase().includes(specialtyName.toLowerCase())
              || specialtyName.toLowerCase().includes(s.name.toLowerCase())
      );
      if (sp) this.selectedSpecialty.set(sp);
    }

    if (doctorId) {
      const doc = this.appointmentService.doctors().find(
        d => d.id === doctorId.toString()
      );
      if (doc) {
        this.selectedDoctor.set(doc);
        if (this.selectedSpecialty()) this.step.set(2);
      }
    }
  }

  get specialties(): Specialty[] {
    return this.appointmentService.specialties();
  }

  filteredDoctors = computed(() => {
    const sp = this.selectedSpecialty();
    return sp ? this.appointmentService.getDoctorsBySpecialty(sp.id) : [];
  });

  get minDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }

  get progressWidth(): string {
    return { 1: '33%', 2: '66%', 3: '100%' }[this.step()] ?? '33%';
  }

  get canConfirm(): boolean {
    return !!this.selectedDate() && !!this.selectedTime();
  }

  selectSpecialty(sp: Specialty): void {
    this.selectedSpecialty.set(sp);
    this.selectedDoctor.set(null);
    this.selectedDate.set('');
    this.selectedTime.set('');
    this.timeSlots.set([]);
  }

  selectDoctor(doc: Doctor): void {
    this.selectedDoctor.set(doc);
    this.selectedDate.set('');
    this.selectedTime.set('');
    this.timeSlots.set([]);
  }

  async setDate(val: string): Promise<void> {
    this.selectedDate.set(val);
    this.selectedTime.set('');
    const doc = this.selectedDoctor();
    if (!doc || !val) return;

    this.loadingSlots.set(true);
    const slots = await this.appointmentService.getAvailableSlots(doc.id, val);
    this.timeSlots.set(slots);
    this.loadingSlots.set(false);
  }

  setTime(slot: TimeSlot): void {
    if (slot.available) this.selectedTime.set(slot.time);
  }

  setReason(val: string): void { this.reason.set(val); }

  nextStep(): void {
    if (this.step() === 1 && this.selectedSpecialty() && this.selectedDoctor()) {
      this.step.set(2);
    } else if (this.step() === 2 && this.canConfirm) {
      this.step.set(3);
    }
  }

  prevStep(): void {
    this.error.set('');
    this.step.update(s => (s - 1) as 1 | 2 | 3);
  }

  async confirm(): Promise<void> {
    const doc = this.selectedDoctor();
    const sp  = this.selectedSpecialty();
    if (!doc || !sp || !this.selectedDate() || !this.selectedTime()) return;

    this.loading.set(true);
    this.error.set('');
    try {
      await this.appointmentService.bookAppointment({
        specialtyId: sp.id,
        doctorId:    doc.id,
        date:        this.selectedDate(),
        time:        this.selectedTime(),
        reason:      this.reason(),
      });
      this.success.set(true);
    } catch (e: any) {
      this.error.set(e?.error?.message ?? 'Failed to book appointment. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  goToAppointments(): void {
    this.router.navigate(['/patient/appointments']);
  }

  starsArray(rating: number): string {
    return '★'.repeat(Math.floor(rating)) + (rating % 1 >= 0.5 ? '½' : '');
  }
}
