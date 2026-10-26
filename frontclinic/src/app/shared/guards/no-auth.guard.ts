import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

const ROLE_ROUTES: Record<string, string> = {
  patient: '/patient',
  doctor:  '/doctor',
  admin:   '/admin',
  labtech: '/labtech',
};

/** Prevents already-logged-in users from reaching /login or /register */
export const noAuthGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  const token = localStorage.getItem('clinic_token');
  if (auth.isLoggedIn() && token) {
    const role = auth.role ?? 'patient';
    router.navigate([ROLE_ROUTES[role] ?? '/patient'], { replaceUrl: true });
    return false;
  }

  return true;
};
