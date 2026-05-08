import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1';

export interface AppointmentItem {
  id: number;
  patientId: number;
  patientFullName: string;
  doctorId: number;
  doctorFullName: string;
  doctorSpecialty: string;
  dateTime: Date;
  status: string;
  statusLabel: string;
  reason: string | null;
  hasConsultation: boolean;
}

export interface ConsultationItem {
  id: number;
  appointmentId: number;
  diagnostic: string | null;
  observations: string | null;
  treatment: string | null;
  createdAt: Date;
  prescriptions: PrescriptionItem[];
  labOrders: LabOrderItem[];
}

export interface PrescriptionItem {
  id: number;
  medication: string;
  dosage: string;
  frequency: string;
  duration: string;
  notes: string | null;
}

export interface LabOrderItem {
  id: number;
  analysisType: string;
  notes: string | null;
  status: string;
}

export interface DoctorLabResult {
  id: number;
  labOrderId: number;
  analysisType: string;
  patientId: number;
  patientName: string;
  technicienName: string;
  labComment: string | null;
  status: string;
  statusLabel: string;
  createdAt: Date;
}

@Injectable({ providedIn: 'root' })
export class DoctorService {
  constructor(private http: HttpClient) {}

  private static _utc(s: string): string {
    if (!s) return s;
    return (s.endsWith('Z') || /[+-]\d{2}:\d{2}$/.test(s)) ? s : s + 'Z';
  }

  async getAppointments(doctorId: number, status?: string): Promise<AppointmentItem[]> {
    let url = `${API}/Appointment/by-doctor/${doctorId}`;
    if (status) url += `?status=${status}`;
    const res = await firstValueFrom(this.http.get<any[]>(url));
    return res.map(a => ({ ...a, dateTime: new Date(DoctorService._utc(a.dateTime)) }));
  }

  async deleteAppointment(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/Appointment/${id}`));
  }

  async accept(id: number): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Appointment/${id}/accept`, {}));
  }

  async refuse(id: number): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Appointment/${id}/refuse`, {}));
  }

  async getConsultation(appointmentId: number): Promise<ConsultationItem | null> {
    try {
      const res = await firstValueFrom(
        this.http.get<any>(`${API}/Consultation/by-appointment/${appointmentId}`)
      );
      return res ? { ...res, createdAt: new Date(res.createdAt) } : null;
    } catch { return null; }
  }

  async createConsultation(dto: {
    appointmentId: number;
    diagnostic?: string;
    observations?: string;
    treatment?: string;
  }): Promise<ConsultationItem> {
    const res = await firstValueFrom(this.http.post<any>(`${API}/Consultation`, dto));
    return { ...res, createdAt: new Date(res.createdAt) };
  }

  async updateConsultation(id: number, dto: {
    diagnostic: string;
    observations: string;
    treatment?: string;
  }): Promise<void> {
    await firstValueFrom(this.http.put(`${API}/Consultation/${id}`, dto));
  }

  async addPrescription(consultationId: number, dto: {
    medication: string;
    dosage: string;
    frequency: string;
    duration: string;
    notes?: string;
  }): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API}/Consultation/${consultationId}/prescriptions`, {
        consultationId, ...dto
      })
    );
  }

  async addLabOrder(consultationId: number, dto: {
    analysisType: string;
    notes?: string;
  }): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API}/Consultation/${consultationId}/lab-orders`, {
        consultationId, ...dto
      })
    );
  }

  async getLabResults(doctorId: number): Promise<DoctorLabResult[]> {
    const res = await firstValueFrom(this.http.get<any[]>(`${API}/LabResult/by-doctor/${doctorId}`));
    return res.map(r => ({ ...r, createdAt: new Date(r.createdAt) }));
  }

  async sendDoctorComment(labResultId: number, comment: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API}/LabResult/${labResultId}/doctor-comment`, { comment })
    );
  }

  async getPatientAppointments(patientId: number): Promise<AppointmentItem[]> {
    const res = await firstValueFrom(this.http.get<any[]>(`${API}/Appointment/by-patient/${patientId}`));
    return res.map(a => ({ ...a, dateTime: new Date(DoctorService._utc(a.dateTime)) }));
  }

  async deleteLabResult(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/LabResult/${id}`));
  }

  async getMedicalRecord(patientId: number): Promise<PatientMedicalRecord> {
    return firstValueFrom(this.http.get<PatientMedicalRecord>(`${API}/Patient/${patientId}/medical-record`));
  }

  async getConsultationDetail(appointmentId: number): Promise<MedicalConsultation | null> {
    try {
      return await firstValueFrom(this.http.get<MedicalConsultation>(`${API}/Consultation/by-appointment/${appointmentId}`));
    } catch { return null; }
  }
}

export interface PatientMedicalRecord {
  patientId:    number;
  fullName:     string;
  cin:          string;
  dateOfBirth:  string;
  bloodGroup:   string | null;
  phone:        string | null;
  appointments: MedicalApptEntry[];
}

export interface MedicalApptEntry {
  id:              number;
  doctorFullName:  string;
  doctorSpecialty: string;
  dateTime:        string;
  status:          string;
  statusLabel:     string;
  reason:          string | null;
  hasConsultation: boolean;
}

export interface MedicalPrescription {
  id:         number;
  medication: string;
  dosage:     string;
  frequency:  string;
  duration:   string;
  notes:      string | null;
}

export interface MedicalLabResult {
  id:             number;
  technicienName: string;
  medicalComment: string | null;
  resultPdfPath:  string | null;
  status:         string;
  createdAt:      string;
}

export interface MedicalLabOrder {
  id:           number;
  analysisType: string;
  status:       string;
  statusLabel:  string;
  notes:        string | null;
  result:       MedicalLabResult | null;
}

export interface MedicalConsultation {
  id:            number;
  appointmentId: number;
  diagnostic:    string | null;
  observations:  string | null;
  treatment:     string | null;
  createdAt:     string;
  prescriptions: MedicalPrescription[];
  labOrders:     MedicalLabOrder[];
}
