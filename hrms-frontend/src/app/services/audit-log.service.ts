import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  AuditLog,
  AuditLogDetail,
  AuditLogFilter,
  AuditLogStatistics,
  PagedResult,
  UserActivity
} from '../models/audit-log.model';

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  private superAdminApiUrl = `${environment.apiUrl}/superadmin/AuditLog`;
  private tenantApiUrl = `${environment.apiUrl}/tenant/TenantAuditLog`;

  constructor(private http: HttpClient) {}

  // ============================================
  // SUPERADMIN METHODS (System-wide access)
  // ============================================

  /**
   * Get system-wide audit logs (all tenants)
   * @param filter Filter criteria
   * @returns Observable of paged audit logs
   */
  getSystemAuditLogs(filter: AuditLogFilter): Observable<PagedResult<AuditLog>> {
    const params = this.buildHttpParams(filter);
    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(this.superAdminApiUrl, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  /**
   * Get single audit log by ID (SuperAdmin)
   * @param id Audit log ID
   * @returns Observable of audit log detail
   */
  getSystemAuditLogById(id: string): Observable<AuditLogDetail> {
    return this.http.get<ApiResponse<AuditLogDetail>>(`${this.superAdminApiUrl}/${id}`)
      .pipe(map(response => this.transformAuditLogDetail(response.data)));
  }

  /**
   * Get system-wide statistics
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @returns Observable of statistics
   */
  getSystemStatistics(startDate?: Date, endDate?: Date): Observable<AuditLogStatistics> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<AuditLogStatistics>>(`${this.superAdminApiUrl}/statistics`, { params })
      .pipe(map(response => response.data));
  }

  /**
   * Export system-wide audit logs to CSV
   * @param filter Filter criteria
   * @returns Observable of Blob
   */
  exportSystemLogs(filter: AuditLogFilter): Observable<Blob> {
    return this.http.post(`${this.superAdminApiUrl}/export`, filter, {
      responseType: 'blob'
    });
  }

  /**
   * Get failed login attempts (system-wide)
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @param pageNumber Page number
   * @param pageSize Page size
   * @returns Observable of paged failed logins
   */
  getSystemFailedLogins(
    startDate?: Date,
    endDate?: Date,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(`${this.superAdminApiUrl}/failed-logins`, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  /**
   * Get critical security events (system-wide)
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @param pageNumber Page number
   * @param pageSize Page size
   * @returns Observable of paged critical events
   */
  getSystemCriticalEvents(
    startDate?: Date,
    endDate?: Date,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(`${this.superAdminApiUrl}/critical-events`, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  // ============================================
  // TENANT METHODS (Tenant-scoped access)
  // ============================================

  /**
   * Get tenant-specific audit logs (auto-filtered by backend)
   * @param filter Filter criteria
   * @returns Observable of paged audit logs
   */
  getTenantAuditLogs(filter: AuditLogFilter): Observable<PagedResult<AuditLog>> {
    const params = this.buildHttpParams(filter);
    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(this.tenantApiUrl, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  /**
   * Get single audit log by ID (validates tenant ownership)
   * @param id Audit log ID
   * @returns Observable of audit log detail
   */
  getTenantAuditLogById(id: string): Observable<AuditLogDetail> {
    return this.http.get<ApiResponse<AuditLogDetail>>(`${this.tenantApiUrl}/${id}`)
      .pipe(map(response => this.transformAuditLogDetail(response.data)));
  }

  /**
   * Get tenant statistics
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @returns Observable of statistics
   */
  getTenantStatistics(startDate?: Date, endDate?: Date): Observable<AuditLogStatistics> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<AuditLogStatistics>>(`${this.tenantApiUrl}/statistics`, { params })
      .pipe(map(response => response.data));
  }

  /**
   * Export tenant audit logs to CSV
   * @param filter Filter criteria
   * @returns Observable of Blob
   */
  exportTenantLogs(filter: AuditLogFilter): Observable<Blob> {
    return this.http.post(`${this.tenantApiUrl}/export`, filter, {
      responseType: 'blob'
    });
  }

  /**
   * Get tenant failed login attempts
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @param pageNumber Page number
   * @param pageSize Page size
   * @returns Observable of paged failed logins
   */
  getTenantFailedLogins(
    startDate?: Date,
    endDate?: Date,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(`${this.tenantApiUrl}/failed-logins`, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  /**
   * Get sensitive data changes (salary updates, etc.)
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @param pageNumber Page number
   * @param pageSize Page size
   * @returns Observable of paged sensitive changes
   */
  getTenantSensitiveChanges(
    startDate?: Date,
    endDate?: Date,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(`${this.tenantApiUrl}/sensitive-changes`, { params })
      .pipe(map(response => this.transformPagedResult(response.data)));
  }

  /**
   * Get user activity report
   * @param startDate Optional start date
   * @param endDate Optional end date
   * @returns Observable of user activities
   */
  getTenantUserActivity(startDate?: Date, endDate?: Date): Observable<UserActivity[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    return this.http.get<ApiResponse<UserActivity[]>>(`${this.tenantApiUrl}/user-activity`, { params })
      .pipe(map(response => response.data));
  }

  // ============================================
  // PRIVATE HELPER METHODS
  // ============================================

  /**
   * Build HTTP params from filter object
   * @param filter Filter criteria
   * @returns HttpParams
   */
  private buildHttpParams(filter: AuditLogFilter): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate.toISOString());
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate.toISOString());
    }
    if (filter.userEmail) {
      params = params.set('userEmail', filter.userEmail);
    }
    if (filter.entityType) {
      params = params.set('entityType', filter.entityType);
    }
    if (filter.entityId) {
      params = params.set('entityId', filter.entityId);
    }
    if (filter.ipAddress) {
      params = params.set('ipAddress', filter.ipAddress);
    }
    if (filter.correlationId) {
      params = params.set('correlationId', filter.correlationId);
    }
    if (filter.changedFieldsSearch) {
      params = params.set('changedFieldsSearch', filter.changedFieldsSearch);
    }
    if (filter.sortBy) {
      params = params.set('sortBy', filter.sortBy);
    }
    if (filter.sortDescending !== undefined) {
      params = params.set('sortDescending', filter.sortDescending.toString());
    }
    if (filter.success !== undefined) {
      params = params.set('success', filter.success.toString());
    }

    // Array parameters
    if (filter.actionTypes && filter.actionTypes.length > 0) {
      filter.actionTypes.forEach(at => {
        params = params.append('actionTypes', at.toString());
      });
    }
    if (filter.categories && filter.categories.length > 0) {
      filter.categories.forEach(c => {
        params = params.append('categories', c.toString());
      });
    }
    if (filter.severities && filter.severities.length > 0) {
      filter.severities.forEach(s => {
        params = params.append('severities', s.toString());
      });
    }

    return params;
  }

  /**
   * Transform paged result (convert date strings to Date objects)
   * @param result Paged result from API
   * @returns Transformed paged result
   */
  private transformPagedResult(result: PagedResult<AuditLog>): PagedResult<AuditLog> {
    return {
      ...result,
      items: result.items.map(log => this.transformAuditLog(log))
    };
  }

  /**
   * Transform audit log (convert date strings to Date objects)
   * @param log Audit log from API
   * @returns Transformed audit log
   */
  private transformAuditLog(log: any): AuditLog {
    return {
      ...log,
      performedAt: new Date(log.performedAt)
    };
  }

  /**
   * Transform audit log detail (convert date strings to Date objects)
   * @param log Audit log detail from API
   * @returns Transformed audit log detail
   */
  private transformAuditLogDetail(log: any): AuditLogDetail {
    return {
      ...log,
      performedAt: new Date(log.performedAt)
    };
  }

  /**
   * Download blob as file
   * @param blob Blob data
   * @param filename Filename
   */
  downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
