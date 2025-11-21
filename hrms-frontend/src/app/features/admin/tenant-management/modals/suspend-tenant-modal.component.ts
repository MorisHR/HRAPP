import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { UiModule } from '../../../../shared/ui/ui.module';
import { Tenant } from '../../../../core/models/tenant.model';

export interface SuspendTenantData {
  tenant: Tenant;
}

/**
 * FORTUNE 500 PATTERN: Suspend tenant with reason tracking
 * Features:
 * - Required reason field (compliance)
 * - Predefined reason templates
 * - Optional notification to tenant
 * - Audit trail ready
 */
@Component({
  selector: 'app-suspend-tenant-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, UiModule],
  template: `
    <div class="suspend-modal">
      <div class="modal-header">
        <div class="header-content">
          <div class="warning-icon">
            <app-icon name="warning" class="icon-warning"></app-icon>
          </div>
          <div>
            <h2>Suspend Tenant</h2>
            <p class="tenant-name">{{ data.tenant.companyName }}</p>
          </div>
        </div>
        <button app-icon-button (click)="close()" class="close-button">
          <app-icon name="close"></app-icon>
        </button>
      </div>

      <form [formGroup]="form" class="modal-body">
        <div class="warning-banner">
          <app-icon name="info"></app-icon>
          <div>
            <strong>This will immediately:</strong>
            <ul>
              <li>Block all user access to the tenant</li>
              <li>Disable API access</li>
              <li>Pause all scheduled jobs</li>
              <li>Send notification email (if enabled)</li>
            </ul>
          </div>
        </div>

        <!-- Quick Reason Templates -->
        <div class="quick-reasons">
          <label>Quick Select:</label>
          <div class="reason-chips">
            @for (template of reasonTemplates; track template) {
              <button
                type="button"
                app-button
                variant="outline"
                size="small"
                (click)="selectReason(template)"
                [class.selected]="form.get('reason')?.value === template"
              >
                {{ template }}
              </button>
            }
          </div>
        </div>

        <!-- Custom Reason -->
        <div class="form-field">
          <label for="reason">Suspension Reason *</label>
          <textarea
            id="reason"
            formControlName="reason"
            placeholder="Explain why this tenant is being suspended..."
            rows="4"
            class="form-textarea"
            [class.error]="form.get('reason')?.invalid && form.get('reason')?.touched"
          ></textarea>
          @if (form.get('reason')?.invalid && form.get('reason')?.touched) {
            <span class="error-message">Reason is required (minimum 10 characters)</span>
          }
          <span class="char-count">{{ form.get('reason')?.value?.length || 0 }} / 500</span>
        </div>

        <!-- Notification Option -->
        <div class="form-checkbox">
          <label>
            <input type="checkbox" formControlName="notifyTenant" />
            <span>Send notification email to tenant admin</span>
          </label>
          <p class="help-text">
            Admin will receive an email explaining the suspension and next steps
          </p>
        </div>

        <!-- Suspension Duration (Optional) -->
        <div class="form-field">
          <label for="duration">Suspension Duration (Optional)</label>
          <select id="duration" formControlName="duration" class="form-select">
            <option value="">Indefinite (until manually reactivated)</option>
            <option value="7">7 days (temporary suspension)</option>
            <option value="14">14 days</option>
            <option value="30">30 days</option>
            <option value="90">90 days</option>
          </select>
          <p class="help-text">
            Leave as "Indefinite" for manual reactivation, or set automatic reactivation date
          </p>
        </div>

        <!-- Tenant Info Summary -->
        <div class="tenant-summary">
          <h4>Affected Tenant:</h4>
          <div class="summary-grid">
            <div class="summary-item">
              <span class="label">Company:</span>
              <span class="value">{{ data.tenant.companyName }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Subdomain:</span>
              <span class="value">{{ data.tenant.subdomain }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Active Users:</span>
              <span class="value">{{ data.tenant.currentUserCount || 0 }}</span>
            </div>
            <div class="summary-item">
              <span class="label">Tier:</span>
              <span class="value">{{ data.tenant.employeeTierDisplay }}</span>
            </div>
          </div>
        </div>
      </form>

      <div class="modal-footer">
        <button app-button variant="text" (click)="close()">
          Cancel
        </button>
        <button
          app-button
          variant="danger"
          (click)="confirm()"
          [disabled]="form.invalid || processing()"
        >
          @if (processing()) {
            <app-icon name="sync" class="spinning"></app-icon>
          }
          Suspend Tenant
        </button>
      </div>
    </div>
  `,
  styles: [`
    .suspend-modal {
      display: flex;
      flex-direction: column;
      max-width: 600px;
      max-height: 90vh;
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

        .warning-icon {
          width: 48px;
          height: 48px;
          border-radius: var(--radius-md);
          background: rgba(234, 179, 8, 0.1);
          display: flex;
          align-items: center;
          justify-content: center;

          app-icon {
            font-size: 28px;
            color: rgb(234, 179, 8);
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

      .close-button {
        flex-shrink: 0;
      }
    }

    .modal-body {
      padding: 24px;
      overflow-y: auto;
      flex: 1;

      .warning-banner {
        display: flex;
        gap: 12px;
        padding: 12px;
        background: rgba(234, 179, 8, 0.1);
        border-left: 3px solid rgb(234, 179, 8);
        border-radius: var(--radius-sm);
        margin-bottom: 24px;

        app-icon {
          font-size: 20px;
          color: rgb(234, 179, 8);
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
          }

          li {
            margin: 4px 0;
          }
        }
      }

      .quick-reasons {
        margin-bottom: 24px;

        label {
          display: block;
          font-size: 13px;
          font-weight: 500;
          margin-bottom: 8px;
          color: var(--color-text-primary);
        }

        .reason-chips {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;

          button {
            &.selected {
              background: var(--color-primary);
              color: white;
              border-color: var(--color-primary);
            }
          }
        }
      }

      .form-field {
        margin-bottom: 20px;

        label {
          display: block;
          font-size: 13px;
          font-weight: 500;
          margin-bottom: 8px;
          color: var(--color-text-primary);
        }

        .form-textarea,
        .form-select {
          width: 100%;
          padding: 10px 12px;
          border: 1px solid var(--color-border);
          border-radius: var(--radius-sm);
          font-size: 14px;
          font-family: inherit;
          transition: border-color var(--transition-fast);

          &:focus {
            outline: none;
            border-color: var(--color-primary);
          }

          &.error {
            border-color: var(--color-error);
          }
        }

        .form-textarea {
          resize: vertical;
          min-height: 100px;
        }

        .error-message {
          display: block;
          margin-top: 4px;
          font-size: 12px;
          color: var(--color-error);
        }

        .char-count {
          display: block;
          margin-top: 4px;
          font-size: 12px;
          color: var(--color-text-tertiary);
          text-align: right;
        }

        .help-text {
          margin: 4px 0 0 0;
          font-size: 12px;
          color: var(--color-text-tertiary);
        }
      }

      .form-checkbox {
        margin-bottom: 20px;

        label {
          display: flex;
          align-items: start;
          gap: 8px;
          cursor: pointer;

          input[type="checkbox"] {
            margin-top: 2px;
            cursor: pointer;
          }

          span {
            font-size: 14px;
            color: var(--color-text-primary);
          }
        }

        .help-text {
          margin: 4px 0 0 28px;
          font-size: 12px;
          color: var(--color-text-tertiary);
        }
      }

      .tenant-summary {
        margin-top: 24px;
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

      .spinning {
        animation: spin 1s linear infinite;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 640px) {
      .modal-body .tenant-summary .summary-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class SuspendTenantModalComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<SuspendTenantModalComponent>);
  data = inject<SuspendTenantData>(MAT_DIALOG_DATA);

  processing = signal(false);

  reasonTemplates = [
    'Non-payment',
    'Terms of Service violation',
    'Suspicious activity',
    'Customer request',
    'Contract dispute',
    'Account under review'
  ];

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
      notifyTenant: [true],
      duration: ['']
    });
  }

  selectReason(reason: string): void {
    this.form.patchValue({ reason });
  }

  confirm(): void {
    if (this.form.invalid) return;

    const formValue = this.form.value;
    this.dialogRef.close({
      confirmed: true,
      reason: formValue.reason,
      notifyTenant: formValue.notifyTenant,
      duration: formValue.duration ? parseInt(formValue.duration) : null
    });
  }

  close(): void {
    this.dialogRef.close({ confirmed: false });
  }
}
