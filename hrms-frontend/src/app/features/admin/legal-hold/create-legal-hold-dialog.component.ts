import { Component, Inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ButtonComponent } from '../../../shared/ui/components/button/button';
import { LegalHoldService } from '../../../services/legal-hold.service';
import { LegalHold, LegalHoldStatus } from '../../../models/legal-hold.model';

export interface CreateLegalHoldDialogData {
  tenantId?: string;
}

/**
 * Create Legal Hold Dialog
 * Fortune 500-grade compliance form for creating legal holds
 *
 * Features:
 * - Comprehensive legal hold information capture
 * - Multi-user selection support
 * - Entity type filtering
 * - Keyword-based search criteria
 * - Compliance framework specification
 * - Production-ready validation
 */
@Component({
  selector: 'app-create-legal-hold-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    ButtonComponent
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <h2 class="dialog-title">Create New Legal Hold</h2>
        <button class="close-button" (click)="onCancel()" type="button">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="dialog-body">

        <!-- Case Information -->
        <div class="form-section">
          <h3 class="section-title">Case Information</h3>

          <div class="form-group">
            <label for="caseNumber" class="form-label">Case Number *</label>
            <input
              id="caseNumber"
              type="text"
              formControlName="caseNumber"
              class="form-input"
              [class.error]="isFieldInvalid('caseNumber')"
              placeholder="e.g., CASE-2025-001">
            <div *ngIf="isFieldInvalid('caseNumber')" class="error-message">
              Case number is required
            </div>
          </div>

          <div class="form-group">
            <label for="description" class="form-label">Description *</label>
            <textarea
              id="description"
              formControlName="description"
              class="form-input"
              rows="3"
              [class.error]="isFieldInvalid('description')"
              placeholder="Brief description of the legal matter"></textarea>
            <div *ngIf="isFieldInvalid('description')" class="error-message">
              Description is required
            </div>
          </div>

          <div class="form-group">
            <label for="reason" class="form-label">Reason</label>
            <input
              id="reason"
              type="text"
              formControlName="reason"
              class="form-input"
              placeholder="e.g., Pending litigation, Regulatory investigation">
          </div>
        </div>

        <!-- Date Range -->
        <div class="form-section">
          <h3 class="section-title">Time Period</h3>

          <div class="form-row">
            <div class="form-group">
              <label for="startDate" class="form-label">Start Date *</label>
              <input
                id="startDate"
                type="date"
                formControlName="startDate"
                class="form-input"
                [class.error]="isFieldInvalid('startDate')">
              <div *ngIf="isFieldInvalid('startDate')" class="error-message">
                Start date is required
              </div>
            </div>

            <div class="form-group">
              <label for="endDate" class="form-label">End Date</label>
              <input
                id="endDate"
                type="date"
                formControlName="endDate"
                class="form-input"
                placeholder="Leave blank for indefinite">
              <div class="help-text">Leave blank for indefinite hold</div>
            </div>
          </div>
        </div>

        <!-- Scope Definition -->
        <div class="form-section">
          <h3 class="section-title">Scope</h3>
          <p class="section-description">Define what data should be preserved</p>

          <div class="form-group">
            <label for="userIds" class="form-label">User IDs</label>
            <textarea
              id="userIds"
              formControlName="userIds"
              class="form-input"
              rows="2"
              placeholder="Comma-separated user IDs (e.g., user1@example.com, user2@example.com)"></textarea>
            <div class="help-text">Leave blank to include all users</div>
          </div>

          <div class="form-group">
            <label for="entityTypes" class="form-label">Entity Types</label>
            <div class="checkbox-group">
              <label *ngFor="let type of entityTypeOptions" class="checkbox-label">
                <input
                  type="checkbox"
                  [value]="type"
                  (change)="onEntityTypeChange(type, $event)"
                  [checked]="isEntityTypeSelected(type)">
                <span>{{ type }}</span>
              </label>
            </div>
          </div>

          <div class="form-group">
            <label for="searchKeywords" class="form-label">Search Keywords</label>
            <textarea
              id="searchKeywords"
              formControlName="searchKeywords"
              class="form-input"
              rows="2"
              placeholder="Comma-separated keywords to identify relevant records"></textarea>
            <div class="help-text">Records matching these keywords will be preserved</div>
          </div>
        </div>

        <!-- Legal Representatives -->
        <div class="form-section">
          <h3 class="section-title">Legal Information</h3>

          <div class="form-group">
            <label for="requestedBy" class="form-label">Requested By *</label>
            <input
              id="requestedBy"
              type="text"
              formControlName="requestedBy"
              class="form-input"
              [class.error]="isFieldInvalid('requestedBy')"
              placeholder="Name of person requesting legal hold">
            <div *ngIf="isFieldInvalid('requestedBy')" class="error-message">
              Required
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="legalRepresentative" class="form-label">Legal Representative</label>
              <input
                id="legalRepresentative"
                type="text"
                formControlName="legalRepresentative"
                class="form-input"
                placeholder="Attorney name">
            </div>

            <div class="form-group">
              <label for="legalRepresentativeEmail" class="form-label">Attorney Email</label>
              <input
                id="legalRepresentativeEmail"
                type="email"
                formControlName="legalRepresentativeEmail"
                class="form-input"
                placeholder="attorney@lawfirm.com">
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="lawFirm" class="form-label">Law Firm</label>
              <input
                id="lawFirm"
                type="text"
                formControlName="lawFirm"
                class="form-input"
                placeholder="Law firm name">
            </div>

            <div class="form-group">
              <label for="courtOrder" class="form-label">Court Order Number</label>
              <input
                id="courtOrder"
                type="text"
                formControlName="courtOrder"
                class="form-input"
                placeholder="Optional court order reference">
            </div>
          </div>
        </div>

        <!-- Compliance Settings -->
        <div class="form-section">
          <h3 class="section-title">Compliance</h3>

          <div class="form-group">
            <label for="complianceFrameworks" class="form-label">Compliance Frameworks</label>
            <div class="checkbox-group">
              <label *ngFor="let framework of complianceFrameworkOptions" class="checkbox-label">
                <input
                  type="checkbox"
                  [value]="framework"
                  (change)="onComplianceFrameworkChange(framework, $event)"
                  [checked]="isComplianceFrameworkSelected(framework)">
                <span>{{ framework }}</span>
              </label>
            </div>
          </div>

          <div class="form-group">
            <label for="retentionPeriodDays" class="form-label">Retention Period (Days)</label>
            <input
              id="retentionPeriodDays"
              type="number"
              formControlName="retentionPeriodDays"
              class="form-input"
              min="1"
              placeholder="e.g., 2555 (7 years)">
            <div class="help-text">How long data must be retained (e.g., 2555 days = 7 years)</div>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="form-actions">
          <app-button
            type="button"
            variant="secondary"
            (click)="onCancel()">
            Cancel
          </app-button>
          <app-button
            type="submit"
            variant="primary"
            [loading]="submitting()"
            [disabled]="!form.valid || submitting()">
            Create Legal Hold
          </app-button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .dialog-container {
      width: 800px;
      max-width: 90vw;
      max-height: 90vh;
      display: flex;
      flex-direction: column;
    }

    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid var(--color-neutral-200, #e5e7eb);
    }

    .dialog-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      margin: 0;
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

    .form-section {
      margin-bottom: 2rem;
    }

    .section-title {
      font-size: 1rem;
      font-weight: 600;
      color: var(--color-neutral-900, #111827);
      margin: 0 0 0.5rem 0;
    }

    .section-description {
      font-size: 0.875rem;
      color: var(--color-neutral-600, #4b5563);
      margin: 0 0 1rem 0;
    }

    .form-group {
      margin-bottom: 1.25rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .form-label {
      display: block;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-neutral-700, #374151);
      margin-bottom: 0.5rem;
    }

    .form-input {
      width: 100%;
      padding: 0.625rem 0.875rem;
      font-size: 0.875rem;
      border: 1px solid var(--color-neutral-300, #d1d5db);
      border-radius: 0.375rem;
      transition: all 0.2s;
      font-family: inherit;
    }

    .form-input:focus {
      outline: none;
      border-color: var(--color-primary, #3b82f6);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .form-input.error {
      border-color: var(--color-error, #ef4444);
    }

    textarea.form-input {
      resize: vertical;
    }

    .checkbox-group {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.75rem;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      padding: 0.5rem;
      border: 1px solid var(--color-neutral-200, #e5e7eb);
      border-radius: 0.375rem;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 0.875rem;
      color: var(--color-neutral-700, #374151);
    }

    .checkbox-label:hover {
      background: var(--color-neutral-50, #f9fafb);
    }

    .checkbox-label input {
      margin-right: 0.5rem;
    }

    .error-message {
      margin-top: 0.375rem;
      font-size: 0.75rem;
      color: var(--color-error, #ef4444);
    }

    .help-text {
      margin-top: 0.375rem;
      font-size: 0.75rem;
      color: var(--color-neutral-500, #6b7280);
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-neutral-200, #e5e7eb);
      margin-top: 2rem;
    }

    @media (max-width: 640px) {
      .form-row, .checkbox-group {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class CreateLegalHoldDialogComponent implements OnInit {
  form!: FormGroup;
  submitting = signal(false);
  selectedEntityTypes = signal<string[]>([]);
  selectedComplianceFrameworks = signal<string[]>([]);

  entityTypeOptions = [
    'Employee',
    'Payroll',
    'TimeTracking',
    'Leave',
    'Performance',
    'Recruitment',
    'Document',
    'Communication'
  ];

  complianceFrameworkOptions = [
    'SOC 2',
    'ISO 27001',
    'GDPR',
    'HIPAA',
    'SOX',
    'PCI DSS',
    'NIST CSF',
    'FedRAMP'
  ];

  constructor(
    private fb: FormBuilder,
    private legalHoldService: LegalHoldService,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<CreateLegalHoldDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CreateLegalHoldDialogData
  ) {}

  ngOnInit(): void {
    this.buildForm();
  }

  private buildForm(): void {
    const today = new Date().toISOString().split('T')[0];

    this.form = this.fb.group({
      caseNumber: ['', Validators.required],
      description: ['', Validators.required],
      reason: [''],
      startDate: [today, Validators.required],
      endDate: [''],
      userIds: [''],
      searchKeywords: [''],
      requestedBy: ['', Validators.required],
      legalRepresentative: [''],
      legalRepresentativeEmail: ['', Validators.email],
      lawFirm: [''],
      courtOrder: [''],
      retentionPeriodDays: ['']
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  onEntityTypeChange(type: string, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.selectedEntityTypes.update(types => [...types, type]);
    } else {
      this.selectedEntityTypes.update(types => types.filter(t => t !== type));
    }
  }

  isEntityTypeSelected(type: string): boolean {
    return this.selectedEntityTypes().includes(type);
  }

  onComplianceFrameworkChange(framework: string, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.selectedComplianceFrameworks.update(frameworks => [...frameworks, framework]);
    } else {
      this.selectedComplianceFrameworks.update(frameworks => frameworks.filter(f => f !== framework));
    }
  }

  isComplianceFrameworkSelected(framework: string): boolean {
    return this.selectedComplianceFrameworks().includes(framework);
  }

  onSubmit(): void {
    if (this.form.invalid || this.submitting()) return;

    this.submitting.set(true);

    const legalHold: Partial<LegalHold> = {
      tenantId: this.data.tenantId,
      caseNumber: this.form.value.caseNumber,
      description: this.form.value.description,
      reason: this.form.value.reason || undefined,
      startDate: new Date(this.form.value.startDate),
      endDate: this.form.value.endDate ? new Date(this.form.value.endDate) : undefined,
      status: LegalHoldStatus.ACTIVE,
      userIds: this.form.value.userIds || undefined,
      entityTypes: this.selectedEntityTypes().length > 0
        ? JSON.stringify(this.selectedEntityTypes())
        : undefined,
      searchKeywords: this.form.value.searchKeywords || undefined,
      requestedBy: this.form.value.requestedBy,
      legalRepresentative: this.form.value.legalRepresentative || undefined,
      legalRepresentativeEmail: this.form.value.legalRepresentativeEmail || undefined,
      lawFirm: this.form.value.lawFirm || undefined,
      courtOrder: this.form.value.courtOrder || undefined,
      complianceFrameworks: this.selectedComplianceFrameworks().length > 0
        ? JSON.stringify(this.selectedComplianceFrameworks())
        : undefined,
      retentionPeriodDays: this.form.value.retentionPeriodDays || undefined,
      affectedAuditLogCount: 0,
      affectedEntityCount: 0,
      createdAt: new Date(),
      createdBy: 'current-user' // This will be set by backend
    };

    this.legalHoldService.createLegalHold(legalHold as LegalHold).subscribe({
      next: () => {
        this.snackBar.open('Legal hold created successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (err) => {
        console.error('Failed to create legal hold:', err);
        this.snackBar.open(err.error?.message || 'Failed to create legal hold', 'Close', { duration: 5000 });
        this.submitting.set(false);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
