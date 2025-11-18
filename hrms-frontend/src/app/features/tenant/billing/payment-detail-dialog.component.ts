import { Component, inject, signal, OnInit } from '@angular/core';

import { DialogRef } from '../../../shared/ui';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../shared/ui/ui.module';
import { Chip, ChipColor } from '@app/shared/ui';
import { ToastService, Divider, List, ListItem } from '../../../shared/ui';
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
    MatButtonModule,
    MatIconModule,
    Chip,
    UiModule,
    Divider,
    List,
    ListItem,
],
  template: `
    <div class="payment-detail-dialog">
      <div class="dialog-title">
        <div class="title-content">
          <mat-icon>receipt</mat-icon>
          <h2>Payment Details</h2>
        </div>
        <button mat-icon-button (click)="dialogRef.close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="dialog-content">
        @if (loading()) {
          <div class="loading-state">
            <app-progress-spinner size="medium" color="primary"></app-progress-spinner>
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
              <app-list>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Payment ID</span>
                    <span class="item-line">{{ payment()!.id }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Invoice Number</span>
                    <span class="item-line">{{ payment()!.invoiceNumber || 'Not generated' }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Amount</span>
                    <span class="item-line amount-value">{{ formatCurrency(payment()!.amount) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Status</span>
                    <span class="item-line">
                      <app-chip
                        [label]="payment()!.status"
                        [color]="getStatusColor(payment()!.status)">
                      </app-chip>
                    </span>
                  </div>
                </app-list-item>
              </app-list>
            </section>

            <app-divider />

            <!-- Billing Period -->
            <section class="detail-section">
              <h3>Billing Period</h3>
              <app-list>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Period Start</span>
                    <span class="item-line">{{ formatDate(payment()!.periodStartDate) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Period End</span>
                    <span class="item-line">{{ formatDate(payment()!.periodEndDate) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Due Date</span>
                    <span class="item-line">{{ formatDate(payment()!.dueDate) }}</span>
                  </div>
                </app-list-item>
                @if (payment()!.paidDate) {
                  <app-list-item>
                    <div class="list-item-content">
                      <span class="item-title">Paid Date</span>
                      <span class="item-line paid-date">{{ formatDate(payment()!.paidDate) }}</span>
                    </div>
                  </app-list-item>
                }
              </app-list>
            </section>

            <app-divider />

            <!-- Payment Status Details -->
            <section class="detail-section">
              <h3>Status Details</h3>
              <app-list>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Payment Method</span>
                    <span class="item-line">{{ payment()!.paymentMethod || 'Not specified' }}</span>
                  </div>
                </app-list-item>
                @if (payment()!.isOverdue) {
                  <app-list-item class="overdue-warning">
                    <div class="list-item-content">
                      <span class="item-title">
                        <mat-icon>warning</mat-icon>
                        Overdue Status
                      </span>
                      <span class="item-line overdue-text">
                        This payment is overdue. Please contact support to arrange payment.
                      </span>
                    </div>
                  </app-list-item>
                }
              </app-list>
            </section>

            @if (payment()!.invoiceNumber) {
              <app-divider />
              <section class="detail-section invoice-section">
                <h3>Invoice</h3>
                <div class="invoice-actions">
                  <button
                    mat-raised-button
                    color="primary"
                    (click)="downloadInvoice()"
                    [disabled]="downloading()">
                    @if (downloading()) {
                      <app-progress-spinner size="small" color="primary"></app-progress-spinner>
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
      </div>

      <div class="dialog-actions">
        <button mat-button (click)="dialogRef.close()">Close</button>
        @if (payment()!.status !== 'Paid' && payment()!.status !== 'Waived') {
          <button mat-raised-button color="accent" (click)="contactSupport()">
            <mat-icon>support_agent</mat-icon>
            Contact Support
          </button>
        }
      </div>
    </div>
  `,
  styles: [`
    .payment-detail-dialog {
      min-width: 500px;
      max-width: 600px;

      .dialog-title {
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

      .dialog-content {
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

            app-list {
              padding: 0;
            }

            .list-item-content {
              display: flex;
              flex-direction: column;
              width: 100%;

              .item-title {
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

              .item-line {
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
            }

            app-list-item.overdue-warning {
              background-color: #ffebee;
              border-left: 4px solid #d32f2f;
              padding-left: 12px;
              margin: 8px 0;
            }

            &.invoice-section {
              .invoice-actions {
                display: flex;
                flex-direction: column;
                gap: 12px;
                align-items: flex-start;

                button {
                  mat-icon,
                  app-progress-spinner {
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

      .dialog-actions {
        padding: 16px 24px;
        border-top: 1px solid rgba(0, 0, 0, 0.12);
        gap: 8px;
        display: flex;
        justify-content: flex-end;

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
        .dialog-title {
          border-bottom-color: rgba(255, 255, 255, 0.12);
        }

        .dialog-content {
          .error-state p,
          .loading-state p {
            color: rgba(255, 255, 255, 0.7);
          }

          .payment-details {
            .detail-section {
              h3 {
                color: rgba(255, 255, 255, 0.87);
              }

              .list-item-content {
                .item-title {
                  color: rgba(255, 255, 255, 0.7);
                }

                .item-line {
                  color: rgba(255, 255, 255, 0.87);
                }
              }

              app-list-item.overdue-warning {
                background-color: rgba(211, 47, 47, 0.1);
              }
            }
          }
        }

        .dialog-actions {
          border-top-color: rgba(255, 255, 255, 0.12);
        }
      }
    }
  `]
})
export class PaymentDetailDialogComponent implements OnInit {
  private billingService = inject(BillingService);
  private toastService = inject(ToastService);
  public dialogRef = inject(DialogRef<PaymentDetailDialogComponent, { action?: string }>);

  get dialogData(): PaymentDetailDialogData {
    return this.dialogRef.data;
  }

  get data(): PaymentDetailDialogData {
    return this.dialogData;
  }

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

  getStatusColor(status: string): ChipColor {
    switch (status) {
      case 'Paid':
        return 'success';
      case 'Pending':
        return 'warning';
      case 'Overdue':
        return 'error';
      case 'Waived':
        return 'primary';
      default:
        return 'neutral';
    }
  }

  downloadInvoice(): void {
    const paymentId = this.payment()?.id;
    if (!paymentId) {
      this.toastService.warning('Payment ID not available', 3000);
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
        this.toastService.success('Invoice downloaded successfully', 3000);
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

        this.toastService.error(errorMessage, 5000);
      }
    });
  }

  contactSupport(): void {
    this.toastService.info('Opening support contact...', 3000);
    // TODO: Implement support contact dialog or navigation
    this.dialogRef.close({ action: 'contact_support' });
  }
}
