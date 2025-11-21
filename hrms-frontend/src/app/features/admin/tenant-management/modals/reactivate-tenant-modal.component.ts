import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { UiModule } from '../../../../shared/ui/ui.module';
import { Tenant } from '../../../../core/models/tenant.model';

export interface ReactivateTenantData {
  tenant: Tenant;
}

@Component({
  selector: 'app-reactivate-tenant-modal',
  standalone: true,
  imports: [CommonModule, MatDialogModule, UiModule],
  template: `
    <div class="reactivate-modal">
      <div class="modal-header">
        <div class="header-content">
          <div class="success-icon">
            <app-icon name="check_circle" class="icon-success"></app-icon>
          </div>
          <div>
            <h2>Reactivate Tenant</h2>
            <p class="tenant-name">{{ data.tenant.companyName }}</p>
          </div>
        </div>
        <button app-icon-button (click)="close()" class="close-button">
          <app-icon name="close"></app-icon>
        </button>
      </div>

      <div class="modal-body">
        <div class="info-banner">
          <app-icon name="info"></app-icon>
          <div>
            <strong>This will immediately:</strong>
            <ul>
              <li>Restore full access for all users</li>
              <li>Re-enable API access</li>
              <li>Resume all scheduled jobs</li>
              <li>Send reactivation notification email</li>
            </ul>
          </div>
        </div>

        @if (data.tenant.suspensionReason) {
          <div class="suspension-info">
            <h4>Previous Suspension:</h4>
            <div class="reason-box">
              <strong>Reason:</strong>
              <p>{{ data.tenant.suspensionReason }}</p>
            </div>
            @if (data.tenant.suspensionDate) {
              <p class="suspension-date">
                Suspended on: {{ formatDate(data.tenant.suspensionDate) }}
              </p>
            }
          </div>
        }

        <div class="tenant-summary">
          <h4>Tenant Details:</h4>
          <div class="summary-grid">
            <div class="summary-item">
              <span class="label">Subdomain:</span>
              <span class="value">{{ data.tenant.subdomain }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Users:</span>
              <span class="value">{{ data.tenant.currentUserCount || 0 }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Tier:</span>
              <span class="value">{{ data.tenant.employeeTierDisplay }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Status:</span>
              <span class="value status-suspended">{{ data.tenant.statusDisplay }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-footer">
        <button app-button variant="text" (click)="close()">
          Cancel
        </button>
        <button app-button variant="primary" (click)="confirm()">
          <app-icon name="check_circle"></app-icon>
          Reactivate Tenant
        </button>
      </div>
    </div>
  `,
  styles: [`
    .reactivate-modal {
      display: flex;
      flex-direction: column;
      max-width: 550px;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: start;
      padding: 24px;
      border-bottom: 1px solid var(--color-border);

      .header-content {
        display: flex;
        gap: 16px;
        align-items: start;

        .success-icon {
          width: 48px;
          height: 48px;
          border-radius: var(--radius-md);
          background: rgba(34, 197, 94, 0.1);
          display: flex;
          align-items: center;
          justify-content: center;

          app-icon {
            font-size: 28px;
            color: rgb(34, 197, 94);
          }
        }

        h2 {
          margin: 0;
          font-size: 20px;
          font-weight: 600;
          color: var(--color-text-primary);
        }

        .tenant-name {
          margin: 4px 0 0 0;
          font-size: 14px;
          color: var(--color-text-secondary);
        }
      }
    }

    .modal-body {
      padding: 24px;

      .info-banner {
        display: flex;
        gap: 12px;
        padding: 12px;
        background: rgba(59, 130, 246, 0.1);
        border-left: 3px solid rgb(59, 130, 246);
        border-radius: var(--radius-sm);
        margin-bottom: 20px;

        app-icon {
          font-size: 20px;
          color: rgb(59, 130, 246);
          flex-shrink: 0;
          margin-top: 2px;
        }

        div {
          font-size: 13px;
          color: var(--color-text-secondary);

          strong {
            display: block;
            margin-bottom: 8px;
            color: var(--color-text-primary);
          }

          ul {
            margin: 0;
            padding-left: 20px;

            li {
              margin: 4px 0;
            }
          }
        }
      }

      .suspension-info {
        margin-bottom: 20px;
        padding: 16px;
        background: rgba(234, 179, 8, 0.05);
        border: 1px solid rgba(234, 179, 8, 0.3);
        border-radius: var(--radius-sm);

        h4 {
          margin: 0 0 12px 0;
          font-size: 13px;
          font-weight: 600;
          color: var(--color-text-secondary);
          text-transform: uppercase;
        }

        .reason-box {
          margin-bottom: 8px;

          strong {
            display: block;
            font-size: 12px;
            color: var(--color-text-tertiary);
            margin-bottom: 4px;
          }

          p {
            margin: 0;
            font-size: 14px;
            color: var(--color-text-primary);
            font-style: italic;
          }
        }

        .suspension-date {
          margin: 0;
          font-size: 12px;
          color: var(--color-text-tertiary);
        }
      }

      .tenant-summary {
        padding: 16px;
        background: var(--color-surface-elevated);
        border-radius: var(--radius-sm);

        h4 {
          margin: 0 0 12px 0;
          font-size: 13px;
          font-weight: 600;
          color: var(--color-text-secondary);
          text-transform: uppercase;
        }

        .summary-grid {
          display: grid;
          grid-template-columns: repeat(2, 1fr);
          gap: 12px;

          .summary-item {
            display: flex;
            flex-direction: column;
            gap: 4px;

            .label {
              font-size: 12px;
              color: var(--color-text-tertiary);
            }

            .value {
              font-size: 14px;
              font-weight: 500;
              color: var(--color-text-primary);

              &.status-suspended {
                color: rgb(234, 179, 8);
              }
            }
          }
        }
      }
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      padding: 16px 24px;
      border-top: 1px solid var(--color-border);
    }
  `]
})
export class ReactivateTenantModalComponent {
  private dialogRef = inject(MatDialogRef<ReactivateTenantModalComponent>);
  data = inject<ReactivateTenantData>(MAT_DIALOG_DATA);

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  confirm(): void {
    this.dialogRef.close({ confirmed: true });
  }

  close(): void {
    this.dialogRef.close({ confirmed: false });
  }
}
