import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  MrrBreakdownResponse,
  ArrTrackingResponse,
  CohortAnalysisItem,
  ExpansionContractionResponse,
  ChurnRateResponse,
  KeyMetricsResponse,
  RevenueAnalyticsDashboard
} from '../models/revenue-analytics.model';

@Injectable({
  providedIn: 'root'
})
export class RevenueAnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/revenue-analytics`;

  getMrrBreakdown(): Observable<MrrBreakdownResponse> {
    return this.http.get<MrrBreakdownResponse>(`${this.baseUrl}/mrr-breakdown`).pipe(
      map(response => this.transformDates(response, ['generatedAt']))
    );
  }

  getArrTracking(): Observable<ArrTrackingResponse> {
    return this.http.get<ArrTrackingResponse>(`${this.baseUrl}/arr`).pipe(
      map(response => ({
        ...response,
        generatedAt: new Date(response.generatedAt),
        trend: response.trend.map(t => ({ ...t, month: new Date(t.month) }))
      }))
    );
  }

  getCohortAnalysis(months: number = 12): Observable<CohortAnalysisItem[]> {
    return this.http.get<CohortAnalysisItem[]>(`${this.baseUrl}/cohort-analysis`, {
      params: { months: months.toString() }
    }).pipe(
      map(items => items.map(item => ({ ...item, cohortMonth: new Date(item.cohortMonth) })))
    );
  }

  getExpansionContraction(months: number = 6): Observable<ExpansionContractionResponse> {
    return this.http.get<ExpansionContractionResponse>(`${this.baseUrl}/expansion-contraction`, {
      params: { months: months.toString() }
    }).pipe(
      map(response => ({
        ...response,
        generatedAt: new Date(response.generatedAt),
        trend: response.trend.map(t => ({ ...t, month: new Date(t.month) }))
      }))
    );
  }

  getChurnRate(months: number = 12): Observable<ChurnRateResponse> {
    return this.http.get<ChurnRateResponse>(`${this.baseUrl}/churn-rate`, {
      params: { months: months.toString() }
    }).pipe(
      map(response => ({
        ...response,
        generatedAt: new Date(response.generatedAt),
        trend: response.trend.map(t => ({ ...t, month: new Date(t.month) }))
      }))
    );
  }

  getKeyMetrics(): Observable<KeyMetricsResponse> {
    return this.http.get<KeyMetricsResponse>(`${this.baseUrl}/key-metrics`).pipe(
      map(response => this.transformDates(response, ['generatedAt']))
    );
  }

  getDashboard(): Observable<RevenueAnalyticsDashboard> {
    return this.http.get<RevenueAnalyticsDashboard>(`${this.baseUrl}/dashboard`).pipe(
      map(dashboard => ({
        ...dashboard,
        generatedAt: new Date(dashboard.generatedAt),
        mrr: this.transformDates(dashboard.mrr, ['generatedAt']),
        arr: {
          ...dashboard.arr,
          generatedAt: new Date(dashboard.arr.generatedAt),
          trend: dashboard.arr.trend.map(t => ({ ...t, month: new Date(t.month) }))
        },
        churnRate: {
          ...dashboard.churnRate,
          generatedAt: new Date(dashboard.churnRate.generatedAt),
          trend: dashboard.churnRate.trend.map(t => ({ ...t, month: new Date(t.month) }))
        },
        keyMetrics: this.transformDates(dashboard.keyMetrics, ['generatedAt'])
      }))
    );
  }

  formatCurrency(value: number, decimals: number = 0): string {
    return new Intl.NumberFormat('en-MU', {
      style: 'currency',
      currency: 'MUR',
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals
    }).format(value);
  }

  formatLargeNumber(value: number): string {
    if (value >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
    if (value >= 1000) return `${(value / 1000).toFixed(1)}K`;
    return value.toFixed(0);
  }

  formatPercentage(value: number, decimals: number = 2): string {
    return `${value.toFixed(decimals)}%`;
  }

  getLtvCacHealth(ratio: number): 'healthy' | 'warning' | 'critical' {
    if (ratio >= 3) return 'healthy';
    if (ratio >= 1) return 'warning';
    return 'critical';
  }

  getChurnHealth(churnRate: number): 'healthy' | 'warning' | 'critical' {
    if (churnRate < 5) return 'healthy';
    if (churnRate < 10) return 'warning';
    return 'critical';
  }

  private transformDates<T extends Record<string, any>>(obj: T, dateFields: string[]): T {
    const result = { ...obj } as any;
    dateFields.forEach(field => {
      if (result[field]) result[field] = new Date(result[field]);
    });
    return result as T;
  }
}
