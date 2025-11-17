// ═══════════════════════════════════════════════════════════
// MONITORING DASHBOARD COMPONENT
// Part of the Fortune 500-grade HRMS real-time monitoring system
// SuperAdmin dashboard for platform health and observability
// ═══════════════════════════════════════════════════════════

import { Component, signal, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { interval, Subscription } from 'rxjs';
import { switchMap, startWith } from 'rxjs/operators';

// UI Components
import { UiModule } from '../../../../shared/ui/ui.module';

// Services
import { MonitoringService } from '../../../../core/services/monitoring.service';
import { DashboardMetrics } from '../../../../core/models/monitoring.models';

/**
 * Monitoring Dashboard Component
 *
 * Provides real-time platform health metrics for SuperAdmin users.
 * Features:
 * - Auto-refresh every 30 seconds
 * - Manual refresh button
 * - Color-coded status indicators
 * - Performance metrics with SLA targets
 * - Security event monitoring
 * - Alert management
 *
 * SECURITY: SuperAdmin role required (enforced at route level)
 * PERFORMANCE: Uses signals for reactive state management
 * SLA TARGETS: P95 <200ms, P99 <500ms, Cache Hit Rate >95%, Error Rate <0.1%
 *
 * @example
 * ```html
 * <app-monitoring-dashboard></app-monitoring-dashboard>
 * ```
 */
@Component({
  selector: 'app-monitoring-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    UiModule
  ],
  templateUrl: './monitoring-dashboard.component.html',
  styleUrl: './monitoring-dashboard.component.scss'
})
export class MonitoringDashboardComponent implements OnInit, OnDestroy {
  private monitoringService = inject(MonitoringService);

  // Signals for reactive state
  metrics = signal<DashboardMetrics | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  lastRefreshTime = signal<Date>(new Date());
  autoRefreshEnabled = signal<boolean>(true);

  // Auto-refresh subscription
  private refreshSubscription?: Subscription;

  // Constants
  readonly REFRESH_INTERVAL_MS = 30000; // 30 seconds

  ngOnInit(): void {
    this.loadDashboardMetrics();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  /**
   * Load dashboard metrics from the service
   */
  private loadDashboardMetrics(): void {
    this.loading.set(true);
    this.error.set(null);

    this.monitoringService.getDashboardMetrics().subscribe({
      next: (data) => {
        this.metrics.set(data);
        this.loading.set(false);
        this.lastRefreshTime.set(new Date());
      },
      error: (err: Error) => {
        this.error.set('Failed to load dashboard metrics. Please try again.');
        this.loading.set(false);
        console.error('Error loading dashboard metrics:', err);
      }
    });
  }

  /**
   * Manually refresh dashboard metrics
   */
  refreshMetrics(): void {
    this.loading.set(true);
    this.error.set(null);

    this.monitoringService.refreshDashboardMetrics().subscribe({
      next: (data) => {
        this.metrics.set(data);
        this.loading.set(false);
        this.lastRefreshTime.set(new Date());
      },
      error: (err: Error) => {
        this.error.set('Failed to refresh dashboard metrics. Please try again.');
        this.loading.set(false);
        console.error('Error refreshing dashboard metrics:', err);
      }
    });
  }

  /**
   * Start auto-refresh interval
   */
  private startAutoRefresh(): void {
    this.refreshSubscription = interval(this.REFRESH_INTERVAL_MS)
      .pipe(
        startWith(0),
        switchMap(() => this.monitoringService.getDashboardMetrics())
      )
      .subscribe({
        next: (data) => {
          this.metrics.set(data);
          this.lastRefreshTime.set(new Date());
        },
        error: (err: Error) => {
          console.error('Auto-refresh error:', err);
        }
      });
  }

  /**
   * Stop auto-refresh interval
   */
  private stopAutoRefresh(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
  }

  /**
   * Toggle auto-refresh on/off
   */
  toggleAutoRefresh(): void {
    this.autoRefreshEnabled.set(!this.autoRefreshEnabled());

    if (this.autoRefreshEnabled()) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  /**
   * Get status badge color based on system status
   */
  getStatusBadgeColor(status: string): 'success' | 'warning' | 'error' {
    switch (status) {
      case 'Healthy':
        return 'success';
      case 'Degraded':
        return 'warning';
      case 'Critical':
        return 'error';
      default:
        return 'warning';
    }
  }

  /**
   * Get progress bar color based on percentage
   */
  getProgressBarColor(value: number, threshold: number = 80): 'success' | 'warning' | 'error' {
    if (value >= threshold) {
      return 'error';
    } else if (value >= threshold * 0.7) {
      return 'warning';
    } else {
      return 'success';
    }
  }

  /**
   * Get cache hit rate color (inverted - higher is better)
   */
  getCacheHitRateColor(value: number): 'success' | 'warning' | 'error' {
    if (value >= 95) {
      return 'success';
    } else if (value >= 90) {
      return 'warning';
    } else {
      return 'error';
    }
  }

  /**
   * Get response time color based on SLA targets
   */
  getResponseTimeColor(value: number, target: number): 'success' | 'warning' | 'error' {
    if (value <= target) {
      return 'success';
    } else if (value <= target * 1.5) {
      return 'warning';
    } else {
      return 'error';
    }
  }

  /**
   * Get error rate color
   */
  getErrorRateColor(value: number): 'success' | 'warning' | 'error' {
    if (value <= 0.1) {
      return 'success';
    } else if (value <= 1.0) {
      return 'warning';
    } else {
      return 'error';
    }
  }

  /**
   * Format number with commas for thousands
   */
  formatNumber(value: number): string {
    return value.toLocaleString('en-US');
  }

  /**
   * Format time ago from timestamp
   */
  formatTimeAgo(timestamp: string): string {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);

    if (diffSecs < 60) {
      return `${diffSecs} seconds ago`;
    } else if (diffMins < 60) {
      return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    } else {
      const diffHours = Math.floor(diffMins / 60);
      return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    }
  }
}
