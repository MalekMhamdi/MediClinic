import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1';

export interface PendingOrder {
  id:              number;
  consultationId:  number;
  patientFullName: string;
  patientCin:      string;
  analysisType:    string;
  status:          string;
  doctorNotes:     string | null;
  orderedAt:       string;
  doctorFullName:  string;
  doctorSpecialty: string;
}

export interface LabResult {
  id:                 number;
  labOrderId:         number;
  analysisType:       string;
  patientFullName:    string;
  technicienFullName: string;
  status:             string;
  resultPdfPath:      string | null;
  medicalComment:     string | null;
  submittedAt:        string;
}

@Injectable({ providedIn: 'root' })
export class LabtechService {
  constructor(private http: HttpClient) {}

  async getPendingOrders(): Promise<PendingOrder[]> {
    return firstValueFrom(this.http.get<PendingOrder[]>(`${API}/labtech/orders/pending`));
  }

  async getAllOrders(status?: string): Promise<PendingOrder[]> {
    const url = status ? `${API}/labtech/orders?status=${status}` : `${API}/labtech/orders`;
    return firstValueFrom(this.http.get<PendingOrder[]>(url));
  }

  async getMyResults(): Promise<LabResult[]> {
    return firstValueFrom(this.http.get<LabResult[]>(`${API}/labtech/results/mine`));
  }

  async submitResult(dto: {
    labOrderId:      number;
    resultPdfPath:   string | null;
    medicalComment:  string | null;
  }): Promise<LabResult> {
    return firstValueFrom(this.http.post<LabResult>(`${API}/labtech/results`, dto));
  }

  async updateResult(id: number, dto: {
    comment:       string;
    resultPdfPath?: string;
  }): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/labtech/results/${id}`, dto));
  }

  async deleteResult(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/LabResult/${id}`));
  }

  /** Upload le fichier PDF vers le backend, retourne l'URL publique */
  async uploadPdf(file: File): Promise<string> {
    const form = new FormData();
    form.append('file', file);
    const res = await firstValueFrom(
      this.http.post<{ url: string }>(`${API}/LabResult/upload-pdf`, form)
    );
    return res.url;
  }
}
