import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  SecurityAlert,
  SecurityAlertDetail,
  SecurityAlertFilter,
  SecurityAlertListResponse,
  SecurityAlertResponse,
  SecurityAlertStatistics,
  SecurityAlertStatisticsResponse,
  AlertSeverityCountsResponse,
  AlertSeverityCounts,
  AssignAlertRequest,
  ResolveAlertRequest,
  FalsePositiveRequest,
  EscalateAlertRequest,
  SecurityAlertStatus,
  AuditSeverity,
  SecurityAlertType
} from '../models/security-alert.model';

/**
 * Security Alert Service
 * Handles all API interactions for security alert management
 * Supports real-time threat detection and compliance monitoring
 */
@Injectable({
  providedIn: 'root'
})
export class SecurityAlertService {
  private readonly apiUrl = `${environment.apiUrl}/security-alerts`;

  constructor(private http: HttpClient) {}

  // ============================================
  // ALERT RETRIEVAL METHODS
  // ============================================

  /**
   * Get security alerts with filtering and pagination
   */
  getAlerts(filter: SecurityAlertFilter): Observable<SecurityAlertListResponse> {
    const params = this.buildHttpParams(filter);
    return this.http.get<SecurityAlertListResponse>(this.apiUrl, { params })
      .pipe(map(response => this.transformListResponse(response)));
  }

  /**
   * Get a single security alert by ID
   */
  getAlertById(id: string): Observable<SecurityAlert> {
    return this.http.get<SecurityAlertResponse>(`${this.apiUrl}/${id}`)
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Get active alert counts grouped by severity
   */
  getActiveAlertCountsBySeverity(tenantId?: string): Observable<AlertSeverityCounts> {
    let params = new HttpParams();
    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }
    return this.http.get<AlertSeverityCountsResponse>(`${this.apiUrl}/counts/by-severity`, { params })
      .pipe(map(response => response.data));
  }

  /**
   * Get recent critical alerts (last 24 hours by default)
   */
  getRecentCriticalAlerts(tenantId?: string, hours: number = 24): Observable<SecurityAlert[]> {
    let params = new HttpParams().set('hours', hours.toString());
    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }
    return this.http.get<SecurityAlertListResponse>(`${this.apiUrl}/critical/recent`, { params })
      .pipe(map(response => response.data.map(alert => this.transformAlert(alert))));
  }

  /**
   * Get alert statistics for a time period
   */
  getAlertStatistics(
    tenantId: string | undefined,
    startDate: Date,
    endDate: Date
  ): Observable<SecurityAlertStatistics> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    return this.http.get<SecurityAlertStatisticsResponse>(`${this.apiUrl}/statistics`, { params })
      .pipe(map(response => response.data));
  }

  // ============================================
  // ALERT LIFECYCLE MANAGEMENT
  // ============================================

  /**
   * Acknowledge a security alert
   */
  acknowledgeAlert(alertId: string): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/acknowledge`, {})
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Assign a security alert to a user
   */
  assignAlert(alertId: string, request: AssignAlertRequest): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/assign`, request)
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Mark alert as in progress
   */
  markAlertInProgress(alertId: string): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/in-progress`, {})
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Resolve a security alert
   */
  resolveAlert(alertId: string, request: ResolveAlertRequest): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/resolve`, request)
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Mark alert as false positive
   */
  markAlertAsFalsePositive(alertId: string, request: FalsePositiveRequest): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/false-positive`, request)
      .pipe(map(response => this.transformAlert(response.data)));
  }

  /**
   * Escalate a security alert
   */
  escalateAlert(alertId: string, request: EscalateAlertRequest): Observable<SecurityAlert> {
    return this.http.post<SecurityAlertResponse>(`${this.apiUrl}/${alertId}/escalate`, request)
      .pipe(map(response => this.transformAlert(response.data)));
  }

  // ============================================
  // HELPER METHODS
  // ============================================

  /**
   * Build HTTP params from filter object
   */
  private buildHttpParams(filter: SecurityAlertFilter): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.tenantId) {
      params = params.set('tenantId', filter.tenantId);
    }

    if (filter.status !== undefined) {
      params = params.set('status', filter.status.toString());
    }

    if (filter.severity !== undefined) {
      params = params.set('severity', filter.severity.toString());
    }

    if (filter.alertType !== undefined) {
      params = params.set('alertType', filter.alertType.toString());
    }

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString());
    }

    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString());
    }

    if (filter.sortBy) {
      params = params.set('sortBy', filter.sortBy);
    }

    if (filter.sortDescending !== undefined) {
      params = params.set('sortDescending', filter.sortDescending.toString());
    }

    return params;
  }

  /**
   * Transform API response dates from strings to Date objects
   */
  private transformAlert(alert: any): SecurityAlert {
    return {
      ...alert,
      createdAt: new Date(alert.createdAt),
      detectedAt: new Date(alert.detectedAt),
      acknowledgedAt: alert.acknowledgedAt ? new Date(alert.acknowledgedAt) : undefined,
      resolvedAt: alert.resolvedAt ? new Date(alert.resolvedAt) : undefined,
      emailSentAt: alert.emailSentAt ? new Date(alert.emailSentAt) : undefined,
      smsSentAt: alert.smsSentAt ? new Date(alert.smsSentAt) : undefined,
      slackSentAt: alert.slackSentAt ? new Date(alert.slackSentAt) : undefined,
      siemSentAt: alert.siemSentAt ? new Date(alert.siemSentAt) : undefined,
      escalatedAt: alert.escalatedAt ? new Date(alert.escalatedAt) : undefined
    };
  }

  /**
   * Transform list response
   */
  private transformListResponse(response: SecurityAlertListResponse): SecurityAlertListResponse {
    return {
      ...response,
      data: response.data.map(alert => this.transformAlert(alert))
    };
  }

  /**
   * Get alert type display name
   */
  getAlertTypeName(type: SecurityAlertType): string {
    return SecurityAlertType[type].replace(/_/g, ' ');
  }

  /**
   * Get alert status display name
   */
  getAlertStatusName(status: SecurityAlertStatus): string {
    return SecurityAlertStatus[status].replace(/_/g, ' ');
  }

  /**
   * Get severity display name
   */
  getSeverityName(severity: AuditSeverity): string {
    return AuditSeverity[severity];
  }
}
