import { Component, inject, signal, OnInit } from '@angular/core';

import { DialogRef, ChipColor } from '../../../shared/ui';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UiModule } from '../../../shared/ui/ui.module';
import { Divider, Chip, List, ListItem } from '../../../shared/ui';

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
    MatButtonModule,
    MatIconModule,
    UiModule,
    Divider,
    List,
    ListItem,
    Chip
],
  template: `
    <div class="payment-detail-dialog">
      <div class="dialog-title">
        <h2>Payment Details</h2>
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

        @if (payment() && history()) {
          <div class="payment-details">
            <!-- Current Payment Info -->
            <section class="detail-section">
              <h3>Current Payment</h3>
              <app-list>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Payment ID</span>
                    <span class="item-line">{{ payment()!.id }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Tenant</span>
                    <span class="item-line">{{ payment()!.tenantName }} ({{ payment()!.tenantSubdomain }})</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Amount</span>
                    <span class="item-line amount">{{ formatCurrency(payment()!.amount) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Due Date</span>
                    <span class="item-line">{{ formatDate(payment()!.dueDate) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Status</span>
                    <span class="item-line">
                      <app-chip [label]="payment()!.status" [color]="getStatusColor(payment()!.status)" />
                    </span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Subscription Tier</span>
                    <span class="item-line">
                      <app-chip [label]="payment()!.subscriptionTier" [color]="getTierColor(payment()!.subscriptionTier)" />
                    </span>
                  </div>
                </app-list-item>
                @if (payment()!.paymentDate) {
                  <app-list-item>
                    <div class="list-item-content">
                      <span class="item-title">Payment Date</span>
                      <span class="item-line">{{ formatDate(payment()!.paymentDate!) }}</span>
                    </div>
                  </app-list-item>
                }
                @if (payment()!.reminderSentDate) {
                  <app-list-item>
                    <div class="list-item-content">
                      <span class="item-title">Last Reminder Sent</span>
                      <span class="item-line">{{ formatDate(payment()!.reminderSentDate!) }}</span>
                    </div>
                  </app-list-item>
                }
              </app-list>
            </section>

            <app-divider />

            <!-- Tenant Subscription Summary -->
            <section class="detail-section">
              <h3>Subscription Summary</h3>
              <app-list>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Current Tier</span>
                    <span class="item-line">
                      <app-chip [label]="history()!.currentTier" [color]="getTierColor(history()!.currentTier)" />
                    </span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Subscription Start</span>
                    <span class="item-line">{{ formatDate(history()!.subscriptionStartDate) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Total Payments</span>
                    <span class="item-line">{{ history()!.payments.length }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Total Paid</span>
                    <span class="item-line amount success">{{ formatCurrency(history()!.totalPaid) }}</span>
                  </div>
                </app-list-item>
                <app-list-item>
                  <div class="list-item-content">
                    <span class="item-title">Total Overdue</span>
                    <span class="item-line amount error">{{ formatCurrency(history()!.totalOverdue) }}</span>
                  </div>
                </app-list-item>
              </app-list>
            </section>

            <app-divider />

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
                        <app-chip [label]="historicalPayment.status" [color]="getStatusColor(historicalPayment.status)" class="history-status" />
                      </div>
                      <div class="history-details">
                        <span class="history-amount">{{ formatCurrency(historicalPayment.amount) }}</span>
                        @if (historicalPayment.paymentDate) {
                          <span class="history-paid">Paid: {{ formatDate(historicalPayment.paymentDate) }}</span>
                        }
                      </div>
                      @if (historicalPayment.id === payment()!.id) {
                        <div class="current-badge">
                          <app-chip label="Current" color="primary" />
                        </div>
                      }
                    </div>
                  }
                </div>
              }
            </section>
          </div>
        }
      </div>

      <div class="dialog-actions">
        <button mat-button (click)="dialogRef.close()">Close</button>
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
      </div>
    </div>
  `,
  styles: [`
    .payment-detail-dialog {
      width: 600px;
      max-width: 90vw;

      .dialog-title {
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
              }

              .item-line {
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
                  border-color: var(--color-neutral-900);
                  background-color: var(--color-neutral-100);
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

    // Dark mode
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

        .dialog-actions {
          border-top-color: rgba(255, 255, 255, 0.12);
        }
      }
    }
  `]
})
export class PaymentDetailDialogComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  public dialogRef = inject(DialogRef<PaymentDetailDialogComponent, void>);

  get dialogData(): PaymentDetailDialogData {
    return this.dialogRef.data;
  }

  get data(): PaymentDetailDialogData {
    return this.dialogData;
  }

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

  getStatusColor(status: string): ChipColor {
    return this.subscriptionService.getStatusChipColor(status as any);
  }

  getTierColor(tier: string): ChipColor {
    return this.subscriptionService.getTierChipColor(tier as any);
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
