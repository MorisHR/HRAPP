import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardStats {
  // People Metrics
  totalEmployees: number;
  presentToday: number;
  employeesOnLeave: number;
  newHiresThisMonth: number;
  employeeGrowthRate: number;

  // Leave Metrics
  pendingLeaveRequests: number;

  // Payroll Metrics
  activePayrollCycles: number;
  totalPayrollAmount: number;

  // Compliance Metrics
  expiringDocumentsCount: number;

  // Organizational Metrics
  departmentCount: number;
  expatriatesCount: number;
  averageTenureYears: number;
  upcomingBirthdays: number;

  // Meta
  generatedAt: Date;
}

export interface ActivityItem {
  id: string;
  type: string;
  icon: string;
  title: string;
  description: string;
  timestamp: Date;
  relatedId: string;
}

export interface AlertItem {
  id: string;
  type: string;
  severity: string; // critical, high, medium, low
  icon: string;
  title: string;
  description: string;
  actionUrl: string;
  createdAt: Date;
}

export interface ChartDataPoint {
  label: string;
  value: number;
}

export interface BirthdayItem {
  employeeId: string;
  employeeName: string;
  department: string;
  birthdayDate: Date;
  daysUntil: number;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  count?: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/dashboard`;

  // Signals for reactive state
  private statsSignal = signal<DashboardStats | null>(null);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly stats = this.statsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Get comprehensive dashboard statistics
   */
  getStats(): Observable<DashboardStats> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<ApiResponse<DashboardStats>>(`${this.apiUrl}/stats`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load dashboard stats');
        }
        return response.data;
      }),
      tap(stats => {
        this.statsSignal.set(stats);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        this.loadingSignal.set(false);
        this.errorSignal.set(error.message || 'Error loading dashboard stats');
        return throwError(() => error);
      })
    );
  }

  /**
   * Get all unique departments
   */
  getDepartments(): Observable<string[]> {
    return this.http.get<ApiResponse<string[]>>(`${this.apiUrl}/departments`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load departments');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Get urgent alerts
   */
  getAlerts(): Observable<AlertItem[]> {
    return this.http.get<ApiResponse<AlertItem[]>>(`${this.apiUrl}/alerts`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load alerts');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Get recent activity feed
   */
  getRecentActivity(limit: number = 10): Observable<ActivityItem[]> {
    return this.http.get<ApiResponse<ActivityItem[]>>(`${this.apiUrl}/recent-activity`, {
      params: { limit: limit.toString() }
    }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load activity');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Refresh dashboard data
   */
  refresh(): Observable<DashboardStats> {
    return this.getStats();
  }

  /**
   * Get department headcount chart data
   */
  getDepartmentHeadcountChart(): Observable<ChartDataPoint[]> {
    return this.http.get<ApiResponse<ChartDataPoint[]>>(`${this.apiUrl}/charts/department-headcount`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load department headcount chart');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Get employee growth chart data
   */
  getEmployeeGrowthChart(): Observable<ChartDataPoint[]> {
    return this.http.get<ApiResponse<ChartDataPoint[]>>(`${this.apiUrl}/charts/employee-growth`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load employee growth chart');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Get employee type distribution chart data
   */
  getEmployeeTypeDistribution(): Observable<ChartDataPoint[]> {
    return this.http.get<ApiResponse<ChartDataPoint[]>>(`${this.apiUrl}/charts/employee-type-distribution`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load employee type distribution chart');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Get upcoming birthdays
   */
  getUpcomingBirthdays(days: number = 7): Observable<BirthdayItem[]> {
    return this.http.get<ApiResponse<BirthdayItem[]>>(`${this.apiUrl}/upcoming-birthdays`, {
      params: { days: days.toString() }
    }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load upcoming birthdays');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }
}
