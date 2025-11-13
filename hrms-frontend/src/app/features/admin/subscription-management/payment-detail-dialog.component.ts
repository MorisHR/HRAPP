import { Component, inject, signal, OnInit } from '@angular/core';

import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';

import { SubscriptionService } from '../../../services/subscription.service';
import { SubscriptionPayment, TenantSubscriptionHistory } from '../../../models/subscription.model';

export interface PaymentDetailDialogData {
  paymentId: number;
  tenantId: number;
}

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
    MatListModule
],
  template: `
    <div class="payment-detail-dialog">
      <div mat-dialog-title>
        <h2>Payment Details</h2>
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

        @if (payment() && history()) {
          <div class="payment-details">
            <!-- Current Payment Info -->
            <section class="detail-section">
              <h3>Current Payment</h3>
              <mat-list>
                <mat-list-item>
                  <span matListItemTitle>Payment ID</span>
                  <span matListItemLine>{{ payment()!.id }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Tenant</span>
                  <span matListItemLine>{{ payment()!.tenantName }} ({{ payment()!.tenantSubdomain }})</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Amount</span>
                  <span matListItemLine class="amount">{{ formatCurrency(payment()!.amount) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Due Date</span>
                  <span matListItemLine>{{ formatDate(payment()!.dueDate) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Status</span>
                  <span matListItemLine>
                    <mat-chip [color]="getStatusColor(payment()!.status)">
                      {{ payment()!.status }}
                    </mat-chip>
                  </span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Subscription Tier</span>
                  <span matListItemLine>
                    <mat-chip [color]="getTierColor(payment()!.subscriptionTier)">
                      {{ payment()!.subscriptionTier }}
                    </mat-chip>
                  </span>
                </mat-list-item>
                @if (payment()!.paymentDate) {
                  <mat-list-item>
                    <span matListItemTitle>Payment Date</span>
                    <span matListItemLine>{{ formatDate(payment()!.paymentDate!) }}</span>
                  </mat-list-item>
                }
                @if (payment()!.reminderSentDate) {
                  <mat-list-item>
                    <span matListItemTitle>Last Reminder Sent</span>
                    <span matListItemLine>{{ formatDate(payment()!.reminderSentDate!) }}</span>
                  </mat-list-item>
                }
              </mat-list>
            </section>

            <mat-divider></mat-divider>

            <!-- Tenant Subscription Summary -->
            <section class="detail-section">
              <h3>Subscription Summary</h3>
              <mat-list>
                <mat-list-item>
                  <span matListItemTitle>Current Tier</span>
                  <span matListItemLine>
                    <mat-chip [color]="getTierColor(history()!.currentTier)">
                      {{ history()!.currentTier }}
                    </mat-chip>
                  </span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Subscription Start</span>
                  <span matListItemLine>{{ formatDate(history()!.subscriptionStartDate) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Total Payments</span>
                  <span matListItemLine>{{ history()!.payments.length }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Total Paid</span>
                  <span matListItemLine class="amount success">{{ formatCurrency(history()!.totalPaid) }}</span>
                </mat-list-item>
                <mat-list-item>
                  <span matListItemTitle>Total Overdue</span>
                  <span matListItemLine class="amount error">{{ formatCurrency(history()!.totalOverdue) }}</span>
                </mat-list-item>
              </mat-list>
            </section>

            <mat-divider></mat-divider>

            <!-- Payment History -->
            <section class="detail-section">
              <h3>Payment History</h3>
              @if (history()!.payments.length === 0) {
                <p class="no-history">No payment history available</p>
              } @else {
                <div class="payment-history">
                  @for (historicalPayment of history()!.payments; track historicalPayment.id) {
                    <div class="history-item" [class.current]="historicalPayment.id === payment()!.id">
                      <div class="history-header">
                        <div class="history-date">
                          <mat-icon>event</mat-icon>
                          <span>{{ formatDate(historicalPayment.dueDate) }}</span>
                        </div>
                        <mat-chip [color]="getStatusColor(historicalPayment.status)" class="history-status">
                          {{ historicalPayment.status }}
                        </mat-chip>
                      </div>
                      <div class="history-details">
                        <span class="history-amount">{{ formatCurrency(historicalPayment.amount) }}</span>
                        @if (historicalPayment.paymentDate) {
                          <span class="history-paid">Paid: {{ formatDate(historicalPayment.paymentDate) }}</span>
                        }
                      </div>
                      @if (historicalPayment.id === payment()!.id) {
                        <div class="current-badge">
                          <mat-chip color="accent">Current</mat-chip>
                        </div>
                      }
                    </div>
                  }
                </div>
              }
            </section>
          </div>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button mat-dialog-close>Close</button>
        @if (payment() && payment()!.status !== 'Paid') {
          <button mat-raised-button color="primary" (click)="recordPayment()">
            <mat-icon>payment</mat-icon>
            Record Payment
          </button>
          <button mat-raised-button color="accent" (click)="sendReminder()">
            <mat-icon>email</mat-icon>
            Send Reminder
          </button>
        }
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .payment-detail-dialog {
      width: 600px;
      max-width: 90vw;

      [mat-dialog-title] {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 0;
        padding: 16px 24px;
        border-bottom: 1px solid rgba(0, 0, 0, 0.12);

        h2 {
          margin: 0;
          font-size: 1.25rem;
          font-weight: 500;
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
          }
        }

        .payment-details {
          .detail-section {
            padding: 24px;

            h3 {
              margin: 0 0 16px 0;
              font-size: 1rem;
              font-weight: 600;
              color: rgba(0, 0, 0, 0.87);
            }

            mat-list {
              padding: 0;

              mat-list-item {
                height: auto;
                min-height: 48px;
                padding: 8px 0;

                [matListItemTitle] {
                  font-weight: 500;
                  color: rgba(0, 0, 0, 0.6);
                  font-size: 0.875rem;
                }

                [matListItemLine] {
                  font-size: 0.875rem;
                  margin-top: 4px;

                  &.amount {
                    font-weight: 600;
                    font-size: 1rem;

                    &.success {
                      color: #4caf50;
                    }

                    &.error {
                      color: #f44336;
                    }
                  }
                }
              }
            }

            .no-history {
              color: rgba(0, 0, 0, 0.6);
              font-style: italic;
              text-align: center;
              padding: 16px;
            }

            .payment-history {
              display: flex;
              flex-direction: column;
              gap: 12px;

              .history-item {
                padding: 12px;
                border: 1px solid rgba(0, 0, 0, 0.12);
                border-radius: 8px;
                background-color: rgba(0, 0, 0, 0.02);
                position: relative;

                &.current {
                  border-color: #3f51b5;
                  background-color: rgba(63, 81, 181, 0.05);
                }

                .history-header {
                  display: flex;
                  justify-content: space-between;
                  align-items: center;
                  margin-bottom: 8px;

                  .history-date {
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    font-size: 0.875rem;
                    color: rgba(0, 0, 0, 0.87);

                    mat-icon {
                      font-size: 18px;
                      width: 18px;
                      height: 18px;
                      color: rgba(0, 0, 0, 0.6);
                    }
                  }

                  .history-status {
                    font-size: 0.75rem;
                  }
                }

                .history-details {
                  display: flex;
                  justify-content: space-between;
                  align-items: center;
                  font-size: 0.875rem;

                  .history-amount {
                    font-weight: 600;
                    font-size: 1rem;
                  }

                  .history-paid {
                    color: rgba(0, 0, 0, 0.6);
                    font-size: 0.75rem;
                  }
                }

                .current-badge {
                  position: absolute;
                  top: -8px;
                  right: 12px;
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

    // Dark mode
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
              }

              .no-history {
                color: rgba(255, 255, 255, 0.6);
              }

              .payment-history {
                .history-item {
                  border-color: rgba(255, 255, 255, 0.12);
                  background-color: rgba(255, 255, 255, 0.02);

                  .history-date {
                    color: rgba(255, 255, 255, 0.87);

                    mat-icon {
                      color: rgba(255, 255, 255, 0.6);
                    }
                  }

                  .history-paid {
                    color: rgba(255, 255, 255, 0.7);
                  }
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
  private subscriptionService = inject(SubscriptionService);
  private dialogRef = inject(MatDialogRef<PaymentDetailDialogComponent>);
  data = inject<PaymentDetailDialogData>(MAT_DIALOG_DATA);

  loading = signal(true);
  error = signal<string | null>(null);
  payment = signal<SubscriptionPayment | null>(null);
  history = signal<TenantSubscriptionHistory | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const [payment, history] = await Promise.all([
        this.subscriptionService.getPaymentById(this.data.paymentId).toPromise(),
        this.subscriptionService.getTenantPaymentHistory(this.data.tenantId).toPromise()
      ]);

      this.payment.set(payment!);
      this.history.set(history!);
    } catch (err) {
      console.error('Error loading payment details:', err);
      this.error.set('Failed to load payment details. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  formatCurrency(amount: number): string {
    return this.subscriptionService.formatCurrency(amount);
  }

  formatDate(date: string): string {
    return this.subscriptionService.formatDate(date);
  }

  getStatusColor(status: string): string {
    return this.subscriptionService.getStatusColor(status as any);
  }

  getTierColor(tier: string): string {
    return this.subscriptionService.getTierColor(tier as any);
  }

  recordPayment(): void {
    // TODO: Open record payment dialog
    console.log('Record payment for:', this.payment()?.id);
    alert('Record payment functionality - to be implemented');
  }

  sendReminder(): void {
    const paymentId = this.payment()?.id;
    if (!paymentId) return;

    this.subscriptionService.sendPaymentReminder(paymentId).subscribe({
      next: (response) => {
        console.log('Reminder sent:', response);
        alert('Payment reminder sent successfully!');
        this.loadData(); // Reload to update reminder date
      },
      error: (err) => {
        console.error('Error sending reminder:', err);
        alert('Failed to send reminder. Please try again.');
      }
    });
  }
}
