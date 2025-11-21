import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { UiModule } from '../../../../shared/ui/ui.module';
import { Tenant } from '../../../../core/models/tenant.model';

export interface HardDeleteTenantData {
  tenant: Tenant;
}

/**
 * FORTUNE 500 PATTERN: Hard delete with multiple safety checks
 * Features:
 * - Must type exact company name
 * - Must type "PERMANENTLY DELETE"
 * - Must check acknowledgment box
 * - Shows impact summary (users, data size)
 * - Cannot be undone warning
 */
@Component({
  selector: 'app-hard-delete-tenant-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, UiModule],
  template: `
    <div class="hard-delete-modal">
      <div class="modal-header danger">
        <div class="header-content">
          <div class="danger-icon">
            <app-icon name="error" class="icon-error"></app-icon>
          </div>
          <div>
            <h2>⚠️ PERMANENT DELETION</h2>
            <p class="warning-text">THIS CANNOT BE UNDONE</p>
          </div>
        </div>
        <button app-icon-button (click)="close()" class="close-button">
          <app-icon name="close"></app-icon>
        </button>
      </div>

      <form [formGroup]="form" class="modal-body">
        <!-- Critical Warning Banner -->
        <div class="critical-warning">
          <app-icon name="warning" class="pulse"></app-icon>
          <div>
            <strong>IRREVERSIBLE ACTION</strong>
            <p>Once deleted, this tenant's data CANNOT be recovered. This includes:</p>
            <ul>
              <li><strong>{{ data.tenant.currentUserCount || 0 }} user accounts</strong> - All passwords, permissions, and profiles</li>
              <li><strong>{{ formatStorage(data.tenant.currentStorageGB) }}</strong> - Documents, files, and attachments</li>
              <li><strong>All historical records</strong> - Timesheets, attendance, payroll data</li>
              <li><strong>Audit trails</strong> - Complete activity history</li>
              <li><strong>Billing history</strong> - Invoices and payment records</li>
            </ul>
          </div>
        </div>

        <!-- Impact Summary -->
        <div class="impact-summary">
          <h4>Deletion Impact:</h4>
          <div class="impact-grid">
            <div class="impact-item critical">
              <app-icon name="group"></app-icon>
              <div>
                <span class="impact-value">{{ data.tenant.currentUserCount || 0 }}</span>
                <span class="impact-label">Users will lose access</span>
              </div>
            </div>
            <div class="impact-item critical">
              <app-icon name="storage"></app-icon>
              <div>
                <span class="impact-value">{{ formatStorage(data.tenant.currentStorageGB) }}</span>
                <span class="impact-label">Data will be erased</span>
              </div>
            </div>
            <div class="impact-item critical">
              <app-icon name="history"></app-icon>
              <div>
                <span class="impact-value">{{ daysSinceCreation() }}</span>
                <span class="impact-label">Days of history lost</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Confirmation Step 1: Type Company Name -->
        <div class="confirmation-step">
          <label for="companyName">
            <app-icon name="looks_one" class="step-icon"></app-icon>
            Type the company name to confirm:
            <code>{{ data.tenant.companyName }}</code>
          </label>
          <input
            id="companyName"
            type="text"
            formControlName="confirmationName"
            placeholder="Type company name exactly as shown above"
            class="confirmation-input"
            [class.valid]="companyNameMatches()"
            [class.invalid]="form.get('confirmationName')?.touched && !companyNameMatches()"
            autocomplete="off"
          />
          @if (companyNameMatches()) {
            <div class="validation-check">
              <app-icon name="check_circle" class="success"></app-icon>
              <span>Company name confirmed</span>
            </div>
          }
        </div>

        <!-- Confirmation Step 2: Type DELETE -->
        <div class="confirmation-step">
          <label for="deleteText">
            <app-icon name="looks_two" class="step-icon"></app-icon>
            Type <code>PERMANENTLY DELETE</code> to proceed:
          </label>
          <input
            id="deleteText"
            type="text"
            formControlName="confirmationText"
            placeholder="Type: PERMANENTLY DELETE"
            class="confirmation-input"
            [class.valid]="deleteTextMatches()"
            [class.invalid]="form.get('confirmationText')?.touched && !deleteTextMatches()"
            autocomplete="off"
          />
          @if (deleteTextMatches()) {
            <div class="validation-check">
              <app-icon name="check_circle" class="success"></app-icon>
              <span>Confirmation text verified</span>
            </div>
          }
        </div>

        <!-- Confirmation Step 3: Acknowledge -->
        <div class="confirmation-step">
          <label class="checkbox-label">
            <input
              type="checkbox"
              formControlName="acknowledgeIrreversible"
            />
            <span>
              <app-icon name="looks_3" class="step-icon"></app-icon>
              I understand this action is <strong>IRREVERSIBLE</strong> and all data will be <strong>PERMANENTLY DELETED</strong>
            </span>
          </label>
        </div>

        <!-- Final Warning -->
        <div class="final-warning">
          <app-icon name="error"></app-icon>
          <div>
            <strong>Last Chance:</strong> Once you click "Permanently Delete", there is NO way to recover this tenant or its data. Customer support CANNOT help you.
          </div>
        </div>

        <!-- Tenant Info (for reference) -->
        <div class="tenant-info">
          <div class="info-row">
            <span class="label">Company:</span>
            <span class="value">{{ data.tenant.companyName }}</span>
          </div>
          <div class="info-row">
            <span class="label">Subdomain:</span>
            <span class="value">{{ data.tenant.subdomain }}</span>
          </div>
          <div class="info-row">
            <span class="label">Created:</span>
            <span class="value">{{ formatDate(data.tenant.createdAt) }}</span>
          </div>
          @if (data.tenant.softDeleteDate) {
            <div class="info-row">
              <span class="label">Archived:</span>
              <span class="value">{{ formatDate(data.tenant.softDeleteDate) }}</span>
            </div>
          }
        </div>
      </form>

      <div class="modal-footer">
        <button app-button variant="text" (click)="close()">
          Cancel (Recommended)
        </button>
        <button
          app-button
          variant="danger"
          (click)="confirm()"
          [disabled]="!canDelete() || processing()"
          class="delete-button"
        >
          @if (processing()) {
            <app-icon name="sync" class="spinning"></app-icon>
          } @else {
            <app-icon name="delete_forever"></app-icon>
          }
          Permanently Delete
        </button>
      </div>
    </div>
  `,
  styles: [`
    .hard-delete-modal {
      display: flex;
      flex-direction: column;
      max-width: 700px;
      max-height: 90vh;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: start;
      padding: 24px;
      border-bottom: 1px solid var(--color-border);
      background: rgba(239, 68, 68, 0.05);

      &.danger {
        border-bottom-color: rgb(239, 68, 68);
      }

      .header-content {
        display: flex;
        gap: 16px;
        align-items: start;

        .danger-icon {
          width: 52px;
          height: 52px;
          border-radius: var(--radius-md);
          background: rgba(239, 68, 68, 0.15);
          display: flex;
          align-items: center;
          justify-content: center;

          app-icon {
            font-size: 32px;
            color: rgb(239, 68, 68);
          }
        }

        h2 {
          margin: 0;
          font-size: 22px;
          font-weight: 700;
          color: rgb(239, 68, 68);
        }

        .warning-text {
          margin: 4px 0 0 0;
          font-size: 13px;
          font-weight: 600;
          color: rgb(239, 68, 68);
          letter-spacing: 0.5px;
        }
      }
    }

    .modal-body {
      padding: 24px;
      overflow-y: auto;
      flex: 1;

      .critical-warning {
        display: flex;
        gap: 12px;
        padding: 16px;
        background: rgba(239, 68, 68, 0.1);
        border: 2px solid rgb(239, 68, 68);
        border-radius: var(--radius-sm);
        margin-bottom: 24px;

        app-icon {
          font-size: 24px;
          color: rgb(239, 68, 68);
          flex-shrink: 0;
          margin-top: 2px;

          &.pulse {
            animation: pulse 2s ease-in-out infinite;
          }
        }

        div {
          font-size: 13px;
          color: var(--color-text-primary);

          strong {
            display: block;
            margin-bottom: 8px;
            font-size: 14px;
            color: rgb(239, 68, 68);
          }

          p {
            margin: 0 0 8px 0;
          }

          ul {
            margin: 0;
            padding-left: 20px;

            li {
              margin: 6px 0;

              strong {
                display: inline;
                margin: 0;
                color: var(--color-text-primary);
              }
            }
          }
        }
      }

      .impact-summary {
        margin-bottom: 24px;
        padding: 16px;
        background: var(--color-surface-elevated);
        border-radius: var(--radius-sm);

        h4 {
          margin: 0 0 12px 0;
          font-size: 13px;
          font-weight: 600;
          color: var(--color-text-secondary);
          text-transform: uppercase;
          letter-spacing: 0.5px;
        }

        .impact-grid {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: 12px;

          .impact-item {
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 12px;
            background: white;
            border: 1px solid var(--color-border);
            border-radius: var(--radius-sm);

            &.critical {
              border-color: rgba(239, 68, 68, 0.3);
              background: rgba(239, 68, 68, 0.03);
            }

            app-icon {
              font-size: 24px;
              color: rgb(239, 68, 68);
            }

            div {
              display: flex;
              flex-direction: column;

              .impact-value {
                font-size: 18px;
                font-weight: 700;
                color: var(--color-text-primary);
              }

              .impact-label {
                font-size: 11px;
                color: var(--color-text-tertiary);
              }
            }
          }
        }
      }

      .confirmation-step {
        margin-bottom: 20px;
        padding: 16px;
        background: var(--color-surface);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-sm);

        label {
          display: block;
          font-size: 14px;
          font-weight: 500;
          margin-bottom: 8px;
          color: var(--color-text-primary);
          display: flex;
          align-items: center;
          gap: 8px;

          .step-icon {
            font-size: 20px;
            color: var(--color-primary);
          }

          code {
            font-family: 'Courier New', monospace;
            background: rgba(0, 0, 0, 0.05);
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 13px;
          }
        }

        .confirmation-input {
          width: 100%;
          padding: 10px 12px;
          border: 2px solid var(--color-border);
          border-radius: var(--radius-sm);
          font-size: 14px;
          font-family: inherit;
          transition: all var(--transition-fast);

          &:focus {
            outline: none;
            border-color: var(--color-primary);
          }

          &.valid {
            border-color: rgb(34, 197, 94);
            background: rgba(34, 197, 94, 0.05);
          }

          &.invalid {
            border-color: rgb(239, 68, 68);
            background: rgba(239, 68, 68, 0.05);
          }
        }

        .validation-check {
          display: flex;
          align-items: center;
          gap: 6px;
          margin-top: 8px;
          font-size: 13px;
          color: rgb(34, 197, 94);

          app-icon {
            font-size: 18px;

            &.success {
              color: rgb(34, 197, 94);
            }
          }
        }

        .checkbox-label {
          display: flex;
          align-items: start;
          gap: 8px;
          cursor: pointer;
          margin-bottom: 0;

          input[type="checkbox"] {
            margin-top: 2px;
            cursor: pointer;
            width: 18px;
            height: 18px;
          }

          span {
            font-size: 14px;
            color: var(--color-text-primary);

            strong {
              color: rgb(239, 68, 68);
            }
          }
        }
      }

      .final-warning {
        display: flex;
        gap: 12px;
        padding: 12px;
        background: rgba(234, 179, 8, 0.1);
        border-left: 3px solid rgb(234, 179, 8);
        border-radius: var(--radius-sm);
        margin-bottom: 20px;

        app-icon {
          font-size: 20px;
          color: rgb(234, 179, 8);
          flex-shrink: 0;
          margin-top: 2px;
        }

        div {
          font-size: 13px;
          color: var(--color-text-primary);

          strong {
            font-weight: 600;
          }
        }
      }

      .tenant-info {
        padding: 12px;
        background: var(--color-surface-elevated);
        border-radius: var(--radius-sm);

        .info-row {
          display: flex;
          justify-content: space-between;
          padding: 8px 0;
          border-bottom: 1px solid var(--color-border);

          &:last-child {
            border-bottom: none;
          }

          .label {
            font-size: 13px;
            color: var(--color-text-tertiary);
          }

          .value {
            font-size: 13px;
            font-weight: 500;
            color: var(--color-text-primary);
          }
        }
      }
    }

    .modal-footer {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding: 16px 24px;
      border-top: 1px solid var(--color-border);
      background: rgba(239, 68, 68, 0.02);

      .delete-button {
        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }

      .spinning {
        animation: spin 1s linear infinite;
      }
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .modal-body .impact-summary .impact-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class HardDeleteTenantModalComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<HardDeleteTenantModalComponent>);
  data = inject<HardDeleteTenantData>(MAT_DIALOG_DATA);

  processing = signal(false);

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      confirmationName: ['', Validators.required],
      confirmationText: ['', Validators.required],
      acknowledgeIrreversible: [false, Validators.requiredTrue]
    });
  }

  companyNameMatches = computed(() => {
    const input = this.form.get('confirmationName')?.value?.trim() || '';
    return input.toLowerCase() === this.data.tenant.companyName.toLowerCase();
  });

  deleteTextMatches = computed(() => {
    const input = this.form.get('confirmationText')?.value?.trim() || '';
    return input.toUpperCase() === 'PERMANENTLY DELETE';
  });

  canDelete = computed(() => {
    return this.companyNameMatches() &&
           this.deleteTextMatches() &&
           this.form.get('acknowledgeIrreversible')?.value === true;
  });

  daysSinceCreation(): number {
    const created = new Date(this.data.tenant.createdAt);
    const now = new Date();
    const diff = now.getTime() - created.getTime();
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  }

  formatStorage(gb?: number): string {
    if (!gb) return '0 GB';
    if (gb < 1) return `${(gb * 1024).toFixed(0)} MB`;
    return `${gb.toFixed(1)} GB`;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  confirm(): void {
    if (!this.canDelete()) return;

    this.dialogRef.close({
      confirmed: true,
      confirmationName: this.form.get('confirmationName')?.value
    });
  }

  close(): void {
    this.dialogRef.close({ confirmed: false });
  }
}
