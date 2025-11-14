// ═══════════════════════════════════════════════════════════
// DIALOG SYSTEM - COMPLETE USAGE EXAMPLES
// Production-ready examples for the HRMS Dialog System
// ═══════════════════════════════════════════════════════════

import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogService, DialogRef } from '../../ui.module';
import type { DialogConfig } from '../../ui.module';

// ═══════════════════════════════════════════════════════════
// EXAMPLE 1: Simple Confirmation Dialog
// ═══════════════════════════════════════════════════════════

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [CommonModule],
  template: `
    <div class="confirm-dialog">
      <h2 class="dialog-title">{{ data?.title }}</h2>
      <p class="dialog-message">{{ data?.message }}</p>
      <div class="dialog-actions">
        <button type="button" class="btn btn-secondary" (click)="onCancel()">
          {{ data?.cancelText || 'Cancel' }}
        </button>
        <button type="button" class="btn btn-danger" (click)="onConfirm()">
          {{ data?.confirmText || 'Confirm' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .confirm-dialog {
      padding: 8px;
    }
    .dialog-title {
      margin: 0 0 16px;
      font-size: 20px;
      font-weight: 600;
      color: #1e293b;
    }
    .dialog-message {
      margin: 0 0 24px;
      font-size: 14px;
      line-height: 1.6;
      color: #64748b;
    }
    .dialog-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
    }
    .btn {
      padding: 8px 16px;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      cursor: pointer;
    }
    .btn-secondary {
      background: #e2e8f0;
      color: #475569;
    }
    .btn-danger {
      background: #ef4444;
      color: white;
    }
  `]
})
export class ConfirmDialogComponent {
  dialogRef = inject(DialogRef<ConfirmDialogComponent, boolean>);

  get data(): ConfirmDialogData | undefined {
    return this.dialogRef.data;
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}

// Usage in parent component:
@Component({
  selector: 'app-example-page',
  template: `<button (click)="openConfirmDialog()">Delete User</button>`
})
export class ExamplePageComponent {
  dialogService = inject(DialogService);

  openConfirmDialog(): void {
    const dialogRef = this.dialogService.open<
      ConfirmDialogComponent,
      ConfirmDialogData,
      boolean
    >(ConfirmDialogComponent, {
      width: '450px',
      disableClose: true,
      data: {
        title: 'Delete User',
        message: 'Are you sure you want to delete this user? This action cannot be undone.',
        confirmText: 'Delete',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        console.log('User deletion confirmed');
        // Perform delete operation
      }
    });
  }
}

// ═══════════════════════════════════════════════════════════
// EXAMPLE 2: Form Dialog with Validation
// ═══════════════════════════════════════════════════════════

export interface UserFormData {
  userId?: number;
  initialData?: {
    name: string;
    email: string;
    role: string;
  };
}

export interface UserFormResult {
  name: string;
  email: string;
  role: string;
}

@Component({
  selector: 'app-user-form-dialog',
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="form-dialog">
      <h2>{{ isEditMode ? 'Edit User' : 'Create User' }}</h2>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="form-field">
          <label for="name">Name *</label>
          <input
            id="name"
            formControlName="name"
            type="text"
            placeholder="Enter name"
            class="form-input"
          />
          <div *ngIf="form.get('name')?.invalid && form.get('name')?.touched" class="error">
            Name is required
          </div>
        </div>

        <div class="form-field">
          <label for="email">Email *</label>
          <input
            id="email"
            formControlName="email"
            type="email"
            placeholder="Enter email"
            class="form-input"
          />
          <div *ngIf="form.get('email')?.invalid && form.get('email')?.touched" class="error">
            Valid email is required
          </div>
        </div>

        <div class="form-field">
          <label for="role">Role *</label>
          <select id="role" formControlName="role" class="form-input">
            <option value="">Select role</option>
            <option value="admin">Admin</option>
            <option value="manager">Manager</option>
            <option value="employee">Employee</option>
          </select>
          <div *ngIf="form.get('role')?.invalid && form.get('role')?.touched" class="error">
            Role is required
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="btn btn-secondary" (click)="onCancel()">
            Cancel
          </button>
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid">
            {{ isEditMode ? 'Update' : 'Create' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .form-dialog {
      padding: 8px;
      min-width: 400px;
    }
    h2 {
      margin: 0 0 24px;
      font-size: 24px;
      font-weight: 600;
    }
    .form-field {
      margin-bottom: 20px;
    }
    label {
      display: block;
      margin-bottom: 6px;
      font-size: 14px;
      font-weight: 500;
      color: #374151;
    }
    .form-input {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 14px;
    }
    .error {
      margin-top: 4px;
      color: #ef4444;
      font-size: 12px;
    }
    .dialog-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      margin-top: 24px;
    }
    .btn {
      padding: 10px 20px;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      cursor: pointer;
    }
    .btn-primary {
      background: #3b82f6;
      color: white;
    }
    .btn-primary:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }
    .btn-secondary {
      background: #e2e8f0;
      color: #475569;
    }
  `]
})
export class UserFormDialogComponent implements OnInit {
  dialogRef = inject(DialogRef<UserFormDialogComponent, UserFormResult>);
  fb = inject(FormBuilder);

  form!: FormGroup;

  get isEditMode(): boolean {
    return !!this.dialogRef.data?.userId;
  }

  ngOnInit(): void {
    const initialData = this.dialogRef.data?.initialData;

    this.form = this.fb.group({
      name: [initialData?.name || '', Validators.required],
      email: [initialData?.email || '', [Validators.required, Validators.email]],
      role: [initialData?.role || '', Validators.required]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSubmit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}

// Usage:
@Component({
  selector: 'app-user-list',
  template: `<button (click)="openUserDialog()">Add User</button>`
})
export class UserListComponent {
  dialogService = inject(DialogService);

  openUserDialog(): void {
    const dialogRef = this.dialogService.open<
      UserFormDialogComponent,
      UserFormData,
      UserFormResult
    >(UserFormDialogComponent, {
      width: '600px',
      maxHeight: '90vh',
      disableClose: true,
      data: {
        // For create mode:
        // No userId

        // For edit mode:
        userId: 123,
        initialData: {
          name: 'John Doe',
          email: 'john@example.com',
          role: 'employee'
        }
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        console.log('Form submitted:', result);
        // Save user data
      }
    });
  }
}

// ═══════════════════════════════════════════════════════════
// EXAMPLE 3: Alert Dialog (No Close Button)
// ═══════════════════════════════════════════════════════════

@Component({
  selector: 'app-alert-dialog',
  imports: [CommonModule],
  template: `
    <div class="alert-dialog">
      <div class="icon-warning">⚠️</div>
      <h2>{{ data?.title }}</h2>
      <p>{{ data?.message }}</p>
      <button type="button" class="btn btn-primary" (click)="onOk()">
        OK
      </button>
    </div>
  `,
  styles: [`
    .alert-dialog {
      padding: 16px;
      text-align: center;
    }
    .icon-warning {
      font-size: 48px;
      margin-bottom: 16px;
    }
    h2 {
      margin: 0 0 12px;
      font-size: 20px;
      font-weight: 600;
    }
    p {
      margin: 0 0 24px;
      color: #64748b;
    }
    .btn-primary {
      padding: 10px 32px;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
    }
  `]
})
export class AlertDialogComponent {
  dialogRef = inject(DialogRef);

  get data() {
    return this.dialogRef.data;
  }

  onOk(): void {
    this.dialogRef.close();
  }
}

// Usage:
export class SomeComponent {
  dialogService = inject(DialogService);

  showAlert(): void {
    this.dialogService.open(AlertDialogComponent, {
      width: '400px',
      hasCloseButton: false,
      disableClose: true,
      role: 'alertdialog',
      data: {
        title: 'Session Expired',
        message: 'Your session has expired. Please log in again.'
      }
    });
  }
}

// ═══════════════════════════════════════════════════════════
// EXAMPLE 4: Full-Screen Dialog
// ═══════════════════════════════════════════════════════════

@Component({
  selector: 'app-details-dialog',
  imports: [CommonModule],
  template: `
    <div class="fullscreen-dialog">
      <header class="dialog-header">
        <h1>{{ data?.title }}</h1>
        <button class="close-btn" (click)="close()">×</button>
      </header>
      <main class="dialog-body">
        <p>Full-screen dialog content goes here...</p>
      </main>
      <footer class="dialog-footer">
        <button class="btn" (click)="close()">Close</button>
      </footer>
    </div>
  `,
  styles: [`
    .fullscreen-dialog {
      display: flex;
      flex-direction: column;
      height: 100vh;
    }
    .dialog-header {
      padding: 16px 24px;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .dialog-body {
      flex: 1;
      padding: 24px;
      overflow-y: auto;
    }
    .dialog-footer {
      padding: 16px 24px;
      border-top: 1px solid #e5e7eb;
    }
  `]
})
export class DetailsDialogComponent {
  dialogRef = inject(DialogRef);

  get data() {
    return this.dialogRef.data;
  }

  close(): void {
    this.dialogRef.close();
  }
}

// Usage:
export class DataComponent {
  dialogService = inject(DialogService);

  openFullscreen(): void {
    this.dialogService.open(DetailsDialogComponent, {
      width: '100vw',
      height: '100vh',
      maxWidth: '100vw',
      maxHeight: '100vh',
      panelClass: 'dialog-panel--fullscreen',
      hasCloseButton: false,
      data: {
        title: 'Detailed View'
      }
    });
  }
}

// ═══════════════════════════════════════════════════════════
// EXAMPLE 5: Multiple Dialogs & Dialog Management
// ═══════════════════════════════════════════════════════════

export class MultiDialogComponent {
  dialogService = inject(DialogService);

  openMultipleDialogs(): void {
    // Open first dialog
    const dialog1 = this.dialogService.open(ConfirmDialogComponent, {
      data: { title: 'First Dialog', message: 'This is the first dialog' }
    });

    // Open second dialog after first closes
    dialog1.afterClosed().subscribe(() => {
      const dialog2 = this.dialogService.open(AlertDialogComponent, {
        data: { title: 'Second Dialog', message: 'This is the second dialog' }
      });
    });
  }

  closeAllDialogs(): void {
    // Close all open dialogs
    this.dialogService.closeAll();
  }

  getOpenDialogCount(): number {
    return this.dialogService.getOpenDialogs().length;
  }
}

// ═══════════════════════════════════════════════════════════
// EXAMPLE 6: Dialog with Custom Styling
// ═══════════════════════════════════════════════════════════

export class StyledDialogComponent {
  dialogService = inject(DialogService);

  openCustomStyledDialog(): void {
    this.dialogService.open(UserFormDialogComponent, {
      width: '700px',
      backdropClass: 'dark-backdrop',
      panelClass: ['custom-panel', 'elevated-panel'],
      data: {
        /* ... */
      }
    });
  }
}

// In your global styles or component styles:
// .dark-backdrop {
//   background-color: rgba(0, 0, 0, 0.85) !important;
// }
// .custom-panel {
//   border: 2px solid #3b82f6;
// }
// .elevated-panel {
//   box-shadow: 0 32px 64px rgba(0, 0, 0, 0.3) !important;
// }
