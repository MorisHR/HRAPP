import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil, interval } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';

import { RevenueAnalyticsService } from '../../../core/services/revenue-analytics.service';
import {
  RevenueAnalyticsDashboard,
  MrrBreakdownResponse,
  ArrTrackingResponse,
  ChurnRateResponse,
  KeyMetricsResponse
} from '../../../core/models/revenue-analytics.model';
import { LineChartComponent } from '../../../shared/ui/components/line-chart/line-chart';
import { BarChartComponent } from '../../../shared/ui/components/bar-chart/bar-chart';
import { ButtonComponent } from '../../../shared/ui/components/button/button';

@Component({
  selector: 'app-revenue-analytics-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTabsModule,
    MatTooltipModule,
    LineChartComponent,
    BarChartComponent,
    ButtonComponent
  ],
  templateUrl: './revenue-analytics-dashboard.component.html',
  styleUrls: ['./revenue-analytics-dashboard.component.scss']
})
export class RevenueAnalyticsDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  loading = signal(true);
  error = signal<string | null>(null);

  // Data signals
  dashboard = signal<RevenueAnalyticsDashboard | null>(null);
  mrr = signal<MrrBreakdownResponse | null>(null);
  arr = signal<ArrTrackingResponse | null>(null);
  churnRate = signal<ChurnRateResponse | null>(null);
  keyMetrics = signal<KeyMetricsResponse | null>(null);

  // Filter signals
  selectedPeriod = signal<number>(12); // months
  selectedView = signal<number>(0); // tab index

  constructor(private revenueService: RevenueAnalyticsService) {}

  ngOnInit(): void {
    this.loadAllData();

    // Auto-refresh every 5 minutes
    interval(300000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadAllData());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAllData(): void {
    this.loading.set(true);
    this.error.set(null);

    this.revenueService.getDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.dashboard.set(data);
          this.mrr.set(data.mrr);
          this.arr.set(data.arr);
          this.churnRate.set(data.churnRate);
          this.keyMetrics.set(data.keyMetrics);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('Failed to load revenue analytics');
          this.loading.set(false);
          console.error('Error loading revenue analytics:', err);
        }
      });
  }

  onPeriodChange(months: number): void {
    this.selectedPeriod.set(months);
    // Note: Backend doesn't support filtered time periods yet
    // this.loadAllData();
  }

  onViewChange(tabIndex: number): void {
    this.selectedView.set(tabIndex);
  }

  exportData(): void {
    // Generate CSV export from current data
    const dashboard = this.dashboard();
    if (!dashboard) return;

    const csvContent = this.generateCsvExport(dashboard);
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `revenue-analytics-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  }

  private generateCsvExport(dashboard: RevenueAnalyticsDashboard): string {
    let csv = 'Revenue Analytics Report\n';
    csv += `Generated: ${dashboard.generatedAt}\n\n`;

    csv += 'Key Metrics\n';
    csv += `Total MRR,${dashboard.mrr.totalMRR}\n`;
    csv += `Total ARR,${dashboard.arr.currentARR}\n`;
    csv += `ARR Growth Rate,${dashboard.arr.growthRate}%\n`;
    csv += `Churn Rate,${dashboard.churnRate.currentMonthChurnRate}%\n`;
    csv += `Total Active Tenants,${dashboard.mrr.totalActiveTenants}\n\n`;

    csv += 'MRR by Tier\n';
    csv += 'Tier,Tenant Count,MRR,Avg per Tenant\n';
    dashboard.mrr.byTier.forEach(tier => {
      csv += `${tier.tier},${tier.tenantCount},${tier.mrr},${tier.averageRevenuePerTenant}\n`;
    });

    return csv;
  }

  // Chart data transformers
  getArrTrendData() {
    const arr = this.arr();
    if (!arr || !arr.trend || arr.trend.length === 0) {
      return {
        labels: [],
        datasets: [{
          label: 'ARR (MUR)',
          data: [],
          borderColor: 'rgb(99, 102, 241)',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          tension: 0.4
        }]
      };
    }
    return {
      labels: arr.trend.map(t => new Date(t.month).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })),
      datasets: [{
        label: 'ARR (MUR)',
        data: arr.trend.map(t => t.arr),
        borderColor: 'rgb(99, 102, 241)',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        tension: 0.4,
        fill: true
      }]
    };
  }

  getMrrByTierData() {
    const mrr = this.mrr();
    if (!mrr || !mrr.byTier) {
      return {
        labels: [],
        datasets: [{
          label: 'MRR (MUR)',
          data: [],
          backgroundColor: 'rgb(34, 197, 94)'
        }]
      };
    }
    return {
      labels: mrr.byTier.map(t => t.tier),
      datasets: [{
        label: 'MRR (MUR)',
        data: mrr.byTier.map(t => t.mrr),
        backgroundColor: [
          'rgba(99, 102, 241, 0.8)',
          'rgba(59, 130, 246, 0.8)',
          'rgba(34, 197, 94, 0.8)',
          'rgba(234, 179, 8, 0.8)',
          'rgba(249, 115, 22, 0.8)'
        ]
      }]
    };
  }

  getChurnTrendData() {
    const churn = this.churnRate();
    if (!churn || !churn.trend || churn.trend.length === 0) {
      return {
        labels: [],
        datasets: [{
          label: 'Churn Rate (%)',
          data: [],
          borderColor: 'rgb(239, 68, 68)',
          backgroundColor: 'rgba(239, 68, 68, 0.1)',
          tension: 0.4
        }]
      };
    }
    return {
      labels: churn.trend.map(t => new Date(t.month).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })),
      datasets: [{
        label: 'Churn Rate (%)',
        data: churn.trend.map(t => t.churnRate),
        borderColor: 'rgb(239, 68, 68)',
        backgroundColor: 'rgba(239, 68, 68, 0.1)',
        tension: 0.4,
        fill: true
      }]
    };
  }

  getGrowthRate(): string {
    const arr = this.arr();
    if (!arr) return '0.00';
    return arr.growthRate.toFixed(2);
  }

  getGrowthTrend(): 'up' | 'down' | 'neutral' {
    const arr = this.arr();
    if (!arr) return 'neutral';
    if (arr.growthRate > 0) return 'up';
    if (arr.growthRate < 0) return 'down';
    return 'neutral';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(value);
  }

  formatPercent(value: number): string {
    return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`;
  }
}
