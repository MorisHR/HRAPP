import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of, catchError, tap, map } from 'rxjs';
import {
  DashboardStats,
  DashboardTrends,
  TrendData,
  CriticalAlert
} from '../models/dashboard.model';
import { Tenant } from '../models/tenant.model';
import { TenantService } from './tenant.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminDashboardService {
  private http = inject(HttpClient);
  private tenantService = inject(TenantService);
  private apiUrl = `${environment.apiUrl}/admin/dashboard`;

  // Reactive state
  private statsSignal = signal<DashboardStats | null>(null);
  private alertsSignal = signal<CriticalAlert[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly stats = this.statsSignal.asReadonly();
  readonly alerts = this.alertsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Get comprehensive dashboard statistics with trends
   */
  getDashboardStats(): Observable<DashboardStats> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<DashboardStats>(`${this.apiUrl}/stats`).pipe(
      tap(stats => {
        this.statsSignal.set(stats);
        this.loadingSignal.set(false);
      }),
      catchError(() => {
        this.errorSignal.set('Failed to fetch dashboard stats');
        this.loadingSignal.set(false);
        // Fallback to calculating from tenant data
        return this.calculateStatsFromTenants();
      })
    );
  }

  /**
   * Get critical alerts requiring attention
   */
  getCriticalAlerts(): Observable<CriticalAlert[]> {
    return this.http.get<CriticalAlert[]>(`${this.apiUrl}/alerts`).pipe(
      tap(alerts => this.alertsSignal.set(alerts)),
      catchError(() => of(this.getMockCriticalAlerts()))
    );
  }

  /**
   * Acknowledge an alert
   */
  acknowledgeAlert(alertId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/alerts/${alertId}/acknowledge`, {}).pipe(
      tap(() => {
        this.alertsSignal.update(alerts =>
          alerts.map(a => a.id === alertId ? { ...a, acknowledged: true } : a)
        );
      })
    );
  }

  /**
   * Resolve an alert
   */
  resolveAlert(alertId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/alerts/${alertId}/resolve`, {}).pipe(
      tap(() => {
        this.alertsSignal.update(alerts =>
          alerts.filter(a => a.id !== alertId)
        );
      })
    );
  }

  /**
   * Handle alert action (e.g., scale_storage, review_tenants, etc.)
   */
  handleAlertAction(alertId: string, action: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/alerts/${alertId}/action`, { action }).pipe(
      catchError(() => {
        // Fallback: log the action locally for now
        console.log(`Alert action triggered: ${action} for alert ${alertId}`);
        return of(void 0);
      })
    );
  }

  /**
   * Calculate stats from tenant data (fallback method)
   */
  private calculateStatsFromTenants(): Observable<DashboardStats> {
    return this.tenantService.getTenants().pipe(
      map(tenants => this.aggregateTenantStats(tenants))
    );
  }

  /**
   * Aggregate statistics from tenant list
   */
  private aggregateTenantStats(tenants: Tenant[]): DashboardStats {
    const activeTenants = tenants.filter(t => t.status === 'Active').length;
    const totalEmployees = tenants.reduce((sum, t) => sum + (t.employeeCount || 0), 0);
    const monthlyRevenue = tenants
      .filter(t => t.status === 'Active')
      .reduce((sum, t) => sum + (t.monthlyPrice || 0), 0);

    // Calculate trends (mock for now - would need historical data)
    const trends = this.calculateMockTrends(tenants.length, activeTenants, totalEmployees, monthlyRevenue);

    return {
      totalTenants: tenants.length,
      activeTenants,
      totalEmployees,
      monthlyRevenue,
      trends
    };
  }

  /**
   * Calculate trend data
   * TODO: Replace with real historical comparison
   */
  private calculateMockTrends(
    totalTenants: number,
    activeTenants: number,
    totalEmployees: number,
    monthlyRevenue: number
  ): DashboardTrends {
    return {
      tenantGrowth: this.createTrend(totalTenants, 12.5),
      activeGrowth: this.createTrend(activeTenants, 8.3),
      employeeGrowth: this.createTrend(totalEmployees, 15.7),
      revenueGrowth: this.createTrend(monthlyRevenue, 18.2)
    };
  }

  /**
   * Create trend data object
   */
  private createTrend(value: number, percentChange: number): TrendData {
    return {
      value,
      percentChange: Math.abs(percentChange),
      direction: percentChange >= 0 ? 'up' : 'down',
      period: 'month'
    };
  }

  /**
   * Get trend icon
   */
  getTrendIcon(direction: 'up' | 'down' | 'stable'): string {
    switch (direction) {
      case 'up': return 'trending_up';
      case 'down': return 'trending_down';
      case 'stable': return 'trending_flat';
    }
  }

  /**
   * Get trend color
   */
  getTrendColor(direction: 'up' | 'down' | 'stable'): string {
    switch (direction) {
      case 'up': return 'success';
      case 'down': return 'error';
      case 'stable': return 'default';
    }
  }

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-MU', {
      style: 'currency',
      currency: 'MUR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  /**
   * Format large numbers (e.g., 1.5K, 2.3M)
   */
  formatNumber(value: number): string {
    if (value >= 1000000) {
      return (value / 1000000).toFixed(1) + 'M';
    }
    if (value >= 1000) {
      return (value / 1000).toFixed(1) + 'K';
    }
    return value.toString();
  }

  /**
   * Mock critical alerts
   * TODO: Remove when backend is implemented
   */
  private getMockCriticalAlerts(): CriticalAlert[] {
    return [
      {
        id: 'alert-1',
        timestamp: new Date(Date.now() - 2 * 3600000),
        severity: 'high',
        title: 'Storage Capacity Warning',
        description: 'Storage usage at 78% - consider scaling storage capacity',
        source: 'Storage Monitor',
        acknowledged: false,
        actions: [
          { label: 'Scale Storage', action: 'scale_storage', primary: true },
          { label: 'View Details', action: 'view_details', primary: false }
        ]
      },
      {
        id: 'alert-2',
        timestamp: new Date(Date.now() - 5 * 3600000),
        severity: 'medium',
        title: 'Payment Failures',
        description: '3 tenants have failed payment attempts in the last 24 hours',
        source: 'Billing System',
        acknowledged: false,
        actions: [
          { label: 'Review Tenants', action: 'review_tenants', primary: true },
          { label: 'Send Reminders', action: 'send_reminders', primary: false }
        ]
      },
      {
        id: 'alert-3',
        timestamp: new Date(Date.now() - 12 * 3600000),
        severity: 'low',
        title: 'API Rate Limit Approaching',
        description: 'Tenant "DataFlow Inc" approaching API rate limit (85% used)',
        source: 'API Gateway',
        acknowledged: true,
        actions: [
          { label: 'Increase Limit', action: 'increase_limit', primary: true },
          { label: 'Contact Tenant', action: 'contact_tenant', primary: false }
        ]
      }
    ];
  }

  /**
   * Get alert severity color
   */
  getAlertSeverityColor(severity: 'critical' | 'high' | 'medium' | 'low'): string {
    switch (severity) {
      case 'critical': return 'error';
      case 'high': return 'warn';
      case 'medium': return 'accent';
      case 'low': return 'primary';
    }
  }

  /**
   * Get alert severity icon
   */
  getAlertSeverityIcon(severity: 'critical' | 'high' | 'medium' | 'low'): string {
    switch (severity) {
      case 'critical': return 'error';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'info';
    }
  }
}
