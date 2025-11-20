import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatBadgeModule } from '@angular/material/badge';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartType } from 'chart.js';
import { SecurityAnalyticsService } from '../../../core/services/security-analytics.service';
import {
  SecurityDashboardAnalytics,
  FailedLoginAnalytics,
  BruteForceStatistics,
  IpBlacklist,
  SessionManagement,
  MfaCompliance,
  PasswordCompliance
} from '../../../core/models/security-analytics.models';
import { interval, Subscription, firstValueFrom } from 'rxjs';

/**
 * FORTUNE 500 SECURITY ANALYTICS DASHBOARD
 * Main entry point for comprehensive security monitoring
 *
 * FEATURES:
 * - Real-time security score with trend analysis
 * - Failed login analytics with time series charts
 * - Brute force attack monitoring
 * - IP blacklist management overview
 * - Session management and forced logout capability
 * - MFA compliance tracking (NIST 800-63B)
 * - Password strength compliance monitoring
 * - Auto-refresh every 60 seconds
 *
 * PATTERNS: AWS GuardDuty, Azure Sentinel, Splunk ES, Datadog Security Monitoring
 * COMPLIANCE: PCI-DSS, NIST 800-53, ISO 27001, SOC 2, GDPR, SOX
 */
@Component({
  selector: 'app-security-analytics-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatTooltipModule,
    MatChipsModule,
    MatTableModule,
    MatBadgeModule,
    BaseChartDirective
  ],
  templateUrl: './security-analytics-dashboard.component.html',
  styleUrls: ['./security-analytics-dashboard.component.scss']
})
export class SecurityAnalyticsDashboardComponent implements OnInit, OnDestroy {
  private securityAnalyticsService = inject(SecurityAnalyticsService);

  // Loading states
  loading = this.securityAnalyticsService.loading;
  loadingSignal = signal<boolean>(false);

  // Dashboard data signals
  dashboardData = signal<SecurityDashboardAnalytics | null>(null);
  failedLoginData = signal<FailedLoginAnalytics | null>(null);
  bruteForceData = signal<BruteForceStatistics | null>(null);
  ipBlacklistData = signal<IpBlacklist | null>(null);
  sessionData = signal<SessionManagement | null>(null);
  mfaComplianceData = signal<MfaCompliance | null>(null);
  passwordComplianceData = signal<PasswordCompliance | null>(null);

  // Error handling
  error = signal<string | null>(null);

  // Auto-refresh subscription
  private refreshSubscription?: Subscription;
  private readonly REFRESH_INTERVAL_MS = 60000; // 60 seconds

  // Computed values
  securityScoreColor = computed(() => {
    const score = this.dashboardData()?.overallSecurityScore ?? 0;
    if (score >= 90) return 'success';
    if (score >= 75) return 'primary';
    if (score >= 60) return 'warn';
    return 'danger';
  });

  securityTrendIcon = computed(() => {
    const trend = this.dashboardData()?.securityTrend;
    if (trend === 'improving') return 'trending_up';
    if (trend === 'declining') return 'trending_down';
    return 'trending_flat';
  });

  securityTrendColor = computed(() => {
    const trend = this.dashboardData()?.securityTrend;
    if (trend === 'improving') return 'success';
    if (trend === 'declining') return 'danger';
    return 'neutral';
  });

  // Chart configurations
  failedLoginsChartData: ChartConfiguration<'line'>['data'] | null = null;
  failedLoginsChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'top' },
      tooltip: { mode: 'index', intersect: false }
    },
    scales: {
      y: { beginAtZero: true, ticks: { stepSize: 1 } }
    }
  };

  bruteForceChartData: ChartConfiguration<'bar'>['data'] | null = null;
  bruteForceChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: { mode: 'index', intersect: false }
    },
    scales: {
      y: { beginAtZero: true, ticks: { stepSize: 1 } }
    }
  };

  mfaComplianceChartData: ChartConfiguration<'doughnut'>['data'] | null = null;
  mfaComplianceChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'bottom' },
      tooltip: { enabled: true }
    }
  };

  passwordStrengthChartData: ChartConfiguration<'pie'>['data'] | null = null;
  passwordStrengthChartOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true, position: 'bottom' },
      tooltip: { enabled: true }
    }
  };

  // Table columns
  criticalActivityColumns: string[] = ['timestamp', 'severity', 'eventType', 'description', 'ipAddress'];
  topFailureIpsColumns: string[] = ['ipAddress', 'failureCount', 'isBlacklisted', 'uniqueUsersTargeted', 'actions'];
  atRiskTenantsColumns: string[] = ['tenantSubdomain', 'riskScore', 'issues', 'failedLogins', 'suspiciousSessions'];

  // Expose Math for template
  Math = Math;

  ngOnInit(): void {
    this.loadDashboardData();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  /**
   * Load comprehensive security dashboard data
   * Fetches all security metrics in one call using backend parallel execution
   */
  async loadDashboardData(): Promise<void> {
    this.loadingSignal.set(true);
    this.error.set(null);

    try {
      // Comprehensive dashboard (aggregates all metrics)
      const dashboard = await firstValueFrom(
        this.securityAnalyticsService.getSecurityDashboard()
      );
      this.dashboardData.set(dashboard);

      // Load detailed component data in parallel
      await Promise.all([
        this.loadFailedLoginAnalytics(),
        this.loadBruteForceStatistics(),
        this.loadIpBlacklist(),
        this.loadSessionManagement(),
        this.loadMfaCompliance(),
        this.loadPasswordCompliance()
      ]);

      this.loadingSignal.set(false);
    } catch (err: any) {
      this.error.set(err.message || 'Failed to load security dashboard');
      this.loadingSignal.set(false);
      console.error('Dashboard load error:', err);
    }
  }

  /**
   * Load failed login analytics with time series chart
   */
  private async loadFailedLoginAnalytics(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getFailedLoginAnalytics()
      );
      this.failedLoginData.set(data);

      // Build Chart.js data for failed logins time series
      if (data.timeSeriesData && data.timeSeriesData.length > 0) {
        this.failedLoginsChartData = {
          labels: data.timeSeriesData.map(d => d.label),
          datasets: [
            {
              label: 'Failed Login Attempts',
              data: data.timeSeriesData.map(d => d.count),
              borderColor: '#f44336',
              backgroundColor: 'rgba(244, 67, 54, 0.1)',
              tension: 0.4,
              fill: true
            }
          ]
        };
      }
    } catch (err) {
      console.error('Failed to load failed login analytics:', err);
    }
  }

  /**
   * Load brute force attack statistics with hourly distribution chart
   */
  private async loadBruteForceStatistics(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getBruteForceStatistics()
      );
      this.bruteForceData.set(data);

      // Build Chart.js data for hourly attack distribution
      if (data.hourlyDistribution && data.hourlyDistribution.length > 0) {
        this.bruteForceChartData = {
          labels: data.hourlyDistribution.map(h => h.label),
          datasets: [
            {
              label: 'Attacks by Hour',
              data: data.hourlyDistribution.map(h => h.count),
              backgroundColor: '#ff9800',
              borderColor: '#f57c00',
              borderWidth: 1
            }
          ]
        };
      }
    } catch (err) {
      console.error('Failed to load brute force statistics:', err);
    }
  }

  /**
   * Load IP blacklist data
   */
  private async loadIpBlacklist(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getIpBlacklist()
      );
      this.ipBlacklistData.set(data);
    } catch (err) {
      console.error('Failed to load IP blacklist:', err);
    }
  }

  /**
   * Load session management data
   */
  private async loadSessionManagement(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getSessionManagement()
      );
      this.sessionData.set(data);
    } catch (err) {
      console.error('Failed to load session management:', err);
    }
  }

  /**
   * Load MFA compliance with adoption chart
   */
  private async loadMfaCompliance(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getMfaCompliance()
      );
      this.mfaComplianceData.set(data);

      // Build Chart.js doughnut chart for MFA adoption
      this.mfaComplianceChartData = {
        labels: ['MFA Enabled', 'MFA Disabled'],
        datasets: [
          {
            data: [data.mfaEnabledCount, data.mfaDisabledCount],
            backgroundColor: ['#4caf50', '#f44336'],
            hoverBackgroundColor: ['#66bb6a', '#ef5350']
          }
        ]
      };
    } catch (err) {
      console.error('Failed to load MFA compliance:', err);
    }
  }

  /**
   * Load password compliance with strength distribution chart
   */
  private async loadPasswordCompliance(): Promise<void> {
    try {
      const data = await firstValueFrom(
        this.securityAnalyticsService.getPasswordCompliance()
      );
      this.passwordComplianceData.set(data);

      // Build Chart.js pie chart for password strength distribution
      if (data.strengthDistribution && data.strengthDistribution.length > 0) {
        this.passwordStrengthChartData = {
          labels: data.strengthDistribution.map(s => s.strength),
          datasets: [
            {
              data: data.strengthDistribution.map(s => s.count),
              backgroundColor: ['#f44336', '#ff9800', '#4caf50', '#2196f3'],
              hoverBackgroundColor: ['#ef5350', '#ffa726', '#66bb6a', '#42a5f5']
            }
          ]
        };
      }
    } catch (err) {
      console.error('Failed to load password compliance:', err);
    }
  }

  /**
   * Manual refresh handler
   */
  async refresh(): Promise<void> {
    await this.loadDashboardData();
  }

  /**
   * Start auto-refresh timer
   */
  private startAutoRefresh(): void {
    this.refreshSubscription = interval(this.REFRESH_INTERVAL_MS).subscribe(() => {
      this.loadDashboardData();
    });
  }

  /**
   * Stop auto-refresh timer
   */
  private stopAutoRefresh(): void {
    this.refreshSubscription?.unsubscribe();
  }

  /**
   * Get severity badge class
   */
  getSeverityClass(severity: string): string {
    switch (severity) {
      case 'Critical':
        return 'severity-critical';
      case 'High':
        return 'severity-high';
      case 'Medium':
        return 'severity-medium';
      case 'Low':
        return 'severity-low';
      default:
        return 'severity-info';
    }
  }

  /**
   * Get compliance status color
   */
  getComplianceColor(status: string): string {
    switch (status) {
      case 'Compliant':
        return 'success';
      case 'At Risk':
        return 'warn';
      case 'Non-Compliant':
        return 'danger';
      default:
        return 'neutral';
    }
  }

  /**
   * Format timestamp for display
   */
  formatTimestamp(date: Date): string {
    return new Date(date).toLocaleString();
  }

  /**
   * Format percentage
   */
  formatPercentage(value: number): string {
    return `${value.toFixed(1)}%`;
  }
}
