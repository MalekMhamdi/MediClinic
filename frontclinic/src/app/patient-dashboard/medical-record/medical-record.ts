import { Component, signal, computed, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';
import { PdfService } from '../../shared/services/pdf.service';
import { PaginationComponent } from '../../shared/pagination/pagination';

const API = 'http://localhost:5217/api/v1';

// ── Interfaces matching the enriched backend DTOs ──────────────────────────

interface MedicalPrescription {
  id: number; medication: string; dosage: string | null;
  duration: string | null; instructions: string | null;
}

interface MedicalLabResult {
  id: number; technicienName: string; medicalComment: string | null;
  resultPdfPath: string | null; status: string; createdAt: string;
}

interface MedicalLabOrder {
  id: number; analysisType: string; status: string; statusLabel: string;
  notes: string | null; result: MedicalLabResult | null;
}

interface MedicalConsultation {
  id: number; diagnostic: string | null; observations: string | null;
  treatment: string | null; createdAt: string;
  prescriptions: MedicalPrescription[];
  labOrders: MedicalLabOrder[];
}

interface MedicalAppointment {
  id: number; doctorFullName: string; doctorSpecialty: string;
  dateTime: string; status: string; statusLabel: string;
  reason: string | null; hasConsultation: boolean;
  consultation: MedicalConsultation | null;
}

interface MedicalRecord {
  patientId: number; fullName: string; cin: string;
  dateOfBirth: string; bloodGroup: string | null; phone: string | null;
  appointments: MedicalAppointment[];
}

export interface ConsultationEntry {
  consultation:      MedicalConsultation;
  appointmentDate:   string;
  appointmentReason: string | null;
}

export interface DoctorGroup {
  doctorName:    string;
  specialty:     string;
  consultations: ConsultationEntry[];
}

// kept for downloadRx / downloadLabPdf signatures
export interface FlatEntry extends ConsultationEntry {
  doctorName: string;
  specialty:  string;
}

@Component({
  selector: 'app-medical-record',
  standalone: true,
  imports: [CommonModule, PaginationComponent],
  templateUrl: './medical-record.html',
  styleUrl:    './medical-record.css'
})
export class MedicalRecordComponent implements OnInit, OnChanges {

  @Input() patientId?: number;

  loading     = signal(true);
  error       = signal('');
  record      = signal<MedicalRecord | null>(null);
  downloading = signal(false);
  fullView    = signal(false);
  mrPage      = signal(1);
  searchQuery    = signal('');
  selectedDoctor = signal<string | null>(null);  // quick-filter badge
  collapsedDocs  = signal<Set<string>>(new Set()); // collapsed doctor sections
  readonly MR_PAGE_SIZE = 3; // doctor groups per page

  rxDownloading  = signal<number | null>(null);
  labDownloading = signal<number | null>(null);

  // ── All consultations flat (for stats + download) ─────────────────
  flatConsultations = computed<FlatEntry[]>(() => {
    const appts = this.record()?.appointments ?? [];
    const list: FlatEntry[] = [];
    for (const a of appts) {
      if (!a.hasConsultation || !a.consultation) continue;
      list.push({
        doctorName:        a.doctorFullName,
        specialty:         a.doctorSpecialty,
        consultation:      a.consultation,
        appointmentDate:   this._utc(a.dateTime),
        appointmentReason: a.reason,
      });
    }
    return list.sort((a, b) =>
      new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime()
    );
  });

  // ── Grouped by doctor, newest consultation first per group ─────────
  groupedByDoctor = computed<DoctorGroup[]>(() => {
    const map = new Map<string, DoctorGroup>();
    for (const e of this.flatConsultations()) {
      const key = `${e.doctorName}||${e.specialty}`;
      if (!map.has(key))
        map.set(key, { doctorName: e.doctorName, specialty: e.specialty, consultations: [] });
      map.get(key)!.consultations.push({
        consultation:      e.consultation,
        appointmentDate:   e.appointmentDate,
        appointmentReason: e.appointmentReason,
      });
    }
    // flatConsultations is already date-sorted, so each group's list is too
    return [...map.values()].sort((a, b) => {
      const la = new Date(a.consultations[0]?.appointmentDate ?? 0).getTime();
      const lb = new Date(b.consultations[0]?.appointmentDate ?? 0).getTime();
      return lb - la;
    });
  });

  // ── List of unique doctor names for badge row ──────────────────────
  doctorBadges = computed(() =>
    this.groupedByDoctor().map(g => ({ name: g.doctorName, specialty: g.specialty }))
  );

  // ── Apply search + badge filter ────────────────────────────────────
  filteredGroups = computed<DoctorGroup[]>(() => {
    const q   = this.searchQuery().trim().toLowerCase();
    const sel = this.selectedDoctor();
    let groups = this.groupedByDoctor();

    if (sel)
      groups = groups.filter(g => g.doctorName === sel);
    else if (q)
      groups = groups.filter(g =>
        g.doctorName.toLowerCase().includes(q) ||
        g.specialty.toLowerCase().includes(q)
      );

    return groups;
  });

  // ── Paginated groups ────────────────────────────────────────────────
  pagedGroups = computed<DoctorGroup[]>(() => {
    if (this.fullView()) return this.filteredGroups();
    const p = this.mrPage(), ps = this.MR_PAGE_SIZE;
    return this.filteredGroups().slice((p - 1) * ps, p * ps);
  });

  constructor(
    private auth: AuthService,
    private http: HttpClient,
    private pdfService: PdfService,
  ) {}

  ngOnInit(): void { this.load(); }

  ngOnChanges(changes: SimpleChanges): void {
    // skip the first change — ngOnInit already handles initial load
    if (changes['patientId'] && !changes['patientId'].firstChange) this.load();
  }

  async load(): Promise<void> {
    const id = this.patientId ?? this.auth.userId;
    if (!id) return;

    this.loading.set(true);
    this.error.set('');
    this.record.set(null);

    try {
      const rec = await firstValueFrom(
        this.http.get<MedicalRecord>(`${API}/Patient/${id}/medical-record`)
      );
      this.record.set(rec);
      this.mrPage.set(1);
      this.searchQuery.set('');
      this.selectedDoctor.set(null);
      this.collapsedDocs.set(new Set());
    } catch (e: any) {
      this.error.set(e?.error?.message ?? e?.message ?? 'Failed to load medical record.');
    } finally {
      this.loading.set(false);
    }
  }

  // ── Stats ───────────────────────────────────────────────────────────────
  get totalConsultations(): number { return this.flatConsultations().length; }

  get totalPrescriptions(): number {
    return this.flatConsultations().reduce((s, e) => s + (e.consultation.prescriptions?.length ?? 0), 0);
  }

  get totalLabResults(): number {
    return this.flatConsultations().reduce((s, e) =>
      s + (e.consultation.labOrders?.filter(lo => lo.result !== null).length ?? 0), 0);
  }

  get totalLabOrders(): number {
    return this.flatConsultations().reduce((s, e) => s + (e.consultation.labOrders?.length ?? 0), 0);
  }

  countResults(orders: MedicalLabOrder[]): number {
    return orders.filter(lo => lo.result !== null).length;
  }

  // ── Helpers ─────────────────────────────────────────────────────────────
  private _utc(s: string): string {
    if (!s) return s;
    return (s.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(s)) ? s : s + 'Z';
  }

  print(): void {
    this.fullView.set(true);
    // Give Angular one tick to render all groups before the browser print dialog
    setTimeout(() => {
      window.print();
      // Restore pagination after the print dialog closes
      window.addEventListener('afterprint', () => this.fullView.set(false), { once: true });
    }, 50);
  }

  minVal(a: number, b: number): number { return Math.min(a, b); }

  async downloadPdf(): Promise<void> {
    const el = document.getElementById('mr-printable');
    if (!el) return;
    this.downloading.set(true);
    this.fullView.set(true); // render all groups before html2canvas captures
    // wait one tick for Angular to re-render
    await new Promise(r => setTimeout(r, 80));
    try {
      const name = `MedicalRecord_${this.record()?.fullName?.replace(/\s+/g, '_') ?? 'Patient'}_${new Date().toISOString().slice(0, 10)}.pdf`;
      await this.pdfService.downloadElement(el, name);
    } finally {
      this.fullView.set(false); // restore pagination after capture
      this.downloading.set(false);
    }
  }

  // ── Badge filter ───────────────────────────────────────────────────
  selectDoctor(name: string | null): void {
    this.selectedDoctor.set(name);
    this.searchQuery.set('');
    this.mrPage.set(1);
  }

  // ── Collapse / expand doctor section ───────────────────────────────
  toggleCollapse(doctorName: string): void {
    this.collapsedDocs.update(set => {
      const next = new Set(set);
      next.has(doctorName) ? next.delete(doctorName) : next.add(doctorName);
      return next;
    });
  }

  isCollapsed(doctorName: string): boolean {
    return this.collapsedDocs().has(doctorName);
  }

  // ── Count helpers ───────────────────────────────────────────────────
  countPrescriptions(entries: ConsultationEntry[]): number {
    return entries.reduce((s, e) => s + (e.consultation.prescriptions?.length ?? 0), 0);
  }

  countLabOrders(entries: ConsultationEntry[]): number {
    return entries.reduce((s, e) => s + (e.consultation.labOrders?.length ?? 0), 0);
  }

  // Converts a DoctorGroup + ConsultationEntry into a FlatEntry for download methods
  toFlat(group: DoctorGroup, entry: ConsultationEntry): FlatEntry {
    return { ...entry, doctorName: group.doctorName, specialty: group.specialty };
  }

  async downloadRx(entry: FlatEntry): Promise<void> {
    const rec = this.record();
    if (!rec) return;
    this.rxDownloading.set(entry.consultation.id);
    try {
      const filename = `Prescription_${rec.fullName.replace(/\s+/g, '_')}_${entry.appointmentDate.slice(0, 10)}.pdf`;
      await this.pdfService.downloadPrescription({
        patientName:     rec.fullName,
        dateOfBirth:     rec.dateOfBirth,
        bloodGroup:      rec.bloodGroup,
        doctorName:      entry.doctorName,
        specialty:       entry.specialty,
        appointmentDate: entry.appointmentDate,
        prescriptions:   entry.consultation.prescriptions,
      }, filename);
    } finally {
      this.rxDownloading.set(null);
    }
  }

  async downloadLabPdf(entry: FlatEntry, lo: MedicalLabOrder): Promise<void> {
    const rec = this.record();
    if (!rec || !lo.result) return;
    this.labDownloading.set(lo.id);
    try {
      const safeName = `LabResult_${lo.analysisType.replace(/\s+/g, '_')}_${rec.fullName.replace(/\s+/g, '_')}_${lo.result.createdAt.slice(0, 10)}.pdf`;
      await this.pdfService.downloadLabResult({
        patientName:    rec.fullName,
        analysisType:   lo.analysisType,
        technicienName: lo.result.technicienName,
        medicalComment: lo.result.medicalComment,
        status:         lo.result.status,
        createdAt:      lo.result.createdAt,
        doctorName:     entry.doctorName,
      }, safeName);
    } finally {
      this.labDownloading.set(null);
    }
  }

  formatDate(d: string): string {
    return new Date(this._utc(d)).toLocaleDateString('en-US', {
      day: '2-digit', month: 'long', year: 'numeric'
    });
  }

  formatDateTime(d: string): string {
    return new Date(this._utc(d)).toLocaleDateString('en-US', {
      weekday: 'long', day: '2-digit', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  today(): string {
    return new Date().toLocaleDateString('en-US', { day: '2-digit', month: 'long', year: 'numeric' });
  }
}
