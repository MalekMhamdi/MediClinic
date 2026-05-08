import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

type Tab = 'upcoming' | 'past' | 'cancelled';

interface Appointment {
  id: string;
  type: string;
  doctor: string;
  location: string;
  date: Date;
  time: string;
  status: 'confirmed' | 'pending' | 'cancelled';
}

@Component({
  selector: 'app-appointments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './appointments.html',
  styleUrl: './appointments.css'
})
export class AppointmentsComponent {

  activeTab = signal<Tab>('upcoming');

  appointments = signal<Appointment[]>([
    {
      id: '1',
      type: 'Consultation générale',
      doctor: 'Dr. Bennani',
      location: 'Salle 3, Étage 1',
      date: new Date(2026, 4, 1),
      time: '09h30',
      status: 'confirmed'
    },
    {
      id: '2',
      type: 'Bilan sanguin complet',
      doctor: 'Laboratoire central',
      location: 'Rez-de-chaussée',
      date: new Date(2026, 4, 8),
      time: '07h00',
      status: 'pending'
    },
    {
      id: '3',
      type: 'Consultation cardiologie',
      doctor: 'Dr. Tlili',
      location: 'Salle 7, Étage 2',
      date: new Date(2026, 4, 15),
      time: '14h00',
      status: 'confirmed'
    },
    {
      id: '4',
      type: 'Suivi diabétologie',
      doctor: 'Dr. Saidi',
      location: 'Salle 2, Étage 1',
      date: new Date(2026, 2, 14),
      time: '11h00',
      status: 'confirmed'
    }
  ]);

  get filtered(): Appointment[] {
    const now = new Date();
    switch (this.activeTab()) {
      case 'upcoming':
        return this.appointments().filter(a => a.status !== 'cancelled' && a.date >= now);
      case 'past':
        return this.appointments().filter(a => a.status !== 'cancelled' && a.date < now);
      case 'cancelled':
        return this.appointments().filter(a => a.status === 'cancelled');
    }
  }

  setTab(tab: Tab): void { this.activeTab.set(tab); }

  cancel(id: string): void {
    if (confirm('Confirmer l\'annulation de ce rendez-vous ?')) {
      this.appointments.update(list =>
        list.map(a => a.id === id ? { ...a, status: 'cancelled' as const } : a)
      );
    }
  }

  statusLabel(status: string): string {
    return status === 'confirmed' ? 'Confirmé' : status === 'pending' ? 'En attente' : 'Annulé';
  }
}