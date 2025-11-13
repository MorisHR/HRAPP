import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subject, interval } from 'rxjs';
import { takeUntil, switchMap, startWith } from 'rxjs/operators';
import { SecurityAlertService } from '../../../../../services/security-alert.service';
import {
  SecurityAlert,
  SecurityAlertStatistics,
  AlertSeverityCounts,
  SecurityAlertHelpers,
  AuditSeverity,
  SecurityAlertStatus
} from '../../../../../models/security-alert.model';

@Component({
  selector: 'app-security-alerts-dashboard',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './security-alerts-dashboard.component.html',
  styleUrls: ['./security-alerts-dashboard.component.css']
})
export class SecurityAlertsDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Dashboard data
  severityCounts: AlertSeverityCounts | null = null;
  recentCriticalAlerts: SecurityAlert[] = [];
  statistics: SecurityAlertStatistics | null = null;

  // Loading states
  loading = true;
  countsLoading = true;
  alertsLoading = true;
  statsLoading = true;

  // Helper for formatting
  helpers = SecurityAlertHelpers;
  AuditSeverity = AuditSeverity;

  // Auto-refresh interval (30 seconds)
  private refreshInterval = 30000;

  constructor(
    private securityAlertService: SecurityAlertService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all dashboard data
   */
  private loadDashboardData(): void {
    this.loadSeverityCounts();
    this.loadRecentCriticalAlerts();
    this.loadStatistics();
  }

  /**
   * Start auto-refresh for real-time updates
   */
  private startAutoRefresh(): void {
    interval(this.refreshInterval)
      .pipe(
        takeUntil(this.destroy$),
        startWith(0)
      )
      .subscribe(() => {
        this.loadSeverityCounts();
        this.loadRecentCriticalAlerts();
      });
  }

  /**
   * Load severity counts
   */
  private loadSeverityCounts(): void {
    this.countsLoading = true;
    this.securityAlertService.getActiveAlertCountsBySeverity()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (counts) => {
          this.severityCounts = counts;
          this.countsLoading = false;
          this.updateLoadingState();
        },
        error: (error) => {
          console.error('Error loading severity counts:', error);
          this.countsLoading = false;
          this.updateLoadingState();
        }
      });
  }

  /**
   * Load recent critical alerts
   */
  private loadRecentCriticalAlerts(): void {
    this.alertsLoading = true;
    this.securityAlertService.getRecentCriticalAlerts(undefined, 24)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (alerts) => {
          this.recentCriticalAlerts = alerts.slice(0, 10); // Show top 10
          this.alertsLoading = false;
          this.updateLoadingState();
        },
        error: (error) => {
          console.error('Error loading recent alerts:', error);
          this.alertsLoading = false;
          this.updateLoadingState();
        }
      });
  }

  /**
   * Load statistics (last 30 days)
   */
  private loadStatistics(): void {
    this.statsLoading = true;
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);

    this.securityAlertService.getAlertStatistics(undefined, startDate, endDate)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.statistics = stats;
          this.statsLoading = false;
          this.updateLoadingState();
        },
        error: (error) => {
          console.error('Error loading statistics:', error);
          this.statsLoading = false;
          this.updateLoadingState();
        }
      });
  }

  /**
   * Update overall loading state
   */
  private updateLoadingState(): void {
    this.loading = this.countsLoading || this.alertsLoading || this.statsLoading;
  }

  /**
   * Get severity count safely
   */
  getSeverityCount(severity: AuditSeverity): number {
    return this.severityCounts?.[severity] ?? 0;
  }

  /**
   * Get total active alerts
   */
  getTotalActiveAlerts(): number {
    if (!this.severityCounts) return 0;
    return Object.values(this.severityCounts).reduce((sum, count) => sum + count, 0);
  }

  /**
   * Navigate to alerts list filtered by severity
   */
  viewAlertsBySeverity(severity: AuditSeverity): void {
    this.router.navigate(['/admin/security-alerts/list'], {
      queryParams: { severity }
    });
  }

  /**
   * Navigate to alert detail
   */
  viewAlertDetail(alertId: string): void {
    this.router.navigate(['/admin/security-alerts/detail', alertId]);
  }

  /**
   * Navigate to full alerts list
   */
  viewAllAlerts(): void {
    this.router.navigate(['/admin/security-alerts/list']);
  }

  /**
   * Manual refresh
   */
  refresh(): void {
    this.loadDashboardData();
  }

  /**
   * Get percentage for progress bar
   */
  getResolutionPercentage(): number {
    if (!this.statistics || this.statistics.totalAlerts === 0) return 0;
    return Math.round((this.statistics.resolvedAlerts / this.statistics.totalAlerts) * 100);
  }

  /**
   * Get severity icon
   */
  getSeverityIcon(severity: AuditSeverity): string {
    return SecurityAlertHelpers.getSeverityIcon(severity);
  }

  /**
   * Get severity color
   */
  getSeverityColor(severity: AuditSeverity): string {
    return SecurityAlertHelpers.getSeverityColor(severity);
  }

  /**
   * Format time ago
   */
  formatTimeAgo(date: Date): string {
    return SecurityAlertHelpers.formatTimeAgo(date);
  }

  /**
   * Get risk score color
   */
  getRiskScoreColor(score: number): string {
    return SecurityAlertHelpers.getRiskScoreColor(score);
  }

  /**
   * Get risk score label
   */
  getRiskScoreLabel(score: number): string {
    return SecurityAlertHelpers.getRiskScoreLabel(score);
  }
}
