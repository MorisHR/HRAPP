import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { LegalHoldService } from '../../../services/legal-hold.service';
import { LegalHold, EDiscoveryFormat } from '../../../models/legal-hold.model';

export interface EDiscoveryExportDialogData {
  legalHold: LegalHold;
}

/**
 * eDiscovery Export Dialog
 * Allows selection and download of legal hold data in various formats
 *
 * Features:
 * - Multiple export format support (EMLX, PDF, JSON, CSV, NATIVE)
 * - Format descriptions and use cases
 * - Chain of custody preservation
 * - Forensically sound exports
 * - Production-ready error handling
 */
@Component({
  selector: 'app-ediscovery-export-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    ButtonComponent
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div>
          <h2 class="dialog-title">Export eDiscovery Package</h2>
          <p class="dialog-subtitle">{{ hold.caseNumber }}</p>
        </div>
        <button class="close-button" (click)="onCancel()" type="button">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="dialog-body">
        <div class="info-banner">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon-info">
            <path stroke-linecap="round" stroke-linejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
          </svg>
          <div>
            <p class="info-title">Chain of Custody Preserved</p>
            <p class="info-text">All exports include metadata and timestamps for legal authenticity</p>
          </div>
        </div>

        <div class="stats-row">
          <div class="stat">
            <span class="stat-label">Affected Records:</span>
            <span class="stat-value">{{ hold.affectedAuditLogCount || 0 }}</span>
          </div>
          <div class="stat">
            <span class="stat-label">Start Date:</span>
            <span class="stat-value">{{ hold.startDate | date:'shortDate' }}</span>
          </div>
        </div>

        <h3 class="section-title">Select Export Format</h3>

        <div class="format-options">
          <label
            *ngFor="let format of formats"
            class="format-option"
            [class.selected]="selectedFormat() === format.value">
            <input
              type="radio"
              name="format"
              [value]="format.value"
              [(ngModel)]="selectedFormatValue"
              (ngModelChange)="selectedFormat.set($event)">
            <div class="format-content">
              <div class="format-header">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="format-icon">
                  <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="format.icon" />
                </svg>
                <span class="format-name">{{ format.name }}</span>
                <span class="format-badge">{{ format.value }}</span>
              </div>
              <p class="format-description">{{ format.description }}</p>
              <div class="format-use-case">
                <strong>Use Case:</strong> {{ format.useCase }}
              </div>
            </div>
          </label>
        </div>

        <div class="warning-box" *ngIf="selectedFormat() === 'PDF'">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon-warning">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
          </svg>
          <div>
            <p class="warning-title">Note: Simplified PDF Export</p>
            <p class="warning-text">Current implementation uses text-based PDF. Production systems typically use QuestPDF or DinkToPdf for enhanced formatting and forensic integrity.</p>
          </div>
        </div>
      </div>

      <div class="dialog-footer">
        <app-button
          type="button"
          variant="secondary"
          (click)="onCancel()">
          Cancel
        </app-button>
        <app-button
          type="button"
          variant="primary"
          [loading]="exporting()"
          [disabled]="!selectedFormat() || exporting()"
          (click)="onExport()">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon-sm">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
          </svg>
          Export {{ selectedFormat() }} Package
        </app-button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 700px;
      max-width: 90vw;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
    }

    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 1.5rem;
      border-bottom: 1px solid var(--color-neutral-200, #e5e7eb);
    }

    .dialog-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      margin: 0 0 0.25rem 0;
    }

    .dialog-subtitle {
      font-size: 0.875rem;
      color: var(--color-neutral-600, #4b5563);
      margin: 0;
      font-family: monospace;
    }

    .close-button {
      background: none;
      border: none;
      padding: 0.5rem;
      cursor: pointer;
      color: var(--color-neutral-500, #6b7280);
      transition: color 0.2s;
    }

    .close-button:hover {
      color: var(--color-neutral-900, #111827);
    }

    .icon {
      width: 1.5rem;
      height: 1.5rem;
    }

    .icon-sm {
      width: 1.25rem;
      height: 1.25rem;
      margin-right: 0.5rem;
    }

    .dialog-body {
      padding: 1.5rem;
      overflow-y: auto;
      flex: 1;
    }

    .info-banner {
      display: flex;
      gap: 0.75rem;
      padding: 1rem;
      background: #dbeafe;
      border: 1px solid #3b82f6;
      border-radius: 0.5rem;
      margin-bottom: 1.5rem;
    }

    .icon-info {
      width: 1.5rem;
      height: 1.5rem;
      color: #3b82f6;
      flex-shrink: 0;
    }

    .info-title {
      font-weight: 600;
      color: #1e40af;
      margin: 0 0 0.25rem 0;
      font-size: 0.875rem;
    }

    .info-text {
      font-size: 0.75rem;
      color: #1e3a8a;
      margin: 0;
    }

    .stats-row {
      display: flex;
      gap: 2rem;
      margin-bottom: 1.5rem;
      padding: 1rem;
      background: var(--color-neutral-50, #f9fafb);
      border-radius: 0.5rem;
    }

    .stat {
      display: flex;
      gap: 0.5rem;
      align-items: center;
    }

    .stat-label {
      font-size: 0.75rem;
      color: var(--color-neutral-600, #4b5563);
      font-weight: 600;
    }

    .stat-value {
      font-size: 0.875rem;
      color: var(--color-neutral-900, #111827);
      font-weight: 600;
    }

    .section-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      margin: 0 0 1rem 0;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .format-options {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-bottom: 1rem;
    }

    .format-option {
      display: block;
      cursor: pointer;
      border: 2px solid var(--color-neutral-200, #e5e7eb);
      border-radius: 0.5rem;
      transition: all 0.2s;
    }

    .format-option:hover {
      border-color: var(--color-primary, #3b82f6);
      background: var(--color-neutral-50, #f9fafb);
    }

    .format-option.selected {
      border-color: var(--color-primary, #3b82f6);
      background: #eff6ff;
    }

    .format-option input[type="radio"] {
      display: none;
    }

    .format-content {
      padding: 1rem;
    }

    .format-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.5rem;
    }

    .format-icon {
      width: 1.5rem;
      height: 1.5rem;
      color: var(--color-primary, #3b82f6);
    }

    .format-name {
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      font-size: 0.875rem;
    }

    .format-badge {
      margin-left: auto;
      padding: 0.25rem 0.5rem;
      background: var(--color-neutral-100, #f3f4f6);
      color: var(--color-neutral-700, #374151);
      border-radius: 0.25rem;
      font-size: 0.75rem;
      font-weight: 600;
      font-family: monospace;
    }

    .format-option.selected .format-badge {
      background: var(--color-primary, #3b82f6);
      color: white;
    }

    .format-description {
      font-size: 0.875rem;
      color: var(--color-neutral-700, #374151);
      margin: 0 0 0.5rem 0;
      line-height: 1.5;
    }

    .format-use-case {
      font-size: 0.75rem;
      color: var(--color-neutral-600, #4b5563);
      padding: 0.5rem;
      background: var(--color-neutral-50, #f9fafb);
      border-radius: 0.25rem;
    }

    .format-option.selected .format-use-case {
      background: white;
    }

    .warning-box {
      display: flex;
      gap: 0.75rem;
      padding: 1rem;
      background: #fef3c7;
      border: 1px solid #f59e0b;
      border-radius: 0.5rem;
    }

    .icon-warning {
      width: 1.5rem;
      height: 1.5rem;
      color: #f59e0b;
      flex-shrink: 0;
    }

    .warning-title {
      font-weight: 600;
      color: #92400e;
      margin: 0 0 0.25rem 0;
      font-size: 0.875rem;
    }

    .warning-text {
      font-size: 0.75rem;
      color: #78350f;
      margin: 0;
      line-height: 1.5;
    }

    .dialog-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--color-neutral-200, #e5e7eb);
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
  `]
})
export class EDiscoveryExportDialogComponent {
  hold: LegalHold;
  selectedFormat = signal<EDiscoveryFormat | null>(null);
  selectedFormatValue: EDiscoveryFormat | null = null;
  exporting = signal(false);

  formats = [
    {
      value: EDiscoveryFormat.NATIVE,
      name: 'Native Format',
      description: 'Complete data preservation with full metadata and forensic integrity',
      useCase: 'Comprehensive litigation support, forensic analysis, maximum data fidelity',
      icon: 'M5.25 14.25h13.5m-13.5 0a3 3 0 01-3-3m3 3a3 3 0 100 6h13.5a3 3 0 100-6m-16.5-3a3 3 0 013-3h13.5a3 3 0 013 3m-19.5 0a4.5 4.5 0 01.9-2.7L5.737 5.1a3.375 3.375 0 012.7-1.35h7.126c1.062 0 2.062.5 2.7 1.35l2.587 3.45a4.5 4.5 0 01.9 2.7m0 0a3 3 0 01-3 3m0 3h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008zm-3 6h.008v.008h-.008v-.008zm0-6h.008v.008h-.008v-.008z'
    },
    {
      value: EDiscoveryFormat.EMLX,
      name: 'EMLX Format',
      description: 'Email-compatible format for legal review platforms',
      useCase: 'Import into Relativity, Everlaw, Nuix for attorney review',
      icon: 'M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75'
    },
    {
      value: EDiscoveryFormat.PDF,
      name: 'PDF Format',
      description: 'PDF with chain of custody documentation',
      useCase: 'Court submissions, attorney review, visual presentation',
      icon: 'M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z'
    },
    {
      value: EDiscoveryFormat.JSON,
      name: 'JSON Format',
      description: 'Structured data for programmatic access and analysis',
      useCase: 'Data analytics, custom processing, API integration',
      icon: 'M17.25 6.75L22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3l-4.5 16.5'
    },
    {
      value: EDiscoveryFormat.CSV,
      name: 'CSV Format',
      description: 'Comma-separated values for spreadsheet import',
      useCase: 'Excel analysis, database import, reporting',
      icon: 'M3.375 19.5h17.25m-17.25 0a1.125 1.125 0 01-1.125-1.125M3.375 19.5h7.5c.621 0 1.125-.504 1.125-1.125m-9.75 0V5.625m0 12.75v-1.5c0-.621.504-1.125 1.125-1.125m18.375 2.625V5.625m0 12.75c0 .621-.504 1.125-1.125 1.125m1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125m0 3.75h-7.5A1.125 1.125 0 0112 18.375m9.75-12.75c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125m19.5 0v1.5c0 .621-.504 1.125-1.125 1.125M2.25 5.625v1.5c0 .621.504 1.125 1.125 1.125m0 0h17.25m-17.25 0h7.5c.621 0 1.125.504 1.125 1.125M3.375 8.25c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125m17.25-3.75h-7.5c-.621 0-1.125.504-1.125 1.125m8.625-1.125c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125M12 10.875v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 10.875c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125M13.125 12h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125M20.625 12c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5M12 14.625v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 14.625c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125m0 1.5v-1.5m0 0c0-.621.504-1.125 1.125-1.125m0 0h7.5'
    }
  ];

  constructor(
    private legalHoldService: LegalHoldService,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<EDiscoveryExportDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: EDiscoveryExportDialogData
  ) {
    this.hold = data.legalHold;
    // Default to NATIVE format
    this.selectedFormat.set(EDiscoveryFormat.NATIVE);
    this.selectedFormatValue = EDiscoveryFormat.NATIVE;
  }

  onExport(): void {
    const format = this.selectedFormat();
    if (!format || this.exporting()) return;

    this.exporting.set(true);

    this.legalHoldService.exportEDiscovery(this.hold.id, format).subscribe({
      next: (blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;

        // Generate filename
        const timestamp = new Date().toISOString().split('T')[0];
        const extension = this.getFileExtension(format);
        link.download = `ediscovery_${this.hold.caseNumber}_${timestamp}.${extension}`;

        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);

        this.snackBar.open(
          `eDiscovery package exported successfully as ${format}`,
          'Close',
          { duration: 3000 }
        );

        this.dialogRef.close(true);
      },
      error: (err) => {
        console.error('Failed to export eDiscovery package:', err);
        this.snackBar.open(
          err.error?.message || 'Failed to export eDiscovery package',
          'Close',
          { duration: 5000 }
        );
        this.exporting.set(false);
      }
    });
  }

  private getFileExtension(format: EDiscoveryFormat): string {
    switch (format) {
      case EDiscoveryFormat.EMLX:
        return 'emlx';
      case EDiscoveryFormat.PDF:
        return 'pdf';
      case EDiscoveryFormat.JSON:
        return 'json';
      case EDiscoveryFormat.CSV:
        return 'csv';
      case EDiscoveryFormat.NATIVE:
        return 'zip';
      default:
        return 'dat';
    }
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
