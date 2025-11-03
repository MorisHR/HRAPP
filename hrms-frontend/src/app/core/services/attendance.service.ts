import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
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

  getAttendanceRecords(startDate: string, endDate: string): Observable<Attendance[]> {
    this.loadingSignal.set(true);
    return this.http.get<Attendance[]>(`${this.apiUrl}?startDate=${startDate}&endDate=${endDate}`).pipe(
      tap(records => {
        this.attendanceRecordsSignal.set(records);
        this.loadingSignal.set(false);
      })
    );
  }

  getEmployeeAttendance(employeeId: string, month: string): Observable<Attendance[]> {
    return this.http.get<Attendance[]>(`${this.apiUrl}/employee/${employeeId}?month=${month}`);
  }

  checkIn(employeeId: string): Observable<Attendance> {
    return this.http.post<Attendance>(`${this.apiUrl}/check-in`, { employeeId });
  }

  checkOut(employeeId: string): Observable<Attendance> {
    return this.http.post<Attendance>(`${this.apiUrl}/check-out`, { employeeId });
  }

  getAttendanceStats(month: string): Observable<AttendanceStats> {
    return this.http.get<AttendanceStats>(`${this.apiUrl}/stats?month=${month}`).pipe(
      tap(stats => this.statsSignal.set(stats))
    );
  }
}
