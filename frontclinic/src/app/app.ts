import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const API = 'http://localhost:5217/api/v1';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit {

  constructor(private http: HttpClient) {}

  async ngOnInit(): Promise<void> {
    // Déclenche les rappels pour les RDV de demain (date locale)
    try {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0]; // YYYY-MM-DD
      await firstValueFrom(
        this.http.post(`${API}/Appointment/send-reminders?targetDate=${dateStr}`, {})
      );
    } catch { /* silencieux si backend non disponible */ }
  }
}