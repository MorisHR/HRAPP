import { Component, signal, inject, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';

// Material imports
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

// Custom UI Module with all components
import { UiModule } from '../../../shared/ui/ui.module';

// Fortune 500 Premium Components
import { MetricCardComponent } from '../../../shared/ui/components/metric-card/metric-card.component';
import { LineChartComponent, LineChartData } from '../../../shared/ui/components/charts/line-chart.component';
import { BarChartComponent } from '../../../shared/ui/components/charts/bar-chart.component';
import { DonutChartComponent, DonutChartData } from '../../../shared/ui/components/charts/donut-chart.component';

import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { DashboardService, DashboardStats, AlertItem, ChartDataPoint, ActivityItem, BirthdayItem } from '../../../core/services/dashboard.service';

// Fortune 500 Metric Card Interface
interface MetricConfig {
  title: string;
  subtitle?: string;
  icon: string;
  getValue: (stats: DashboardStats) => number | string;
  getTrend?: (stats: DashboardStats) => number;
  getSparklineData?: (stats: DashboardStats) => number[];
  size: 'small' | 'medium' | 'large' | 'hero';
  theme: 'default' | 'primary' | 'success' | 'warning' | 'error';
  context?: (stats: DashboardStats) => string;
}

@Component({
  selector: 'app-tenant-dashboard',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    // Material imports
    MatSelectModule,
    MatFormFieldModule,
    MatButtonModule,
    MatTooltipModule,
    // Custom UI Module
    UiModule,
    // Fortune 500 Premium Components
    MetricCardComponent,
    LineChartComponent,
    BarChartComponent,
    DonutChartComponent
  ],
  templateUrl: './tenant-dashboard.component.html',
  styleUrl: './tenant-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantDashboardComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);
  private dashboardService = inject(DashboardService);
  private destroy$ = new Subject<void>();

  user = this.authService.user;
  isDark = this.themeService.isDark;

  // State signals
  stats = signal<DashboardStats | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  alerts = signal<AlertItem[]>([]);
  alertsLoading = signal<boolean>(false);
  activities = signal<ActivityItem[]>([]);
  activitiesLoading = signal<boolean>(false);
  birthdays = signal<BirthdayItem[]>([]);
  birthdaysLoading = signal<boolean>(false);

  // Filter state
  selectedTimePeriod = signal<string>('all');
  selectedDepartment = signal<string>('all');
  departments = signal<string[]>([]);

  // Filter options
  timePeriodOptions = [
    { value: 'all', label: 'All Time' },
    { value: 'today', label: 'Today' },
    { value: 'week', label: 'This Week' },
    { value: 'month', label: 'This Month' },
    { value: 'year', label: 'This Year' }
  ];

  // ECharts data for Fortune 500 components
  departmentChartLabels = signal<string[]>([]);
  departmentChartData = signal<number[]>([]);

  growthChartLabels = signal<string[]>([]);
  growthChartSeries = signal<LineChartData[]>([]);

  typeChartData = signal<DonutChartData[]>([]);

  // Fortune 500 Metric Card Configurations (Visual Hierarchy)
  // Hero metrics (top 3) - Primary KPIs with maximum visibility
  heroMetrics: MetricConfig[] = [
    {
      title: 'Total Employees',
      subtitle: 'Active Workforce',
      icon: 'people',
      getValue: (stats) => stats.totalEmployees,
      getTrend: (stats) => stats.employeeGrowthRate,
      getSparklineData: () => [1180, 1195, 1210, 1225, 1232, 1240, 1245], // Mock 7-day trend
      size: 'hero',
      theme: 'primary',
      context: (stats) => `${stats.newHiresThisMonth} new hires this month`
    },
    {
      title: 'Present Today',
      subtitle: 'Attendance',
      icon: 'check_circle',
      getValue: (stats) => stats.presentToday,
      getTrend: (stats) => 2.1, // Mock trend: +2.1% vs yesterday
      getSparklineData: () => [1165, 1170, 1178, 1182, 1185, 1187, 1189], // Mock 7-day attendance
      size: 'hero',
      theme: 'success',
      context: (stats) => `${((stats.presentToday / stats.totalEmployees) * 100).toFixed(1)}% attendance rate`
    },
    {
      title: 'On Leave',
      subtitle: 'Current Absences',
      icon: 'event_busy',
      getValue: (stats) => stats.employeesOnLeave,
      getTrend: () => -12.5, // Mock trend: -12.5% vs last week
      getSparklineData: () => [64, 61, 58, 57, 56, 56, 56], // Mock 7-day leave trend
      size: 'hero',
      theme: 'warning',
      context: (stats) => `${stats.pendingLeaveRequests} pending approvals`
    }
  ];

  // Primary metrics - Important KPIs with supporting context
  primaryMetrics: MetricConfig[] = [
    {
      title: 'New Hires',
      subtitle: 'This Month',
      icon: 'person_add',
      getValue: (stats) => stats.newHiresThisMonth,
      getTrend: () => 15.3, // Mock trend
      size: 'large',
      theme: 'default',
      context: () => '8 more planned this quarter'
    },
    {
      title: 'Pending Leaves',
      subtitle: 'Requires Action',
      icon: 'pending_actions',
      getValue: (stats) => stats.pendingLeaveRequests,
      getTrend: () => -22.4, // Mock trend: down is good
      size: 'large',
      theme: 'default',
      context: () => 'Avg approval time: 24h'
    },
    {
      title: 'Total Payroll',
      subtitle: 'Monthly Cost',
      icon: 'account_balance',
      getValue: (stats) => this.formatCurrency(stats.totalPayrollAmount),
      getTrend: () => 3.2, // Mock trend
      size: 'large',
      theme: 'default',
      context: (stats) => `${stats.activePayrollCycles} active cycles`
    }
  ];

  // Supporting metrics - Tertiary data points
  supportingMetrics: MetricConfig[] = [
    {
      title: 'Departments',
      icon: 'corporate_fare',
      getValue: (stats) => stats.departmentCount,
      size: 'medium',
      theme: 'default'
    },
    {
      title: 'Expatriates',
      icon: 'flight',
      getValue: (stats) => stats.expatriatesCount,
      getTrend: () => 5.2,
      size: 'medium',
      theme: 'default'
    },
    {
      title: 'Avg Tenure',
      subtitle: 'Years',
      icon: 'timeline',
      getValue: (stats) => stats.averageTenureYears.toFixed(1),
      size: 'medium',
      theme: 'default'
    },
    {
      title: 'Expiring Docs',
      subtitle: 'Next 30 Days',
      icon: 'warning',
      getValue: (stats: DashboardStats) => stats.expiringDocumentsCount,
      size: 'medium',
      theme: 'warning'
    },
    {
      title: 'Upcoming Birthdays',
      subtitle: 'Next 30 Days',
      icon: 'cake',
      getValue: (stats) => stats.upcomingBirthdays,
      size: 'medium',
      theme: 'default'
    },
    {
      title: 'Active Payroll',
      subtitle: 'Cycles',
      icon: 'payments',
      getValue: (stats) => stats.activePayrollCycles,
      size: 'medium',
      theme: 'default'
    }
  ];

  ngOnInit(): void {
    this.loadDashboardData();
    this.loadAlerts();
    this.loadDepartments();
    this.loadCharts();
    this.loadActivities();
    this.loadBirthdays();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadDashboardData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Use realistic mock data directly (API endpoints not fully implemented)
    this.stats.set({
      totalEmployees: 1245,
      presentToday: 1189,
      employeesOnLeave: 56,
      newHiresThisMonth: 27,
      employeeGrowthRate: 2.2,
      pendingLeaveRequests: 14,
      activePayrollCycles: 2,
      totalPayrollAmount: 2847500, // 2.85M MUR
      expiringDocumentsCount: 8,
      departmentCount: 12,
      expatriatesCount: 34,
      averageTenureYears: 3.8,
      upcomingBirthdays: 15,
      generatedAt: new Date()
    });
    this.loading.set(false);
    this.error.set(null);
  }

  retry(): void {
    this.loadDashboardData();
  }

  formatCurrency(amount: number): string {
    if (amount >= 1000000) {
      const value = (amount / 1000000).toFixed(2);
      return `${value}M MUR`;
    } else if (amount >= 1000) {
      const value = (amount / 1000).toFixed(1);
      return `${value}K MUR`;
    }
    return `${amount.toLocaleString('en-US')} MUR`;
  }

  loadAlerts(): void {
    this.alertsLoading.set(true);
    this.dashboardService.getAlerts()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (alerts) => {
          this.alerts.set(alerts);
          this.alertsLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load alerts:', err);
          this.alertsLoading.set(false);
        }
      });
  }

  loadDepartments(): void {
    this.dashboardService.getDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (departments) => {
          this.departments.set(departments);
        },
        error: (err) => {
          console.error('Failed to load departments:', err);
        }
      });
  }

  onTimePeriodChange(period: string): void {
    this.selectedTimePeriod.set(period);
    this.loadDashboardData();
  }

  onDepartmentChange(department: string): void {
    this.selectedDepartment.set(department);
    this.loadDashboardData();
  }

  getSeverityColor(severity: string): string {
    switch (severity) {
      case 'critical': return 'warn';
      case 'high': return 'accent';
      default: return 'primary';
    }
  }

  loadCharts(): void {
    this.loadDepartmentChart();
    this.loadGrowthChart();
    this.loadTypeChart();
  }

  loadDepartmentChart(): void {
    // Use realistic mock data directly (API endpoints not implemented yet)
    this.departmentChartLabels.set([
      'Engineering', 'Sales', 'Operations', 'Finance',
      'HR', 'Marketing', 'Customer Support', 'IT',
      'Legal', 'R&D', 'Admin', 'Logistics'
    ]);
    this.departmentChartData.set([
      287, 198, 156, 89,
      54, 78, 112, 67,
      23, 94, 45, 42
    ]);
  }

  loadGrowthChart(): void {
    // Use realistic 12-month growth story with natural variation
    this.growthChartLabels.set([
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ]);
    this.growthChartSeries.set([{
      name: 'Total Employees',
      // Realistic growth: Some months flat, some growth, seasonal patterns
      // Started at 1,125, grew to 1,245 (+10.7% YoY) with realistic variation
      data: [1125, 1130, 1145, 1158, 1180, 1195, 1198, 1205, 1215, 1222, 1235, 1245],
      color: '#0F62FE'
    }]);
  }

  loadTypeChart(): void {
    // Use realistic employee type distribution
    this.typeChartData.set([
      { name: 'Full-Time', value: 1089 },      // 87.5% - majority
      { name: 'Contract', value: 112 },        // 9.0% - project-based
      { name: 'Expatriate', value: 34 },       // 2.7% - international
      { name: 'Part-Time', value: 10 }         // 0.8% - minimal
    ]);
  }

  loadActivities(): void {
    this.activitiesLoading.set(true);
    this.dashboardService.getRecentActivity(10)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (activities) => {
          this.activities.set(activities);
          this.activitiesLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load activities:', err);
          this.activitiesLoading.set(false);
        }
      });
  }

  getTimeAgo(timestamp: Date): string {
    const now = new Date();
    const activityDate = new Date(timestamp);
    const seconds = Math.floor((now.getTime() - activityDate.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return activityDate.toLocaleDateString();
  }

  loadBirthdays(): void {
    this.birthdaysLoading.set(true);
    this.dashboardService.getUpcomingBirthdays(30)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (birthdays) => {
          this.birthdays.set(birthdays);
          this.birthdaysLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load birthdays:', err);
          this.birthdaysLoading.set(false);
        }
      });
  }
}
