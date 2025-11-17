import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  DashboardMetrics,
  InfrastructureHealth,
  SlowQuery,
  ApiPerformance,
  TenantActivity,
  SecurityEvent,
  Alert,
  ApiResponse,
  SlowQueriesParams,
  SlowQueriesByTenantParams,
  ApiPerformanceParams,
  EndpointPerformanceParams,
  SlaViolationsParams,
  TenantActivityParams,
  TenantActivityBySubdomainParams,
  AtRiskTenantsParams,
  SecurityEventsParams,
  CriticalSecurityEventsParams,
  AlertsParams,
  SecurityEventReviewRequest,
  AlertResolutionRequest
} from '../models/monitoring.models';

/**
 * FORTUNE 50-GRADE MONITORING SERVICE
 * Provides real-time platform health, performance metrics, and security monitoring
 *
 * SECURITY:
 * - SuperAdmin role required for ALL endpoints
 * - Only accessible via admin.hrms.com subdomain
 * - Read-only operations (zero impact on production)
 *
 * COMPLIANCE: ISO 27001, SOC 2, NIST 800-53
 * SLA TARGETS: P95 <200ms, P99 <500ms, Cache Hit Rate >95%, Error Rate <0.1%
 *
 * FEATURES:
 * - Dashboard overview metrics with 5-minute caching
 * - Infrastructure health monitoring (database, cache, connection pools)
 * - API performance tracking and SLA violation detection
 * - Multi-tenant activity monitoring and health scoring
 * - Security event monitoring and threat detection
 * - Alert management with acknowledgment and resolution
 */
@Injectable({
  providedIn: 'root'
})
export class MonitoringService {
  private http = inject(HttpClient);

  // API URL from environment configuration
  private apiUrl = environment.apiUrl;

  // Loading state signals
  private loadingSignal = signal<boolean>(false);
  readonly loading = this.loadingSignal.asReadonly();

  // ============================================
  // DASHBOARD OVERVIEW METRICS
  // ============================================

  /**
   * Get high-level dashboard metrics for SuperAdmin overview
   * CACHING: 5-minute TTL to reduce database load
   */
  getDashboardMetrics(): Observable<DashboardMetrics> {
    this.loadingSignal.set(true);

    return this.http.get<ApiResponse<DashboardMetrics>>(`${this.apiUrl}/monitoring/dashboard`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve dashboard metrics');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching dashboard metrics:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve dashboard metrics'));
      })
    );
  }

  /**
   * Force refresh of dashboard metrics (bypasses cache)
   * Use sparingly - triggers immediate database query
   */
  refreshDashboardMetrics(): Observable<DashboardMetrics> {
    this.loadingSignal.set(true);

    return this.http.post<ApiResponse<DashboardMetrics>>(`${this.apiUrl}/monitoring/dashboard/refresh`, {}).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to refresh dashboard metrics');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error refreshing dashboard metrics:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to refresh dashboard metrics'));
      })
    );
  }

  // ============================================
  // INFRASTRUCTURE HEALTH MONITORING
  // ============================================

  /**
   * Get infrastructure layer health metrics
   * Monitors database performance, connection pooling, cache hit rates
   */
  getInfrastructureHealth(): Observable<InfrastructureHealth> {
    this.loadingSignal.set(true);

    return this.http.get<ApiResponse<InfrastructureHealth>>(`${this.apiUrl}/monitoring/infrastructure/health`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve infrastructure health');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching infrastructure health:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve infrastructure health'));
      })
    );
  }

  /**
   * Get list of slow queries requiring optimization
   * Identifies queries exceeding performance thresholds
   */
  getSlowQueries(params?: SlowQueriesParams): Observable<SlowQuery[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.minExecutionTimeMs !== undefined) {
      httpParams = httpParams.set('minExecutionTimeMs', params.minExecutionTimeMs.toString());
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<SlowQuery[]>>(`${this.apiUrl}/monitoring/infrastructure/slow-queries`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve slow queries');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching slow queries:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve slow queries'));
      })
    );
  }

  /**
   * Get slow queries for a specific tenant
   * Useful for identifying tenant-specific performance issues
   */
  getSlowQueriesByTenant(params: SlowQueriesByTenantParams): Observable<SlowQuery[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params.minExecutionTimeMs !== undefined) {
      httpParams = httpParams.set('minExecutionTimeMs', params.minExecutionTimeMs.toString());
    }
    if (params.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<SlowQuery[]>>(
      `${this.apiUrl}/monitoring/infrastructure/slow-queries/${params.tenantSubdomain}`,
      { params: httpParams }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve slow queries');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching slow queries by tenant:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve slow queries by tenant'));
      })
    );
  }

  // ============================================
  // API PERFORMANCE MONITORING
  // ============================================

  /**
   * Get API endpoint performance metrics
   * Tracks response times, throughput, and error rates
   */
  getApiPerformance(params?: ApiPerformanceParams): Observable<ApiPerformance[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }
    if (params?.tenantSubdomain) {
      httpParams = httpParams.set('tenantSubdomain', params.tenantSubdomain);
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<ApiPerformance[]>>(`${this.apiUrl}/monitoring/api/performance`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve API performance');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching API performance:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve API performance'));
      })
    );
  }

  /**
   * Get performance metrics for a specific API endpoint
   * Useful for drilling down into endpoint-specific issues
   */
  getEndpointPerformance(params: EndpointPerformanceParams): Observable<ApiPerformance | null> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params.httpMethod) {
      httpParams = httpParams.set('httpMethod', params.httpMethod);
    }
    if (params.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }

    // Remove leading slash if present (API expects it without leading slash)
    const endpoint = params.endpoint.startsWith('/') ? params.endpoint.substring(1) : params.endpoint;

    return this.http.get<ApiResponse<ApiPerformance>>(
      `${this.apiUrl}/monitoring/api/performance/${endpoint}`,
      { params: httpParams }
    ).pipe(
      map(response => {
        if (!response.success) {
          return null; // 404 case - no performance data found
        }
        return response.data || null;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        if (error.status === 404) {
          return throwError(() => new Error('No performance data found for this endpoint'));
        }
        console.error('Error fetching endpoint performance:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve endpoint performance'));
      })
    );
  }

  /**
   * Get endpoints exceeding SLA targets
   * Identifies endpoints requiring immediate attention
   * SLA TARGETS: P95 <200ms, Error Rate <0.1%
   */
  getSlaViolations(params?: SlaViolationsParams): Observable<ApiPerformance[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }

    return this.http.get<ApiResponse<ApiPerformance[]>>(`${this.apiUrl}/monitoring/api/sla-violations`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve SLA violations');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching SLA violations:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve SLA violations'));
      })
    );
  }

  // ============================================
  // MULTI-TENANT ACTIVITY MONITORING
  // ============================================

  /**
   * Get activity and resource utilization metrics for all tenants
   * Provides visibility into tenant usage patterns and health scores
   */
  getTenantActivity(params?: TenantActivityParams): Observable<TenantActivity[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }
    if (params?.minActiveUsers !== undefined) {
      httpParams = httpParams.set('minActiveUsers', params.minActiveUsers.toString());
    }
    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params?.sortBy) {
      httpParams = httpParams.set('sortBy', params.sortBy);
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<TenantActivity[]>>(`${this.apiUrl}/monitoring/tenants/activity`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve tenant activity');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching tenant activity:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve tenant activity'));
      })
    );
  }

  /**
   * Get activity metrics for a specific tenant
   * Detailed view of single tenant performance and resource usage
   */
  getTenantActivityBySubdomain(params: TenantActivityBySubdomainParams): Observable<TenantActivity | null> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }

    return this.http.get<ApiResponse<TenantActivity>>(
      `${this.apiUrl}/monitoring/tenants/activity/${params.tenantSubdomain}`,
      { params: httpParams }
    ).pipe(
      map(response => {
        if (!response.success) {
          return null; // 404 case - tenant not found
        }
        return response.data || null;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        if (error.status === 404) {
          return throwError(() => new Error('Tenant not found'));
        }
        console.error('Error fetching tenant activity by subdomain:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve tenant activity'));
      })
    );
  }

  /**
   * Get tenants with health score below threshold
   * Identifies at-risk tenants requiring attention (low activity, high errors)
   * HEALTH SCORE: 0-100 based on activity, performance, error rate
   */
  getAtRiskTenants(params?: AtRiskTenantsParams): Observable<TenantActivity[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.maxHealthScore !== undefined) {
      httpParams = httpParams.set('maxHealthScore', params.maxHealthScore.toString());
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<TenantActivity[]>>(`${this.apiUrl}/monitoring/tenants/at-risk`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve at-risk tenants');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching at-risk tenants:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve at-risk tenants'));
      })
    );
  }

  // ============================================
  // SECURITY EVENT MONITORING
  // ============================================

  /**
   * Get security events and threat detection metrics
   * Monitors authentication failures, IDOR attempts, rate limit violations
   * COMPLIANCE: SOC 2, ISO 27001, PCI-DSS security logging requirements
   */
  getSecurityEvents(params?: SecurityEventsParams): Observable<SecurityEvent[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }
    if (params?.eventType) {
      httpParams = httpParams.set('eventType', params.eventType);
    }
    if (params?.severity) {
      httpParams = httpParams.set('severity', params.severity);
    }
    if (params?.tenantSubdomain) {
      httpParams = httpParams.set('tenantSubdomain', params.tenantSubdomain);
    }
    if (params?.isBlocked !== undefined) {
      httpParams = httpParams.set('isBlocked', params.isBlocked.toString());
    }
    if (params?.isReviewed !== undefined) {
      httpParams = httpParams.set('isReviewed', params.isReviewed.toString());
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<SecurityEvent[]>>(`${this.apiUrl}/monitoring/security/events`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve security events');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching security events:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve security events'));
      })
    );
  }

  /**
   * Get critical security events requiring immediate review
   * Auto-filters for Critical/High severity unreviewed events
   */
  getCriticalSecurityEvents(params?: CriticalSecurityEventsParams): Observable<SecurityEvent[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }

    return this.http.get<ApiResponse<SecurityEvent[]>>(`${this.apiUrl}/monitoring/security/critical`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve critical security events');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching critical security events:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve critical security events'));
      })
    );
  }

  /**
   * Mark security event as reviewed
   * Records reviewer identity and notes for audit trail
   */
  markSecurityEventReviewed(eventId: number, notes?: string): Observable<void> {
    this.loadingSignal.set(true);

    const request: SecurityEventReviewRequest = {
      reviewNotes: notes
    };

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/events/${eventId}/review`,
      request
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to mark security event as reviewed');
        }
        return;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        if (error.status === 404) {
          return throwError(() => new Error('Security event not found'));
        }
        console.error('Error marking security event as reviewed:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to mark security event as reviewed'));
      })
    );
  }

  // ============================================
  // ALERT MANAGEMENT
  // ============================================

  /**
   * Get active and historical alerts
   * Monitors SLA violations, security incidents, capacity issues
   * ALERT SEVERITY: Critical (immediate action), High (1h), Medium (4h), Low (24h)
   */
  getAlerts(params?: AlertsParams): Observable<Alert[]> {
    this.loadingSignal.set(true);

    let httpParams = new HttpParams();
    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }
    if (params?.severity) {
      httpParams = httpParams.set('severity', params.severity);
    }
    if (params?.alertType) {
      httpParams = httpParams.set('alertType', params.alertType);
    }
    if (params?.tenantSubdomain) {
      httpParams = httpParams.set('tenantSubdomain', params.tenantSubdomain);
    }
    if (params?.periodStart) {
      httpParams = httpParams.set('periodStart', params.periodStart);
    }
    if (params?.periodEnd) {
      httpParams = httpParams.set('periodEnd', params.periodEnd);
    }
    if (params?.limit !== undefined) {
      httpParams = httpParams.set('limit', params.limit.toString());
    }

    return this.http.get<ApiResponse<Alert[]>>(`${this.apiUrl}/monitoring/alerts`, { params: httpParams }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve alerts');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching alerts:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve alerts'));
      })
    );
  }

  /**
   * Get active critical and high severity alerts
   * Quick view of alerts requiring immediate attention
   */
  getActiveAlerts(): Observable<Alert[]> {
    this.loadingSignal.set(true);

    return this.http.get<ApiResponse<Alert[]>>(`${this.apiUrl}/monitoring/alerts/active`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve active alerts');
        }
        return response.data;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching active alerts:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve active alerts'));
      })
    );
  }

  /**
   * Acknowledge an alert (mark as being worked on)
   * Records who acknowledged and when for accountability
   */
  acknowledgeAlert(alertId: number): Observable<void> {
    this.loadingSignal.set(true);

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/alerts/${alertId}/acknowledge`,
      {}
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to acknowledge alert');
        }
        return;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        if (error.status === 404) {
          return throwError(() => new Error('Alert not found or already acknowledged'));
        }
        console.error('Error acknowledging alert:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to acknowledge alert'));
      })
    );
  }

  /**
   * Resolve an alert (mark as fixed)
   * Records resolution details for post-mortem analysis
   */
  resolveAlert(alertId: number, notes?: string): Observable<void> {
    this.loadingSignal.set(true);

    const request: AlertResolutionRequest = {
      resolutionNotes: notes
    };

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/alerts/${alertId}/resolve`,
      request
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to resolve alert');
        }
        return;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        if (error.status === 404) {
          return throwError(() => new Error('Alert not found or already resolved'));
        }
        console.error('Error resolving alert:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to resolve alert'));
      })
    );
  }
}
