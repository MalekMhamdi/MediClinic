import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

// ── Types ──────────────────────────────────────────────────────────────
export type Role = 'Patient' | 'Doctor' | 'Admin' | 'LabTechnicien';
export type FrontRole = 'patient' | 'doctor' | 'admin' | 'labtech';

export interface AuthResponse {
  userId:    number;
  fullName:  string;
  email:     string;
  role:      Role;
  token:     string;
  expiresAt: string;
}

export interface CurrentUser {
  id:       number;
  name:     string;
  email:    string;
  role:     FrontRole;
  backRole: Role;
  token:    string;
}

// ── Keys ───────────────────────────────────────────────────────────────
const TOKEN_KEY = 'clinic_token';
const USER_KEY  = 'clinic_user';

const API = 'http://localhost:5217/api/v1';

// Map backend role → front route segment
const ROLE_MAP: Record<Role, FrontRole> = {
  Patient:       'patient',
  Doctor:        'doctor',
  Admin:         'admin',
  LabTechnicien: 'labtech'
};

const ROLE_ROUTES: Record<FrontRole, string> = {
  patient: '/patient/profile',
  doctor:  '/doctor',
  admin:   '/admin',
  labtech: '/labtech'
};

@Injectable({ providedIn: 'root' })
export class AuthService {

  // ── Reactive state ────────────────────────────────────────────────────
  currentUser = signal<CurrentUser | null>(this._loadUser());
  isLoggedIn  = signal<boolean>(this.currentUser() !== null);

  constructor(private http: HttpClient, private router: Router) {}

  // ── Login ─────────────────────────────────────────────────────────────
  async login(email: string, password: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${API}/Auth/login`, { email, password })
    );
    this._persist(res);
    const frontRole = ROLE_MAP[res.role] ?? 'patient';
    // replaceUrl removes /login from history so back button can't return to it
    this.router.navigate([ROLE_ROUTES[frontRole]], { replaceUrl: true });
  }

  // ── Register patient (public) ─────────────────────────────────────────
  async registerPatient(dto: {
    firstName: string; lastName: string; email: string; password: string;
    cin: string; dateOfBirth: string; bloodGroup?: string; phone?: string;
    address?: string;
    emergencyContactName?: string; emergencyContactRelation?: string; emergencyContactPhone?: string;
  }): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${API}/Auth/register`, dto)
    );
    this._persist(res);
    this.router.navigate(['/patient/profile'], { replaceUrl: true });
  }

  // ── Profile ────────────────────────────────────────────────────────────
  async getProfile(): Promise<any> {
    return await firstValueFrom(this.http.get(`${API}/Auth/me`));
  }

  async refreshCurrentUser(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<any>(`${API}/Auth/me`));
      const current = this.currentUser();
      if (current && me) {
        const fullName = `${me.firstName ?? ''} ${me.lastName ?? ''}`.trim() || current.name;
        const updated = { ...current, name: fullName };
        localStorage.setItem(USER_KEY, JSON.stringify(updated));
        this.currentUser.set(updated);
      }
    } catch { }
  }

  async updateProfile(data: any): Promise<void> {
    await firstValueFrom(this.http.put(`${API}/Auth/profile`, data));
  }

  // ── Change password ────────────────────────────────────────────────────
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.put(`${API}/Auth/change-password`, { currentPassword, newPassword })
    );
  }

  // ── Logout ────────────────────────────────────────────────────────────
  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.isLoggedIn.set(false);
    this.router.navigate(['/login']);
  }

  // ── Helpers ───────────────────────────────────────────────────────────
  get token(): string | null  { return localStorage.getItem(TOKEN_KEY); }
  get role(): FrontRole | null { return this.currentUser()?.role ?? null; }
  get userName(): string       { return this.currentUser()?.name ?? ''; }
  get userId(): number         { return this.currentUser()?.id ?? 0; }

  private _persist(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    const user: CurrentUser = {
      id:       res.userId,
      name:     res.fullName,
      email:    res.email,
      role:     ROLE_MAP[res.role] ?? 'patient',
      backRole: res.role,
      token:    res.token
    };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
    this.isLoggedIn.set(true);
  }

  private _loadUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      if (!raw) return null;
      const user = JSON.parse(raw) as CurrentUser;
      // Decode JWT and check expiration
      const payload = JSON.parse(atob(user.token.split('.')[1]));
      if (payload.exp && payload.exp * 1000 < Date.now()) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        return null;
      }
      return user;
    } catch { return null; }
  }
}