import { Component, inject, signal, OnInit } from '@angular/core';

import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BillingService, PaymentHistory } from '../../../core/services/billing.service';

export interface PaymentDetailDialogData {
  payment: PaymentHistory;
}

/**
 * PRODUCTION-READY: Payment detail dialog for tenant billing
 * Displays detailed information about a specific payment
 */
@Component({
  selector: 'app-payment-detail-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule,
    MatListModule,
    MatSnackBarModule
],
  template: `
    <div class="payment-detail-dialog">
      <div mat-dialog-title>
        <div class="title-content">
          <mat-icon>receipt</mat-icon>
          <h2>Payment Details</h2>
        </div>
        <button mat-icon-button mat-dialog-close>
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-dialog-content>
        @if (loading()) {
          <div class="loading-state">
            <mat-spinner diameter="40"></mat-spinner>
            <p>Loading payment details...</p>
          </div>
        }

        @if (error()) {
          <div class="error-state">
            <mat-icon color="warn">error</mat-icon>
            <p>{{ error() }}</p>
          </div>
        }

        @if (payment() && !loading()) {
          <div class="payment-details">
            <!-- Payment Overview -->
            <section class="detail-section">
              <h3>Payment Information</h3>
              <mat-list>
                <mat-list-item>
                  <span matListItemTitle>Payment ID</span>
                  <span matListItemLine>{{ payment()!.id }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Invoice Number</span>
                  <span matListItemLine>{{ payment()!.invoiceNumber || 'Not generated' }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Amount</span>
                  <span matListItemLine class="amount-value">{{ formatCurrency(payment()!.amount) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Status</span>
                  <span matListItemLine>
                    <mat-chip [color]="getStatusColor(payment()!.status)">
                      {{ payment()!.status }}
                    </mat-chip>
                  </span>
                </mat-list-item>
              </mat-list>
            </section>

            <mat-divider></mat-divider>

            <!-- Billing Period -->
            <section class="detail-section">
              <h3>Billing Period</h3>
              <mat-list>
                <mat-list-item>
                  <span matListItemTitle>Period Start</span>
                  <span matListItemLine>{{ formatDate(payment()!.periodStartDate) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Period End</span>
                  <span matListItemLine>{{ formatDate(payment()!.periodEndDate) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Due Date</span>
                  <span matListItemLine>{{ formatDate(payment()!.dueDate) }}</span>
                </mat-list-item>
                @if (payment()!.paidDate) {
                  <mat-list-item>
                    <span matListItemTitle>Paid Date</span>
                    <span matListItemLine class="paid-date">{{ formatDate(payment()!.paidDate) }}</span>
                  </mat-list-item>
                }
              </mat-list>
            </section>

            <mat-divider></mat-divider>

            <!-- Payment Status Details -->
            <section class="detail-section">
              <h3>Status Details</h3>
              <mat-list>
                <mat-list-item>
                  <span matListItemTitle>Payment Method</span>
                  <span matListItemLine>{{ payment()!.paymentMethod || 'Not specified' }}</span>
                </mat-list-item>
                @if (payment()!.isOverdue) {
                  <mat-list-item class="overdue-warning">
                    <span matListItemTitle>
                      <mat-icon>warning</mat-icon>
                      Overdue Status
                    </span>
                    <span matListItemLine class="overdue-text">
                      This payment is overdue. Please contact support to arrange payment.
                    </span>
                  </mat-list-item>
                }
              </mat-list>
            </section>

            @if (payment()!.invoiceNumber) {
              <mat-divider></mat-divider>
              <section class="detail-section invoice-section">
                <h3>Invoice</h3>
                <div class="invoice-actions">
                  <button
                    mat-raised-button
                    color="primary"
                    (click)="downloadInvoice()"
                    [disabled]="downloading()">
                    @if (downloading()) {
                      <mat-spinner diameter="20"></mat-spinner>
                    } @else {
                      <mat-icon>download</mat-icon>
                    }
                    Download Invoice
                  </button>
                  <p class="invoice-note">
                    Invoice #{{ payment()!.invoiceNumber }}
                  </p>
                </div>
              </section>
            }
          </div>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button mat-dialog-close>Close</button>
        @if (payment()!.status !== 'Paid' && payment()!.status !== 'Waived') {
          <button mat-raised-button color="accent" (click)="contactSupport()">
            <mat-icon>support_agent</mat-icon>
            Contact Support
          </button>
        }
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .payment-detail-dialog {
      min-width: 500px;
      max-width: 600px;

      [mat-dialog-title] {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 0;
        padding: 20px 24px;
        border-bottom: 1px solid rgba(0, 0, 0, 0.12);

        .title-content {
          display: flex;
          align-items: center;
          gap: 12px;

          mat-icon {
            color: #1976d2;
            font-size: 28px;
            width: 28px;
            height: 28px;
          }

          h2 {
            margin: 0;
            font-size: 1.5rem;
            font-weight: 500;
          }
        }
      }

      mat-dialog-content {
        padding: 0;
        max-height: 70vh;
        overflow-y: auto;

        .loading-state,
        .error-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 48px;
          gap: 16px;

          mat-icon {
            font-size: 48px;
            width: 48px;
            height: 48px;
          }

          p {
            color: rgba(0, 0, 0, 0.6);
            text-align: center;
            margin: 0;
          }
        }

        .payment-details {
          .detail-section {
            padding: 24px;

            h3 {
              margin: 0 0 16px 0;
              font-size: 1.125rem;
              font-weight: 600;
              color: rgba(0, 0, 0, 0.87);
            }

            mat-list {
              padding: 0;

              mat-list-item {
                height: auto;
                min-height: 56px;
                padding: 12px 0;

                [matListItemTitle] {
                  font-weight: 500;
                  color: rgba(0, 0, 0, 0.6);
                  font-size: 0.875rem;
                  display: flex;
                  align-items: center;
                  gap: 8px;

                  mat-icon {
                    font-size: 18px;
                    width: 18px;
                    height: 18px;
                    color: #ff9800;
                  }
                }

                [matListItemLine] {
                  font-size: 1rem;
                  margin-top: 4px;
                  color: rgba(0, 0, 0, 0.87);

                  &.amount-value {
                    font-weight: 600;
                    font-size: 1.25rem;
                    color: #1976d2;
                  }

                  &.paid-date {
                    color: #4caf50;
                    font-weight: 500;
                  }

                  &.overdue-text {
                    color: #d32f2f;
                    font-weight: 500;
                  }
                }

                &.overdue-warning {
                  background-color: #ffebee;
                  border-left: 4px solid #d32f2f;
                  padding-left: 12px;
                  margin: 8px 0;
                }
              }
            }

            &.invoice-section {
              .invoice-actions {
                display: flex;
                flex-direction: column;
                gap: 12px;
                align-items: flex-start;

                button {
                  mat-icon,
                  mat-spinner {
                    margin-right: 8px;
                  }
                }

                .invoice-note {
                  margin: 0;
                  font-size: 0.875rem;
                  color: rgba(0, 0, 0, 0.6);
                }
              }
            }
          }
        }
      }

      mat-dialog-actions {
        padding: 16px 24px;
        border-top: 1px solid rgba(0, 0, 0, 0.12);
        gap: 8px;

        button {
          mat-icon {
            margin-right: 8px;
          }
        }
      }
    }

    // Responsive
    @media (max-width: 600px) {
      .payment-detail-dialog {
        min-width: 0;
        width: 100%;
      }
    }

    // Dark mode support
    :host-context(.dark-theme) {
      .payment-detail-dialog {
        [mat-dialog-title] {
          border-bottom-color: rgba(255, 255, 255, 0.12);
        }

        mat-dialog-content {
          .error-state p,
          .loading-state p {
            color: rgba(255, 255, 255, 0.7);
          }

          .payment-details {
            .detail-section {
              h3 {
                color: rgba(255, 255, 255, 0.87);
              }

              mat-list-item {
                [matListItemTitle] {
                  color: rgba(255, 255, 255, 0.7);
                }

                [matListItemLine] {
                  color: rgba(255, 255, 255, 0.87);
                }

                &.overdue-warning {
                  background-color: rgba(211, 47, 47, 0.1);
                }
              }
            }
          }
        }

        mat-dialog-actions {
          border-top-color: rgba(255, 255, 255, 0.12);
        }
      }
    }
  `]
})
export class PaymentDetailDialogComponent implements OnInit {
  private billingService = inject(BillingService);
  private snackBar = inject(MatSnackBar);
  private dialogRef = inject(MatDialogRef<PaymentDetailDialogComponent>);
  data = inject<PaymentDetailDialogData>(MAT_DIALOG_DATA);

  loading = signal(false);
  downloading = signal(false);
  error = signal<string | null>(null);
  payment = signal<PaymentHistory | null>(null);

  ngOnInit(): void {
    // Set the payment data from dialog input
    if (this.data?.payment) {
      this.payment.set(this.data.payment);
    } else {
      this.error.set('Payment data not provided');
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-MU', {
      style: 'currency',
      currency: 'MUR',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-MU', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Paid':
        return 'success';
      case 'Pending':
        return 'warning';
      case 'Overdue':
        return 'error';
      case 'Waived':
        return 'info';
      default:
        return 'default';
    }
  }

  downloadInvoice(): void {
    const paymentId = this.payment()?.id;
    if (!paymentId) {
      this.snackBar.open('Payment ID not available', 'Close', { duration: 3000 });
      return;
    }

    this.downloading.set(true);

    this.billingService.downloadInvoice(paymentId).subscribe({
      next: (blob: Blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `invoice-${this.payment()!.invoiceNumber || paymentId}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.downloading.set(false);
        this.snackBar.open('Invoice downloaded successfully', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
      },
      error: (err) => {
        console.error('Error downloading invoice:', err);
        this.downloading.set(false);

        let errorMessage = 'Failed to download invoice';
        if (err.status === 404) {
          errorMessage = 'Invoice not found';
        } else if (err.status === 403) {
          errorMessage = 'Access denied to download invoice';
        }

        this.snackBar.open(errorMessage, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  contactSupport(): void {
    this.snackBar.open('Opening support contact...', 'Close', { duration: 3000 });
    // TODO: Implement support contact dialog or navigation
    this.dialogRef.close({ action: 'contact_support' });
  }
}
