import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SoxComplianceReport,
  GdprComplianceReport,
  ActivityCorrelation
} from '../models/compliance-report.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ComplianceReportService {
  private apiUrl = `${environment.apiUrl}/compliancereports`;

  constructor(private http: HttpClient) {}

  // SOX Reports
  generateSoxFullReport(startDate: Date, endDate: Date, tenantId?: string): Observable<SoxComplianceReport> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    return this.http.get<SoxComplianceReport>(`${this.apiUrl}/sox/full`, { params });
  }

  generateFinancialAccessReport(startDate: Date, endDate: Date, tenantId?: string): Observable<any> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    return this.http.get<any>(`${this.apiUrl}/sox/financial-access`, { params });
  }

  generateUserAccessChangesReport(startDate: Date, endDate: Date, tenantId?: string): Observable<any> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    return this.http.get<any>(`${this.apiUrl}/sox/user-access-changes`, { params });
  }

  generateITGCReport(startDate: Date, endDate: Date, tenantId?: string): Observable<any> {
    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    return this.http.get<any>(`${this.apiUrl}/sox/itgc`, { params });
  }

  // GDPR Reports
  generateRightToAccessReport(userId: string): Observable<GdprComplianceReport> {
    return this.http.get<GdprComplianceReport>(`${this.apiUrl}/gdpr/right-to-access/${userId}`);
  }

  generateRightToBeForgottenReport(userId: string): Observable<GdprComplianceReport> {
    return this.http.get<GdprComplianceReport>(`${this.apiUrl}/gdpr/right-to-be-forgotten/${userId}`);
  }

  generateDataPortabilityReport(userId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/gdpr/data-portability/${userId}`, {
      responseType: 'blob'
    });
  }

  // Activity Correlation
  getUserActivityCorrelation(
    userId: string,
    startDate: Date,
    endDate: Date
  ): Observable<ActivityCorrelation> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<ActivityCorrelation>(
      `${this.apiUrl}/correlation/user-activity/${userId}`,
      { params }
    );
  }

  getCorrelatedEvents(correlationId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/correlation/events/${correlationId}`);
  }

  // Export Reports
  exportReport(reportType: string, format: string, params: any): Observable<Blob> {
    let httpParams = new HttpParams();
    Object.keys(params).forEach(key => {
      if (params[key]) {
        httpParams = httpParams.set(key, params[key].toString());
      }
    });

    return this.http.get(`${this.apiUrl}/export/${reportType}/${format}`, {
      params: httpParams,
      responseType: 'blob'
    });
  }
}
