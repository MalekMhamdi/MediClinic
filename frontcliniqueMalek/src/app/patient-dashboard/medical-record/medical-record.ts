import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../shared/services/auth.service';

const API = 'http://localhost:5217/api/v1';

interface MedicalRecord {
  patientId:   number;
  fullName:    string;
  cin:         string;
  dateOfBirth: string;
  bloodGroup:  string | null;
  phone:       string | null;
  appointments: AppointmentEntry[];
}

interface AppointmentEntry {
  id:              number;
  doctorFullName:  string;
  doctorSpecialty: string;
  dateTime:        string;
  status:          string;
  statusLabel:     string;
  reason:          string | null;
  hasConsultation: boolean;
}

interface Prescription {
  id:        number;
  medication: string;
  dosage:     string;
  frequency:  string;
  duration:   string;
  notes:      string | null;
}

interface LabResult {
  id:             number;
  technicienName: string;
  medicalComment: string | null;
  resultPdfPath:  string | null;
  status:         string;
  createdAt:      string;
}

interface LabOrder {
  id:           number;
  analysisType: string;
  status:       string;
  statusLabel:  string;
  notes:        string | null;
  result:       LabResult | null;
}

interface ConsultationDetail {
  id:            number;
  appointmentId: number;
  diagnostic:    string | null;
  observations:  string | null;
  treatment:     string | null;
  createdAt:     string;
  prescriptions: Prescription[];
  labOrders:     LabOrder[];
}

@Component({
  selector: 'app-medical-record',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medical-record.html',
  styleUrl: './medical-record.css'
})
export class MedicalRecordComponent implements OnInit {

  loading       = signal(true);
  error         = signal('');
  record        = signal<MedicalRecord | null>(null);
  consultations = signal<ConsultationDetail[]>([]);
  activeTab     = signal<'appointments' | 'prescriptions' | 'laborders'>('appointments');

  constructor(private auth: AuthService, private http: HttpClient) {}

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const id = this.auth.userId;
      const rec = await firstValueFrom(
        this.http.get<MedicalRecord>(`${API}/Patient/${id}/medical-record`)
      );
      this.record.set(rec);

      // Load consultations for appointments that have them
      const consulted = rec.appointments.filter(a => a.hasConsultation);
      const details = await Promise.all(
        consulted.map(a =>
          firstValueFrom(
            this.http.get<ConsultationDetail>(`${API}/Consultation/by-appointment/${a.id}`)
          ).catch(() => null)
        )
      );
      this.consultations.set(details.filter(Boolean) as ConsultationDetail[]);
    } catch (e: any) {
      this.error.set('Failed to load medical record. Make sure the backend is running.');
    } finally {
      this.loading.set(false);
    }
  }

  get allPrescriptions(): Prescription[] {
    return this.consultations().flatMap(c => c.prescriptions ?? []);
  }

  get allLabOrders(): LabOrder[] {
    return this.consultations().flatMap(c => c.labOrders ?? []);
  }

  labStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'done':     return 'green';
      case 'reviewed': return 'blue';
      default:         return 'amber';
    }
  }

  setTab(t: 'appointments' | 'prescriptions' | 'laborders'): void {
    this.activeTab.set(t);
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }
}
