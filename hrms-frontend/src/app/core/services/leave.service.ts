import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
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

  getLeaves(): Observable<Leave[]> {
    this.loadingSignal.set(true);
    return this.http.get<Leave[]>(this.apiUrl).pipe(
      tap(leaves => {
        this.leavesSignal.set(leaves);
        this.loadingSignal.set(false);
      })
    );
  }

  getEmployeeLeaves(employeeId: string): Observable<Leave[]> {
    return this.http.get<Leave[]>(`${this.apiUrl}/employee/${employeeId}`);
  }

  getLeaveBalance(employeeId: string): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(`${this.apiUrl}/employee/${employeeId}/balance`).pipe(
      tap(balances => this.balancesSignal.set(balances))
    );
  }

  applyLeave(request: ApplyLeaveRequest): Observable<Leave> {
    return this.http.post<Leave>(`${this.apiUrl}/apply`, request).pipe(
      tap(leave => {
        this.leavesSignal.update(leaves => [leave, ...leaves]);
      })
    );
  }

  approveLeave(leaveId: string): Observable<Leave> {
    return this.http.patch<Leave>(`${this.apiUrl}/${leaveId}/approve`, {}).pipe(
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }

  rejectLeave(leaveId: string, reason: string): Observable<Leave> {
    return this.http.patch<Leave>(`${this.apiUrl}/${leaveId}/reject`, { reason }).pipe(
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }

  cancelLeave(leaveId: string): Observable<Leave> {
    return this.http.patch<Leave>(`${this.apiUrl}/${leaveId}/cancel`, {}).pipe(
      tap(updatedLeave => {
        this.leavesSignal.update(leaves =>
          leaves.map(l => l.id === leaveId ? updatedLeave : l)
        );
      })
    );
  }
}
