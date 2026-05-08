import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../shared/services/auth.service';
import { LabtechService, PendingOrder, LabResult } from '../shared/services/labtech.service';

type Tab = 'orders' | 'results' | 'profile';
type StatusFilter = 'All' | 'Pending' | 'Done' | 'InProgress';

@Component({
  selector: 'app-labtech-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './labtech-dashboard.html',
  styleUrl: './labtech-dashboard.css'
})
export class LabtechDashboard implements OnInit {

  tab     = signal<Tab>('orders');
  loading = signal(true);
  error   = signal('');

  // ── Orders ───────────────────────────────────────────────────
  allOrders      = signal<PendingOrder[]>([]);
  statusFilter   = signal<StatusFilter>('Pending');

  filtered = computed(() => {
    const f = this.statusFilter();
    const all = this.allOrders();
    return f === 'All' ? all : all.filter(o => o.status === f);
  });

  pendingCount  = computed(() => this.allOrders().filter(o => o.status === 'Pending').length);

  // ── Submit result form ───────────────────────────────────────
  selectedOrder   = signal<PendingOrder | null>(null);
  resultComment   = signal('');
  resultPdfPath   = signal('');
  selectedFile    = signal<File | null>(null);
  submitting      = signal(false);
  submitError     = signal('');
  submitOk        = signal('');

  // ── My results ───────────────────────────────────────────────
  myResults     = signal<LabResult[]>([]);

  // ── Profile ──────────────────────────────────────────────────
  profFirst  = signal('');
  profLast   = signal('');
  profEmail  = signal('');
  profOk     = signal('');
  profError  = signal('');
  savingProf = signal(false);
  curPwd = signal(''); newPwd = signal(''); confPwd = signal('');
  pwdOk  = signal(''); pwdError = signal(''); savingPwd = signal(false);

  constructor(public auth: AuthService, private labtechService: LabtechService) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadOrders(), this.auth.refreshCurrentUser()]);
    const u = this.auth.currentUser();
    if (u) {
      const parts = u.name.split(' ');
      this.profFirst.set(parts[0] ?? '');
      this.profLast.set(parts.slice(1).join(' ') ?? '');
      this.profEmail.set(u.email);
    }
  }

  // ── Loaders ──────────────────────────────────────────────────

  async loadOrders(): Promise<void> {
    this.loading.set(true); this.error.set('');
    try {
      this.allOrders.set(await this.labtechService.getAllOrders());
    } catch {
      this.error.set('Failed to load orders.');
    } finally {
      this.loading.set(false);
    }
  }

  async loadResults(): Promise<void> {
    this.loading.set(true); this.error.set('');
    try {
      this.myResults.set(await this.labtechService.getMyResults());
    } catch {
      this.error.set('Failed to load results.');
    } finally {
      this.loading.set(false);
    }
  }

  async setTab(t: Tab): Promise<void> {
    this.tab.set(t); this.error.set('');
    this.selectedOrder.set(null);
    if (t === 'results' && this.myResults().length === 0) await this.loadResults();
  }

  // ── Order selection ───────────────────────────────────────────

  selectOrder(o: PendingOrder): void {
    const same = this.selectedOrder()?.id === o.id;
    this.selectedOrder.set(same ? null : o);
    this.submitError.set(''); this.submitOk.set('');
    this.resultComment.set(''); this.resultPdfPath.set('');
    this.selectedFile.set(null);
  }

  // ── File handling ─────────────────────────────────────────────

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    if (file) this.resultPdfPath.set(file.name);
  }

  // ── Submit result ─────────────────────────────────────────────

  async submitResult(): Promise<void> {
    const order = this.selectedOrder();
    if (!order) return;
    this.submitError.set(''); this.submitting.set(true);
    try {
      // 1. Upload PDF si un fichier est sélectionné
      let pdfUrl: string | null = null;
      const file = this.selectedFile();
      if (file) {
        pdfUrl = await this.labtechService.uploadPdf(file);
      }

      // 2. Soumettre le résultat avec l'URL publique
      await this.labtechService.submitResult({
        labOrderId:     order.id,
        resultPdfPath:  pdfUrl,
        medicalComment: this.resultComment() || null,
      });

      this.allOrders.update(list =>
        list.map(o => o.id === order.id ? { ...o, status: 'Done' } : o)
      );
      this.selectedOrder.set(null);
      this.selectedFile.set(null);
      this.resultPdfPath.set('');
      this.resultComment.set('');
      this.submitOk.set(`✔ Result submitted for "${order.analysisType}". Patient and doctor notified.`);
      setTimeout(() => this.submitOk.set(''), 5000);
    } catch (e: any) {
      this.submitError.set(e?.error?.error ?? e?.error?.message ?? 'Submission failed.');
    } finally {
      this.submitting.set(false);
    }
  }

  // ── Delete result ─────────────────────────────────────────────

  async deleteResult(id: number): Promise<void> {
    if (!confirm('Delete this result? The lab order will return to Pending.')) return;
    try {
      await this.labtechService.deleteResult(id);
      this.myResults.update(list => list.filter(r => r.id !== id));
    } catch (e: any) {
      alert(e?.error?.error ?? e?.error?.message ?? 'Failed to delete result.');
    }
  }

  // ── Profile ───────────────────────────────────────────────────

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
    } catch (e: any) { this.profError.set(e?.error?.error ?? 'Failed.'); }
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
      this.pwdOk.set('Password changed.');
      this.curPwd.set(''); this.newPwd.set(''); this.confPwd.set('');
      setTimeout(() => this.pwdOk.set(''), 3000);
    } catch (e: any) { this.pwdError.set(e?.error?.error ?? 'Incorrect current password.'); }
    finally { this.savingPwd.set(false); }
  }

  // ── Helpers ───────────────────────────────────────────────────

  get today(): string {
    return new Date().toLocaleDateString('en-US', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
    });
  }

  logout(): void { this.auth.logout(); }

  statusClass(s: string): string {
    return { 'Done': 'done', 'InProgress': 'inprogress' }[s] ?? 'pending';
  }

  statusLabel(s: string): string {
    return { 'Pending': 'Pending', 'Done': 'Done', 'InProgress': 'In Progress' }[s] ?? s;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  get initials(): string {
    const n = this.auth.userName?.split(' ') ?? [];
    return n.length >= 2 ? `${n[0][0]}${n[1][0]}`.toUpperCase() : (n[0]?.[0] ?? 'L').toUpperCase();
  }
}
