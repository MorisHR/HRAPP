import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LegalHold, EDiscoveryFormat } from '../models/legal-hold.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LegalHoldService {
  private apiUrl = `${environment.apiUrl}/legalhold`;

  constructor(private http: HttpClient) {}

  getLegalHolds(tenantId?: string): Observable<LegalHold[]> {
    let params = new HttpParams();
    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }
    return this.http.get<LegalHold[]>(this.apiUrl, { params });
  }

  getActiveLegalHolds(tenantId?: string): Observable<LegalHold[]> {
    let params = new HttpParams();
    if (tenantId) {
      params = params.set('tenantId', tenantId);
    }
    return this.http.get<LegalHold[]>(`${this.apiUrl}/active`, { params });
  }

  getLegalHoldById(id: string): Observable<LegalHold> {
    return this.http.get<LegalHold>(`${this.apiUrl}/${id}`);
  }

  createLegalHold(legalHold: Partial<LegalHold>): Observable<LegalHold> {
    return this.http.post<LegalHold>(this.apiUrl, legalHold);
  }

  updateLegalHold(id: string, legalHold: Partial<LegalHold>): Observable<LegalHold> {
    return this.http.put<LegalHold>(`${this.apiUrl}/${id}`, legalHold);
  }

  releaseLegalHold(id: string, releaseNotes: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/release`, { releaseNotes });
  }

  exportEDiscovery(legalHoldId: string, format: EDiscoveryFormat): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${legalHoldId}/ediscovery/${format}`, {
      responseType: 'blob'
    });
  }

  getAffectedAuditLogs(legalHoldId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${legalHoldId}/audit-logs`);
  }
}
