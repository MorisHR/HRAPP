import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, catchError, tap, map } from 'rxjs';
import {
  SystemMetrics,
  ApiMetrics,
  DatabaseMetrics,
  StorageMetrics,
  PerformanceMetrics,
  DashboardChartData,
  TenantGrowthData,
  RevenueData
} from '../models/dashboard.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MetricsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/metrics`;

  // Reactive state
  private metricsSignal = signal<SystemMetrics | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly metrics = this.metricsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Get current system metrics
   */
  getSystemMetrics(): Observable<SystemMetrics> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<SystemMetrics>(this.apiUrl).pipe(
      tap(metrics => {
        this.metricsSignal.set(metrics);
        this.loadingSignal.set(false);
      }),
      catchError(() => {
        this.errorSignal.set('Failed to fetch metrics');
        this.loadingSignal.set(false);
        return of(this.getMockSystemMetrics());
      })
    );
  }

  /**
   * Get tenant growth data for charts
   */
  getTenantGrowthData(months: number = 6): Observable<TenantGrowthData[]> {
    return this.http.get<TenantGrowthData[]>(`${this.apiUrl}/tenant-growth`, {
      params: new HttpParams().set('months', months.toString())
    }).pipe(
      map((data: TenantGrowthData[]) => this.deserializeDates<TenantGrowthData>(data, 'period')),
      catchError(() => of(this.getMockTenantGrowthData(months)))
    );
  }

  /**
   * Get revenue data for charts
   */
  getRevenueData(months: number = 6): Observable<RevenueData[]> {
    return this.http.get<RevenueData[]>(`${this.apiUrl}/revenue`, {
      params: new HttpParams().set('months', months.toString())
    }).pipe(
      map((data: RevenueData[]) => this.deserializeDates<RevenueData>(data, 'period')),
      catchError(() => of(this.getMockRevenueData(months)))
    );
  }

  /**
   * Get API performance metrics
   */
  getApiMetrics(): Observable<ApiMetrics> {
    return this.http.get<ApiMetrics>(`${this.apiUrl}/api`).pipe(
      catchError(() => of(this.getMockApiMetrics()))
    );
  }

  /**
   * Get database metrics
   */
  getDatabaseMetrics(): Observable<DatabaseMetrics> {
    return this.http.get<DatabaseMetrics>(`${this.apiUrl}/database`).pipe(
      catchError(() => of(this.getMockDatabaseMetrics()))
    );
  }

  /**
   * Get storage metrics
   */
  getStorageMetrics(): Observable<StorageMetrics> {
    return this.http.get<StorageMetrics>(`${this.apiUrl}/storage`).pipe(
      catchError(() => of(this.getMockStorageMetrics()))
    );
  }

  /**
   * Convert tenant growth data to chart format
   */
  tenantGrowthToChartData(data: TenantGrowthData[]): DashboardChartData {
    return {
      labels: data.map(d => this.formatMonthLabel(d.period)),
      datasets: [
        {
          label: 'Total Tenants',
          data: data.map(d => d.totalTenants),
          borderColor: 'rgb(59, 130, 246)',
          backgroundColor: 'rgba(59, 130, 246, 0.1)',
          fill: true,
          tension: 0.4
        },
        {
          label: 'Active Tenants',
          data: data.map(d => d.activeTenants),
          borderColor: 'rgb(34, 197, 94)',
          backgroundColor: 'rgba(34, 197, 94, 0.1)',
          fill: true,
          tension: 0.4
        }
      ]
    };
  }

  /**
   * Convert revenue data to chart format
   */
  revenueToChartData(data: RevenueData[]): DashboardChartData {
    return {
      labels: data.map(d => this.formatMonthLabel(d.period)),
      datasets: [
        {
          label: 'Total Revenue',
          data: data.map(d => d.totalRevenue),
          backgroundColor: 'rgba(234, 179, 8, 0.8)',
          borderColor: 'rgb(234, 179, 8)',
          borderWidth: 1
        }
      ]
    };
  }

  /**
   * Format month label for charts
   */
  private formatMonthLabel(date: Date): string {
    return new Intl.DateTimeFormat('en-US', { month: 'short', year: 'numeric' }).format(date);
  }

  /**
   * Deserialize date fields with proper type preservation
   */
  private deserializeDates<T extends Record<string, any>>(
    data: T[],
    dateField: keyof T
  ): T[] {
    return data.map(item => ({
      ...item,
      [dateField]: new Date(item[dateField] as string | number | Date)
    })) as T[];
  }

  /**
   * Mock data generators
   * TODO: Remove when backend is fully implemented
   */
  private getMockSystemMetrics(): SystemMetrics {
    return {
      timestamp: new Date(),
      apiMetrics: this.getMockApiMetrics(),
      databaseMetrics: this.getMockDatabaseMetrics(),
      storageMetrics: this.getMockStorageMetrics(),
      performanceMetrics: {
        cpuUsage: 45.5,
        memoryUsage: 62.3,
        diskIOPS: 1250,
        networkThroughputMBps: 85.4
      }
    };
  }

  private getMockApiMetrics(): ApiMetrics {
    return {
      totalRequests: 125847,
      successRate: 99.8,
      averageResponseTime: 120,
      requestsPerSecond: 42,
      errorCount: 251,
      topEndpoints: [
        { path: '/api/tenants', method: 'GET', requests: 25000, averageResponseTime: 95, errorRate: 0.1 },
        { path: '/api/employees', method: 'GET', requests: 18500, averageResponseTime: 110, errorRate: 0.2 },
        { path: '/api/attendance', method: 'POST', requests: 15000, averageResponseTime: 150, errorRate: 0.3 },
        { path: '/api/payroll', method: 'GET', requests: 12000, averageResponseTime: 200, errorRate: 0.1 },
        { path: '/api/reports', method: 'GET', requests: 8500, averageResponseTime: 450, errorRate: 0.5 }
      ]
    };
  }

  private getMockDatabaseMetrics(): DatabaseMetrics {
    return {
      connectionPoolSize: 20,
      activeConnections: 12,
      averageQueryTime: 45,
      slowQueries: 3,
      diskUsage: 68.5,
      status: 'healthy'
    };
  }

  private getMockStorageMetrics(): StorageMetrics {
    return {
      totalCapacityGB: 500,
      usedCapacityGB: 390,
      availableCapacityGB: 110,
      utilizationPercent: 78,
      largestTenants: [
        { tenantId: '1', tenantName: 'MegaCorp', storageUsedGB: 85.5, percentOfTotal: 17.1 },
        { tenantId: '2', tenantName: 'TechGiant', storageUsedGB: 62.3, percentOfTotal: 12.5 },
        { tenantId: '3', tenantName: 'DataFlow Inc', storageUsedGB: 45.8, percentOfTotal: 9.2 },
        { tenantId: '4', tenantName: 'Global Industries', storageUsedGB: 38.2, percentOfTotal: 7.6 },
        { tenantId: '5', tenantName: 'Enterprise LLC', storageUsedGB: 29.7, percentOfTotal: 5.9 }
      ]
    };
  }

  private getMockTenantGrowthData(months: number): TenantGrowthData[] {
    const data: TenantGrowthData[] = [];
    const now = new Date();

    for (let i = months - 1; i >= 0; i--) {
      const period = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const baseGrowth = months - i;

      data.push({
        period,
        totalTenants: 30 + baseGrowth * 3,
        activeTenants: 28 + baseGrowth * 3,
        newTenants: 3 + Math.floor(Math.random() * 2),
        churnedTenants: Math.floor(Math.random() * 2)
      });
    }

    return data;
  }

  private getMockRevenueData(months: number): RevenueData[] {
    const data: RevenueData[] = [];
    const now = new Date();

    for (let i = months - 1; i >= 0; i--) {
      const period = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const baseRevenue = 12000 + (months - i) * 1000;
      const randomVariation = Math.random() * 1000;

      data.push({
        period,
        totalRevenue: baseRevenue + randomVariation,
        recurringRevenue: baseRevenue * 0.85,
        newRevenue: baseRevenue * 0.12,
        churnRevenue: baseRevenue * 0.03
      });
    }

    return data;
  }
}
