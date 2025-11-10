import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { DetectedAnomaly, AnomalyStatus, AnomalyStatistics } from '../models/anomaly.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AnomalyDetectionService {
  private apiUrl = `${environment.apiUrl}/anomalydetection`;

  constructor(private http: HttpClient) {}

  getAnomalies(
    tenantId?: string,
    status?: AnomalyStatus,
    page: number = 1,
    pageSize: number = 20
  ): Observable<{ anomalies: DetectedAnomaly[], totalCount: number }> {
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    if (status) {
      params = params.set('status', status);
    }

    // Backend returns {data, pagination}, map to {anomalies, totalCount}
    return this.http.get<{ data: DetectedAnomaly[], pagination: { totalCount: number } }>(this.apiUrl, { params })
      .pipe(
        map(response => ({
          anomalies: response.data,
          totalCount: response.pagination.totalCount
        }))
      );
  }

  getAnomalyById(id: string): Observable<DetectedAnomaly> {
    return this.http.get<DetectedAnomaly>(`${this.apiUrl}/${id}`);
  }

  updateAnomalyStatus(
    id: string,
    status: AnomalyStatus,
    investigationNotes?: string,
    resolution?: string
  ): Observable<DetectedAnomaly> {
    return this.http.put<DetectedAnomaly>(`${this.apiUrl}/${id}/status`, {
      status,
      investigationNotes,
      resolution
    });
  }

  getStatistics(tenantId?: string, startDate?: Date, endDate?: Date): Observable<AnomalyStatistics> {
    let params = new HttpParams();

    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }

    if (startDate) {
      params = params.set('startDate', startDate.toISOString());
    }

    if (endDate) {
      params = params.set('endDate', endDate.toISOString());
    }

    return this.http.get<AnomalyStatistics>(`${this.apiUrl}/statistics`, { params });
  }

  getUserAnomalies(userId: string, daysBack: number = 30): Observable<DetectedAnomaly[]> {
    const params = new HttpParams()
      .set('userId', userId)
      .set('daysBack', daysBack.toString());

    return this.http.get<DetectedAnomaly[]>(`${this.apiUrl}/user/${userId}`, { params });
  }
}
