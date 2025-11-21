import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, catchError, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InfrastructureMetrics,
  QueueMetrics,
  DatabasePerformanceMetrics,
  ApiRateLimitingMetrics,
  CacheMetrics,
  CdnMetrics,
  BackgroundJobMetrics,
  SlowQuery,
  ApiRateViolation,
  TenantApiUsage
} from '../models/dashboard.model';

/**
 * Infrastructure Metrics Service
 * Monitors system health, performance, and resource usage
 */
@Injectable({
  providedIn: 'root'
})
export class InfrastructureMetricsService {
  private http = HttpClient;
  private apiUrl = `${environment.apiUrl}/admin/infrastructure`;

  // Reactive state
  private metricsSignal = signal<InfrastructureMetrics | null>(null);
  private loadingSignal = signal<boolean>(false);

  readonly metrics = this.metricsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  constructor() {}

  /**
   * Get comprehensive infrastructure metrics
   */
  getInfrastructureMetrics(): Observable<InfrastructureMetrics> {
    this.loadingSignal.set(true);

    // TODO: Replace with real API call when backend is ready
    return of(this.getMockInfrastructureMetrics()).pipe(
      map(metrics => {
        this.metricsSignal.set(metrics);
        this.loadingSignal.set(false);
        return metrics;
      }),
      catchError(error => {
        this.loadingSignal.set(false);
        return of(this.getMockInfrastructureMetrics());
      })
    );
  }

  /**
   * Get queue metrics
   */
  getQueueMetrics(): Observable<QueueMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.queueMetrics)
    );
  }

  /**
   * Get database performance metrics
   */
  getDatabasePerformance(): Observable<DatabasePerformanceMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.databasePerformance)
    );
  }

  /**
   * Get API rate limiting metrics
   */
  getApiRateLimiting(): Observable<ApiRateLimitingMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.apiRateLimiting)
    );
  }

  /**
   * Get cache metrics
   */
  getCacheMetrics(): Observable<CacheMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.cacheMetrics)
    );
  }

  /**
   * Get CDN metrics
   */
  getCdnMetrics(): Observable<CdnMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.cdnMetrics)
    );
  }

  /**
   * Get background job metrics
   */
  getBackgroundJobMetrics(): Observable<BackgroundJobMetrics> {
    return this.getInfrastructureMetrics().pipe(
      map(metrics => metrics.backgroundJobs)
    );
  }

  /**
   * Generate comprehensive mock infrastructure metrics
   * TODO: Remove when backend is ready
   */
  private getMockInfrastructureMetrics(): InfrastructureMetrics {
    const now = new Date();

    return {
      timestamp: now,
      queueMetrics: {
        totalQueues: 6,
        activeJobs: 42,
        pendingJobs: 127,
        failedJobs: 8,
        completedJobsToday: 18453,
        averageProcessingTime: 2340,
        oldestJobAge: 145,
        queues: [
          {
            name: 'emails',
            pending: 45,
            active: 12,
            failed: 2,
            completed: 8234,
            averageWaitTime: 1200,
            status: 'healthy'
          },
          {
            name: 'reports',
            pending: 38,
            active: 8,
            failed: 1,
            completed: 2456,
            averageWaitTime: 3200,
            status: 'healthy'
          },
          {
            name: 'payroll',
            pending: 22,
            active: 15,
            failed: 4,
            completed: 1834,
            averageWaitTime: 4500,
            status: 'warning'
          },
          {
            name: 'notifications',
            pending: 12,
            active: 5,
            failed: 0,
            completed: 4521,
            averageWaitTime: 800,
            status: 'healthy'
          },
          {
            name: 'data-export',
            pending: 8,
            active: 2,
            failed: 1,
            completed: 987,
            averageWaitTime: 6700,
            status: 'healthy'
          },
          {
            name: 'backup',
            pending: 2,
            active: 0,
            failed: 0,
            completed: 421,
            averageWaitTime: 1800,
            status: 'healthy'
          }
        ]
      },
      backgroundJobs: {
        totalJobs: 284,
        running: 15,
        scheduled: 42,
        failed: 8,
        retrying: 3,
        recentJobs: [
          {
            id: 'job-001',
            name: 'Monthly Payroll Processing',
            status: 'running',
            startedAt: new Date(now.getTime() - 15 * 60000),
            progress: 67,
            tenantId: 'tenant-012'
          },
          {
            id: 'job-002',
            name: 'Database Backup',
            status: 'running',
            startedAt: new Date(now.getTime() - 8 * 60000),
            progress: 42
          },
          {
            id: 'job-003',
            name: 'Email Campaign',
            status: 'completed',
            startedAt: new Date(now.getTime() - 25 * 60000),
            completedAt: new Date(now.getTime() - 5 * 60000),
            duration: 1200000,
            tenantId: 'tenant-005'
          },
          {
            id: 'job-004',
            name: 'Report Generation',
            status: 'failed',
            startedAt: new Date(now.getTime() - 30 * 60000),
            completedAt: new Date(now.getTime() - 25 * 60000),
            error: 'Database connection timeout after 30s',
            tenantId: 'tenant-008'
          },
          {
            id: 'job-005',
            name: 'Data Synchronization',
            status: 'retrying',
            startedAt: new Date(now.getTime() - 10 * 60000),
            progress: 34,
            tenantId: 'tenant-003'
          }
        ]
      },
      databasePerformance: {
        connectionPool: {
          maxConnections: 200,
          activeConnections: 142,
          idleConnections: 48,
          waitingRequests: 5,
          utilizationPercent: 71,
          averageWaitTime: 45,
          connectionErrors: 2
        },
        queryPerformance: {
          totalQueries: 1248392,
          averageQueryTime: 23.4,
          slowQueryCount: 47,
          queryErrorRate: 0.02,
          queriesPerSecond: 342.8,
          topQueries: [
            {
              queryHash: 'a3f4b2c1',
              queryTemplate: 'SELECT * FROM employees WHERE tenant_id = ? AND status = ?',
              executionCount: 24587,
              averageTime: 12.3,
              maxTime: 245.8,
              minTime: 3.2
            },
            {
              queryHash: 'b8d3e1f2',
              queryTemplate: 'SELECT * FROM attendance WHERE date >= ? AND date <= ?',
              executionCount: 18234,
              averageTime: 34.7,
              maxTime: 512.3,
              minTime: 8.1
            },
            {
              queryHash: 'c9a2d4e3',
              queryTemplate: 'UPDATE payroll SET status = ? WHERE id = ?',
              executionCount: 12456,
              averageTime: 18.9,
              maxTime: 187.4,
              minTime: 5.6
            }
          ]
        },
        slowQueries: [
          {
            query: 'SELECT e.*, a.* FROM employees e LEFT JOIN attendance a ON e.id = a.employee_id WHERE e.tenant_id = \'tenant-012\' AND a.date >= \'2025-01-01\'',
            executionTime: 8453.2,
            timestamp: new Date(now.getTime() - 10 * 60000),
            database: 'hrms_master',
            tenantId: 'tenant-012',
            rowsReturned: 125847
          },
          {
            query: 'SELECT * FROM payroll WHERE tenant_id = \'tenant-005\' ORDER BY created_at DESC',
            executionTime: 5234.7,
            timestamp: new Date(now.getTime() - 18 * 60000),
            database: 'hrms_master',
            tenantId: 'tenant-005',
            rowsReturned: 45678
          },
          {
            query: 'DELETE FROM audit_logs WHERE created_at < \'2024-01-01\'',
            executionTime: 12847.3,
            timestamp: new Date(now.getTime() - 35 * 60000),
            database: 'hrms_master',
            rowsReturned: 0
          }
        ],
        tenantSizes: [
          {
            tenantId: 'tenant-012',
            tenantName: 'Fortune 500 Enterprise',
            sizeGB: 47.8,
            tableCount: 42,
            rowCount: 2847234,
            indexSizeGB: 12.3,
            growthRate: 3.2
          },
          {
            tenantId: 'tenant-005',
            tenantName: 'Legacy Corp',
            sizeGB: 28.4,
            tableCount: 42,
            rowCount: 1534892,
            indexSizeGB: 7.8,
            growthRate: 1.8
          },
          {
            tenantId: 'tenant-008',
            tenantName: 'DataFlow Inc',
            sizeGB: 23.7,
            tableCount: 42,
            rowCount: 1247834,
            indexSizeGB: 6.2,
            growthRate: 2.1
          },
          {
            tenantId: 'tenant-003',
            tenantName: 'Global Industries Inc',
            sizeGB: 18.9,
            tableCount: 42,
            rowCount: 987234,
            indexSizeGB: 4.7,
            growthRate: 1.4
          },
          {
            tenantId: 'tenant-002',
            tenantName: 'TechCo Solutions',
            sizeGB: 15.2,
            tableCount: 42,
            rowCount: 784523,
            indexSizeGB: 3.8,
            growthRate: 1.1
          }
        ],
        overallHealth: 'healthy'
      },
      apiRateLimiting: {
        totalRequests: 2847234,
        rateLimitedRequests: 1247,
        throttledRequests: 834,
        rateLimitViolations: [
          {
            timestamp: new Date(now.getTime() - 15 * 60000),
            tenantId: 'tenant-008',
            tenantName: 'DataFlow Inc',
            endpoint: '/api/employees',
            requestCount: 15234,
            limit: 10000,
            timeWindow: '1 hour',
            action: 'throttled'
          },
          {
            timestamp: new Date(now.getTime() - 32 * 60000),
            tenantId: 'tenant-005',
            tenantName: 'Legacy Corp',
            endpoint: '/api/attendance/bulk',
            requestCount: 5847,
            limit: 5000,
            timeWindow: '1 hour',
            action: 'throttled'
          },
          {
            timestamp: new Date(now.getTime() - 45 * 60000),
            tenantId: 'tenant-012',
            tenantName: 'Fortune 500 Enterprise',
            endpoint: '/api/reports',
            requestCount: 3234,
            limit: 3000,
            timeWindow: '1 hour',
            action: 'warned'
          }
        ],
        tenantRequestCounts: [
          {
            tenantId: 'tenant-012',
            tenantName: 'Fortune 500 Enterprise',
            requestCount: 847234,
            rateLimitHits: 23,
            averageResponseTime: 145,
            errorRate: 0.03,
            tier: 'Enterprise',
            limit: 1000000,
            utilizationPercent: 84.7
          },
          {
            tenantId: 'tenant-008',
            tenantName: 'DataFlow Inc',
            requestCount: 523847,
            rateLimitHits: 847,
            averageResponseTime: 234,
            errorRate: 0.12,
            tier: 'Professional',
            limit: 500000,
            utilizationPercent: 104.8
          },
          {
            tenantId: 'tenant-005',
            tenantName: 'Legacy Corp',
            requestCount: 384234,
            rateLimitHits: 234,
            averageResponseTime: 187,
            errorRate: 0.05,
            tier: 'Professional',
            limit: 500000,
            utilizationPercent: 76.8
          }
        ],
        topEndpoints: [
          {
            endpoint: '/api/employees',
            method: 'GET',
            requestCount: 847234,
            limit: 100000,
            violations: 234,
            averageResponseTime: 123
          },
          {
            endpoint: '/api/attendance',
            method: 'POST',
            requestCount: 534892,
            limit: 50000,
            violations: 87,
            averageResponseTime: 234
          },
          {
            endpoint: '/api/reports',
            method: 'GET',
            requestCount: 384723,
            limit: 20000,
            violations: 45,
            averageResponseTime: 456
          }
        ]
      },
      cacheMetrics: {
        totalKeys: 284734,
        memoryUsedMB: 3847,
        memoryMaxMB: 8192,
        hitRate: 94.7,
        missRate: 5.3,
        evictions: 1247,
        connections: 142,
        opsPerSecond: 15234,
        averageLatency: 1.2,
        health: 'healthy',
        cacheBreakdown: [
          {
            category: 'Session Data',
            keys: 124847,
            memoryMB: 1234,
            hitRate: 98.2,
            ttlAverage: 3600
          },
          {
            category: 'Employee Records',
            keys: 87234,
            memoryMB: 1547,
            hitRate: 92.4,
            ttlAverage: 1800
          },
          {
            category: 'Query Results',
            keys: 42387,
            memoryMB: 834,
            hitRate: 88.7,
            ttlAverage: 300
          },
          {
            category: 'Static Assets',
            keys: 30266,
            memoryMB: 232,
            hitRate: 99.4,
            ttlAverage: 86400
          }
        ]
      },
      cdnMetrics: {
        totalRequests: 8472834,
        cacheHitRate: 96.8,
        bandwidthGB: 2847.3,
        averageLatency: 45,
        errorRate: 0.02,
        origins: [
          {
            origin: 'api.hrms.com',
            requests: 3847234,
            cacheHitRate: 88.4,
            errorRate: 0.03,
            averageLatency: 78
          },
          {
            origin: 'assets.hrms.com',
            requests: 4625600,
            cacheHitRate: 99.2,
            errorRate: 0.01,
            averageLatency: 23
          }
        ],
        topAssets: [
          {
            path: '/static/js/main.bundle.js',
            requests: 1247834,
            cacheHitRate: 99.8,
            bandwidthGB: 487.3,
            contentType: 'application/javascript'
          },
          {
            path: '/static/css/styles.css',
            requests: 1134782,
            cacheHitRate: 99.9,
            bandwidthGB: 123.4,
            contentType: 'text/css'
          },
          {
            path: '/api/employees',
            requests: 847234,
            cacheHitRate: 76.4,
            bandwidthGB: 234.7,
            contentType: 'application/json'
          }
        ],
        geographicDistribution: [
          {
            region: 'North America',
            requests: 4234782,
            bandwidthGB: 1423.8,
            averageLatency: 34
          },
          {
            region: 'Europe',
            requests: 2847234,
            bandwidthGB: 934.2,
            averageLatency: 52
          },
          {
            region: 'Asia Pacific',
            requests: 1390818,
            bandwidthGB: 489.3,
            averageLatency: 67
          }
        ]
      }
    };
  }
}
