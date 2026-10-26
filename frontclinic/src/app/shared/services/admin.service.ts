import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1';

export interface AdminStats {
  totalPatients:           number;
  totalDoctors:            number;
  totalLabTechs:           number;
  totalAppointments:       number;
  pendingAppointments:     number;
  cancelledAppointments:   number;
  cancellationRate:        number;
  totalConsultations:      number;
  totalLabOrders:          number;
  appointmentsByMonth:     { month: string; count: number }[];
  appointmentsBySpecialty: { specialty: string; count: number }[];
  appointmentsByDoctor:    { doctor: string; specialty: string; appointments: number; consultations: number }[];
}

export interface AdminAppointment {
  id:              number;
  patientName:     string;
  doctorName:      string;
  doctorSpecialty: string;
  dateTime:        string;
  status:          string;
  statusLabel:     string;
  hasConsultation: boolean;
  reason:          string | null;
}

export interface AdminUser {
  id:        number;
  fullName:  string;
  email:     string;
  role:      string;
  isActive:  boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private http: HttpClient) {}

  async getStats(): Promise<AdminStats> {
    return firstValueFrom(this.http.get<AdminStats>(`${API}/Admin/stats`));
  }

  async getUsers(): Promise<AdminUser[]> {
    return firstValueFrom(this.http.get<AdminUser[]>(`${API}/Admin/users`));
  }

  async toggleUser(id: number): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Admin/users/${id}/toggle`, {}));
  }

  async changeRole(id: number, newRole: string): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Admin/users/${id}/role`, { role: newRole }));
  }

  async createUser(dto: {
    firstName: string; lastName: string; email: string; password: string; role: string;
  }): Promise<AdminUser> {
    return firstValueFrom(this.http.post<AdminUser>(`${API}/Admin/users`, dto));
  }

  async updateUser(id: number, dto: {
    firstName: string; lastName: string; email: string;
  }): Promise<void> {
    await firstValueFrom(this.http.put(`${API}/Admin/users/${id}`, dto));
  }

  async deleteUser(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/Admin/users/${id}`));
  }

  async getAppointments(status?: string, specialty?: string): Promise<AdminAppointment[]> {
    let url = `${API}/Admin/appointments`;
    const params: string[] = [];
    if (status)    params.push(`status=${encodeURIComponent(status)}`);
    if (specialty) params.push(`specialty=${encodeURIComponent(specialty)}`);
    if (params.length) url += '?' + params.join('&');
    const raw = await firstValueFrom(this.http.get<any[]>(url));
    return raw.map(a => ({ ...a, dateTime: a.dateTime ?? a.DateTime }));
  }

  async getSpecialties(): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`${API}/Specialty`));
  }

  async createSpecialty(name: string, description?: string): Promise<void> {
    await firstValueFrom(this.http.post(`${API}/Specialty`, { name, description }));
  }

  async updateSpecialty(id: number, name: string, description?: string): Promise<void> {
    await firstValueFrom(this.http.put(`${API}/Specialty/${id}`, { name, description }));
  }

  async deleteSpecialty(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/Specialty/${id}`));
  }
}
