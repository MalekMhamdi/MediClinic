import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1';

export type NotifType = 'appointment' | 'result' | 'system';

export interface NotificationItem {
  id: number;
  message: string;
  type: NotifType;
  title: string;
  appointmentId: number | null;
  isRead: boolean;
  createdAt: Date;
}

const TYPE_TITLE: Record<string, string> = {
  AppointmentBooked:      'New Appointment',
  AppointmentConfirmed:   'Appointment Confirmed',
  AppointmentCancelled:   'Appointment Cancelled',
  AppointmentRescheduled: 'Appointment Rescheduled',
  LabResultAvailable:     'Result Available',
  ConsultationCreated:    'Consultation Recorded',
  SystemAlert:            'System Notice',
};

@Injectable({ providedIn: 'root' })
export class NotificationService {

  items = signal<NotificationItem[]>([]);
  unreadCount = computed(() => this.items().filter(n => !n.isRead).length);

  constructor(private http: HttpClient) {}

  async load(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<any[]>(`${API}/Notification`));
      this.items.set(res.map(n => this._map(n)));
    } catch (e) {
      console.error('Failed to load notifications', e);
    }
  }

  async loadUnreadCount(): Promise<void> {
    try {
      const res = await firstValueFrom(
        this.http.get<{ unreadCount: number }>(`${API}/Notification/unread-count`)
      );
      // We set a placeholder; the real count is computed from items()
      // but if items are not loaded yet, pre-populate signal via a one-shot signal patch
      if (this.items().length === 0) {
        const fakes = Array.from({ length: res.unreadCount }, (_, i) => ({
          id: -(i + 1), message: '', type: 'system' as NotifType,
          title: '', appointmentId: null, isRead: false, createdAt: new Date()
        }));
        this.items.set(fakes);
      }
    } catch { }
  }

  async markRead(id: number): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Notification/${id}/read`, {}));
    this.items.update(list => list.map(n => n.id === id ? { ...n, isRead: true } : n));
  }

  async markAllRead(): Promise<void> {
    await firstValueFrom(this.http.patch(`${API}/Notification/read-all`, {}));
    this.items.update(list => list.map(n => ({ ...n, isRead: true })));
  }

  async delete(id: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${API}/Notification/${id}`));
    this.items.update(list => list.filter(n => n.id !== id));
  }

  private _map(n: any): NotificationItem {
    return {
      id:            n.id,
      message:       n.message,
      type:          this._mapType(n.type),
      title:         TYPE_TITLE[n.type] ?? 'Notification',
      appointmentId: n.appointmentId ?? null,
      isRead:        n.isRead,
      createdAt:     new Date(n.createdAt),
    };
  }

  private _mapType(t: string): NotifType {
    if (t?.includes('Lab')) return 'result';
    if (t?.includes('Appointment') || t?.includes('Consultation')) return 'appointment';
    return 'system';
  }
}
