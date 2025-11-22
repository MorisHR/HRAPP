import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { Chip, ChipColor } from '@app/shared/ui';
import { LegalHold, LegalHoldStatus } from '../../../models/legal-hold.model';

export interface LegalHoldDetailsDialogData {
  legalHold: LegalHold;
}

/**
 * Legal Hold Details Dialog
 * Displays comprehensive information about a legal hold
 *
 * Features:
 * - Complete legal hold information display
 * - Case details and timeline
 * - Scope and affected records
 * - Legal representative information
 * - Compliance framework details
 * - Audit trail information
 */
@Component({
  selector: 'app-legal-hold-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    ButtonComponent,
    Chip
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div>
          <h2 class="dialog-title">Legal Hold Details</h2>
          <p class="dialog-subtitle">{{ hold.caseNumber }}</p>
        </div>
        <button class="close-button" (click)="onClose()" type="button">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="dialog-body">
        <!-- Status Banner -->
        <div class="status-banner" [class]="'status-' + hold.status.toLowerCase()">
          <div class="status-info">
            <span class="status-label">Status:</span>
            <app-chip [color]="getStatusColor(hold.status)" [label]="hold.status"></app-chip>
          </div>
          <div class="affected-records">
            <span class="record-count">{{ hold.affectedAuditLogCount || 0 }}</span>
            <span class="record-label">Affected Audit Records</span>
          </div>
        </div>

        <!-- Case Information -->
        <div class="details-section">
          <h3 class="section-title">Case Information</h3>
          <div class="details-grid">
            <div class="detail-item">
              <span class="detail-label">Case Number</span>
              <span class="detail-value">{{ hold.caseNumber }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Requested By</span>
              <span class="detail-value">{{ hold.requestedBy }}</span>
            </div>
            <div class="detail-item full-width">
              <span class="detail-label">Description</span>
              <span class="detail-value">{{ hold.description }}</span>
            </div>
            <div class="detail-item full-width" *ngIf="hold.reason">
              <span class="detail-label">Reason</span>
              <span class="detail-value">{{ hold.reason }}</span>
            </div>
          </div>
        </div>

        <!-- Timeline -->
        <div class="details-section">
          <h3 class="section-title">Timeline</h3>
          <div class="details-grid">
            <div class="detail-item">
              <span class="detail-label">Start Date</span>
              <span class="detail-value">{{ hold.startDate | date:'medium' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">End Date</span>
              <span class="detail-value">{{ hold.endDate ? (hold.endDate | date:'medium') : 'Indefinite' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Created At</span>
              <span class="detail-value">{{ hold.createdAt | date:'medium' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Created By</span>
              <span class="detail-value">{{ hold.createdBy }}</span>
            </div>
            <div class="detail-item" *ngIf="hold.updatedAt">
              <span class="detail-label">Last Updated</span>
              <span class="detail-value">{{ hold.updatedAt | date:'medium' }}</span>
            </div>
            <div class="detail-item" *ngIf="hold.updatedBy">
              <span class="detail-label">Updated By</span>
              <span class="detail-value">{{ hold.updatedBy }}</span>
            </div>
          </div>
        </div>

        <!-- Scope -->
        <div class="details-section">
          <h3 class="section-title">Scope</h3>
          <div class="details-grid">
            <div class="detail-item full-width" *ngIf="hold.userIds">
              <span class="detail-label">User IDs</span>
              <span class="detail-value">{{ hold.userIds }}</span>
            </div>
            <div class="detail-item full-width" *ngIf="hold.entityTypes">
              <span class="detail-label">Entity Types</span>
              <div class="chip-list">
                <app-chip
                  *ngFor="let type of parseJsonArray(hold.entityTypes)"
                  [color]="'neutral'"
                  [label]="type">
                </app-chip>
              </div>
            </div>
            <div class="detail-item full-width" *ngIf="hold.searchKeywords">
              <span class="detail-label">Search Keywords</span>
              <span class="detail-value">{{ hold.searchKeywords }}</span>
            </div>
          </div>
        </div>

        <!-- Legal Information -->
        <div class="details-section">
          <h3 class="section-title">Legal Information</h3>
          <div class="details-grid">
            <div class="detail-item" *ngIf="hold.legalRepresentative">
              <span class="detail-label">Legal Representative</span>
              <span class="detail-value">{{ hold.legalRepresentative }}</span>
            </div>
            <div class="detail-item" *ngIf="hold.legalRepresentativeEmail">
              <span class="detail-label">Attorney Email</span>
              <span class="detail-value">
                <a [href]="'mailto:' + hold.legalRepresentativeEmail">{{ hold.legalRepresentativeEmail }}</a>
              </span>
            </div>
            <div class="detail-item" *ngIf="hold.lawFirm">
              <span class="detail-label">Law Firm</span>
              <span class="detail-value">{{ hold.lawFirm }}</span>
            </div>
            <div class="detail-item" *ngIf="hold.courtOrder">
              <span class="detail-label">Court Order</span>
              <span class="detail-value">{{ hold.courtOrder }}</span>
            </div>
          </div>
        </div>

        <!-- Compliance -->
        <div class="details-section" *ngIf="hold.complianceFrameworks || hold.retentionPeriodDays">
          <h3 class="section-title">Compliance</h3>
          <div class="details-grid">
            <div class="detail-item full-width" *ngIf="hold.complianceFrameworks">
              <span class="detail-label">Compliance Frameworks</span>
              <div class="chip-list">
                <app-chip
                  *ngFor="let framework of parseJsonArray(hold.complianceFrameworks)"
                  [color]="'primary'"
                  [label]="framework">
                </app-chip>
              </div>
            </div>
            <div class="detail-item" *ngIf="hold.retentionPeriodDays">
              <span class="detail-label">Retention Period</span>
              <span class="detail-value">{{ hold.retentionPeriodDays }} days ({{ (hold.retentionPeriodDays / 365) | number:'1.1-1' }} years)</span>
            </div>
          </div>
        </div>

        <!-- Release Information -->
        <div class="details-section" *ngIf="hold.status === 'RELEASED' && hold.releasedBy">
          <h3 class="section-title">Release Information</h3>
          <div class="details-grid">
            <div class="detail-item">
              <span class="detail-label">Released By</span>
              <span class="detail-value">{{ hold.releasedBy }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Released At</span>
              <span class="detail-value">{{ hold.releasedAt | date:'medium' }}</span>
            </div>
            <div class="detail-item full-width" *ngIf="hold.releaseNotes">
              <span class="detail-label">Release Notes</span>
              <span class="detail-value">{{ hold.releaseNotes }}</span>
            </div>
          </div>
        </div>

        <!-- Statistics -->
        <div class="details-section">
          <h3 class="section-title">Statistics</h3>
          <div class="stats-grid">
            <div class="stat-card">
              <div class="stat-value">{{ hold.affectedAuditLogCount || 0 }}</div>
              <div class="stat-label">Audit Logs Under Hold</div>
            </div>
            <div class="stat-card">
              <div class="stat-value">{{ hold.affectedEntityCount || 0 }}</div>
              <div class="stat-label">Affected Entities</div>
            </div>
            <div class="stat-card" *ngIf="hold.startDate && hold.endDate">
              <div class="stat-value">{{ getDurationDays() }}</div>
              <div class="stat-label">Duration (Days)</div>
            </div>
          </div>
        </div>
      </div>

      <div class="dialog-footer">
        <app-button
          type="button"
          variant="primary"
          (click)="onClose()">
          Close
        </app-button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 900px;
      max-width: 95vw;
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
      font-size: 1.5rem;
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

    .dialog-body {
      padding: 1.5rem;
      overflow-y: auto;
      flex: 1;
    }

    .status-banner {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem;
      border-radius: 0.5rem;
      margin-bottom: 1.5rem;
      border: 1px solid;
    }

    .status-banner.status-active {
      background: #fef3c7;
      border-color: #fbbf24;
    }

    .status-banner.status-released {
      background: #d1fae5;
      border-color: #10b981;
    }

    .status-banner.status-expired {
      background: #f3f4f6;
      border-color: #9ca3af;
    }

    .status-banner.status-pending {
      background: #dbeafe;
      border-color: #3b82f6;
    }

    .status-info {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .status-label {
      font-weight: 600;
      font-size: 0.875rem;
    }

    .affected-records {
      text-align: right;
    }

    .record-count {
      display: block;
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--color-neutral-900, #111827);
    }

    .record-label {
      display: block;
      font-size: 0.75rem;
      color: var(--color-neutral-600, #4b5563);
    }

    .details-section {
      margin-bottom: 2rem;
    }

    .section-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      margin: 0 0 1rem 0;
      padding-bottom: 0.5rem;
      border-bottom: 2px solid var(--color-neutral-200, #e5e7eb);
    }

    .details-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.25rem;
    }

    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .detail-item.full-width {
      grid-column: 1 / -1;
    }

    .detail-label {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--color-neutral-600, #4b5563);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .detail-value {
      font-size: 0.875rem;
      color: var(--color-neutral-900, #111827);
      word-break: break-word;
    }

    .detail-value a {
      color: var(--color-primary, #3b82f6);
      text-decoration: none;
    }

    .detail-value a:hover {
      text-decoration: underline;
    }

    .chip-list {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 1rem;
    }

    .stat-card {
      padding: 1rem;
      background: var(--color-neutral-50, #f9fafb);
      border: 1px solid var(--color-neutral-200, #e5e7eb);
      border-radius: 0.5rem;
      text-align: center;
    }

    .stat-value {
      font-size: 2rem;
      font-weight: 700;
      color: var(--color-primary, #3b82f6);
    }

    .stat-label {
      font-size: 0.75rem;
      color: var(--color-neutral-600, #4b5563);
      margin-top: 0.25rem;
    }

    .dialog-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--color-neutral-200, #e5e7eb);
      display: flex;
      justify-content: flex-end;
    }

    @media (max-width: 768px) {
      .details-grid {
        grid-template-columns: 1fr;
      }

      .detail-item.full-width {
        grid-column: 1;
      }

      .status-banner {
        flex-direction: column;
        gap: 1rem;
        align-items: flex-start;
      }

      .affected-records {
        text-align: left;
      }
    }
  `]
})
export class LegalHoldDetailsDialogComponent {
  hold: LegalHold;

  constructor(
    public dialogRef: MatDialogRef<LegalHoldDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LegalHoldDetailsDialogData
  ) {
    this.hold = data.legalHold;
  }

  getStatusColor(status: LegalHoldStatus): ChipColor {
    switch (status) {
      case LegalHoldStatus.ACTIVE:
        return 'warning';
      case LegalHoldStatus.RELEASED:
        return 'success';
      case LegalHoldStatus.EXPIRED:
        return 'neutral';
      case LegalHoldStatus.PENDING:
        return 'primary';
      case LegalHoldStatus.CANCELLED:
        return 'danger';
      default:
        return 'neutral';
    }
  }

  parseJsonArray(jsonString?: string): string[] {
    if (!jsonString) return [];
    try {
      const parsed = JSON.parse(jsonString);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  getDurationDays(): number {
    if (!this.hold.startDate || !this.hold.endDate) return 0;
    const start = new Date(this.hold.startDate);
    const end = new Date(this.hold.endDate);
    const diff = end.getTime() - start.getTime();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
