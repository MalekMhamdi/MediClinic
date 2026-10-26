import { Component, Input, Output, EventEmitter, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
@if (totalPages() > 1) {
  <div class="pg-bar">
    <button class="pg-btn" [disabled]="page === 1" (click)="go(page - 1)">‹</button>

    @for (p of visiblePages(); track p) {
      @if (p === -1) {
        <span class="pg-ellipsis">…</span>
      } @else {
        <button class="pg-btn" [class.pg-active]="p === page" (click)="go(p)">{{ p }}</button>
      }
    }

    <button class="pg-btn" [disabled]="page === totalPages()" (click)="go(page + 1)">›</button>

    <span class="pg-info">
      {{ (page - 1) * pageSize + 1 }}–{{ min(page * pageSize, total) }} of {{ total }}
    </span>
  </div>
}
  `,
  styles: [`
.pg-bar { display: flex; align-items: center; gap: 4px; padding: 16px 0 4px; flex-wrap: wrap; }
.pg-btn {
  min-width: 32px; height: 32px; padding: 0 8px;
  border-radius: 7px; border: 1.5px solid #e4eaf3;
  background: white; color: #0B2D5C; font-size: 13px;
  cursor: pointer; font-weight: 600; transition: all .15s;
}
.pg-btn:hover:not(:disabled) { background: #ebf5fb; border-color: #2980b9; color: #2980b9; }
.pg-btn:disabled { opacity: .4; cursor: not-allowed; }
.pg-active { background: #0B2D5C !important; color: white !important; border-color: #0B2D5C !important; }
.pg-ellipsis { padding: 0 4px; color: #aab4cc; font-size: 14px; }
.pg-info { margin-left: 8px; font-size: 12px; color: #6b7a99; }
  `]
})
export class PaginationComponent {
  @Input() total    = 0;
  @Input() pageSize = 10;
  @Input() page     = 1;
  @Output() pageChange = new EventEmitter<number>();

  totalPages = computed(() => Math.max(1, Math.ceil(this.total / this.pageSize)));

  visiblePages = computed((): number[] => {
    const tp  = this.totalPages();
    const cur = this.page;
    if (tp <= 7) return Array.from({ length: tp }, (_, i) => i + 1);
    const pages: number[] = [1];
    if (cur > 3)            pages.push(-1);
    for (let p = Math.max(2, cur - 1); p <= Math.min(tp - 1, cur + 1); p++) pages.push(p);
    if (cur < tp - 2)       pages.push(-1);
    pages.push(tp);
    return pages;
  });

  go(p: number): void {
    const tp = this.totalPages();
    if (p < 1 || p > tp || p === this.page) return;
    this.pageChange.emit(p);
  }

  min(a: number, b: number): number { return Math.min(a, b); }
}
