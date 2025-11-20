import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, catchError, tap, map } from 'rxjs';
import { ActivityLog, ActivityType } from '../models/dashboard.model';
import { environment } from '../../../environments/environment';

export interface ActivityLogFilter {
  limit?: number;
  offset?: number;
  type?: ActivityType;
  severity?: 'info' | 'warning' | 'error' | 'success';
  tenantId?: string;
  startDate?: Date;
  endDate?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ActivityLogService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/activity-logs`;

  // Reactive state
  private logsSignal = signal<ActivityLog[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly logs = this.logsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Fetch activity logs with optional filtering
   */
  getActivityLogs(filter: ActivityLogFilter = {}): Observable<ActivityLog[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    let params = new HttpParams();
    if (filter.limit) params = params.set('limit', filter.limit.toString());
    if (filter.offset) params = params.set('offset', filter.offset.toString());
    if (filter.type) params = params.set('type', filter.type);
    if (filter.severity) params = params.set('severity', filter.severity);
    if (filter.tenantId) params = params.set('tenantId', filter.tenantId);

    return this.http.get<ActivityLog[]>(this.apiUrl, { params }).pipe(
      map(logs => this.deserializeLogs(logs)),
      tap(logs => {
        this.logsSignal.set(logs);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        this.errorSignal.set('Failed to fetch activity logs');
        this.loadingSignal.set(false);
        // Return mock data for development/fallback
        return of(this.getMockActivityLogs(filter.limit || 10));
      })
    );
  }

  /**
   * Get recent activity (last N items)
   */
  getRecentActivity(limit: number = 10): Observable<ActivityLog[]> {
    return this.getActivityLogs({ limit });
  }

  /**
   * Get activity logs for specific tenant
   */
  getTenantActivity(tenantId: string, limit: number = 20): Observable<ActivityLog[]> {
    return this.getActivityLogs({ tenantId, limit });
  }

  /**
   * Get activity logs by type
   */
  getActivityByType(type: ActivityType, limit: number = 20): Observable<ActivityLog[]> {
    return this.getActivityLogs({ type, limit });
  }

  /**
   * Get critical activities (errors and warnings)
   */
  getCriticalActivity(limit: number = 10): Observable<ActivityLog[]> {
    return this.http.get<ActivityLog[]>(`${this.apiUrl}/critical`, {
      params: new HttpParams().set('limit', limit.toString())
    }).pipe(
      map(logs => this.deserializeLogs(logs)),
      catchError(() => of(this.getMockCriticalActivity(limit)))
    );
  }

  /**
   * Deserialize date strings to Date objects
   */
  private deserializeLogs(logs: any[]): ActivityLog[] {
    return logs.map(log => ({
      ...log,
      timestamp: new Date(log.timestamp)
    }));
  }

  /**
   * Get icon for activity type
   */
  getActivityIcon(type: ActivityType): string {
    const iconMap: Record<ActivityType, string> = {
      [ActivityType.TenantCreated]: 'business',
      [ActivityType.TenantSuspended]: 'block',
      [ActivityType.TenantUpgraded]: 'upgrade',
      [ActivityType.TenantDowngraded]: 'downgrade',
      [ActivityType.SecurityAlert]: 'security',
      [ActivityType.PaymentFailed]: 'payment',
      [ActivityType.PaymentSuccess]: 'check_circle',
      [ActivityType.UserLogin]: 'login',
      [ActivityType.UserLogout]: 'logout',
      [ActivityType.SystemError]: 'error',
      [ActivityType.BackupCompleted]: 'backup',
      [ActivityType.MaintenanceScheduled]: 'schedule'
    };
    return iconMap[type] || 'info';
  }

  /**
   * Get color for severity
   */
  getSeverityColor(severity: 'info' | 'warning' | 'error' | 'success'): string {
    const colorMap = {
      info: 'primary',
      warning: 'warn',
      error: 'error',
      success: 'success'
    };
    return colorMap[severity];
  }

  /**
   * Format relative time (e.g., "2 hours ago")
   */
  getRelativeTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    return date.toLocaleDateString();
  }

  /**
   * Mock data generator for development/fallback
   * TODO: Remove when backend is fully implemented
   */
  private getMockActivityLogs(limit: number): ActivityLog[] {
    const now = new Date();
    const logs: ActivityLog[] = [
      {
        id: '1',
        timestamp: new Date(now.getTime() - 2 * 3600000),
        type: ActivityType.TenantCreated,
        severity: 'success',
        title: 'New Tenant Created',
        description: 'Acme Corp has been successfully onboarded',
        tenantId: 'tenant-001',
        tenantName: 'Acme Corp',
        userName: 'System Admin'
      },
      {
        id: '2',
        timestamp: new Date(now.getTime() - 5 * 3600000),
        type: ActivityType.TenantUpgraded,
        severity: 'success',
        title: 'Tenant Upgraded',
        description: 'TechCo upgraded from Professional to Enterprise tier',
        tenantId: 'tenant-002',
        tenantName: 'TechCo',
        metadata: { fromTier: 'Professional', toTier: 'Enterprise' }
      },
      {
        id: '3',
        timestamp: new Date(now.getTime() - 24 * 3600000),
        type: ActivityType.SecurityAlert,
        severity: 'warning',
        title: 'Security Alert',
        description: 'Multiple failed login attempts detected from IP 192.168.1.100',
        metadata: { ip: '192.168.1.100', attempts: 5 }
      },
      {
        id: '4',
        timestamp: new Date(now.getTime() - 2 * 24 * 3600000),
        type: ActivityType.PaymentSuccess,
        severity: 'success',
        title: 'Payment Processed',
        description: 'Monthly subscription payment of $499 received',
        tenantId: 'tenant-003',
        tenantName: 'Global Industries',
        metadata: { amount: 499, currency: 'USD' }
      },
      {
        id: '5',
        timestamp: new Date(now.getTime() - 2 * 24 * 3600000),
        type: ActivityType.BackupCompleted,
        severity: 'info',
        title: 'Backup Completed',
        description: 'Daily database backup completed successfully (2.4 GB)',
        metadata: { sizeGB: 2.4, duration: 120 }
      },
      {
        id: '6',
        timestamp: new Date(now.getTime() - 3 * 24 * 3600000),
        type: ActivityType.PaymentFailed,
        severity: 'error',
        title: 'Payment Failed',
        description: 'Payment failed for OldCo - card declined',
        tenantId: 'tenant-004',
        tenantName: 'OldCo',
        metadata: { reason: 'Card declined' }
      },
      {
        id: '7',
        timestamp: new Date(now.getTime() - 4 * 24 * 3600000),
        type: ActivityType.TenantCreated,
        severity: 'success',
        title: 'New Tenant Created',
        description: 'StartupXYZ has been successfully onboarded',
        tenantId: 'tenant-005',
        tenantName: 'StartupXYZ'
      },
      {
        id: '8',
        timestamp: new Date(now.getTime() - 5 * 24 * 3600000),
        type: ActivityType.MaintenanceScheduled,
        severity: 'info',
        title: 'Maintenance Scheduled',
        description: 'System maintenance scheduled for Sunday 2 AM - 4 AM UTC',
        metadata: { scheduledTime: '2025-11-24T02:00:00Z' }
      },
      {
        id: '9',
        timestamp: new Date(now.getTime() - 6 * 24 * 3600000),
        type: ActivityType.TenantUpgraded,
        severity: 'success',
        title: 'Tenant Upgraded',
        description: 'MegaCorp upgraded from Small to Professional tier',
        tenantId: 'tenant-006',
        tenantName: 'MegaCorp',
        metadata: { fromTier: 'Small', toTier: 'Professional' }
      },
      {
        id: '10',
        timestamp: new Date(now.getTime() - 7 * 24 * 3600000),
        type: ActivityType.SecurityAlert,
        severity: 'warning',
        title: 'Security Alert',
        description: 'Unusual API activity detected for tenant DataFlow Inc',
        tenantId: 'tenant-007',
        tenantName: 'DataFlow Inc',
        metadata: { requestCount: 50000, threshold: 10000 }
      }
    ];

    return logs.slice(0, limit);
  }

  private getMockCriticalActivity(limit: number): ActivityLog[] {
    return this.getMockActivityLogs(20)
      .filter(log => log.severity === 'error' || log.severity === 'warning')
      .slice(0, limit);
  }
}
