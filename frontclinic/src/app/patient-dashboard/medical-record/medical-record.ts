import { Component, signal, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';

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

export interface DoctorGroup {
  doctorName: string;
  specialty:  string;
  consultations: ConsultationEntry[];
}

export interface ConsultationEntry {
  consultation:      MedicalConsultation;
  appointmentDate:   string;
  appointmentReason: string | null;
}

@Component({
  selector: 'app-medical-record',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medical-record.html',
  styleUrl:    './medical-record.css'
})
export class MedicalRecordComponent implements OnChanges {

  @Input() patientId?: number;

  loading = signal(true);
  error   = signal('');
  record  = signal<MedicalRecord | null>(null);

  constructor(private auth: AuthService, private http: HttpClient) {
    this.load();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patientId']) this.load();
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
    } catch (e: any) {
      this.error.set(e?.error?.message ?? e?.message ?? 'Failed to load medical record.');
    } finally {
      this.loading.set(false);
    }
  }

  // ── Grouped by doctor — only appointments that have a consultation ──────
  get groupedByDoctor(): DoctorGroup[] {
    const appts = this.record()?.appointments ?? [];
    const map = new Map<string, DoctorGroup>();

    for (const a of appts) {
      if (!a.hasConsultation || !a.consultation) continue;

      const key = `${a.doctorFullName}|${a.doctorSpecialty}`;
      if (!map.has(key)) {
        map.set(key, { doctorName: a.doctorFullName, specialty: a.doctorSpecialty, consultations: [] });
      }
      map.get(key)!.consultations.push({
        consultation:      a.consultation,
        appointmentDate:   this._utc(a.dateTime),
        appointmentReason: a.reason,
      });
    }

    // Sort each group newest first, then sort groups by most recent
    const groups = [...map.values()].map(g => ({
      ...g,
      consultations: g.consultations.sort(
        (a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime()
      ),
    }));

    return groups.sort((a, b) => {
      const la = new Date(a.consultations[0]?.appointmentDate ?? 0).getTime();
      const lb = new Date(b.consultations[0]?.appointmentDate ?? 0).getTime();
      return lb - la;
    });
  }

  // ── Stats ───────────────────────────────────────────────────────────────
  get totalConsultations(): number {
    return this.groupedByDoctor.reduce((s, g) => s + g.consultations.length, 0);
  }

  get totalPrescriptions(): number {
    return this.groupedByDoctor.reduce((s, g) =>
      s + g.consultations.reduce((s2, c) => s2 + (c.consultation.prescriptions?.length ?? 0), 0), 0);
  }

  get totalLabResults(): number {
    return this.groupedByDoctor.reduce((s, g) =>
      s + g.consultations.reduce((s2, c) =>
        s2 + (c.consultation.labOrders?.filter(lo => lo.result !== null).length ?? 0), 0), 0);
  }

  get totalLabOrders(): number {
    return this.groupedByDoctor.reduce((s, g) =>
      s + g.consultations.reduce((s2, c) => s2 + (c.consultation.labOrders?.length ?? 0), 0), 0);
  }

  countResults(orders: MedicalLabOrder[]): number {
    return orders.filter(lo => lo.result !== null).length;
  }

  countGroupPrescriptions(consultations: ConsultationEntry[]): number {
    return consultations.reduce((s, c) => s + (c.consultation.prescriptions?.length ?? 0), 0);
  }

  countGroupLabOrders(consultations: ConsultationEntry[]): number {
    return consultations.reduce((s, c) => s + (c.consultation.labOrders?.length ?? 0), 0);
  }

  // ── Helpers ─────────────────────────────────────────────────────────────
  private _utc(s: string): string {
    if (!s) return s;
    return (s.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(s)) ? s : s + 'Z';
  }

  print(): void { window.print(); }

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
