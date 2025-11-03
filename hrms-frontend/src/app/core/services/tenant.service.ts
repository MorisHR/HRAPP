import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { Tenant, CreateTenantRequest } from '../models/tenant.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/tenants`;

  // Signals for reactive state
  private tenantsSignal = signal<Tenant[]>([]);
  private loadingSignal = signal<boolean>(false);

  readonly tenants = this.tenantsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  getTenants(): Observable<Tenant[]> {
    this.loadingSignal.set(true);
    return this.http.get<any>(this.apiUrl).pipe(
      map(response => response.data || response),
      tap(tenants => {
        this.tenantsSignal.set(tenants);
        this.loadingSignal.set(false);
      })
    );
  }

  getTenantById(id: string): Observable<Tenant> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(response => response.data || response)
    );
  }

  createTenant(request: CreateTenantRequest): Observable<Tenant> {
    return this.http.post<any>(this.apiUrl, request).pipe(
      map(response => response.data || response),
      tap(tenant => {
        this.tenantsSignal.update(tenants => [...tenants, tenant]);
      })
    );
  }

  updateTenant(id: string, tenant: Partial<Tenant>): Observable<Tenant> {
    return this.http.put<Tenant>(`${this.apiUrl}/${id}`, tenant).pipe(
      tap(updatedTenant => {
        this.tenantsSignal.update(tenants =>
          tenants.map(t => t.id === id ? updatedTenant : t)
        );
      })
    );
  }

  suspendTenant(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/suspend`, {});
  }

  deleteTenant(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this.tenantsSignal.update(tenants => tenants.filter(t => t.id !== id));
      })
    );
  }
}
