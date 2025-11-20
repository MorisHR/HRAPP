import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, of, switchMap, catchError, tap } from 'rxjs';
import { SystemHealth, ServiceHealth } from '../models/dashboard.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SystemHealthService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/system-health`;

  // Reactive state
  private healthSignal = signal<SystemHealth | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly health = this.healthSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Fetch current system health status
   */
  getSystemHealth(): Observable<SystemHealth> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<SystemHealth>(this.apiUrl).pipe(
      tap(health => {
        this.healthSignal.set(health);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        this.errorSignal.set('Failed to fetch system health');
        this.loadingSignal.set(false);
        // Return mock data on error for resilience
        return of(this.getMockSystemHealth());
      })
    );
  }

  /**
   * Start polling system health at specified interval
   */
  startHealthMonitoring(intervalMs: number = 30000): Observable<SystemHealth> {
    return interval(intervalMs).pipe(
      switchMap(() => this.getSystemHealth())
    );
  }

  /**
   * Check health of a specific service
   */
  checkServiceHealth(serviceName: string): Observable<ServiceHealth> {
    return this.http.get<ServiceHealth>(`${this.apiUrl}/services/${serviceName}`).pipe(
      catchError(() => of(this.getMockServiceHealth(serviceName)))
    );
  }

  /**
   * Mock data generator for development/fallback
   * TODO: Remove when backend is fully implemented
   */
  private getMockSystemHealth(): SystemHealth {
    const now = new Date();

    return {
      status: 'healthy',
      timestamp: now,
      uptime: 99.97,
      services: [
        {
          name: 'API',
          status: 'healthy',
          responseTime: 120,
          uptime: 99.98,
          lastCheck: now,
          errorRate: 0.02
        },
        {
          name: 'Database',
          status: 'healthy',
          responseTime: 45,
          uptime: 99.99,
          lastCheck: now,
          errorRate: 0.01
        },
        {
          name: 'Storage',
          status: 'degraded',
          responseTime: 200,
          uptime: 99.95,
          lastCheck: now,
          errorRate: 0.05,
          details: '78% capacity - consider scaling'
        },
        {
          name: 'Redis Cache',
          status: 'healthy',
          responseTime: 5,
          uptime: 99.99,
          lastCheck: now,
          errorRate: 0.00
        },
        {
          name: 'Email Service',
          status: 'healthy',
          responseTime: 350,
          uptime: 99.90,
          lastCheck: now,
          errorRate: 0.10
        }
      ]
    };
  }

  private getMockServiceHealth(serviceName: string): ServiceHealth {
    return {
      name: serviceName,
      status: 'healthy',
      responseTime: 100,
      uptime: 99.9,
      lastCheck: new Date(),
      errorRate: 0.1
    };
  }

  /**
   * Calculate overall system status based on service statuses
   */
  calculateOverallStatus(services: ServiceHealth[]): 'healthy' | 'degraded' | 'down' {
    const hasDown = services.some(s => s.status === 'down');
    const hasDegraded = services.some(s => s.status === 'degraded');

    if (hasDown) return 'down';
    if (hasDegraded) return 'degraded';
    return 'healthy';
  }

  /**
   * Get status badge color
   */
  getStatusColor(status: 'healthy' | 'degraded' | 'down'): string {
    switch (status) {
      case 'healthy': return 'success';
      case 'degraded': return 'warning';
      case 'down': return 'error';
    }
  }

  /**
   * Get status icon
   */
  getStatusIcon(status: 'healthy' | 'degraded' | 'down'): string {
    switch (status) {
      case 'healthy': return 'check_circle';
      case 'degraded': return 'warning';
      case 'down': return 'error';
    }
  }
}
