import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { Attendance, AttendanceStats } from '../models/attendance.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/attendance`;

  // Signals for reactive state
  private attendanceRecordsSignal = signal<Attendance[]>([]);
  private statsSignal = signal<AttendanceStats | null>(null);
  private loadingSignal = signal<boolean>(false);

  readonly attendanceRecords = this.attendanceRecordsSignal.asReadonly();
  readonly stats = this.statsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  /**
   * ✅ FORTUNE 500: Get attendance records with proper parameter names
   * Backend expects: fromDate and toDate (not startDate/endDate)
   * Backend returns: { total, data } (not plain array)
   */
  getAttendanceRecords(startDate: string, endDate: string): Observable<Attendance[]> {
    this.loadingSignal.set(true);
    // Fix #2: Use fromDate/toDate to match backend API
    return this.http.get<{ total: number; data: Attendance[] }>(
      `${this.apiUrl}?fromDate=${startDate}&toDate=${endDate}`
    ).pipe(
      // Fix #6: Unwrap response to get data array
      map(response => response.data),
      tap(records => {
        this.attendanceRecordsSignal.set(records);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * ✅ FORTUNE 500: Get monthly attendance for employee
   * Backend expects: /monthly/employee/{id}?year=2025&month=11
   */
  getEmployeeAttendance(employeeId: string, year: number, month: number): Observable<Attendance[]> {
    return this.http.get<any>(
      `${this.apiUrl}/monthly/employee/${employeeId}?year=${year}&month=${month}`
    ).pipe(
      // Backend returns MonthlyAttendanceSummaryDto with dailyRecords array
      map(response => response.dailyRecords || [])
    );
  }

  /**
   * ✅ FORTUNE 500: Employee self-service check-in
   * Backend returns: { message, data: AttendanceDetailsDto }
   */
  checkIn(employeeId: string): Observable<Attendance> {
    return this.http.post<{ message: string; data: Attendance }>(
      `${this.apiUrl}/check-in`,
      { employeeId }
    ).pipe(
      // Unwrap response to get attendance data
      map(response => response.data)
    );
  }

  /**
   * ✅ FORTUNE 500: Employee self-service check-out
   * Backend returns: { message, data: AttendanceDetailsDto }
   */
  checkOut(employeeId: string): Observable<Attendance> {
    return this.http.post<{ message: string; data: Attendance }>(
      `${this.apiUrl}/check-out`,
      { employeeId }
    ).pipe(
      // Unwrap response to get attendance data
      map(response => response.data)
    );
  }

  getAttendanceStats(month: string): Observable<AttendanceStats> {
    return this.http.get<AttendanceStats>(`${this.apiUrl}/stats?month=${month}`).pipe(
      tap(stats => this.statsSignal.set(stats))
    );
  }
}
