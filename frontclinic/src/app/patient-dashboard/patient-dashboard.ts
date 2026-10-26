import { Component, computed, signal, OnInit } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';
import { AuthService } from '../shared/services/auth.service';
import { NotificationService } from '../shared/services/notification.service';
import { ChatPopupComponent } from '../shared/chat-popup/chat-popup';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ChatPopupComponent],
  templateUrl: './patient-dashboard.html',
  styleUrl: './patient-dashboard.css'
})
export class PatientDashboard implements OnInit {

  unreadCount = computed(() => this.notifService.unreadCount());
  private _url = signal('');
  isAiRoute    = computed(() => this._url().includes('/ai-agent'));

  constructor(
    private authService: AuthService,
    private notifService: NotificationService,
    private router: Router
  ) {
    this._url.set(router.url);
    router.events.pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this._url.set(router.url));
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.notifService.loadUnreadCount(),
      this.authService.refreshCurrentUser(),
    ]);
  }

  get patient() { return this.authService.currentUser; }

  get initials(): string {
    const name = this.authService.userName;
    if (!name) return 'P';
    const parts = name.split(' ');
    return parts.length >= 2
      ? `${parts[0][0]}${parts[1][0]}`.toUpperCase()
      : parts[0][0].toUpperCase();
  }

  userId() { return this.authService.userId; }

  get today(): string {
    return new Date().toLocaleDateString('en-US', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
    });
  }

  logout(): void { this.authService.logout(); }
}
