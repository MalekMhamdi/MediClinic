import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { Register } from './auth/register/register';
import { Home } from './pages/home/home';

import { PatientDashboard } from './patient-dashboard/patient-dashboard';
import { AppointmentsComponent } from './patient-dashboard/appointments/appointments';
import { ProfileComponent } from './patient-dashboard/profile/profile';
import { MedicalRecordComponent } from './patient-dashboard/medical-record/medical-record';
import { BookAppointmentComponent } from './patient-dashboard/book-appointment/book-appointment';
import { NotificationsComponent } from './patient-dashboard/notifications/notifications';
import { AiAgentComponent } from './patient-dashboard/ai-agent/ai-agent';

import { DoctorDashboard } from './doctor-dashboard/doctor-dashboard';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { LabtechDashboard } from './labtech-dashboard/labtech-dashboard';

import { authGuard } from './shared/guards/auth.guard';

export const routes: Routes = [

  // Public routes — no guard needed
  { path: '',         component: Home },
  { path: 'login',    component: LoginComponent },
  { path: 'register', component: Register },

  // Protected routes — require login
  {
    path: 'patient',
    component: PatientDashboard,
    canActivate: [authGuard],
    children: [
      { path: '',                 redirectTo: 'profile', pathMatch: 'full' },
      { path: 'profile',          component: ProfileComponent },
      { path: 'appointments',     component: AppointmentsComponent },
      { path: 'medical-record',   component: MedicalRecordComponent },
      { path: 'book-appointment', component: BookAppointmentComponent },
      { path: 'notifications',    component: NotificationsComponent },
      { path: 'ai-agent',         component: AiAgentComponent },
    ]
  },
  {
    path: 'doctor',
    component: DoctorDashboard,
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [authGuard]
  },
  {
    path: 'labtech',
    component: LabtechDashboard,
    canActivate: [authGuard]
  },

  // Fallback
  { path: '**', redirectTo: '/login' }
];