import { Component, signal, inject, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

// Custom UI Module with all components
import { UiModule } from '../../../shared/ui/ui.module';

// Chart Components
import { LineChartComponent } from '../../../shared/ui/components/line-chart/line-chart';
import { BarChartComponent } from '../../../shared/ui/components/bar-chart/bar-chart';

// Dashboard Components
import { SystemHealthComponent } from './components/system-health/system-health.component';
import { RecentActivityComponent } from './components/recent-activity/recent-activity.component';
import { CriticalAlertsComponent } from './components/critical-alerts/critical-alerts.component';

// Services
import { AdminDashboardService } from '../../../core/services/admin-dashboard.service';
import { MetricsService } from '../../../core/services/metrics.service';

// Models
import { DashboardStats, TenantGrowthData, RevenueData } from '../../../core/models/dashboard.model';

// Chart.js registration
import {
  Chart,
  ChartData,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js';

Chart.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    UiModule,
    LineChartComponent,
    BarChartComponent,
    SystemHealthComponent,
    RecentActivityComponent,
    CriticalAlertsComponent
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  private dashboardService = inject(AdminDashboardService);
  private metricsService = inject(MetricsService);
  private subscriptions: Subscription[] = [];

  // Signals for reactive state
  stats = signal<DashboardStats | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  // Chart data signals
  tenantGrowthChart = signal<ChartData<'line'> | null>(null);
  revenueChart = signal<ChartData<'bar'> | null>(null);
  chartsLoading = signal(false);

  ngOnInit(): void {
    this.loadDashboardData();
    this.loadChartData();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  private loadDashboardData(): void {
    this.loading.set(true);
    const sub = this.dashboardService.getDashboardStats().subscribe({
      next: (stats) => {
        this.stats.set(stats);
        this.loading.set(false);
        this.error.set(null);
      },
      error: (err) => {
        this.error.set('Failed to load dashboard data');
        this.loading.set(false);
      }
    });
    this.subscriptions.push(sub);
  }

  private loadChartData(): void {
    this.chartsLoading.set(true);

    // Load tenant growth data
    const tenantSub = this.metricsService.getTenantGrowthData(6).subscribe({
      next: (data: TenantGrowthData[]) => {
        this.tenantGrowthChart.set(this.metricsService.tenantGrowthToChartData(data));
      }
    });

    // Load revenue data
    const revenueSub = this.metricsService.getRevenueData(6).subscribe({
      next: (data: RevenueData[]) => {
        this.revenueChart.set(this.metricsService.revenueToChartData(data));
        this.chartsLoading.set(false);
      }
    });

    this.subscriptions.push(tenantSub, revenueSub);
  }

  getTrendIcon(direction?: 'up' | 'down' | 'stable'): string {
    return this.dashboardService.getTrendIcon(direction || 'stable');
  }

  getTrendColor(direction?: 'up' | 'down' | 'stable'): string {
    return this.dashboardService.getTrendColor(direction || 'stable');
  }

  formatCurrency(value: number): string {
    return this.dashboardService.formatCurrency(value);
  }

  formatNumber(value: number): string {
    return this.dashboardService.formatNumber(value);
  }

  refresh(): void {
    this.loadDashboardData();
    this.loadChartData();
  }
}
