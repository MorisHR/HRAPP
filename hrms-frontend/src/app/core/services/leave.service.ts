import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map } from 'rxjs';
import { Leave, LeaveBalance, ApplyLeaveRequest } from '../models/leave.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/leaves`;

  // Signals for reactive state
  private leavesSignal = signal<Leave[]>([]);
  private balancesSignal = signal<LeaveBalance[]>([]);
  private loadingSignal = signal<boolean>(false);

  readonly leaves = this.leavesSignal.asReadonly();
  readonly balances = this.balancesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  /**
   * ✅ FORTUNE 500: Get all leaves with response unwrapping
   * Backend returns: { success, data, count }
   */
  getLeaves(): Observable<Leave[]> {
    this.loadingSignal.set(true);
    return this.http.get<{ success: boolean; data: Leave[]; count: number }>(this.apiUrl).pipe(
      map(response => response.data),
      tap(leaves => {
        this.leavesSignal.set(leaves);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * ✅ FORTUNE 500: Get employee leaves (uses my-leaves endpoint)
   * Backend: GET /api/leaves/my-leaves returns { success, data, count }
   */
  getEmployeeLeaves(employeeId: string): Observable<Leave[]> {
    return this.http.get<{ success: boolean; data: Leave[]; count: number }>(`${this.apiUrl}/my-leaves`).pipe(
      map(response => response.data)
    );
  }

  /**
   * ✅ FORTUNE 500: Get leave balance
   * Backend: GET /api/leaves/employee/{id}/balance
   */
  getLeaveBalance(employeeId: string): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(`${this.apiUrl}/employee/${employeeId}/balance`).pipe(
      tap(balances => this.balancesSignal.set(balances))
    );
  }

  /**
   * ✅ FORTUNE 500: Apply for leave
   * Fix #7: Backend expects POST at root path (not /apply)
   * Backend returns: { success, message, data }
   */
  applyLeave(request: ApplyLeaveRequest): Observable<Leave> {
    return this.http.post<{ success: boolean; message: string; data: Leave }>(this.apiUrl, request).pipe(
      map(response => response.data),
      tap(leave => {
        this.leavesSignal.update(leaves => [leave, ...leaves]);
      })
    );
  }

  /**
   * ✅ FORTUNE 500: Approve leave
   * Fix #3: Backend uses POST (not PATCH)
   * Backend returns: { success, message, data }
   */
  approveLeave(leaveId: string): Observable<Leave> {
    return this.http.post<{ success: boolean; message: string; data: Leave }>(
      `${this.apiUrl}/${leaveId}/approve`,
      {}
    ).pipe(
      map(response => response.data),
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }

  /**
   * ✅ FORTUNE 500: Reject leave
   * Fix #3: Backend uses POST (not PATCH)
   * Backend returns: { success, message, data }
   */
  rejectLeave(leaveId: string, reason: string): Observable<Leave> {
    return this.http.post<{ success: boolean; message: string; data: Leave }>(
      `${this.apiUrl}/${leaveId}/reject`,
      { reason }
    ).pipe(
      map(response => response.data),
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }

  /**
   * ✅ FORTUNE 500: Cancel leave
   * Fix #3: Backend uses POST (not PATCH)
   * Backend returns: { success, message, data }
   */
  cancelLeave(leaveId: string): Observable<Leave> {
    return this.http.post<{ success: boolean; message: string; data: Leave }>(
      `${this.apiUrl}/${leaveId}/cancel`,
      { reason: 'Cancelled by employee' }
    ).pipe(
      map(response => response.data),
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }
}
