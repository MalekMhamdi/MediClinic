import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../shared/services/auth.service';
import { ChatPopupComponent } from '../shared/chat-popup/chat-popup';
import { MedicalRecordComponent } from '../patient-dashboard/medical-record/medical-record';
import { PrescriptionPrintComponent, PrescriptionData } from './prescription-print/prescription-print';
import { PdfService } from '../shared/services/pdf.service';
import { PaginationComponent } from '../shared/pagination/pagination';
import {
  DoctorService, AppointmentItem, ConsultationItem, DoctorLabResult,
  PatientMedicalRecord, MedicalConsultation, MedicalPrescription, MedicalLabOrder
} from '../shared/services/doctor.service';
import { NotificationService } from '../shared/services/notification.service';

type Tab = 'appointments' | 'planning' | 'patients' | 'lab-results' | 'notifications' | 'consultation';
type ApptFilter = 'all' | 'Pending' | 'Confirmed' | 'Completed' | 'Refused';

interface DayPlan {
  date: string;       // YYYY-MM-DD
  label: string;      // "Mon 5 May"
  count: number;
  appointments: AppointmentItem[];
}

@Component({
  selector: 'app-doctor-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ChatPopupComponent, MedicalRecordComponent, PrescriptionPrintComponent, PaginationComponent],
  templateUrl: './doctor-dashboard.html',
  styleUrl: './doctor-dashboard.css'
})
export class DoctorDashboard implements OnInit {

  tab         = signal<Tab>('appointments');
  loading     = signal(true);
  error       = signal('');
  unreadCount = computed(() => this.notifService.unreadCount());

  // ── Appointments ────────────────────────────────────────────
  appointments  = signal<AppointmentItem[]>([]);
  apptFilter    = signal<ApptFilter>('all');
  apptPage      = signal(1);
  readonly APPT_PAGE_SIZE = 8;

  filtered = computed(() => {
    const f = this.apptFilter();
    const all = this.appointments();
    return f === 'all' ? all : all.filter(a => a.status === f);
  });

  pagedAppts = computed(() => {
    const p  = this.apptPage();
    const ps = this.APPT_PAGE_SIZE;
    return this.filtered().slice((p - 1) * ps, p * ps);
  });

  // ── Planning (calendar) ─────────────────────────────────────
  planDays    = signal<DayPlan[]>([]);
  selectedDay = signal<DayPlan | null>(null);

  // ── Patients ────────────────────────────────────────────────
  patientHistory      = signal<AppointmentItem[]>([]);
  loadingPatient      = signal(false);
  selectedPatientId   = signal<number | null>(null);
  selectedPatientName = signal('');
  patientSearch       = signal('');
  patientPage         = signal(1);
  readonly PATIENT_PAGE_SIZE = 8;

  filteredPatients = computed(() => {
    const q = this.patientSearch().toLowerCase().trim();
    const list = q ? this.uniquePatients.filter(p => p.name.toLowerCase().includes(q))
                   : this.uniquePatients;
    return list;
  });

  pagedPatients = computed(() => {
    const p  = this.patientPage();
    const ps = this.PATIENT_PAGE_SIZE;
    return this.filteredPatients().slice((p - 1) * ps, p * ps);
  });

  // ── Dossier médical ─────────────────────────────────────────
  patientRecord        = signal<PatientMedicalRecord | null>(null);
  patientConsultations = signal<MedicalConsultation[]>([]);
  loadingRecord        = signal(false);
  showMedicalRecord    = signal(false);
  recordTab            = signal<'appointments' | 'prescriptions' | 'laborders'>('appointments');

  // Ajout ordonnance depuis dossier
  showAddRxRecord  = signal(false);
  recordConsultId  = signal<number | null>(null);
  recordRxMed      = signal('');
  recordRxDosage   = signal('');
  recordRxFreq     = signal('');
  recordRxDuration = signal('');
  recordRxNotes    = signal('');
  savingRecordRx   = signal(false);

  // Ajout analyse depuis dossier
  showAddLabRecord = signal(false);
  recordLabType    = signal('');
  recordLabNotes   = signal('');
  savingRecordLab  = signal(false);

  // ── Notifications ────────────────────────────────────────────
  notifications     = computed(() => this.notifService.items());
  loadingNotif      = signal(false);
  notifLoaded       = signal(false);

  // ── Lab Results ─────────────────────────────────────────────
  labResults      = signal<DoctorLabResult[]>([]);
  loadingLab      = signal(false);
  commentTarget   = signal<DoctorLabResult | null>(null);
  commentText     = signal('');
  sendingComment  = signal(false);
  commentOk       = signal('');

  // ── Consultation ────────────────────────────────────────────
  selectedAppt   = signal<AppointmentItem | null>(null);
  consultation   = signal<ConsultationItem | null>(null);
  loadingConsult = signal(false);
  savingConsult  = signal(false);
  diagnostic     = signal('');
  observations   = signal('');
  treatment      = signal('');
  consultError   = signal('');
  consultOk      = signal('');

  showRxForm  = signal(false);
  rxMed       = signal('');
  rxDosage    = signal('');
  rxFreq      = signal('');
  rxDuration  = signal('');
  rxNotes     = signal('');
  savingRx    = signal(false);

  showLabForm = signal(false);
  labType     = signal('');
  labNotes    = signal('');
  savingLab   = signal(false);

  // ── Prescription print ───────────────────────────────────────
  showPrescription  = signal(false);
  prescriptionData  = signal<PrescriptionData | null>(null);

  constructor(
    public auth: AuthService,
    private doctorService: DoctorService,
    private notifService: NotificationService,
    private pdfService: PdfService,
  ) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.loadAppointments(),
      this.notifService.loadUnreadCount(),
      this.auth.refreshCurrentUser(),
    ]);
  }

  // ── Loaders ─────────────────────────────────────────────────

  async loadAppointments(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const list = await this.doctorService.getAppointments(this.auth.userId);
      this.appointments.set(list);
      this._buildPlanning(list);
    } catch {
      this.error.set('Failed to load appointments.');
    } finally {
      this.loading.set(false);
    }
  }

  async loadLabResults(): Promise<void> {
    if (this.labResults().length > 0) return;
    this.loadingLab.set(true);
    try {
      this.labResults.set(await this.doctorService.getLabResults(this.auth.userId));
    } catch {
      this.labResults.set([]);
    } finally {
      this.loadingLab.set(false);
    }
  }

  async openPatientHistory(patientId: number, patientName: string): Promise<void> {
    this.selectedPatientId.set(patientId);
    this.selectedPatientName.set(patientName);
    this.showMedicalRecord.set(false);
    this.patientRecord.set(null);
    this.patientConsultations.set([]);
    this.loadingPatient.set(true);
    try {
      this.patientHistory.set(await this.doctorService.getPatientAppointments(patientId));
    } catch {
      this.patientHistory.set([]);
    } finally {
      this.loadingPatient.set(false);
    }
  }

  clearPatient(): void {
    this.selectedPatientId.set(null);
    this.patientHistory.set([]);
    this.showMedicalRecord.set(false);
    this.patientRecord.set(null);
    this.patientConsultations.set([]);
  }

  async openMedicalRecord(): Promise<void> {
    const pid = this.selectedPatientId();
    if (!pid) return;
    this.showMedicalRecord.set(true);
    this.recordTab.set('appointments');
    this.loadingRecord.set(true);
    try {
      const rec = await this.doctorService.getMedicalRecord(pid);
      this.patientRecord.set(rec);
      const consulted = rec.appointments.filter(a => a.hasConsultation);
      const details = await Promise.all(
        consulted.map(a => this.doctorService.getConsultationDetail(a.id))
      );
      this.patientConsultations.set(details.filter(Boolean) as MedicalConsultation[]);
    } catch {
      this.patientRecord.set(null);
    } finally {
      this.loadingRecord.set(false);
    }
  }

  closeMedicalRecord(): void {
    this.showMedicalRecord.set(false);
    this.patientRecord.set(null);
    this.patientConsultations.set([]);
  }

  get recordAllPrescriptions(): MedicalPrescription[] {
    return this.patientConsultations().flatMap(c => c.prescriptions ?? []);
  }

  get recordAllLabOrders(): MedicalLabOrder[] {
    return this.patientConsultations().flatMap(c => c.labOrders ?? []);
  }

  get consultationOptions(): { id: number; label: string }[] {
    const appts = this.patientRecord()?.appointments ?? [];
    return this.patientConsultations().map(c => {
      const appt = appts.find(a => a.id === c.appointmentId);
      const date = appt
        ? new Date(appt.dateTime).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' })
        : '';
      return { id: c.id, label: `${date} — ${appt?.doctorFullName ?? ''}` };
    });
  }

  openAddRxRecord(): void {
    const opts = this.consultationOptions;
    if (opts.length > 0) this.recordConsultId.set(opts[0].id);
    this.showAddRxRecord.set(true);
    this.showAddLabRecord.set(false);
  }

  openAddLabRecord(): void {
    const opts = this.consultationOptions;
    if (opts.length > 0) this.recordConsultId.set(opts[0].id);
    this.showAddLabRecord.set(true);
    this.showAddRxRecord.set(false);
  }

  async addRxFromRecord(): Promise<void> {
    const cid = this.recordConsultId();
    if (!cid || !this.recordRxMed() || !this.recordRxDosage() || !this.recordRxFreq() || !this.recordRxDuration()) return;
    this.savingRecordRx.set(true);
    try {
      await this.doctorService.addPrescription(cid, {
        medication: this.recordRxMed(), dosage: this.recordRxDosage(),
        frequency: this.recordRxFreq(), duration: this.recordRxDuration(),
        notes: this.recordRxNotes() || undefined,
      });
      this.patientConsultations.update(list =>
        list.map(c => c.id === cid ? {
          ...c,
          prescriptions: [...(c.prescriptions ?? []), {
            id: Date.now(), medication: this.recordRxMed(), dosage: this.recordRxDosage(),
            frequency: this.recordRxFreq(), duration: this.recordRxDuration(),
            notes: this.recordRxNotes() || null,
          }]
        } : c)
      );
      this.recordRxMed.set(''); this.recordRxDosage.set(''); this.recordRxFreq.set('');
      this.recordRxDuration.set(''); this.recordRxNotes.set('');
      this.showAddRxRecord.set(false);
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to add.');
    } finally {
      this.savingRecordRx.set(false);
    }
  }

  async addLabFromRecord(): Promise<void> {
    const cid = this.recordConsultId();
    if (!cid || !this.recordLabType()) return;
    this.savingRecordLab.set(true);
    try {
      await this.doctorService.addLabOrder(cid, {
        analysisType: this.recordLabType(), notes: this.recordLabNotes() || undefined,
      });
      this.patientConsultations.update(list =>
        list.map(c => c.id === cid ? {
          ...c,
          labOrders: [...(c.labOrders ?? []), {
            id: Date.now(), analysisType: this.recordLabType(),
            status: 'Pending', statusLabel: 'Pending',
            notes: this.recordLabNotes() || null, result: null,
          }]
        } : c)
      );
      this.recordLabType.set(''); this.recordLabNotes.set('');
      this.showAddLabRecord.set(false);
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to add.');
    } finally {
      this.savingRecordLab.set(false);
    }
  }

  // ── Planning builder ─────────────────────────────────────────

  private _buildPlanning(appointments: AppointmentItem[]): void {
    const map = new Map<string, AppointmentItem[]>();
    appointments.forEach(a => {
      const key = new Date(a.dateTime).toISOString().split('T')[0];
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(a);
    });

    const days: DayPlan[] = [];
    // Show next 30 days + past 7 days
    for (let i = -7; i <= 30; i++) {
      const d = new Date();
      d.setDate(d.getDate() + i);
      const key = d.toISOString().split('T')[0];
      const appts = (map.get(key) ?? []).sort((a, b) =>
        new Date(a.dateTime).getTime() - new Date(b.dateTime).getTime()
      );
      days.push({
        date: key,
        label: d.toLocaleDateString('en-US', { weekday: 'short', day: 'numeric', month: 'short' }),
        count: appts.length,
        appointments: appts,
      });
    }
    this.planDays.set(days);
  }

  selectDay(day: DayPlan): void {
    this.selectedDay.set(this.selectedDay()?.date === day.date ? null : day);
  }

  isToday(dateStr: string): boolean {
    return new Date().toISOString().split('T')[0] === dateStr;
  }

  // ── Appointment actions ──────────────────────────────────────

  async accept(id: number): Promise<void> {
    try {
      await this.doctorService.accept(id);
      this._updateAppt(id, 'Confirmed', 'Confirmed');
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to accept.');
    }
  }

  async refuse(id: number): Promise<void> {
    if (!confirm('Refuse this appointment?')) return;
    try {
      await this.doctorService.refuse(id);
      this._updateAppt(id, 'Refused', 'Refused');
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to refuse.');
    }
  }

  async deleteAppt(id: number): Promise<void> {
    if (!confirm('Permanently delete this appointment?')) return;
    try {
      await this.doctorService.deleteAppointment(id);
      this.appointments.update(list => list.filter(a => a.id !== id));
      this._buildPlanning(this.appointments());
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to delete.');
    }
  }

  async downloadLabResult(r: DoctorLabResult): Promise<void> {
    await this.pdfService.downloadLabResult(
      {
        patientName:    r.patientName,
        analysisType:   r.analysisType,
        technicienName: r.technicienName,
        medicalComment: r.labComment,
        status:         r.statusLabel,
        createdAt:      r.createdAt,
        doctorName:     this.auth.userName ?? undefined,
      },
      `LabResult_${r.patientName.replace(/\s+/g, '_')}_${r.analysisType.replace(/\s+/g, '_')}_${new Date(r.createdAt).toISOString().slice(0, 10)}.pdf`,
    );
  }

  async deleteLabResult(id: number): Promise<void> {
    if (!confirm('Delete this lab result? The order will return to Pending.')) return;
    try {
      await this.doctorService.deleteLabResult(id);
      this.labResults.update(list => list.filter(r => r.id !== id));
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to delete lab result.');
    }
  }

  private _updateAppt(id: number, status: string, label: string): void {
    this.appointments.update(list =>
      list.map(a => a.id === id ? { ...a, status, statusLabel: label } : a)
    );
    this._buildPlanning(this.appointments());
  }

  // ── Consultation ─────────────────────────────────────────────

  async openConsultation(appt: AppointmentItem): Promise<void> {
    this.selectedAppt.set(appt);
    this.consultation.set(null);
    this.consultError.set('');
    this.consultOk.set('');
    this.tab.set('consultation');
    this.loadingConsult.set(true);
    try {
      const c = await this.doctorService.getConsultation(appt.id);
      if (c) {
        this.consultation.set(c);
        this.diagnostic.set(c.diagnostic ?? '');
        this.observations.set(c.observations ?? '');
        this.treatment.set(c.treatment ?? '');
      } else {
        this.diagnostic.set('');
        this.observations.set('');
        this.treatment.set('');
      }
    } finally {
      this.loadingConsult.set(false);
    }
  }

  async saveConsultation(): Promise<void> {
    const appt = this.selectedAppt();
    if (!appt) return;
    this.savingConsult.set(true);
    this.consultError.set('');
    try {
      const c = this.consultation();
      if (c) {
        await this.doctorService.updateConsultation(c.id, {
          diagnostic: this.diagnostic(), observations: this.observations(), treatment: this.treatment(),
        });
        this.consultation.update(x => x ? {
          ...x, diagnostic: this.diagnostic(), observations: this.observations(), treatment: this.treatment()
        } : x);
      } else {
        const created = await this.doctorService.createConsultation({
          appointmentId: appt.id,
          diagnostic: this.diagnostic(), observations: this.observations(), treatment: this.treatment(),
        });
        this.consultation.set(created);
        this._updateAppt(appt.id, 'Completed', 'Completed');
        this.appointments.update(l => l.map(a => a.id === appt.id ? { ...a, hasConsultation: true } : a));
      }
      this.consultOk.set('Consultation saved.');
      setTimeout(() => this.consultOk.set(''), 3000);
    } catch (e: any) {
      this.consultError.set(e?.error?.error ?? e?.error?.message ?? 'Failed to save.');
    } finally {
      this.savingConsult.set(false);
    }
  }

  async addPrescription(): Promise<void> {
    const c = this.consultation();
    if (!c || !this.rxMed() || !this.rxDosage() || !this.rxFreq() || !this.rxDuration()) return;
    this.savingRx.set(true);
    try {
      await this.doctorService.addPrescription(c.id, {
        medication: this.rxMed(), dosage: this.rxDosage(),
        frequency: this.rxFreq(), duration: this.rxDuration(), notes: this.rxNotes() || undefined,
      });
      this.consultation.update(x => x ? {
        ...x, prescriptions: [...(x.prescriptions ?? []), {
          id: Date.now(), medication: this.rxMed(), dosage: this.rxDosage(),
          frequency: this.rxFreq(), duration: this.rxDuration(), notes: this.rxNotes() || null
        }]
      } : x);
      this.rxMed.set(''); this.rxDosage.set(''); this.rxFreq.set('');
      this.rxDuration.set(''); this.rxNotes.set('');
      this.showRxForm.set(false);
    } catch (e: any) { alert(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingRx.set(false); }
  }

  async addLabOrder(): Promise<void> {
    const c = this.consultation();
    if (!c || !this.labType()) return;
    this.savingLab.set(true);
    try {
      await this.doctorService.addLabOrder(c.id, {
        analysisType: this.labType(), notes: this.labNotes() || undefined,
      });
      this.consultation.update(x => x ? {
        ...x, labOrders: [...(x.labOrders ?? []), {
          id: Date.now(), analysisType: this.labType(), status: 'Pending', notes: this.labNotes() || null
        }]
      } : x);
      this.labType.set(''); this.labNotes.set('');
      this.showLabForm.set(false);
    } catch (e: any) { alert(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingLab.set(false); }
  }

  // ── Lab result comment ───────────────────────────────────────

  openComment(result: DoctorLabResult): void {
    this.commentTarget.set(result);
    this.commentText.set('');
    this.commentOk.set('');
  }

  closeComment(): void { this.commentTarget.set(null); }

  async sendComment(): Promise<void> {
    const target = this.commentTarget();
    if (!target || !this.commentText().trim()) return;
    this.sendingComment.set(true);
    try {
      await this.doctorService.sendDoctorComment(target.id, this.commentText().trim());
      this.commentOk.set('Comment sent to patient.');
      this.commentText.set('');
      setTimeout(() => { this.closeComment(); this.commentOk.set(''); }, 2000);
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to send comment.');
    } finally {
      this.sendingComment.set(false);
    }
  }

  // ── Navigation ───────────────────────────────────────────────

  async loadNotifications(): Promise<void> {
    if (this.notifLoaded()) return;
    this.loadingNotif.set(true);
    try {
      await this.notifService.load();
      this.notifLoaded.set(true);
    } finally {
      this.loadingNotif.set(false);
    }
  }

  async markRead(id: number): Promise<void> {
    await this.notifService.markRead(id);
  }

  async markAllRead(): Promise<void> {
    await this.notifService.markAllRead();
  }

  async deleteNotif(id: number): Promise<void> {
    try { await this.notifService.delete(id); } catch { }
  }

  notifIcon(type: string): string {
    return { appointment: '📅', result: '🔬', system: '⚙️' }[type] ?? '🔔';
  }

  setTab(t: Tab): void {
    this.tab.set(t);
    if (t === 'lab-results') this.loadLabResults();
    if (t === 'notifications') this.loadNotifications();
    if (t !== 'consultation') {
      this.selectedAppt.set(null);
      this.consultation.set(null);
    }
  }

  backFromConsultation(): void { this.setTab('appointments'); }
  logout(): void { this.auth.logout(); }

  // ── Stats ────────────────────────────────────────────────────

  get stats() {
    const all = this.appointments();
    return {
      total:     all.length,
      pending:   all.filter(a => a.status === 'Pending').length,
      confirmed: all.filter(a => a.status === 'Confirmed').length,
      completed: all.filter(a => a.status === 'Completed').length,
    };
  }

  get uniquePatients(): { id: number; name: string; lastVisit: Date }[] {
    const map = new Map<number, { id: number; name: string; lastVisit: Date }>();
    this.appointments().forEach(a => {
      const existing = map.get(a.patientId);
      const dt = new Date(a.dateTime);
      if (!existing || dt > existing.lastVisit) {
        map.set(a.patientId, { id: a.patientId, name: a.patientFullName, lastVisit: dt });
      }
    });
    return [...map.values()].sort((a, b) => b.lastVisit.getTime() - a.lastVisit.getTime());
  }

  // ── Helpers ──────────────────────────────────────────────────

  get initials(): string {
    const n = this.auth.userName?.split(' ') ?? [];
    return n.length >= 2 ? `${n[0][0]}${n[1][0]}`.toUpperCase() : (n[0]?.[0] ?? 'D').toUpperCase();
  }

  get today(): string {
    return new Date().toLocaleDateString('en-US', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }

  formatDate(d: Date | string): string {
    return new Date(d).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  formatTime(d: Date | string): string {
    return new Date(d).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  }

  statusClass(s: string): string {
    return { 'Pending': 'pending', 'Confirmed': 'confirmed', 'Completed': 'completed' }[s] ?? 'cancelled';
  }

  labStatusClass(s: string): string {
    return s === 'Done' || s === 'Reviewed' ? 'completed' : 'pending';
  }

  printPatientRecord(): void { window.print(); }

  setApptFilter(f: ApptFilter): void { this.apptFilter.set(f); this.apptPage.set(1); }
  setPatientSearch(q: string): void  { this.patientSearch.set(q); this.patientPage.set(1); }

  openPrescription(): void {
    const c    = this.consultation();
    const appt = this.selectedAppt();
    if (!c || !appt || !c.prescriptions?.length) return;

    this.prescriptionData.set({
      doctorName:      this.auth.userName ?? '',
      doctorSpecialty: appt.doctorSpecialty ?? 'Médecine Générale',
      clinicName:      'MediClinic',
      clinicAddress:   'Tunis, Tunisie',
      clinicPhone:     '+216 71 000 000',
      patientName:     appt.patientFullName,
      date:            new Date(appt.dateTime),
      medications:     c.prescriptions.map(p => ({
        medication: p.medication,
        dosage:     p.dosage,
        frequency:  p.frequency,
        duration:   p.duration,
        notes:      p.notes ?? null,
      })),
    });
    this.showPrescription.set(true);
  }
}
