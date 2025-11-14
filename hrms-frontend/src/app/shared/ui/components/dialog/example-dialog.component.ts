// ═══════════════════════════════════════════════════════════
// EXAMPLE DIALOG COMPONENT
// Demonstrates how to create a dialog content component
// ═══════════════════════════════════════════════════════════

import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogRef } from '../../services/dialog-ref';

export interface ExampleDialogData {
  title: string;
  message: string;
}

@Component({
  selector: 'app-example-dialog',
  imports: [CommonModule],
  template: `
    <div class="example-dialog">
      <h2 class="dialog-title">{{ dialogData?.title || 'Dialog Title' }}</h2>
      <p class="dialog-message">{{ dialogData?.message || 'Dialog content goes here.' }}</p>

      <div class="dialog-actions">
        <button type="button" class="btn btn-secondary" (click)="onCancel()">
          Cancel
        </button>
        <button type="button" class="btn btn-primary" (click)="onConfirm()">
          Confirm
        </button>
      </div>
    </div>
  `,
  styles: [`
    .example-dialog {
      min-width: 300px;
    }

    .dialog-title {
      margin: 0 0 16px 0;
      font-size: 24px;
      font-weight: 600;
      color: #1e293b;
    }

    .dialog-message {
      margin: 0 0 24px 0;
      font-size: 16px;
      line-height: 1.5;
      color: #64748b;
    }

    .dialog-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
    }

    .btn {
      padding: 10px 20px;
      border: none;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 200ms ease;
    }

    .btn-primary {
      background-color: #3b82f6;
      color: white;
    }

    .btn-primary:hover {
      background-color: #2563eb;
    }

    .btn-secondary {
      background-color: #e2e8f0;
      color: #475569;
    }

    .btn-secondary:hover {
      background-color: #cbd5e1;
    }
  `]
})
export class ExampleDialogComponent {
  // Inject DialogRef to get access to dialog data and control
  private dialogRef = inject(DialogRef<ExampleDialogComponent, boolean>);

  // Access the data passed to the dialog
  get dialogData(): ExampleDialogData | undefined {
    return this.dialogRef.data;
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
