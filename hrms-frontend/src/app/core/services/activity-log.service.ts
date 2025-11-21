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
      [ActivityType.TenantSuspended]: 'cancel',
      [ActivityType.TenantUpgraded]: 'upgrade',
      [ActivityType.TenantDowngraded]: 'arrow_downward',
      [ActivityType.SecurityAlert]: 'security',
      [ActivityType.PaymentFailed]: 'payment',
      [ActivityType.PaymentSuccess]: 'check_circle',
      [ActivityType.UserLogin]: 'person',
      [ActivityType.UserLogout]: 'exit_to_app',
      [ActivityType.SystemError]: 'error',
      [ActivityType.BackupCompleted]: 'backup',
      [ActivityType.MaintenanceScheduled]: 'schedule',
      // Infrastructure Icons
      [ActivityType.QueueOverload]: 'queue',
      [ActivityType.JobFailed]: 'work_off',
      [ActivityType.DatabaseSlowQuery]: 'speed',
      [ActivityType.DatabaseConnectionPoolExhausted]: 'storage',
      [ActivityType.RateLimitExceeded]: 'cancel',
      [ActivityType.CacheMiss]: 'memory',
      [ActivityType.CDNError]: 'cloud_off',
      [ActivityType.HighCPUUsage]: 'trending_up',
      [ActivityType.HighMemoryUsage]: 'memory',
      [ActivityType.DiskSpaceWarning]: 'storage'
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
   * Enhanced with comprehensive event details and edge cases
   * TODO: Remove when backend is fully implemented
   */
  private getMockActivityLogs(limit: number): ActivityLog[] {
    const now = new Date();
    const logs: ActivityLog[] = [
      {
        id: '1',
        timestamp: new Date(now.getTime() - 45 * 60000), // 45 minutes ago
        type: ActivityType.TenantCreated,
        severity: 'success',
        title: 'New Tenant Onboarded',
        description: 'Acme Corporation successfully created with Professional tier subscription. Initial setup includes 50 employee licenses, payroll module, and attendance tracking enabled.',
        tenantId: 'tenant-001',
        tenantName: 'Acme Corporation',
        userName: 'Sarah Johnson (Super Admin)',
        metadata: {
          tier: 'Professional',
          employeeCount: 50,
          modules: ['payroll', 'attendance', 'leave-management'],
          monthlyRate: 499
        }
      },
      {
        id: '2',
        timestamp: new Date(now.getTime() - 2 * 3600000), // 2 hours ago
        type: ActivityType.TenantUpgraded,
        severity: 'success',
        title: 'Subscription Tier Upgraded',
        description: 'TechCo Solutions upgraded from Professional ($499/mo) to Enterprise tier ($999/mo). Added features: advanced analytics, custom reports, API access, dedicated support.',
        tenantId: 'tenant-002',
        tenantName: 'TechCo Solutions',
        userName: 'Michael Chen (Account Manager)',
        metadata: {
          fromTier: 'Professional',
          toTier: 'Enterprise',
          oldPrice: 499,
          newPrice: 999,
          addedFeatures: ['analytics', 'custom-reports', 'api-access', 'dedicated-support']
        }
      },
      {
        id: '3',
        timestamp: new Date(now.getTime() - 5 * 3600000), // 5 hours ago
        type: ActivityType.SecurityAlert,
        severity: 'warning',
        title: 'Multiple Failed Login Attempts',
        description: 'Security system detected 8 consecutive failed login attempts from IP 192.168.1.100 (Location: Unknown) targeting admin account. Account temporarily locked for security.',
        tenantId: 'tenant-003',
        tenantName: 'Global Industries Inc',
        metadata: {
          ip: '192.168.1.100',
          attempts: 8,
          targetAccount: 'admin@globalindustries.com',
          action: 'account_locked',
          location: 'Unknown'
        }
      },
      {
        id: '4',
        timestamp: new Date(now.getTime() - 8 * 3600000), // 8 hours ago
        type: ActivityType.PaymentSuccess,
        severity: 'success',
        title: 'Monthly Payment Processed',
        description: 'Monthly subscription payment of $499.00 successfully processed for Professional tier. Payment method: Visa ending in 4242. Next billing date: December 21, 2025.',
        tenantId: 'tenant-004',
        tenantName: 'DataFlow Systems',
        metadata: {
          amount: 499,
          currency: 'USD',
          paymentMethod: 'Visa ****4242',
          nextBillingDate: '2025-12-21',
          transactionId: 'txn_1PqR2sT3uV4wX5yZ'
        }
      },
      {
        id: '5',
        timestamp: new Date(now.getTime() - 12 * 3600000), // 12 hours ago
        type: ActivityType.BackupCompleted,
        severity: 'info',
        title: 'Automated Database Backup Completed',
        description: 'Daily incremental backup completed successfully. Backed up 2.4 GB of data (125,432 records) across all tenant databases. Backup stored in encrypted S3 bucket with 90-day retention.',
        metadata: {
          sizeGB: 2.4,
          duration: 120,
          recordCount: 125432,
          backupType: 'incremental',
          storage: 'AWS S3 us-east-1',
          encryption: 'AES-256'
        }
      },
      {
        id: '6',
        timestamp: new Date(now.getTime() - 1 * 24 * 3600000), // 1 day ago
        type: ActivityType.PaymentFailed,
        severity: 'error',
        title: 'Payment Transaction Failed',
        description: 'Monthly subscription payment of $299.00 failed for Legacy Corp. Reason: Card declined by issuer (Insufficient funds). 3 retry attempts scheduled. Customer notified via email.',
        tenantId: 'tenant-005',
        tenantName: 'Legacy Corp',
        metadata: {
          amount: 299,
          reason: 'insufficient_funds',
          paymentMethod: 'Mastercard ****8765',
          retryAttempts: 3,
          customerNotified: true
        }
      },
      {
        id: '7',
        timestamp: new Date(now.getTime() - 2 * 24 * 3600000), // 2 days ago
        type: ActivityType.TenantCreated,
        severity: 'success',
        title: 'New Enterprise Tenant Created',
        description: 'StartupXYZ Technologies successfully onboarded with Enterprise tier. Setup includes 200 employee licenses, all premium modules, custom branding, and dedicated account manager.',
        tenantId: 'tenant-006',
        tenantName: 'StartupXYZ Technologies',
        userName: 'Jennifer Martinez (Sales Team)',
        metadata: {
          tier: 'Enterprise',
          employeeCount: 200,
          customBranding: true,
          dedicatedSupport: true
        }
      },
      {
        id: '8',
        timestamp: new Date(now.getTime() - 3 * 24 * 3600000), // 3 days ago
        type: ActivityType.MaintenanceScheduled,
        severity: 'info',
        title: 'System Maintenance Window Scheduled',
        description: 'Planned system maintenance for database optimization and security patches. Scheduled for Sunday, November 24, 2025, 2:00 AM - 4:00 AM UTC. Expected downtime: 30 minutes. All tenants notified.',
        metadata: {
          scheduledTime: '2025-11-24T02:00:00Z',
          duration: 120,
          expectedDowntime: 30,
          reason: 'database-optimization',
          tenantsNotified: true
        }
      },
      {
        id: '9',
        timestamp: new Date(now.getTime() - 4 * 24 * 3600000), // 4 days ago
        type: ActivityType.TenantUpgraded,
        severity: 'success',
        title: 'Tier Upgrade Completed',
        description: 'MegaCorp International upgraded from Starter ($99/mo) to Professional tier ($499/mo). Added 150 employee licenses. New features: payroll processing, benefits management, performance reviews.',
        tenantId: 'tenant-007',
        tenantName: 'MegaCorp International',
        metadata: {
          fromTier: 'Starter',
          toTier: 'Professional',
          oldPrice: 99,
          newPrice: 499,
          addedLicenses: 150
        }
      },
      {
        id: '10',
        timestamp: new Date(now.getTime() - 5 * 24 * 3600000), // 5 days ago
        type: ActivityType.SecurityAlert,
        severity: 'warning',
        title: 'Unusual API Activity Detected',
        description: 'Abnormal API request volume detected for DataFlow Inc. Observed 52,847 requests in 1 hour (normal: ~10,000/hour). Rate limiting applied automatically. Possible integration issue or attempted abuse.',
        tenantId: 'tenant-008',
        tenantName: 'DataFlow Inc',
        metadata: {
          requestCount: 52847,
          threshold: 10000,
          timeWindow: '1 hour',
          action: 'rate_limiting_applied',
          possibleCause: 'integration_loop'
        }
      },
      {
        id: '11',
        timestamp: new Date(now.getTime() - 6 * 24 * 3600000), // 6 days ago
        type: ActivityType.TenantSuspended,
        severity: 'error',
        title: 'Tenant Account Suspended',
        description: 'The Really Long Company Name International Global Solutions Worldwide Corporation Ltd. account suspended due to 30+ days of non-payment ($999.00 outstanding). All access disabled. Payment plan offered.',
        tenantId: 'tenant-009',
        tenantName: 'The Really Long Company Name International Global Solutions Worldwide Corporation Ltd.',
        metadata: {
          reason: 'non_payment',
          daysOverdue: 32,
          outstandingAmount: 999,
          accessDisabled: true,
          paymentPlanOffered: true
        }
      },
      {
        id: '12',
        timestamp: new Date(now.getTime() - 7 * 24 * 3600000), // 7 days ago
        type: ActivityType.UserLogin,
        severity: 'info',
        title: 'Admin Login from New Location',
        description: 'Super Admin "admin@cloudtech.io" logged in from new location: Tokyo, Japan (IP: 203.0.113.45). Two-factor authentication verified. Previous login: San Francisco, USA.',
        tenantId: 'tenant-010',
        tenantName: 'CloudTech Solutions',
        userName: 'admin@cloudtech.io',
        metadata: {
          location: 'Tokyo, Japan',
          ip: '203.0.113.45',
          mfaVerified: true,
          previousLocation: 'San Francisco, USA'
        }
      },
      {
        id: '13',
        timestamp: new Date(now.getTime() - 8 * 24 * 3600000), // 8 days ago
        type: ActivityType.SystemError,
        severity: 'error',
        title: 'Critical System Error',
        description: 'Database connection pool exhausted for tenant "HighTraffic Co". Error: Max connections (100) reached. Automatic scaling triggered. Additional 50 connections provisioned. Incident resolved in 3 minutes.',
        tenantId: 'tenant-011',
        tenantName: 'HighTraffic Co',
        metadata: {
          errorType: 'connection_pool_exhausted',
          maxConnections: 100,
          action: 'auto_scale',
          addedConnections: 50,
          resolutionTime: 3
        }
      },
      {
        id: '14',
        timestamp: new Date(now.getTime() - 9 * 24 * 3600000), // 9 days ago
        type: ActivityType.PaymentSuccess,
        severity: 'success',
        title: 'Annual Subscription Renewed',
        description: 'Enterprise Plus annual subscription renewed for $10,788.00 (10% discount). Payment method: ACH transfer. Contract extended through November 21, 2026. Invoice #INV-2025-001234 sent.',
        tenantId: 'tenant-012',
        tenantName: 'Fortune 500 Enterprise',
        metadata: {
          amount: 10788,
          billingCycle: 'annual',
          discount: 10,
          paymentMethod: 'ACH',
          invoiceNumber: 'INV-2025-001234',
          contractEnd: '2026-11-21'
        }
      },
      {
        id: '15',
        timestamp: new Date(now.getTime() - 10 * 24 * 3600000), // 10 days ago
        type: ActivityType.BackupCompleted,
        severity: 'info',
        title: 'Weekly Full Backup Completed',
        description: 'Weekly full system backup completed. Total data: 28.7 GB across 1,247,392 records from 156 active tenants. Backup verification passed. Geographic redundancy: 3 regions (US-East, EU-West, Asia-Pacific).',
        metadata: {
          sizeGB: 28.7,
          duration: 1847,
          recordCount: 1247392,
          tenantCount: 156,
          backupType: 'full',
          regions: ['us-east-1', 'eu-west-1', 'ap-southeast-1']
        }
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
