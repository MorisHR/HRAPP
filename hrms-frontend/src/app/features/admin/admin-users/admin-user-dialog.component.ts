import { Component, Inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

// Custom UI Components
import { CardComponent } from '../../../shared/ui/components/card/card';
import { ButtonComponent } from '../../../shared/ui/components/button/button';

// Services
import { AdminUserService, AdminUser, CreateAdminUserRequest, UpdateAdminUserRequest } from '../../../core/services/admin-user.service';

export interface AdminUserDialogData {
  mode: 'create' | 'edit';
  user?: AdminUser;
}

/**
 * Admin User Create/Edit Dialog
 * Fortune 500-grade form with validation, password strength indicator, and permission management
 *
 * Features:
 * - Create new SuperAdmin users
 * - Edit existing SuperAdmin users
 * - Password strength validation (12+ chars, complexity requirements)
 * - Permission multi-select
 * - Session timeout configuration
 * - Production-ready error handling
 */
@Component({
  selector: 'app-admin-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    CardComponent,
    ButtonComponent
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <h2 class="dialog-title">{{ dialogTitle() }}</h2>
        <button class="close-button" (click)="onCancel()" type="button">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="dialog-body">

        <!-- Basic Information -->
        <div class="form-section">
          <h3 class="section-title">Basic Information</h3>

          <div class="form-group">
            <label for="userName" class="form-label">Username *</label>
            <input
              id="userName"
              type="text"
              formControlName="userName"
              class="form-input"
              [class.error]="isFieldInvalid('userName')"
              placeholder="Enter username">
            <div *ngIf="isFieldInvalid('userName')" class="error-message">
              <span *ngIf="form.get('userName')?.errors?.['required']">Username is required</span>
              <span *ngIf="form.get('userName')?.errors?.['minlength']">Username must be at least 3 characters</span>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="firstName" class="form-label">First Name *</label>
              <input
                id="firstName"
                type="text"
                formControlName="firstName"
                class="form-input"
                [class.error]="isFieldInvalid('firstName')"
                placeholder="Enter first name">
              <div *ngIf="isFieldInvalid('firstName')" class="error-message">
                First name is required
              </div>
            </div>

            <div class="form-group">
              <label for="lastName" class="form-label">Last Name *</label>
              <input
                id="lastName"
                type="text"
                formControlName="lastName"
                class="form-input"
                [class.error]="isFieldInvalid('lastName')"
                placeholder="Enter last name">
              <div *ngIf="isFieldInvalid('lastName')" class="error-message">
                Last name is required
              </div>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="email" class="form-label">Email *</label>
              <input
                id="email"
                type="email"
                formControlName="email"
                class="form-input"
                [class.error]="isFieldInvalid('email')"
                placeholder="admin&#64;example.com">
              <div *ngIf="isFieldInvalid('email')" class="error-message">
                <span *ngIf="form.get('email')?.errors?.['required']">Email is required</span>
                <span *ngIf="form.get('email')?.errors?.['email']">Invalid email format</span>
              </div>
            </div>

            <div class="form-group">
              <label for="phoneNumber" class="form-label">Phone Number</label>
              <input
                id="phoneNumber"
                type="tel"
                formControlName="phoneNumber"
                class="form-input"
                placeholder="+1 234 567 8900">
            </div>
          </div>
        </div>

        <!-- Password (Create Mode Only) -->
        <div class="form-section" *ngIf="data.mode === 'create'">
          <h3 class="section-title">Security</h3>

          <div class="form-group">
            <label for="password" class="form-label">Password *</label>
            <div class="password-input-wrapper">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                class="form-input"
                [class.error]="isFieldInvalid('password')"
                placeholder="Enter strong password (min 12 chars)">
              <button
                type="button"
                class="toggle-password"
                (click)="togglePassword()">
                <svg *ngIf="!showPassword()" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon-sm">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <svg *ngIf="showPassword()" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon-sm">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                </svg>
              </button>
            </div>

            <!-- Password Strength Indicator -->
            <div class="password-strength" *ngIf="form.get('password')?.value">
              <div class="strength-bar">
                <div
                  class="strength-fill"
                  [class.weak]="passwordStrength() === 'weak'"
                  [class.medium]="passwordStrength() === 'medium'"
                  [class.strong]="passwordStrength() === 'strong'"
                  [style.width.%]="passwordStrengthPercent()">
                </div>
              </div>
              <span class="strength-label" [class]="passwordStrength()">
                {{ passwordStrength() | titlecase }} Password
              </span>
            </div>

            <div *ngIf="isFieldInvalid('password')" class="error-message">
              <span *ngIf="form.get('password')?.errors?.['required']">Password is required</span>
              <span *ngIf="form.get('password')?.errors?.['minlength']">Password must be at least 12 characters</span>
              <span *ngIf="form.get('password')?.errors?.['passwordStrength']">Password must contain uppercase, lowercase, number, and special character</span>
            </div>

            <div class="password-requirements">
              <p class="requirements-title">Password Requirements:</p>
              <ul class="requirements-list">
                <li [class.met]="passwordMeetsLength()">At least 12 characters</li>
                <li [class.met]="passwordHasUppercase()">One uppercase letter</li>
                <li [class.met]="passwordHasLowercase()">One lowercase letter</li>
                <li [class.met]="passwordHasNumber()">One number</li>
                <li [class.met]="passwordHasSpecial()">One special character</li>
              </ul>
            </div>
          </div>
        </div>

        <!-- Permissions -->
        <div class="form-section">
          <h3 class="section-title">Permissions</h3>
          <p class="section-description">Select the permissions for this admin user</p>

          <div class="permissions-grid">
            <label *ngFor="let permission of availablePermissions" class="permission-checkbox">
              <input
                type="checkbox"
                [value]="permission"
                (change)="onPermissionChange(permission, $event)"
                [checked]="isPermissionSelected(permission)">
              <span class="checkbox-label">{{ permission }}</span>
            </label>
          </div>
        </div>

        <!-- Session Configuration -->
        <div class="form-section">
          <h3 class="section-title">Session Settings</h3>

          <div class="form-group">
            <label for="sessionTimeout" class="form-label">Session Timeout (minutes)</label>
            <input
              id="sessionTimeout"
              type="number"
              formControlName="sessionTimeoutMinutes"
              class="form-input"
              min="5"
              max="1440"
              placeholder="Default: 30 minutes">
            <div class="help-text">Session will auto-logout after this period of inactivity</div>
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
            {{ data.mode === 'create' ? 'Create User' : 'Update User' }}
          </app-button>
        </div>
      </form>
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

    .icon-sm {
      width: 1.25rem;
      height: 1.25rem;
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
    }

    .form-input:focus {
      outline: none;
      border-color: var(--color-primary, #3b82f6);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .form-input.error {
      border-color: var(--color-error, #ef4444);
    }

    .password-input-wrapper {
      position: relative;
    }

    .toggle-password {
      position: absolute;
      right: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      padding: 0.25rem;
      cursor: pointer;
      color: var(--color-neutral-500, #6b7280);
    }

    .password-strength {
      margin-top: 0.5rem;
    }

    .strength-bar {
      height: 4px;
      background: var(--color-neutral-200, #e5e7eb);
      border-radius: 2px;
      overflow: hidden;
    }

    .strength-fill {
      height: 100%;
      transition: all 0.3s;
      border-radius: 2px;
    }

    .strength-fill.weak {
      width: 33%;
      background: var(--color-error, #ef4444);
    }

    .strength-fill.medium {
      width: 66%;
      background: var(--color-warning, #f59e0b);
    }

    .strength-fill.strong {
      width: 100%;
      background: var(--color-success, #10b981);
    }

    .strength-label {
      display: inline-block;
      margin-top: 0.25rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .strength-label.weak {
      color: var(--color-error, #ef4444);
    }

    .strength-label.medium {
      color: var(--color-warning, #f59e0b);
    }

    .strength-label.strong {
      color: var(--color-success, #10b981);
    }

    .password-requirements {
      margin-top: 0.75rem;
      padding: 0.75rem;
      background: var(--color-neutral-50, #f9fafb);
      border-radius: 0.375rem;
    }

    .requirements-title {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--color-neutral-700, #374151);
      margin: 0 0 0.5rem 0;
    }

    .requirements-list {
      list-style: none;
      padding: 0;
      margin: 0;
      font-size: 0.75rem;
      color: var(--color-neutral-600, #4b5563);
    }

    .requirements-list li {
      padding: 0.25rem 0;
      position: relative;
      padding-left: 1.5rem;
    }

    .requirements-list li::before {
      content: "×";
      position: absolute;
      left: 0;
      color: var(--color-neutral-400, #9ca3af);
      font-weight: bold;
    }

    .requirements-list li.met {
      color: var(--color-success, #10b981);
    }

    .requirements-list li.met::before {
      content: "✓";
      color: var(--color-success, #10b981);
    }

    .permissions-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 0.75rem;
    }

    .permission-checkbox {
      display: flex;
      align-items: center;
      padding: 0.75rem;
      border: 1px solid var(--color-neutral-200, #e5e7eb);
      border-radius: 0.375rem;
      cursor: pointer;
      transition: all 0.2s;
    }

    .permission-checkbox:hover {
      background: var(--color-neutral-50, #f9fafb);
    }

    .permission-checkbox input {
      margin-right: 0.5rem;
    }

    .checkbox-label {
      font-size: 0.875rem;
      color: var(--color-neutral-700, #374151);
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
      .form-row {
        grid-template-columns: 1fr;
      }

      .permissions-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AdminUserDialogComponent implements OnInit {
  form!: FormGroup;
  showPassword = signal(false);
  submitting = signal(false);
  selectedPermissions = signal<string[]>([]);

  availablePermissions: string[] = [
    'SUPERADMIN_READ',
    'SUPERADMIN_WRITE',
    'SUPERADMIN_DELETE',
    'TENANT_MANAGEMENT',
    'USER_MANAGEMENT',
    'BILLING_MANAGEMENT',
    'SYSTEM_SETTINGS',
    'AUDIT_LOGS',
    'IMPERSONATION',
    'SECURITY_SETTINGS'
  ];

  dialogTitle = computed(() =>
    this.data.mode === 'create' ? 'Create New Admin User' : `Edit Admin User: ${this.data.user?.userName}`
  );

  // Password strength indicators
  passwordStrength = computed(() => {
    const password = this.form?.get('password')?.value || '';
    if (password.length < 8) return 'weak';

    let strength = 0;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    if (strength >= 5) return 'strong';
    if (strength >= 3) return 'medium';
    return 'weak';
  });

  passwordStrengthPercent = computed(() => {
    const strength = this.passwordStrength();
    if (strength === 'weak') return 33;
    if (strength === 'medium') return 66;
    return 100;
  });

  passwordMeetsLength = computed(() => (this.form?.get('password')?.value || '').length >= 12);
  passwordHasUppercase = computed(() => /[A-Z]/.test(this.form?.get('password')?.value || ''));
  passwordHasLowercase = computed(() => /[a-z]/.test(this.form?.get('password')?.value || ''));
  passwordHasNumber = computed(() => /[0-9]/.test(this.form?.get('password')?.value || ''));
  passwordHasSpecial = computed(() => /[^a-zA-Z0-9]/.test(this.form?.get('password')?.value || ''));

  constructor(
    private fb: FormBuilder,
    private adminUserService: AdminUserService,
    private snackBar: MatSnackBar,
    public dialogRef: MatDialogRef<AdminUserDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AdminUserDialogData
  ) {}

  ngOnInit(): void {
    this.buildForm();

    if (this.data.mode === 'edit' && this.data.user) {
      this.populateForm(this.data.user);
      this.selectedPermissions.set(this.data.user.permissions || []);
    }
  }

  private buildForm(): void {
    this.form = this.fb.group({
      userName: [
        '',
        this.data.mode === 'create' ? [Validators.required, Validators.minLength(3)] : []
      ],
      email: [
        '',
        this.data.mode === 'create' ? [Validators.required, Validators.email] : []
      ],
      password: [
        '',
        this.data.mode === 'create'
          ? [Validators.required, Validators.minLength(12), this.passwordStrengthValidator]
          : []
      ],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phoneNumber: [''],
      sessionTimeoutMinutes: [30, [Validators.min(5), Validators.max(1440)]]
    });
  }

  private populateForm(user: AdminUser): void {
    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      phoneNumber: user.phoneNumber,
      sessionTimeoutMinutes: user.sessionTimeoutMinutes
    });
  }

  private passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    if (!password) return null;

    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);
    const hasSpecial = /[^a-zA-Z0-9]/.test(password);

    if (!hasUpper || !hasLower || !hasNumber || !hasSpecial) {
      return { passwordStrength: true };
    }

    return null;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  onPermissionChange(permission: string, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.selectedPermissions.update(perms => [...perms, permission]);
    } else {
      this.selectedPermissions.update(perms => perms.filter(p => p !== permission));
    }
  }

  isPermissionSelected(permission: string): boolean {
    return this.selectedPermissions().includes(permission);
  }

  onSubmit(): void {
    if (this.form.invalid || this.submitting()) return;

    this.submitting.set(true);

    if (this.data.mode === 'create') {
      this.createUser();
    } else {
      this.updateUser();
    }
  }

  private createUser(): void {
    const request: CreateAdminUserRequest = {
      userName: this.form.value.userName,
      email: this.form.value.email,
      password: this.form.value.password,
      firstName: this.form.value.firstName,
      lastName: this.form.value.lastName,
      phoneNumber: this.form.value.phoneNumber || undefined,
      permissions: this.selectedPermissions(),
      sessionTimeoutMinutes: this.form.value.sessionTimeoutMinutes
    };

    this.adminUserService.create(request).subscribe({
      next: (response) => {
        this.snackBar.open(response.message, 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (err) => {
        console.error('Failed to create user:', err);
        this.snackBar.open(err.error?.message || 'Failed to create user', 'Close', { duration: 5000 });
        this.submitting.set(false);
      }
    });
  }

  private updateUser(): void {
    if (!this.data.user) return;

    const request: UpdateAdminUserRequest = {
      firstName: this.form.value.firstName,
      lastName: this.form.value.lastName,
      phoneNumber: this.form.value.phoneNumber || undefined,
      permissions: this.selectedPermissions(),
      sessionTimeoutMinutes: this.form.value.sessionTimeoutMinutes
    };

    this.adminUserService.update(this.data.user.id, request).subscribe({
      next: (response) => {
        this.snackBar.open(response.message, 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (err) => {
        console.error('Failed to update user:', err);
        this.snackBar.open(err.error?.message || 'Failed to update user', 'Close', { duration: 5000 });
        this.submitting.set(false);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
