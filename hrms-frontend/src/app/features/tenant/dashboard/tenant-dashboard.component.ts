import { Component, signal, inject, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';

// Material imports (keeping for form fields and icons)
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';

// Custom UI Module with all components (includes ProgressSpinner)
import { UiModule } from '../../../shared/ui/ui.module';

import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { DashboardService, DashboardStats, AlertItem, ChartDataPoint, ActivityItem, BirthdayItem } from '../../../core/services/dashboard.service';
import { BaseChartDirective } from 'ng2-charts';
import {
  Chart,
  ChartConfiguration,
  ChartData,
  LinearScale,
  CategoryScale,
  BarController,
  DoughnutController,
  ArcElement,
  BarElement,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js';

// Register Chart.js components
Chart.register(
  LinearScale,
  CategoryScale,
  BarController,
  DoughnutController,
  ArcElement,
  BarElement,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface KpiCard {
  title: string;
  icon: string;
  color: string | ((stats: DashboardStats) => string);
  getValue: (stats: DashboardStats) => number | string;
  suffix: string;
}

@Component({
  selector: 'app-tenant-dashboard',
  standalone: true,
  imports: [
    RouterModule,
    FormsModule,
    // Material imports (keeping for form fields and icons)
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule,
    BaseChartDirective,
    // Custom UI Module (includes ProgressSpinner - replaces MatProgressSpinnerModule)
    UiModule
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

  // Chart data
  departmentChartData: ChartData<'bar'> = { labels: [], datasets: [] };
  growthChartData: ChartData<'line'> = { labels: [], datasets: [] };
  typeChartData: ChartData<'doughnut'> = { labels: [], datasets: [] };

  // Chart options
  departmentChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      title: { display: false }
    }
  };

  growthChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      title: { display: false }
    },
    scales: {
      y: { beginAtZero: true }
    }
  };

  typeChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom' },
      title: { display: false }
    }
  };

  // Helper method to get card color
  getCardColor(card: KpiCard, stats: DashboardStats): string {
    return typeof card.color === 'function' ? card.color(stats) : card.color;
  }

  // Helper method to check if value is positive
  isPositiveGrowth(card: KpiCard, stats: DashboardStats): boolean {
    const value = card.getValue(stats);
    return card.title === 'Growth Rate' && typeof value === 'number' && value >= 0;
  }

  // Helper method to check if value is negative
  isNegativeGrowth(card: KpiCard, stats: DashboardStats): boolean {
    const value = card.getValue(stats);
    return card.title === 'Growth Rate' && typeof value === 'number' && value < 0;
  }

  // KPI card configurations (12+ metrics)
  kpiCards: KpiCard[] = [
    {
      title: 'Total Employees',
      icon: 'people',
      color: 'primary',
      getValue: (stats: DashboardStats) => stats.totalEmployees,
      suffix: ''
    },
    {
      title: 'Present Today',
      icon: 'check_circle',
      color: 'accent',
      getValue: (stats: DashboardStats) => stats.presentToday,
      suffix: ''
    },
    {
      title: 'On Leave',
      icon: 'event_busy',
      color: 'warn',
      getValue: (stats: DashboardStats) => stats.employeesOnLeave,
      suffix: ''
    },
    {
      title: 'New Hires (Month)',
      icon: 'person_add',
      color: 'primary',
      getValue: (stats: DashboardStats) => stats.newHiresThisMonth,
      suffix: ''
    },
    {
      title: 'Pending Leaves',
      icon: 'pending_actions',
      color: 'warn',
      getValue: (stats: DashboardStats) => stats.pendingLeaveRequests,
      suffix: ''
    },
    {
      title: 'Active Payroll',
      icon: 'payments',
      color: 'primary',
      getValue: (stats: DashboardStats) => stats.activePayrollCycles,
      suffix: ''
    },
    {
      title: 'Departments',
      icon: 'corporate_fare',
      color: 'accent',
      getValue: (stats: DashboardStats) => stats.departmentCount,
      suffix: ''
    },
    {
      title: 'Expatriates',
      icon: 'flight',
      color: 'primary',
      getValue: (stats: DashboardStats) => stats.expatriatesCount,
      suffix: ''
    },
    {
      title: 'Avg Tenure',
      icon: 'timeline',
      color: 'accent',
      getValue: (stats: DashboardStats) => stats.averageTenureYears,
      suffix: ' yrs'
    },
    {
      title: 'Expiring Docs',
      icon: 'warning',
      color: 'warn',
      getValue: (stats: DashboardStats) => stats.expiringDocumentsCount,
      suffix: ''
    },
    {
      title: 'Upcoming Birthdays',
      icon: 'cake',
      color: 'accent',
      getValue: (stats: DashboardStats) => stats.upcomingBirthdays,
      suffix: ''
    },
    {
      title: 'Total Payroll',
      icon: 'account_balance',
      color: 'primary',
      getValue: (stats: DashboardStats) => this.formatCurrency(stats.totalPayrollAmount),
      suffix: ''
    },
    {
      title: 'Growth Rate',
      icon: 'trending_up',
      color: (stats: DashboardStats) => stats.employeeGrowthRate >= 0 ? 'accent' : 'warn',
      getValue: (stats: DashboardStats) => stats.employeeGrowthRate,
      suffix: '%'
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

    this.dashboardService.getStats()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.stats.set(stats);
          this.loading.set(false);
          this.error.set(null);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err.message || 'Failed to load dashboard data');
        }
      });
  }

  retry(): void {
    this.loadDashboardData();
  }

  formatCurrency(amount: number): string {
    if (amount >= 1000000) {
      return `${(amount / 1000000).toFixed(1)}M`;
    } else if (amount >= 1000) {
      return `${(amount / 1000).toFixed(0)}K`;
    }
    return amount.toFixed(0);
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
    this.dashboardService.getDepartmentHeadcountChart()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.departmentChartData = {
            labels: data.map(d => d.label),
            datasets: [{
              label: 'Employees',
              data: data.map(d => d.value),
              backgroundColor: '#0288d1',
              borderColor: '#01579b',
              borderWidth: 1
            }]
          };
        },
        error: (err) => {
          console.error('Failed to load department chart:', err);
        }
      });
  }

  loadGrowthChart(): void {
    this.dashboardService.getEmployeeGrowthChart()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.growthChartData = {
            labels: data.map(d => d.label),
            datasets: [{
              label: 'Total Employees',
              data: data.map(d => d.value),
              borderColor: '#0288d1',
              backgroundColor: 'rgba(2, 136, 209, 0.1)',
              fill: true,
              tension: 0.4
            }]
          };
        },
        error: (err) => {
          console.error('Failed to load growth chart:', err);
        }
      });
  }

  loadTypeChart(): void {
    this.dashboardService.getEmployeeTypeDistribution()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.typeChartData = {
            labels: data.map(d => d.label),
            datasets: [{
              data: data.map(d => d.value),
              backgroundColor: ['#0288d1', '#1a237e', '#00838f'],
              borderWidth: 2,
              borderColor: '#ffffff'
            }]
          };
        },
        error: (err) => {
          console.error('Failed to load type distribution chart:', err);
        }
      });
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
