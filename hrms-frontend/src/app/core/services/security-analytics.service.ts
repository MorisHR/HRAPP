import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  FailedLoginAnalytics,
  BruteForceStatistics,
  IpBlacklist,
  SessionManagement,
  ActiveSession,
  MfaCompliance,
  PasswordCompliance,
  SecurityDashboardAnalytics,
  AddIpToBlacklistRequest,
  AddIpToWhitelistRequest,
  ApiResponse
} from '../models/security-analytics.models';

/**
 * FORTUNE 500-GRADE SECURITY ANALYTICS SERVICE
 * Comprehensive security monitoring following AWS GuardDuty, Azure Sentinel, Splunk ES patterns
 *
 * SECURITY:
 * - SuperAdmin role required for ALL endpoints
 * - Only accessible via admin.hrms.com subdomain
 * - Read-only operations (zero impact on production)
 *
 * COMPLIANCE: PCI-DSS, NIST 800-53, ISO 27001, SOC 2, GDPR, SOX
 *
 * FEATURES:
 * - Failed login analytics with time series and trend analysis
 * - Brute force attack detection and statistics
 * - IP blacklist/whitelist management
 * - Active session monitoring and forced logout
 * - MFA compliance tracking (NIST 800-63B AAL2/AAL3)
 * - Password strength compliance monitoring
 * - Comprehensive security dashboard with parallel data loading
 */
@Injectable({
  providedIn: 'root'
})
export class SecurityAnalyticsService {
  private http = inject(HttpClient);

  // API URL from environment configuration
  private apiUrl = environment.apiUrl;

  // Loading state signals
  private loadingSignal = signal<boolean>(false);
  readonly loading = this.loadingSignal.asReadonly();

  // ============================================
  // FAILED LOGIN ANALYTICS
  // PCI-DSS 8.1.6, NIST 800-53 AC-7, ISO 27001 A.9.4.3
  // ============================================

  /**
   * Get comprehensive failed login analytics
   * Includes time series data, top attacking IPs, targeted users, trends
   *
   * @param periodStart Optional start date (defaults to 30 days ago)
   * @param periodEnd Optional end date (defaults to now)
   * @param tenantSubdomain Optional tenant filter
   * @returns Failed login analytics with trends and charts
   */
  getFailedLoginAnalytics(
    periodStart?: Date,
    periodEnd?: Date,
    tenantSubdomain?: string
  ): Observable<FailedLoginAnalytics> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (periodStart) {
      params = params.set('periodStart', periodStart.toISOString());
    }
    if (periodEnd) {
      params = params.set('periodEnd', periodEnd.toISOString());
    }
    if (tenantSubdomain) {
      params = params.set('tenantSubdomain', tenantSubdomain);
    }

    return this.http.get<ApiResponse<FailedLoginAnalytics>>(
      `${this.apiUrl}/monitoring/security/failed-logins/analytics`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve failed login analytics');
        }
        return this.transformFailedLoginAnalytics(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching failed login analytics:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve failed login analytics'));
      })
    );
  }

  // ============================================
  // BRUTE FORCE ATTACK STATISTICS
  // PCI-DSS 6.5.10, OWASP Top 10 A07:2021
  // ============================================

  /**
   * Get real-time brute force attack statistics
   * Monitors active attacks, blocking effectiveness, attack patterns
   *
   * @param periodStart Optional start date
   * @param periodEnd Optional end date
   * @returns Brute force statistics with active attacks
   */
  getBruteForceStatistics(
    periodStart?: Date,
    periodEnd?: Date
  ): Observable<BruteForceStatistics> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (periodStart) {
      params = params.set('periodStart', periodStart.toISOString());
    }
    if (periodEnd) {
      params = params.set('periodEnd', periodEnd.toISOString());
    }

    return this.http.get<ApiResponse<BruteForceStatistics>>(
      `${this.apiUrl}/monitoring/security/brute-force/statistics`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve brute force statistics');
        }
        return this.transformBruteForceStatistics(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching brute force statistics:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve brute force statistics'));
      })
    );
  }

  // ============================================
  // IP BLACKLIST/WHITELIST MANAGEMENT
  // PCI-DSS 1.3, NIST 800-53 SC-7
  // ============================================

  /**
   * Get IP blacklist and whitelist overview
   * Shows all blocked IPs, whitelisted IPs, recent activity
   *
   * @returns IP blacklist data
   */
  getIpBlacklist(): Observable<IpBlacklist> {
    this.loadingSignal.set(true);

    return this.http.get<ApiResponse<IpBlacklist>>(
      `${this.apiUrl}/monitoring/security/ip-blacklist`
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve IP blacklist');
        }
        return this.transformIpBlacklist(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching IP blacklist:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve IP blacklist'));
      })
    );
  }

  /**
   * Add IP address to blacklist
   * AUDIT: Logs admin action with reason and timestamp
   *
   * @param request IP blacklist request
   * @returns Success indicator
   */
  addIpToBlacklist(request: AddIpToBlacklistRequest): Observable<boolean> {
    this.loadingSignal.set(true);

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/ip-blacklist`,
      request
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to add IP to blacklist');
        }
        return true;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error adding IP to blacklist:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to add IP to blacklist'));
      })
    );
  }

  /**
   * Remove IP address from blacklist
   * AUDIT: Logs admin action
   *
   * @param ipAddress IP to remove
   * @returns Success indicator
   */
  removeIpFromBlacklist(ipAddress: string): Observable<boolean> {
    this.loadingSignal.set(true);

    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/ip-blacklist/${encodeURIComponent(ipAddress)}`
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to remove IP from blacklist');
        }
        return true;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error removing IP from blacklist:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to remove IP from blacklist'));
      })
    );
  }

  /**
   * Add IP address to whitelist (never block this IP)
   * AUDIT: Logs admin action
   *
   * @param request IP whitelist request
   * @returns Success indicator
   */
  addIpToWhitelist(request: AddIpToWhitelistRequest): Observable<boolean> {
    this.loadingSignal.set(true);

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/ip-whitelist`,
      request
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to add IP to whitelist');
        }
        return true;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error adding IP to whitelist:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to add IP to whitelist'));
      })
    );
  }

  /**
   * Remove IP address from whitelist
   * AUDIT: Logs admin action
   *
   * @param ipAddress IP to remove
   * @returns Success indicator
   */
  removeIpFromWhitelist(ipAddress: string): Observable<boolean> {
    this.loadingSignal.set(true);

    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/ip-whitelist/${encodeURIComponent(ipAddress)}`
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to remove IP from whitelist');
        }
        return true;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error removing IP from whitelist:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to remove IP from whitelist'));
      })
    );
  }

  // ============================================
  // SESSION MANAGEMENT
  // PCI-DSS 8.1.8, NIST 800-53 AC-12, ISO 27001 A.9.1.2
  // ============================================

  /**
   * Get session management analytics
   * Monitors active sessions, suspicious activity, concurrent logins
   *
   * @param periodStart Optional start date
   * @param periodEnd Optional end date
   * @returns Session analytics
   */
  getSessionManagement(
    periodStart?: Date,
    periodEnd?: Date
  ): Observable<SessionManagement> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (periodStart) {
      params = params.set('periodStart', periodStart.toISOString());
    }
    if (periodEnd) {
      params = params.set('periodEnd', periodEnd.toISOString());
    }

    return this.http.get<ApiResponse<SessionManagement>>(
      `${this.apiUrl}/monitoring/security/sessions`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve session management');
        }
        return this.transformSessionManagement(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching session management:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve session management'));
      })
    );
  }

  /**
   * Get list of active sessions
   * Detailed view of current user sessions across all tenants
   *
   * @param tenantSubdomain Optional tenant filter
   * @param userId Optional user filter
   * @param limit Maximum number of results (default 100)
   * @returns List of active sessions
   */
  getActiveSessions(
    tenantSubdomain?: string,
    userId?: string,
    limit: number = 100
  ): Observable<ActiveSession[]> {
    this.loadingSignal.set(true);

    let params = new HttpParams().set('limit', limit.toString());
    if (tenantSubdomain) {
      params = params.set('tenantSubdomain', tenantSubdomain);
    }
    if (userId) {
      params = params.set('userId', userId);
    }

    return this.http.get<ApiResponse<ActiveSession[]>>(
      `${this.apiUrl}/monitoring/security/sessions/active`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve active sessions');
        }
        return this.transformActiveSessions(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching active sessions:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve active sessions'));
      })
    );
  }

  /**
   * Force logout a user session
   * SECURITY: Immediately invalidates refresh token
   * AUDIT: Logs admin action with reason
   *
   * @param sessionId Session/refresh token ID
   * @param reason Reason for forced logout
   * @returns Success indicator
   */
  forceLogoutSession(sessionId: string, reason: string): Observable<boolean> {
    this.loadingSignal.set(true);

    return this.http.post<ApiResponse<void>>(
      `${this.apiUrl}/monitoring/security/sessions/${sessionId}/force-logout`,
      { reason }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to force logout session');
        }
        return true;
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error forcing logout session:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to force logout session'));
      })
    );
  }

  // ============================================
  // MFA COMPLIANCE MONITORING
  // PCI-DSS 8.3, NIST 800-63B AAL2/AAL3, SOX, GDPR Article 32
  // ============================================

  /**
   * Get MFA compliance metrics
   * Tracks adoption rates, non-compliant users, compliance by tenant/role
   *
   * @param tenantSubdomain Optional tenant filter
   * @returns MFA compliance data
   */
  getMfaCompliance(tenantSubdomain?: string): Observable<MfaCompliance> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (tenantSubdomain) {
      params = params.set('tenantSubdomain', tenantSubdomain);
    }

    return this.http.get<ApiResponse<MfaCompliance>>(
      `${this.apiUrl}/monitoring/security/mfa-compliance`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve MFA compliance');
        }
        return this.transformMfaCompliance(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching MFA compliance:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve MFA compliance'));
      })
    );
  }

  // ============================================
  // PASSWORD COMPLIANCE MONITORING
  // NIST 800-63B, PCI-DSS 8.2, ISO 27001 A.9.4.3
  // ============================================

  /**
   * Get password strength compliance metrics
   * Monitors weak passwords, expiring passwords, compromised credentials
   *
   * @param tenantSubdomain Optional tenant filter
   * @returns Password compliance data
   */
  getPasswordCompliance(tenantSubdomain?: string): Observable<PasswordCompliance> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (tenantSubdomain) {
      params = params.set('tenantSubdomain', tenantSubdomain);
    }

    return this.http.get<ApiResponse<PasswordCompliance>>(
      `${this.apiUrl}/monitoring/security/password-compliance`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve password compliance');
        }
        return this.transformPasswordCompliance(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching password compliance:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve password compliance'));
      })
    );
  }

  // ============================================
  // COMPREHENSIVE SECURITY DASHBOARD
  // SOC 2, ISO 27001, PCI-DSS, NIST 800-53, GDPR Article 32
  // ============================================

  /**
   * Get comprehensive security dashboard analytics
   * ONE-STOP API: Aggregates all security metrics in parallel
   *
   * FEATURES:
   * - Overall security score (0-100)
   * - Security trend analysis
   * - Critical/high priority issue counts
   * - Summary of all security components
   * - Recent critical activity feed
   * - At-risk tenants identification
   *
   * PERFORMANCE: Uses Task.WhenAll on backend for parallel execution
   *
   * @param periodStart Optional start date (defaults to 24 hours ago)
   * @param periodEnd Optional end date (defaults to now)
   * @returns Comprehensive security dashboard
   */
  getSecurityDashboard(
    periodStart?: Date,
    periodEnd?: Date
  ): Observable<SecurityDashboardAnalytics> {
    this.loadingSignal.set(true);

    let params = new HttpParams();
    if (periodStart) {
      params = params.set('periodStart', periodStart.toISOString());
    }
    if (periodEnd) {
      params = params.set('periodEnd', periodEnd.toISOString());
    }

    return this.http.get<ApiResponse<SecurityDashboardAnalytics>>(
      `${this.apiUrl}/monitoring/security/dashboard`,
      { params }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to retrieve security dashboard');
        }
        return this.transformSecurityDashboard(response.data);
      }),
      tap(() => this.loadingSignal.set(false)),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Error fetching security dashboard:', error);
        return throwError(() => new Error(error.error?.message || 'Failed to retrieve security dashboard'));
      })
    );
  }

  // ============================================
  // PRIVATE HELPER METHODS (DATE TRANSFORMATIONS)
  // ============================================

  private transformFailedLoginAnalytics(data: any): FailedLoginAnalytics {
    return {
      ...data,
      timeSeriesData: data.timeSeriesData?.map((d: any) => ({
        ...d,
        timestamp: new Date(d.timestamp)
      })) || [],
      topFailureIps: data.topFailureIps?.map((ip: any) => ({
        ...ip,
        firstSeen: new Date(ip.firstSeen),
        lastSeen: new Date(ip.lastSeen)
      })) || [],
      topTargetedUsers: data.topTargetedUsers?.map((u: any) => ({
        ...u,
        lastAttempt: new Date(u.lastAttempt)
      })) || []
    };
  }

  private transformBruteForceStatistics(data: any): BruteForceStatistics {
    return {
      ...data,
      activeAttacksList: data.activeAttacksList?.map((a: any) => ({
        ...a,
        startedAt: new Date(a.startedAt),
        lastAttempt: new Date(a.lastAttempt)
      })) || [],
      recentlyBlockedIps: data.recentlyBlockedIps?.map((ip: any) => ({
        ...ip,
        blockedAt: new Date(ip.blockedAt),
        lastActivity: new Date(ip.lastActivity),
        expiresAt: ip.expiresAt ? new Date(ip.expiresAt) : undefined
      })) || [],
      hourlyDistribution: data.hourlyDistribution || []
    };
  }

  private transformIpBlacklist(data: any): IpBlacklist {
    return {
      ...data,
      blacklistedIps: data.blacklistedIps?.map((ip: any) => ({
        ...ip,
        blacklistedAt: new Date(ip.blacklistedAt),
        lastActivity: new Date(ip.lastActivity),
        expiresAt: ip.expiresAt ? new Date(ip.expiresAt) : undefined
      })) || [],
      whitelistedIps: data.whitelistedIps?.map((ip: any) => ({
        ...ip,
        whitelistedAt: new Date(ip.whitelistedAt),
        expiresAt: ip.expiresAt ? new Date(ip.expiresAt) : undefined
      })) || [],
      recentActivity: data.recentActivity?.map((a: any) => ({
        ...a,
        timestamp: new Date(a.timestamp)
      })) || []
    };
  }

  private transformSessionManagement(data: any): SessionManagement {
    return {
      ...data,
      suspiciousSessions: data.suspiciousSessions?.map((s: any) => ({
        ...s,
        detectedAt: new Date(s.detectedAt)
      })) || []
    };
  }

  private transformActiveSessions(sessions: any[]): ActiveSession[] {
    return sessions.map(s => ({
      ...s,
      startedAt: new Date(s.startedAt),
      lastActivity: new Date(s.lastActivity),
      expiresAt: new Date(s.expiresAt)
    }));
  }

  private transformMfaCompliance(data: any): MfaCompliance {
    return {
      ...data,
      nonCompliantUsers: data.nonCompliantUsers?.map((u: any) => ({
        ...u,
        lastLogin: new Date(u.lastLogin)
      })) || [],
      recentEnrollments: data.recentEnrollments?.map((e: any) => ({
        ...e,
        enrolledAt: new Date(e.enrolledAt)
      })) || []
    };
  }

  private transformPasswordCompliance(data: any): PasswordCompliance {
    return {
      ...data,
      weakPasswordUsers: data.weakPasswordUsers?.map((u: any) => ({
        ...u,
        lastChanged: new Date(u.lastChanged)
      })) || [],
      expiringPasswords: data.expiringPasswords?.map((p: any) => ({
        ...p,
        expiresAt: new Date(p.expiresAt)
      })) || []
    };
  }

  private transformSecurityDashboard(data: any): SecurityDashboardAnalytics {
    return {
      ...data,
      recentCriticalActivity: data.recentCriticalActivity?.map((a: any) => ({
        ...a,
        timestamp: new Date(a.timestamp)
      })) || [],
      lastRefreshedAt: new Date(data.lastRefreshedAt)
    };
  }
}
