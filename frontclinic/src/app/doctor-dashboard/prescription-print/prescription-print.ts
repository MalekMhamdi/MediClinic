import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PdfService } from '../../shared/services/pdf.service';

export interface RxMedication {
  medication: string;
  dosage:     string;
  frequency:  string;
  duration:   string;
  notes:      string | null;
}

export interface PrescriptionData {
  doctorName:      string;
  doctorSpecialty: string;
  clinicName:      string;
  clinicAddress:   string;
  clinicPhone:     string;
  patientName:     string;
  date:            Date;
  medications:     RxMedication[];
}

@Component({
  selector: 'app-prescription-print',
  standalone: true,
  imports: [CommonModule],
  template: `
<!-- ── Modal backdrop (hidden on print) ── -->
<div class="rx-overlay" (click)="close.emit()">
  <div class="rx-modal" (click)="$event.stopPropagation()">

    <!-- Toolbar (hidden on print) -->
    <div class="rx-toolbar no-print">
      <span class="rx-toolbar-title">📋 Prescription Preview</span>
      <div class="rx-toolbar-actions">
        <button class="rx-btn-download" [disabled]="downloading" (click)="download()">
          {{ downloading ? '⏳ Generating...' : '⬇ Download PDF' }}
        </button>
        <button class="rx-btn-print" (click)="print()">🖨️ Print</button>
        <button class="rx-btn-close" (click)="close.emit()">✕</button>
      </div>
    </div>

    <!-- ══ PRESCRIPTION DOCUMENT ══ -->
    <div class="rx-doc" id="rx-printable">

      <!-- Header -->
      <div class="rx-header">
        <div class="rx-doctor-block">
          <div class="rx-clinic-logo">
            <div class="rx-cross"></div>
          </div>
          <div class="rx-doctor-info">
            <div class="rx-doctor-name">Dr. {{ data.doctorName }}</div>
            <div class="rx-doctor-specialty">{{ data.doctorSpecialty }}</div>
            <div class="rx-clinic-name">{{ data.clinicName }}</div>
            <div class="rx-clinic-address">{{ data.clinicAddress }}</div>
            <div class="rx-clinic-phone">Tél. {{ data.clinicPhone }}</div>
          </div>
        </div>
        <div class="rx-date-block">
          <div class="rx-date-label">Tunis, le</div>
          <div class="rx-date-value">{{ data.date | date:'dd/MM/yyyy' }}</div>
        </div>
      </div>

      <div class="rx-divider"></div>

      <!-- Patient -->
      <div class="rx-patient-row">
        <div class="rx-patient-field">
          <span class="rx-field-label">Nom et Prénom&nbsp;:</span>
          <span class="rx-field-value">{{ data.patientName }}</span>
        </div>
        <div class="rx-patient-field">
          <span class="rx-field-label">Date&nbsp;:</span>
          <span class="rx-field-value">{{ data.date | date:'dd/MM/yyyy' }}</span>
        </div>
      </div>

      <!-- Rx body -->
      <div class="rx-body">
        <div class="rx-rp-symbol">℞</div>
        <div class="rx-meds-list">
          @for (med of data.medications; track $index) {
            <div class="rx-med-item">
              <div class="rx-med-number">{{ $index + 1 }}.</div>
              <div class="rx-med-content">
                <div class="rx-med-name">{{ med.medication }}
                  @if (med.dosage) { <span class="rx-med-dosage">{{ med.dosage }}</span> }
                </div>
                <div class="rx-med-instructions">
                  @if (med.frequency) { <span>{{ med.frequency }}</span> }
                  @if (med.duration)  { <span class="rx-med-duration"> — {{ med.duration }}</span> }
                </div>
                @if (med.notes) {
                  <div class="rx-med-notes">{{ med.notes }}</div>
                }
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Signature -->
      <div class="rx-signature-block">
        <div class="rx-signature-line">Signature et cachet du médecin</div>
        <div class="rx-signature-name">Dr. {{ data.doctorName }}</div>
        <div class="rx-stamp-circle">
          <span class="rx-stamp-inner">MediClinic<br>{{ data.doctorSpecialty }}</span>
        </div>
      </div>

      <!-- Footer -->
      <div class="rx-footer">
        <div class="rx-footer-note">
          Ce document est une ordonnance médicale officielle délivrée par {{ data.clinicName }}.
          À conserver et à présenter au pharmacien.
        </div>
      </div>

    </div><!-- end rx-doc -->
  </div>
</div>
  `,
  styles: [`
/* ── Modal / overlay ── */
.rx-overlay {
  position: fixed; inset: 0; z-index: 9999;
  background: rgba(0,0,0,.55);
  display: flex; align-items: center; justify-content: center;
  padding: 16px;
}
.rx-modal {
  background: #f0f0f0;
  border-radius: 12px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  max-height: 95vh;
  width: 740px;
  max-width: 100%;
  box-shadow: 0 20px 60px rgba(0,0,0,.35);
}

/* ── Toolbar ── */
.rx-toolbar {
  display: flex; align-items: center; justify-content: space-between;
  background: #0B2D5C; color: white;
  padding: 12px 20px; flex-shrink: 0;
}
.rx-toolbar-title { font-size: 14px; font-weight: 600; }
.rx-toolbar-actions { display: flex; gap: 10px; }
.rx-btn-download {
  padding: 7px 16px; background: #2980b9; color: white;
  border: none; border-radius: 6px; font-size: 13px;
  font-weight: 600; cursor: pointer;
}
.rx-btn-download:hover:not(:disabled) { background: #1f6d9e; }
.rx-btn-download:disabled { opacity: .6; cursor: not-allowed; }
.rx-btn-print {
  padding: 7px 16px; background: #27AE60; color: white;
  border: none; border-radius: 6px; font-size: 13px;
  font-weight: 600; cursor: pointer;
}
.rx-btn-print:hover { background: #219a52; }
.rx-btn-close {
  padding: 7px 12px; background: rgba(255,255,255,.15); color: white;
  border: none; border-radius: 6px; font-size: 14px; cursor: pointer;
}
.rx-btn-close:hover { background: rgba(255,255,255,.25); }

/* ── Document ── */
.rx-doc {
  background: white;
  margin: 16px;
  border-radius: 4px;
  padding: 36px 40px 28px;
  font-family: 'Times New Roman', Times, serif;
  color: #111;
  box-shadow: 0 2px 12px rgba(0,0,0,.12);
  overflow-y: auto;
  flex: 1;
}

/* ── Header ── */
.rx-header {
  display: flex; justify-content: space-between; align-items: flex-start;
  margin-bottom: 18px;
}
.rx-doctor-block { display: flex; gap: 14px; align-items: flex-start; }
.rx-clinic-logo {
  width: 46px; height: 46px; border-radius: 50%;
  background: #0B2D5C;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0; margin-top: 4px;
}
.rx-cross {
  width: 18px; height: 18px; background: white;
  clip-path: polygon(38% 0,62% 0,62% 38%,100% 38%,100% 62%,62% 62%,62% 100%,38% 100%,38% 62%,0 62%,0 38%,38% 38%);
}
.rx-doctor-name { font-size: 17px; font-weight: 700; color: #0B2D5C; }
.rx-doctor-specialty { font-size: 13px; color: #555; font-style: italic; margin-bottom: 4px; }
.rx-clinic-name { font-size: 13px; font-weight: 600; color: #333; }
.rx-clinic-address, .rx-clinic-phone { font-size: 11px; color: #666; }
.rx-date-block { text-align: right; }
.rx-date-label { font-size: 12px; color: #555; }
.rx-date-value { font-size: 15px; font-weight: 700; color: #0B2D5C; }

/* ── Divider ── */
.rx-divider { border: none; border-top: 2px solid #0B2D5C; margin: 12px 0 16px; }

/* ── Patient row ── */
.rx-patient-row {
  display: flex; justify-content: space-between;
  margin-bottom: 20px; gap: 16px;
}
.rx-patient-field { display: flex; gap: 8px; align-items: baseline; }
.rx-field-label { font-size: 12px; color: #555; white-space: nowrap; }
.rx-field-value {
  font-size: 14px; font-weight: 600; color: #111;
  border-bottom: 1px solid #999; padding-bottom: 1px; min-width: 160px;
}

/* ── Rx body ── */
.rx-body { display: flex; gap: 16px; margin-bottom: 32px; }
.rx-rp-symbol {
  font-size: 44px; font-weight: 900; color: #0B2D5C;
  line-height: 1; padding-top: 4px; flex-shrink: 0;
  font-family: 'Georgia', serif;
}
.rx-meds-list { flex: 1; display: flex; flex-direction: column; gap: 16px; padding-top: 8px; }
.rx-med-item { display: flex; gap: 8px; }
.rx-med-number { font-size: 14px; font-weight: 700; color: #0B2D5C; flex-shrink: 0; padding-top: 1px; }
.rx-med-content { flex: 1; }
.rx-med-name { font-size: 15px; font-weight: 700; color: #111; display: flex; align-items: baseline; gap: 8px; }
.rx-med-dosage { font-size: 13px; font-weight: 400; color: #0B2D5C; }
.rx-med-instructions { font-size: 13px; color: #444; margin-top: 3px; font-style: italic; }
.rx-med-duration { color: #777; }
.rx-med-notes { font-size: 12px; color: #888; margin-top: 2px; }

/* ── Signature ── */
.rx-signature-block {
  display: flex; flex-direction: column; align-items: flex-end;
  gap: 6px; margin-bottom: 24px; position: relative;
}
.rx-signature-line { font-size: 11px; color: #666; border-top: 1px solid #999; padding-top: 6px; width: 220px; text-align: center; }
.rx-signature-name { font-size: 16px; font-weight: 700; color: #0B2D5C; font-family: 'Georgia', cursive; width: 220px; text-align: center; }
.rx-stamp-circle {
  width: 80px; height: 80px; border-radius: 50%;
  border: 3px solid #0B2D5C; display: flex; align-items: center; justify-content: center;
  text-align: center; margin-top: -10px; margin-right: 60px;
  opacity: .7;
}
.rx-stamp-inner { font-size: 9px; font-weight: 700; color: #0B2D5C; line-height: 1.4; text-transform: uppercase; }

/* ── Footer ── */
.rx-footer { border-top: 1px solid #ccc; padding-top: 10px; }
.rx-footer-note { font-size: 10px; color: #999; text-align: center; font-style: italic; }

/* ══ PRINT STYLES ══ */
@media print {
  .no-print { display: none !important; }
  .rx-overlay { position: static !important; background: none !important; padding: 0 !important; }
  .rx-modal { box-shadow: none !important; border-radius: 0 !important; width: 100% !important; max-height: none !important; background: white !important; }
  .rx-doc { margin: 0 !important; box-shadow: none !important; overflow: visible !important; padding: 20mm 24mm !important; }
  * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
}
  `]
})
export class PrescriptionPrintComponent {
  @Input()  data!: PrescriptionData;
  @Output() close = new EventEmitter<void>();

  downloading = false;

  constructor(private pdf: PdfService) {}

  print(): void { window.print(); }

  async download(): Promise<void> {
    const el = document.getElementById('rx-printable');
    if (!el) return;
    this.downloading = true;
    try {
      const name = `Prescription_${this.data.patientName.replace(/\s+/g, '_')}_${new Date().toISOString().slice(0,10)}.pdf`;
      await this.pdf.downloadElement(el, name);
    } finally {
      this.downloading = false;
    }
  }
}
