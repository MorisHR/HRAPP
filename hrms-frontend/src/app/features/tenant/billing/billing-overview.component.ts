import { Component, OnInit, signal, inject, computed } from '@angular/core';

import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Chip, ChipColor } from '@app/shared/ui';
import { UiModule } from '../../../shared/ui/ui.module';
import { ToastService, Divider, TooltipDirective } from '../../../shared/ui';
import { TableComponent, TableColumn, TableColumnDirective } from '../../../shared/ui';
import { BillingService, PaymentHistory } from '../../../core/services/billing.service';
import { PaymentDetailDialogComponent } from './payment-detail-dialog.component';
import { TierUpgradeDialogComponent } from './tier-upgrade-dialog.component';

interface TenantSubscription {
  tenantId: string;
  companyName: string;
  subdomain: string;
  tier: string;
  employeeLimit: number;
  currentEmployeeCount: number;
  yearlyPriceMUR: number;
  subscriptionStartDate: string;
  subscriptionEndDate: string;
  status: string;
  daysUntilExpiry: number;
  hasOverduePayments: boolean;
}

interface SubscriptionPayment {
  id: string;
  tenantId: string;
  periodStart: string;
  periodEnd: string;
  amountMUR: number;
  dueDate: string;
  paidDate: string | null;
  status: string;
  invoiceNumber: string | null;
  paymentReference: string | null;
  notes: string | null;
}

@Component({
  selector: 'app-billing-overview',
  standalone: true,
  imports: [
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    Chip,
    TooltipDirective,
    UiModule,
    Divider,
    TableComponent,
    TableColumnDirective
],
  templateUrl: './billing-overview.component.html',
  styleUrl: './billing-overview.component.scss'
})
export class BillingOverviewComponent implements OnInit {
  private toastService = inject(ToastService);
  private billingService = inject(BillingService);

  // Signals for reactive state
  subscription = signal<TenantSubscription | null>(null);
  payments = signal<SubscriptionPayment[]>([]);
  paymentHistoryRaw = signal<PaymentHistory[]>([]); // Store raw payment history for dialogs
  loading = signal<boolean>(true);
  error = signal<string | null>(null);
  retryCount = signal<number>(0);
  maxRetries = 3;

  // Computed values
  outstandingAmount = computed(() => {
    const paymentsList = this.payments();
    return paymentsList
      .filter(p => p.status === 'Pending' || p.status === 'Overdue')
      .reduce((sum, p) => sum + p.amountMUR, 0);
  });

  nextPaymentDue = computed(() => {
    const paymentsList = this.payments();
    const pending = paymentsList
      .filter(p => p.status === 'Pending' || p.status === 'Overdue')
      .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime());
    return pending.length > 0 ? pending[0] : null;
  });

  getSubscriptionStatusColor(status: string): ChipColor {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Trial':
        return 'primary';
      case 'Suspended':
        return 'warning';
      case 'Expired':
        return 'error';
      default:
        return 'neutral';
    }
  }

  expiryWarningClass = computed(() => {
    const sub = this.subscription();
    if (!sub) return '';

    if (sub.daysUntilExpiry <= 7) return 'warning-critical';
    if (sub.daysUntilExpiry <= 30) return 'warning-high';
    return '';
  });

  // Table columns
  columns: TableColumn[] = [
    { key: 'period', label: 'Period' },
    { key: 'dueDate', label: 'Due Date' },
    { key: 'amount', label: 'Amount' },
    { key: 'status', label: 'Status' },
    { key: 'paidDate', label: 'Paid Date' },
    { key: 'actions', label: 'Actions' }
  ];

  ngOnInit(): void {
    this.loadSubscriptionData();
  }

  private loadSubscriptionData(): void {
    this.loading.set(true);
    this.error.set(null);

    // PRODUCTION-READY: Call real backend API
    // Backend: GET /api/tenant/subscription (TenantBillingController)
    // Security: Requires authentication with Admin or HR role
    this.billingService.getSubscription().subscribe({
      next: (data) => {
        // Map backend DTO to frontend interface
        this.subscription.set({
          tenantId: data.subscription.tenantId,
          companyName: '', // TODO: Get from tenant context
          subdomain: '', // TODO: Get from tenant context
          tier: data.subscription.tier,
          employeeLimit: this.getEmployeeLimitFromTier(data.subscription.tier),
          currentEmployeeCount: 0, // TODO: Get from backend
          yearlyPriceMUR: data.subscription.monthlyPrice * 12,
          subscriptionStartDate: data.subscription.currentPeriodStart || '',
          subscriptionEndDate: data.subscription.currentPeriodEnd || '',
          status: data.subscription.status,
          daysUntilExpiry: data.subscription.daysUntilExpiry || 0,
          hasOverduePayments: data.payments.some(p => p.isOverdue)
        });

        // Store raw payment history for dialogs
        this.paymentHistoryRaw.set(data.payments);

        // Map payment history for display
        this.payments.set(data.payments.map(p => ({
          id: p.id,
          tenantId: data.subscription.tenantId,
          periodStart: p.periodStartDate,
          periodEnd: p.periodEndDate,
          amountMUR: p.amount,
          dueDate: p.dueDate,
          paidDate: p.paidDate,
          status: p.status,
          invoiceNumber: p.invoiceNumber,
          paymentReference: null, // TODO: Add to backend if needed
          notes: null
        })));

        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading subscription data:', err);

        // Handle specific error cases
        let errorMessage = 'Failed to load subscription data. Please try again later.';
        let shouldRetry = false;

        if (err.status === 0) {
          // Network error
          errorMessage = 'Network error. Please check your internet connection.';
          shouldRetry = true;
        } else if (err.status === 401) {
          errorMessage = 'Authentication required. Please log in again.';
        } else if (err.status === 403) {
          errorMessage = 'Access denied. This feature requires Admin or HR role.';
        } else if (err.status === 404) {
          errorMessage = 'Subscription not found for your tenant.';
        } else if (err.status === 500) {
          errorMessage = 'Server error. Please try again later.';
          shouldRetry = true;
        } else if (err.status === 503) {
          errorMessage = 'Service temporarily unavailable. Please try again.';
          shouldRetry = true;
        }

        this.error.set(errorMessage);
        this.loading.set(false);

        // Show error with retry option for transient errors
        if (shouldRetry && this.retryCount() < this.maxRetries) {
          this.toastService.show({
            message: errorMessage,
            type: 'error',
            duration: 0, // Keep open for retry errors
            action: {
              label: 'Retry',
              callback: () => {
                this.retryLoadData();
              }
            }
          });
        } else {
          this.toastService.error(errorMessage, 5000);
        }
      }
    });
  }

  /**
   * Helper method to get employee limit from tier name
   */
  private getEmployeeLimitFromTier(tier: string): number {
    const tierLimits: { [key: string]: number } = {
      'Tier1': 25,
      'Tier2': 50,
      'Tier3': 100,
      'Tier4': 250,
      'Tier5': 500,
      'Tier6': 1000,
      'Tier7': 2500,
      'Tier8': 5000
    };
    return tierLimits[tier] || 0;
  }

  refresh(): void {
    this.retryCount.set(0); // Reset retry count on manual refresh
    this.loadSubscriptionData();
  }

  /**
   * Retry loading subscription data with exponential backoff
   */
  private retryLoadData(): void {
    const currentRetry = this.retryCount();
    if (currentRetry < this.maxRetries) {
      const delay = Math.pow(2, currentRetry) * 1000; // Exponential backoff: 1s, 2s, 4s
      this.retryCount.set(currentRetry + 1);

      this.toastService.info(
        `Retrying... (Attempt ${currentRetry + 1}/${this.maxRetries})`,
        delay
      );

      setTimeout(() => {
        this.loadSubscriptionData();
      }, delay);
    }
  }

  downloadInvoice(payment: SubscriptionPayment): void {
    if (!payment.invoiceNumber) {
      this.toastService.warning('Invoice not available for this payment', 3000);
      return;
    }

    // Call BillingService to download invoice
    // API endpoint: GET /api/tenant/subscription/invoices/{paymentId}/download
    this.toastService.info('Downloading invoice...', 2000);

    this.billingService.downloadInvoice(payment.id).subscribe({
      next: (blob) => {
        // Create a download link for the PDF blob
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Invoice_${payment.invoiceNumber}_${payment.periodStart}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);

        this.toastService.success('Invoice downloaded successfully', 3000);
      },
      error: (err) => {
        console.error('Error downloading invoice:', err);

        let errorMessage = 'Failed to download invoice. Please try again later.';

        if (err.status === 401) {
          errorMessage = 'Authentication required. Please log in again.';
        } else if (err.status === 403) {
          errorMessage = 'Access denied. You do not have permission to download invoices.';
        } else if (err.status === 404) {
          errorMessage = 'Invoice not found. It may not have been generated yet.';
        } else if (err.status === 501) {
          errorMessage = 'Invoice download feature not yet implemented on server.';
        }

        this.toastService.error(errorMessage, 5000);
      }
    });
  }

  contactSupport(): void {
    // TODO: Open support dialog or navigate to support page
    this.toastService.info('Opening support contact...', 3000);
  }

  upgradeTier(): void {
    // TODO: Implement tier upgrade dialog
    this.toastService.info('Tier upgrade feature not yet implemented', 3000);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-MU', {
      style: 'currency',
      currency: 'MUR',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-MU', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
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

  getPeriodLabel(payment: SubscriptionPayment): string {
    const start = this.formatDate(payment.periodStart);
    const end = this.formatDate(payment.periodEnd);
    return `${start} - ${end}`;
  }

  /**
   * Open payment detail dialog
   */
  viewPaymentDetails(payment: SubscriptionPayment): void {
    // TODO: Implement payment detail dialog
    this.toastService.info('Payment details view not yet implemented', 3000);
  }
}
