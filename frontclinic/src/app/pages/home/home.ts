import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  scrollTo(sectionId: string): void {
    document.getElementById(sectionId)
      ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
