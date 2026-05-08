import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, NotificationItem, NotifType } from '../../shared/services/notification.service';

type FilterTab = 'all' | 'unread' | 'appointment' | 'result' | 'system';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class NotificationsComponent implements OnInit {

  loading = signal(true);
  error   = signal('');
  activeFilter = signal<FilterTab>('all');

  constructor(private notifService: NotificationService) {}

  async ngOnInit(): Promise<void> {
    await this.loadNotifications();
  }

  async loadNotifications(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.notifService.load();
    } catch {
      this.error.set('Failed to load notifications.');
    } finally {
      this.loading.set(false);
    }
  }

  notifications = computed(() => this.notifService.items());
  unreadCount   = computed(() => this.notifService.unreadCount());

  filtered = computed(() => {
    const f   = this.activeFilter();
    const all = this.notifications();
    if (f === 'all')     return all;
    if (f === 'unread')  return all.filter(n => !n.isRead);
    return all.filter(n => n.type === f);
  });

  setFilter(f: FilterTab): void { this.activeFilter.set(f); }

  async markRead(id: number): Promise<void> {
    try { await this.notifService.markRead(id); } catch { }
  }

  async markAllRead(): Promise<void> {
    try { await this.notifService.markAllRead(); } catch { }
  }

  async deleteNotif(id: number, event: Event): Promise<void> {
    event.stopPropagation();
    try { await this.notifService.delete(id); } catch { }
  }

  typeIcon(type: NotifType): string {
    const map: Record<NotifType, string> = {
      appointment: '📅',
      result:      '🔬',
      system:      '⚙️',
    };
    return map[type] ?? '🔔';
  }

  typeClass(type: NotifType): string {
    const map: Record<NotifType, string> = {
      appointment: 'blue',
      result:      'green',
      system:      'gray',
    };
    return map[type] ?? 'gray';
  }

  typeLabel(type: NotifType): string {
    const map: Record<NotifType, string> = {
      appointment: 'Appointment',
      result:      'Result',
      system:      'System',
    };
    return map[type] ?? 'Info';
  }

  formatDate(d: Date): string {
    return new Date(d).toLocaleString('en-US', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
