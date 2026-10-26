import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class PdfService {
  private _downloading = false;

  constructor(@Inject(PLATFORM_ID) private platformId: object) {}

  /** Capture a DOM element and save as A4 PDF (multi-page aware). */
  async downloadElement(el: HTMLElement, filename: string): Promise<void> {
    if (!isPlatformBrowser(this.platformId) || this._downloading) return;
    this._downloading = true;
    try {
      const [{ default: html2canvas }, { default: jsPDF }] = await Promise.all([
        import('html2canvas'),
        import('jspdf'),
      ]);

      const canvas = await html2canvas(el, {
        scale: 2,
        useCORS: true,
        allowTaint: true,
        backgroundColor: '#ffffff',
        logging: false,
      });

      const imgData = canvas.toDataURL('image/png');
      const A4_W = 210; // mm
      const A4_H = 297; // mm
      const imgH = (canvas.height * A4_W) / canvas.width;

      const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });

      let remaining = imgH;
      let srcY = 0;

      while (remaining > 0) {
        const sliceH = Math.min(remaining, A4_H);
        // Crop the canvas slice
        const sliceCanvas = document.createElement('canvas');
        sliceCanvas.width  = canvas.width;
        sliceCanvas.height = (sliceH / imgH) * canvas.height;
        const ctx = sliceCanvas.getContext('2d')!;
        ctx.drawImage(canvas, 0, srcY * (canvas.height / imgH), canvas.width, sliceCanvas.height, 0, 0, canvas.width, sliceCanvas.height);
        if (srcY > 0) pdf.addPage();
        pdf.addImage(sliceCanvas.toDataURL('image/png'), 'PNG', 0, 0, A4_W, sliceH);
        srcY += sliceH;
        remaining -= sliceH;
      }

      pdf.save(filename);
    } finally {
      this._downloading = false;
    }
  }

  /** Generate a prescription PDF using jsPDF text API. */
  async downloadPrescription(data: {
    patientName:   string;
    dateOfBirth:   string;
    bloodGroup:    string | null;
    doctorName:    string;
    specialty:     string;
    appointmentDate: string;
    prescriptions: { medication: string; dosage: string | null; duration: string | null; instructions: string | null }[];
  }, filename: string): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const { default: jsPDF } = await import('jspdf');
    const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });

    const navy  = [11,  45,  92] as const;
    const mint  = [29, 158, 117] as const;
    const gray  = [107, 122, 153] as const;
    const light = [240, 247, 244] as const;

    // ── Header band ───────────────────────────────────────────────
    pdf.setFillColor(...navy);
    pdf.rect(0, 0, 210, 30, 'F');

    // Cross logo
    pdf.setFillColor(255, 255, 255);
    pdf.rect(12, 8, 5, 14, 'F');
    pdf.rect(8, 12, 13, 5, 'F');

    pdf.setTextColor(255, 255, 255);
    pdf.setFontSize(17); pdf.setFont('helvetica', 'bold');
    pdf.text('MediClinic', 30, 16);
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.text('Accredited Medical Center', 30, 23);

    // Rx stamp top-right
    pdf.setFontSize(26); pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(255, 255, 255);
    pdf.text('Rx', 190, 22, { align: 'right' });

    // ── Doctor info band ──────────────────────────────────────────
    pdf.setFillColor(...light);
    pdf.rect(0, 30, 210, 20, 'F');
    pdf.setTextColor(...navy);
    pdf.setFontSize(11); pdf.setFont('helvetica', 'bold');
    pdf.text(`Dr. ${data.doctorName}`, 14, 40);
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(...gray);
    pdf.text(`${data.specialty}   ·   ${new Date(data.appointmentDate).toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' })}`, 14, 47);

    // ── Patient info ──────────────────────────────────────────────
    pdf.setDrawColor(...navy); pdf.setLineWidth(0.3);
    pdf.line(14, 54, 196, 54);
    pdf.setFontSize(8); pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(...gray);
    pdf.text('PATIENT', 14, 60);
    pdf.setFontSize(10); pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(...navy);
    pdf.text(data.patientName.toUpperCase(), 14, 67);
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(...gray);
    const dob = data.dateOfBirth ? new Date(data.dateOfBirth).toLocaleDateString('en-GB') : '—';
    pdf.text(`DOB: ${dob}   ·   Blood type: ${data.bloodGroup || 'N/A'}`, 14, 73);
    pdf.line(14, 77, 196, 77);

    // ── Medications list ──────────────────────────────────────────
    let y = 88;
    pdf.setFontSize(9); pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(...navy);
    pdf.text('PRESCRIBED MEDICATIONS', 14, y);
    y += 8;

    data.prescriptions.forEach((rx, idx) => {
      // Number circle
      pdf.setFillColor(...mint);
      pdf.circle(18, y - 1.5, 4, 'F');
      pdf.setTextColor(255, 255, 255);
      pdf.setFontSize(8); pdf.setFont('helvetica', 'bold');
      pdf.text(String(idx + 1), 18, y + 1, { align: 'center' });

      // Medication name
      pdf.setTextColor(...navy);
      pdf.setFontSize(11); pdf.setFont('helvetica', 'bold');
      pdf.text(rx.medication, 26, y + 1);
      y += 7;

      // Details
      pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(...gray);
      const details: string[] = [];
      if (rx.dosage)    details.push(`Dosage: ${rx.dosage}`);
      if (rx.duration)  details.push(`Duration: ${rx.duration}`);
      if (details.length) { pdf.text(details.join('   ·   '), 26, y); y += 5; }

      if (rx.instructions) {
        pdf.setTextColor(186, 117, 23);
        pdf.text(`⚠ ${rx.instructions}`, 26, y);
        y += 5;
      }

      // Separator
      pdf.setDrawColor(230, 230, 230); pdf.setLineWidth(0.2);
      pdf.line(26, y + 1, 196, y + 1);
      y += 8;
    });

    // ── Signature area ────────────────────────────────────────────
    y = Math.max(y + 10, 230);
    pdf.setFillColor(...light);
    pdf.roundedRect(120, y, 72, 25, 2, 2, 'F');
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(...gray);
    pdf.text("Doctor's signature & stamp", 156, y + 8, { align: 'center' });
    pdf.setDrawColor(...gray); pdf.setLineWidth(0.3);
    pdf.line(128, y + 20, 184, y + 20);

    // ── Disclaimer ────────────────────────────────────────────────
    pdf.setFontSize(7); pdf.setFont('helvetica', 'italic');
    pdf.setTextColor(...gray);
    pdf.text(
      'Confidential medical document. Not valid without doctor\'s original signature. For medical use only.',
      105, 276, { align: 'center' }
    );

    // ── Footer line ───────────────────────────────────────────────
    pdf.setFillColor(...navy);
    pdf.rect(0, 282, 210, 5, 'F');
    pdf.setTextColor(255, 255, 255);
    pdf.setFontSize(7); pdf.setFont('helvetica', 'normal');
    pdf.text('MediClinic  ·  Tunis, Tunisie  ·  Tel: +216 71 000 000', 105, 285.5, { align: 'center' });

    pdf.save(filename);
  }

  /** Generate a lab result PDF using jsPDF text API (no html2canvas needed). */
  async downloadLabResult(data: {
    patientName: string;
    analysisType: string;
    technicienName: string;
    medicalComment: string | null;
    status: string;
    createdAt: Date | string;
    doctorName?: string;
  }, filename: string): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const { default: jsPDF } = await import('jspdf');
    const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });

    const blue  = [11, 45, 92] as const;
    const green = [39, 174, 96] as const;
    const gray  = [107, 122, 153] as const;

    // ── Header band
    pdf.setFillColor(...blue);
    pdf.rect(0, 0, 210, 32, 'F');

    // Cross logo
    pdf.setFillColor(255, 255, 255);
    pdf.rect(12, 8, 6, 16, 'F');
    pdf.rect(7, 13, 16, 6, 'F');

    pdf.setTextColor(255, 255, 255);
    pdf.setFontSize(18); pdf.setFont('helvetica', 'bold');
    pdf.text('MediClinic', 32, 17);
    pdf.setFontSize(9); pdf.setFont('helvetica', 'normal');
    pdf.text('Accredited Medical Center · Tunis, Tunisie', 32, 24);

    // Title
    pdf.setFontSize(11); pdf.setFont('helvetica', 'bold');
    pdf.text('LABORATORY REPORT', 140, 14, { align: 'right' });
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.text(`Generated on ${new Date().toLocaleDateString('en-GB')}`, 140, 20, { align: 'right' });

    // ── Patient band
    pdf.setFillColor(240, 244, 251);
    pdf.rect(0, 32, 210, 22, 'F');
    pdf.setTextColor(...blue);
    pdf.setFontSize(13); pdf.setFont('helvetica', 'bold');
    pdf.text(data.patientName.toUpperCase(), 14, 43);
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(...gray);
    pdf.text(`Performed on: ${new Date(data.createdAt).toLocaleDateString('en-GB')}`, 14, 50);

    // ── Divider
    pdf.setDrawColor(...blue);
    pdf.setLineWidth(0.5);
    pdf.line(14, 58, 196, 58);

    // ── Analysis block
    let y = 68;
    const field = (label: string, value: string) => {
      pdf.setFontSize(8); pdf.setFont('helvetica', 'bold');
      pdf.setTextColor(...gray);
      pdf.text(label.toUpperCase(), 14, y);
      pdf.setFontSize(11); pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(30, 30, 30);
      pdf.text(value || '—', 14, y + 6);
      y += 16;
    };

    field('Analysis Type', data.analysisType);
    field('Laboratory Technician', data.technicienName);
    field('Status', data.status);
    if (data.doctorName) field('Ordering Physician', `Dr. ${data.doctorName}`);

    // ── Medical interpretation
    y += 4;
    pdf.setFillColor(248, 250, 252);
    pdf.roundedRect(12, y, 186, 8, 2, 2, 'F');
    pdf.setFontSize(9); pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(...blue);
    pdf.text('MEDICAL INTERPRETATION', 16, y + 5.5);
    y += 14;

    pdf.setTextColor(30, 30, 30);
    pdf.setFontSize(10); pdf.setFont('helvetica', 'normal');
    const comment = data.medicalComment || 'No medical comment added yet.';
    const lines = pdf.splitTextToSize(comment, 180);
    pdf.text(lines, 14, y);
    y += lines.length * 6 + 10;

    // ── Disclaimer
    pdf.setDrawColor(220, 220, 220);
    pdf.line(14, y, 196, y);
    y += 6;
    pdf.setFontSize(7.5); pdf.setFont('helvetica', 'italic');
    pdf.setTextColor(...gray);
    pdf.text(
      'This document is a confidential laboratory report issued by MediClinic. For medical use only.',
      105, y, { align: 'center' }
    );

    // ── Footer
    pdf.setFontSize(8); pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(...gray);
    pdf.text('MediClinic · Tunis, Tunisie · Tel: +216 71 000 000', 105, 284, { align: 'center' });
    pdf.setDrawColor(...blue); pdf.setLineWidth(1);
    pdf.line(0, 287, 210, 287);

    pdf.save(filename);
  }
}
