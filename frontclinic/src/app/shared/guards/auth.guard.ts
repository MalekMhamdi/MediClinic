import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, _state) => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  // Check both the signal state AND the raw localStorage token
  const token = localStorage.getItem('clinic_token');
  if (!auth.isLoggedIn() || !token) {
    router.navigate(['/login']);
    return false;
  }

  return true;
};