import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDialog } from '@angular/material/dialog';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartType } from 'chart.js';

import { SubscriptionService } from '../../../services/subscription.service';
import {
  SubscriptionOverview,
  RevenueAnalytics,
  SubscriptionPayment,
  UpcomingRenewal,
  PaymentStatus,
  SubscriptionTier
} from '../../../models/subscription.model';
import { PaymentDetailDialogComponent } from './payment-detail-dialog.component';

@Component({
  selector: 'app-subscription-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatBadgeModule,
    BaseChartDirective
  ],
  templateUrl: './subscription-dashboard.component.html',
  styleUrl: './subscription-dashboard.component.scss'
})
export class SubscriptionDashboardComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  private dialog = inject(MatDialog);

  // Signals for reactive state
  loading = signal(true);
  overview = signal<SubscriptionOverview | null>(null);
  analytics = signal<RevenueAnalytics | null>(null);
  pendingPayments = signal<SubscriptionPayment[]>([]);
  overduePayments = signal<SubscriptionPayment[]>([]);
  upcomingRenewals = signal<UpcomingRenewal[]>([]);
  error = signal<string | null>(null);

  // Enums for template
  PaymentStatus = PaymentStatus;
  SubscriptionTier = SubscriptionTier;

  // Table columns
  paymentColumns = ['tenant', 'amount', 'dueDate', 'tier', 'status', 'actions'];
  renewalColumns = ['tenant', 'renewalDate', 'amount', 'tier', 'daysUntil'];

  // Chart configurations
  revenueChartData = signal<ChartConfiguration<'line'>['data'] | null>(null);
  tierChartData = signal<ChartConfiguration<'bar'>['data'] | null>(null);

  revenueChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'top' },
      title: { display: true, text: 'Monthly Revenue Trend' }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => '$' + value.toLocaleString()
        }
      }
    }
  };

  tierChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'top' },
      title: { display: true, text: 'Subscription Tier Distribution' }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => '$' + value.toLocaleString()
        }
      }
    }
  };

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private async loadDashboardData(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      // Load all data in parallel
      const [overview, analytics, pending, overdue, renewals] = await Promise.all([
        this.subscriptionService.getSubscriptionOverview().toPromise(),
        this.subscriptionService.getRevenueAnalytics().toPromise(),
        this.subscriptionService.getPendingPayments().toPromise(),
        this.subscriptionService.getOverduePayments().toPromise(),
        this.subscriptionService.getUpcomingRenewals(30).toPromise()
      ]);

      this.overview.set(overview!);
      this.analytics.set(analytics!);
      this.pendingPayments.set(pending!);
      this.overduePayments.set(overdue!);
      this.upcomingRenewals.set(renewals!);

      // Setup charts
      this.setupRevenueChart(analytics!);
      this.setupTierChart(analytics!);
    } catch (err) {
      console.error('Error loading dashboard data:', err);
      this.error.set('Failed to load dashboard data. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }

  private setupRevenueChart(analytics: RevenueAnalytics): void {
    const labels = analytics.monthlyRevenueData.map(d => `${d.month} ${d.year}`);
    const revenueData = analytics.monthlyRevenueData.map(d => d.revenue);
    const subscriptionData = analytics.monthlyRevenueData.map(d => d.subscriptionCount);

    this.revenueChartData.set({
      labels,
      datasets: [
        {
          label: 'Monthly Revenue ($)',
          data: revenueData,
          borderColor: 'rgb(75, 192, 192)',
          backgroundColor: 'rgba(75, 192, 192, 0.2)',
          tension: 0.4,
          yAxisID: 'y'
        },
        {
          label: 'Active Subscriptions',
          data: subscriptionData,
          borderColor: 'rgb(153, 102, 255)',
          backgroundColor: 'rgba(153, 102, 255, 0.2)',
          tension: 0.4,
          yAxisID: 'y1'
        }
      ]
    });
  }

  private setupTierChart(analytics: RevenueAnalytics): void {
    const labels = analytics.tierDistribution.map(d => d.tier);
    const counts = analytics.tierDistribution.map(d => d.count);
    const revenues = analytics.tierDistribution.map(d => d.revenue);

    this.tierChartData.set({
      labels,
      datasets: [
        {
          label: 'Subscriber Count',
          data: counts,
          backgroundColor: 'rgba(54, 162, 235, 0.6)',
          borderColor: 'rgba(54, 162, 235, 1)',
          borderWidth: 1,
          yAxisID: 'y'
        },
        {
          label: 'Revenue ($)',
          data: revenues,
          backgroundColor: 'rgba(255, 99, 132, 0.6)',
          borderColor: 'rgba(255, 99, 132, 1)',
          borderWidth: 1,
          yAxisID: 'y1'
        }
      ]
    });
  }

  sendReminder(paymentId: number): void {
    this.subscriptionService.sendPaymentReminder(paymentId).subscribe({
      next: (response) => {
        console.log('Reminder sent:', response);
        alert('Payment reminder sent successfully!');
      },
      error: (err) => {
        console.error('Error sending reminder:', err);
        alert('Failed to send reminder. Please try again.');
      }
    });
  }

  recordPayment(paymentId: number): void {
    // TODO: Open dialog to record payment
    console.log('Record payment for:', paymentId);
    alert('Record payment dialog - to be implemented');
  }

  viewPaymentDetails(payment: SubscriptionPayment): void {
    this.dialog.open(PaymentDetailDialogComponent, {
      width: '600px',
      maxWidth: '90vw',
      data: {
        paymentId: payment.id,
        tenantId: payment.tenantId
      }
    });
  }

  formatCurrency(amount: number): string {
    return this.subscriptionService.formatCurrency(amount);
  }

  formatDate(date: string): string {
    return this.subscriptionService.formatDate(date);
  }

  formatRelativeDate(date: string): string {
    return this.subscriptionService.formatRelativeDate(date);
  }

  getStatusColor(status: PaymentStatus): string {
    return this.subscriptionService.getStatusColor(status);
  }

  getTierColor(tier: SubscriptionTier): string {
    return this.subscriptionService.getTierColor(tier);
  }

  getDaysOverdue(dueDate: string): number {
    return this.subscriptionService.getDaysOverdue(dueDate);
  }

  refresh(): void {
    this.loadDashboardData();
  }
}
