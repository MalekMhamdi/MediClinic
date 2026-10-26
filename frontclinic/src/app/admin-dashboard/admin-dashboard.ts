import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../shared/pagination/pagination';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../shared/services/auth.service';
import { AdminService, AdminStats, AdminUser, AdminAppointment } from '../shared/services/admin.service';

const IA_API = 'http://localhost:8000';
const IA_KEY = 'clinic-admin-2025';

type Tab = 'stats' | 'users' | 'specialties' | 'profile' | 'ia-analytics' | 'appointments';

interface IaStats {
  queue: {
    active_sessions:       number;
    total_waiting:         number;
    total_served_today:    number;
    total_sessions_created: number;
    urgent_count:          number;
    medium_count:          number;
    low_count:             number;
  };
  llm: {
    model:                string;
    total_requests:       number;
    total_tokens:         number;
    prompt_tokens:        number;
    completion_tokens:    number;
    errors:               number;
    cost_usd:             number;
    avg_tokens_per_request: number;
  };
  uptime_seconds: number;
}

interface IaQueueEntry {
  name:      string;
  specialty: string;
  timestamp: string;
  urgency?:  number;
  symptoms?: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {

  tab     = signal<Tab>('stats');
  loading = signal(true);
  error   = signal('');

  // ── Confirm dialog ───────────────────────────────────────────
  confirmMsg     = signal('');
  confirmAction  = signal<(() => void) | null>(null);

  private _confirm(message: string, action: () => void): void {
    this.confirmMsg.set(message);
    this.confirmAction.set(action);
  }

  confirmOk(): void {
    this.confirmAction()?.();
    this.confirmMsg.set('');
    this.confirmAction.set(null);
  }

  confirmCancel(): void {
    this.confirmMsg.set('');
    this.confirmAction.set(null);
  }

  // ── Data ────────────────────────────────────────────────────
  stats       = signal<AdminStats | null>(null);
  users       = signal<AdminUser[]>([]);
  specialties = signal<any[]>([]);

  // ── User filters ─────────────────────────────────────────────
  userSearch  = signal('');
  roleFilter  = signal('');
  usersPage   = signal(1);
  readonly USERS_PAGE_SIZE = 10;

  filteredUsers = computed(() => {
    let list = this.users();
    const q = this.userSearch().toLowerCase();
    const r = this.roleFilter();
    if (q) list = list.filter(u =>
      u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    );
    if (r) list = list.filter(u => u.role === r);
    return list;
  });

  pagedUsers = computed(() => {
    const p  = this.usersPage();
    const ps = this.USERS_PAGE_SIZE;
    return this.filteredUsers().slice((p - 1) * ps, p * ps);
  });

  // ── Create user modal ────────────────────────────────────────
  showCreateUser = signal(false);
  cuFirst   = signal(''); cuLast  = signal('');
  cuEmail   = signal(''); cuPwd   = signal('');
  cuRole    = signal('Patient');
  cuError   = signal(''); cuOk    = signal('');
  savingCu  = signal(false);

  // ── Edit user modal ──────────────────────────────────────────
  editingUser = signal<AdminUser | null>(null);
  euFirst = signal(''); euLast = signal(''); euEmail = signal('');
  euError = signal(''); euOk   = signal('');
  savingEu = signal(false);

  // ── Role modal ───────────────────────────────────────────────
  roleTarget = signal<AdminUser | null>(null);
  newRole    = signal('');
  savingRole = signal(false);

  // ── Specialty form ───────────────────────────────────────────
  editingSp   = signal<any | null>(null);
  spName      = signal(''); spDesc  = signal('');
  spError     = signal(''); spOk    = signal('');
  savingSp    = signal(false);

  // ── Admin profile ────────────────────────────────────────────
  profFirst   = signal(''); profLast  = signal('');
  profEmail   = signal('');
  profOk      = signal(''); profError = signal('');
  savingProf  = signal(false);
  showPwdForm = signal(false);
  curPwd = signal(''); newPwd = signal(''); confPwd = signal('');
  pwdOk = signal(''); pwdError = signal(''); savingPwd = signal(false);

  readonly roles = ['Patient', 'Doctor', 'LabTechnicien', 'Admin'];

  // ── Appointments tab ──────────────────────────────────────────
  appointments      = signal<AdminAppointment[]>([]);
  apptStatusFilter  = signal('');
  apptSpecialty     = signal('');
  apptSearch        = signal('');
  loadingAppts      = signal(false);
  apptsPage         = signal(1);
  readonly APPTS_PAGE_SIZE = 12;

  filteredAppts = computed(() => {
    let list = this.appointments();
    const q  = this.apptSearch().toLowerCase();
    const s  = this.apptStatusFilter();
    const sp = this.apptSpecialty().toLowerCase();
    if (q)  list = list.filter(a =>
      a.patientName.toLowerCase().includes(q) ||
      a.doctorName.toLowerCase().includes(q));
    if (s)  list = list.filter(a => a.status === s);
    if (sp) list = list.filter(a => a.doctorSpecialty.toLowerCase().includes(sp));
    return list;
  });

  pagedAppts = computed(() => {
    const p  = this.apptsPage();
    const ps = this.APPTS_PAGE_SIZE;
    return this.filteredAppts().slice((p - 1) * ps, p * ps);
  });

  // ── IA Analytics ─────────────────────────────────────────────
  iaStats       = signal<IaStats | null>(null);
  iaQueue       = signal<IaQueueEntry[]>([]);
  loadingIa     = signal(false);
  iaRefreshAt   = signal<Date | null>(null);

  constructor(
    public auth: AuthService,
    private adminService: AdminService,
    private http: HttpClient
  ) {}

  async loadIaAnalytics(): Promise<void> {
    this.loadingIa.set(true);
    try {
      const stats = await firstValueFrom(
        this.http.get<IaStats>(`${IA_API}/api/stats`)
      );
      this.iaStats.set(stats);

      const queueRes = await firstValueFrom(
        this.http.get<{ queue: IaQueueEntry[] }>(`${IA_API}/api/queue/status`, {
          headers: { 'X-Admin-Key': IA_KEY }
        })
      );
      this.iaQueue.set(queueRes.queue ?? []);
      this.iaRefreshAt.set(new Date());
    } catch {
      this.iaStats.set(null);
    } finally {
      this.loadingIa.set(false);
    }
  }

  async callNextPatient(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post<any>(`${IA_API}/api/queue/next`, {}, {
          headers: { 'X-Admin-Key': IA_KEY }
        })
      );
      await this.loadIaAnalytics();
    } catch { }
  }

  async cleanupSessions(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post<any>(`${IA_API}/api/queue/cleanup`, {}, {
          headers: { 'X-Admin-Key': IA_KEY }
        })
      );
      await this.loadIaAnalytics();
    } catch { }
  }

  iaUptime(seconds: number): string {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    return `${h}h ${m}m`;
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadStats(), this.auth.refreshCurrentUser()]);
    // Pre-fill profile fields
    const u = this.auth.currentUser();
    if (u) {
      const parts = u.name.split(' ');
      this.profFirst.set(parts[0] ?? '');
      this.profLast.set(parts.slice(1).join(' ') ?? '');
      this.profEmail.set(u.email);
    }
  }

  // ── Loaders ──────────────────────────────────────────────────

  async loadStats(): Promise<void> {
    this.loading.set(true); this.error.set('');
    try { this.stats.set(await this.adminService.getStats()); }
    catch { this.error.set('Failed to load stats.'); }
    finally { this.loading.set(false); }
  }

  async loadUsers(): Promise<void> {
    this.loading.set(true); this.error.set('');
    try { this.users.set(await this.adminService.getUsers()); }
    catch { this.error.set('Failed to load users.'); }
    finally { this.loading.set(false); }
  }

  async loadSpecialties(): Promise<void> {
    this.loading.set(true); this.error.set('');
    try { this.specialties.set(await this.adminService.getSpecialties()); }
    catch { this.error.set('Failed to load specialties.'); }
    finally { this.loading.set(false); }
  }

  async setTab(t: Tab): Promise<void> {
    this.tab.set(t); this.error.set('');
    if (t === 'users'         && this.users().length === 0)       await this.loadUsers();
    if (t === 'specialties'   && this.specialties().length === 0) await this.loadSpecialties();
    if (t === 'ia-analytics'  && !this.iaStats())                 await this.loadIaAnalytics();
    if (t === 'appointments'  && this.appointments().length === 0) await this.loadAppointments();
  }

  async loadAppointments(): Promise<void> {
    this.loadingAppts.set(true);
    try { this.appointments.set(await this.adminService.getAppointments()); }
    catch { this.error.set('Failed to load appointments.'); }
    finally { this.loadingAppts.set(false); }
  }

  async goToUsers(role: string): Promise<void> {
    this.roleFilter.set(role);
    this.userSearch.set('');
    this.usersPage.set(1);
    await this.setTab('users');
  }

  async goToAppointments(status = '', specialty = ''): Promise<void> {
    this.apptStatusFilter.set(status);
    this.apptSpecialty.set(specialty);
    this.apptSearch.set('');
    this.apptsPage.set(1);
    await this.setTab('appointments');
  }

  // ── Toggle user ───────────────────────────────────────────────

  async toggleUser(u: AdminUser): Promise<void> {
    try {
      await this.adminService.toggleUser(u.id);
      this.users.update(l => l.map(x => x.id === u.id ? { ...x, isActive: !x.isActive } : x));
    } catch (e: any) { alert(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
  }

  // ── Create user ───────────────────────────────────────────────

  openCreate(): void {
    this.cuFirst.set(''); this.cuLast.set(''); this.cuEmail.set('');
    this.cuPwd.set(''); this.cuRole.set('Patient');
    this.cuError.set(''); this.cuOk.set('');
    this.showCreateUser.set(true);
  }

  closeCreate(): void { this.showCreateUser.set(false); }

  async createUser(): Promise<void> {
    this.cuError.set('');
    if (!this.cuFirst() || !this.cuLast() || !this.cuEmail() || !this.cuPwd()) {
      this.cuError.set('All fields are required.'); return;
    }
    this.savingCu.set(true);
    try {
      const created = await this.adminService.createUser({
        firstName: this.cuFirst(), lastName: this.cuLast(),
        email: this.cuEmail(), password: this.cuPwd(), role: this.cuRole(),
      });
      this.users.update(l => [created, ...l]);
      this.cuOk.set('User created.');
      setTimeout(() => this.closeCreate(), 1500);
    } catch (e: any) { this.cuError.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingCu.set(false); }
  }

  // ── Edit user ─────────────────────────────────────────────────

  openEdit(u: AdminUser): void {
    const parts = u.fullName.split(' ');
    this.euFirst.set(parts[0] ?? '');
    this.euLast.set(parts.slice(1).join(' ') ?? '');
    this.euEmail.set(u.email);
    this.euError.set(''); this.euOk.set('');
    this.editingUser.set(u);
  }

  closeEdit(): void { this.editingUser.set(null); }

  async saveEdit(): Promise<void> {
    const u = this.editingUser();
    if (!u) return;
    this.euError.set('');
    this.savingEu.set(true);
    try {
      await this.adminService.updateUser(u.id, {
        firstName: this.euFirst(), lastName: this.euLast(), email: this.euEmail(),
      });
      this.users.update(l => l.map(x => x.id === u.id
        ? { ...x, fullName: `${this.euFirst()} ${this.euLast()}`.trim(), email: this.euEmail() }
        : x
      ));
      this.euOk.set('Updated.');
      setTimeout(() => this.closeEdit(), 1500);
    } catch (e: any) { this.euError.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingEu.set(false); }
  }

  // ── Change role ───────────────────────────────────────────────

  openRole(u: AdminUser): void {
    this.roleTarget.set(u);
    this.newRole.set(u.role);
  }

  closeRole(): void { this.roleTarget.set(null); }

  async saveRole(): Promise<void> {
    const u = this.roleTarget();
    if (!u) return;
    this.savingRole.set(true);
    try {
      await this.adminService.changeRole(u.id, this.newRole());
      this.users.update(l => l.map(x => x.id === u.id ? { ...x, role: this.newRole() } : x));
      this.closeRole();
    } catch (e: any) { alert(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingRole.set(false); }
  }

  // ── Delete user ───────────────────────────────────────────────

  deleteUser(u: AdminUser): void {
    this._confirm(`Delete "${u.fullName}"? This action cannot be undone.`, async () => {
      try {
        await this.adminService.deleteUser(u.id);
        this.users.update(l => l.filter(x => x.id !== u.id));
      } catch (e: any) { this.error.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    });
  }

  // ── Specialties ───────────────────────────────────────────────

  openSpCreate(): void {
    this.editingSp.set(null);
    this.spName.set(''); this.spDesc.set('');
    this.spError.set(''); this.spOk.set('');
  }

  openSpEdit(sp: any): void {
    this.editingSp.set(sp);
    this.spName.set(sp.name); this.spDesc.set(sp.description ?? '');
    this.spError.set(''); this.spOk.set('');
  }

  cancelSp(): void { this.editingSp.set(null); this.spName.set(''); this.spDesc.set(''); }

  async saveSp(): Promise<void> {
    this.spError.set('');
    if (!this.spName()) { this.spError.set('Name is required.'); return; }
    this.savingSp.set(true);
    try {
      const sp = this.editingSp();
      if (sp) {
        await this.adminService.updateSpecialty(sp.id, this.spName(), this.spDesc() || undefined);
        this.specialties.update(l => l.map(s => s.id === sp.id
          ? { ...s, name: this.spName(), description: this.spDesc() } : s
        ));
        this.spOk.set('Specialty updated.');
      } else {
        await this.adminService.createSpecialty(this.spName(), this.spDesc() || undefined);
        await this.loadSpecialties();
        this.spOk.set('Specialty added.');
      }
      this.spName.set(''); this.spDesc.set('');
      this.editingSp.set(null);
      setTimeout(() => this.spOk.set(''), 3000);
    } catch (e: any) { this.spError.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingSp.set(false); }
  }

  deleteSp(sp: any): void {
    this._confirm(`Delete specialty "${sp.name}"?`, async () => {
      try {
        await this.adminService.deleteSpecialty(sp.id);
        this.specialties.update(l => l.filter(s => s.id !== sp.id));
      } catch (e: any) { this.spError.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    });
  }

  // ── Admin profile ─────────────────────────────────────────────

  async saveProfile(): Promise<void> {
    this.profError.set(''); this.profOk.set('');
    if (!this.profFirst() || !this.profLast()) {
      this.profError.set('Name fields are required.'); return;
    }
    this.savingProf.set(true);
    try {
      await this.auth.updateProfile({
        firstName: this.profFirst(), lastName: this.profLast(),
        phone: null, bloodGroup: null, address: null,
      });
      await this.auth.refreshCurrentUser();
      this.profOk.set('Profile updated.');
      setTimeout(() => this.profOk.set(''), 3000);
    } catch (e: any) { this.profError.set(e?.error?.error ?? e?.error?.message ?? 'Operation failed.'); }
    finally { this.savingProf.set(false); }
  }

  async changePassword(): Promise<void> {
    this.pwdError.set(''); this.pwdOk.set('');
    if (!this.curPwd() || !this.newPwd() || !this.confPwd()) {
      this.pwdError.set('All fields required.'); return;
    }
    if (this.newPwd().length < 8) { this.pwdError.set('Min 8 characters.'); return; }
    if (this.newPwd() !== this.confPwd()) { this.pwdError.set('Passwords do not match.'); return; }
    this.savingPwd.set(true);
    try {
      await this.auth.changePassword(this.curPwd(), this.newPwd());
      this.pwdOk.set('Password changed successfully.');
      this.curPwd.set(''); this.newPwd.set(''); this.confPwd.set('');
      this.showPwdForm.set(false);
      setTimeout(() => this.pwdOk.set(''), 3000);
    } catch (e: any) { this.pwdError.set(e?.error?.message ?? 'Incorrect current password.'); }
    finally { this.savingPwd.set(false); }
  }

  // ── Helpers ───────────────────────────────────────────────────

  get initials(): string {
    const n = this.auth.userName?.split(' ') ?? [];
    return n.length >= 2 ? `${n[0][0]}${n[1][0]}`.toUpperCase() : (n[0]?.[0] ?? 'A').toUpperCase();
  }

  get today(): string {
    return new Date().toLocaleDateString('en-US', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }

  logout(): void { this.auth.logout(); }

  roleClass(role: string): string {
    return { 'Admin': 'role-admin', 'Doctor': 'role-doctor', 'Patient': 'role-patient', 'LabTechnicien': 'role-lab' }[role] ?? '';
  }

  roleLabel(role: string): string {
    return { 'LabTechnicien': 'Lab Tech' }[role] ?? role;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  apptStatusClass(s: string): string {
    return { Pending: 'pending', Confirmed: 'confirmed', Completed: 'completed',
             Cancelled: 'cancelled', Refused: 'cancelled' }[s] ?? '';
  }

  maxDoctorCount(): number {
    const data = this.stats()?.appointmentsByDoctor ?? [];
    return Math.max(...data.map(d => d.appointments), 1);
  }

  maxBarCount(): number {
    const data = this.stats()?.appointmentsBySpecialty ?? [];
    return Math.max(...data.map(s => s.count), 1);
  }
}
