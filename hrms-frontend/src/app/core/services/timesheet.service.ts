import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  Timesheet,
  TimesheetEntry,
  TimesheetAdjustment,
  TimesheetComment,
  TimesheetStats,
  GenerateTimesheetRequest,
  UpdateTimesheetEntryRequest,
  CreateAdjustmentRequest,
  BulkApproveRequest,
  RejectTimesheetRequest,
  AddCommentRequest,
  PeriodType
} from '../models/timesheet.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TimesheetService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/timesheet`;

  // Signals for reactive state
  private timesheetsSignal = signal<Timesheet[]>([]);
  private currentTimesheetSignal = signal<Timesheet | null>(null);
  private pendingApprovalsSignal = signal<Timesheet[]>([]);
  private statsSignal = signal<TimesheetStats | null>(null);
  private loadingSignal = signal<boolean>(false);

  readonly timesheets = this.timesheetsSignal.asReadonly();
  readonly currentTimesheet = this.currentTimesheetSignal.asReadonly();
  readonly pendingApprovals = this.pendingApprovalsSignal.asReadonly();
  readonly stats = this.statsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  // ==================== Employee Endpoints ====================

  /**
   * Get all timesheets for the current employee
   */
  getMyTimesheets(status?: number, limit: number = 50): Observable<Timesheet[]> {
    this.loadingSignal.set(true);
    const params = status ? `?status=${status}&limit=${limit}` : `?limit=${limit}`;
    return this.http.get<Timesheet[]>(`${this.apiUrl}/my-timesheets${params}`).pipe(
      tap(timesheets => {
        this.timesheetsSignal.set(timesheets);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * Get a specific timesheet by ID
   */
  getTimesheetById(id: string): Observable<Timesheet> {
    this.loadingSignal.set(true);
    return this.http.get<Timesheet>(`${this.apiUrl}/${id}`).pipe(
      tap(timesheet => {
        this.currentTimesheetSignal.set(timesheet);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * Submit timesheet for approval
   */
  submitTimesheet(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/submit`, {}).pipe(
      tap(() => {
        // Update local state
        const current = this.currentTimesheetSignal();
        if (current && current.id === id) {
          this.currentTimesheetSignal.set({ ...current, status: 2 }); // Submitted
        }
      })
    );
  }

  /**
   * Update a timesheet entry
   */
  updateTimesheetEntry(entryId: string, request: UpdateTimesheetEntryRequest): Observable<TimesheetEntry> {
    return this.http.put<TimesheetEntry>(`${this.apiUrl}/entries/${entryId}`, request);
  }

  /**
   * Add a comment to a timesheet
   */
  addComment(timesheetId: string, request: AddCommentRequest): Observable<TimesheetComment> {
    return this.http.post<TimesheetComment>(`${this.apiUrl}/${timesheetId}/comments`, request);
  }

  // ==================== Manager Endpoints ====================

  /**
   * Get timesheets pending approval (for managers)
   */
  getPendingApprovals(): Observable<Timesheet[]> {
    this.loadingSignal.set(true);
    return this.http.get<Timesheet[]>(`${this.apiUrl}/pending-approvals`).pipe(
      tap(timesheets => {
        this.pendingApprovalsSignal.set(timesheets);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * Get timesheets for a specific employee (manager view)
   */
  getEmployeeTimesheets(employeeId: string, startDate: string, endDate: string): Observable<Timesheet[]> {
    return this.http.get<Timesheet[]>(
      `${this.apiUrl}/employee/${employeeId}?startDate=${startDate}&endDate=${endDate}`
    );
  }

  /**
   * Approve a timesheet
   */
  approveTimesheet(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, {}).pipe(
      tap(() => {
        // Remove from pending approvals
        const pending = this.pendingApprovalsSignal();
        this.pendingApprovalsSignal.set(pending.filter(t => t.id !== id));
      })
    );
  }

  /**
   * Reject a timesheet
   */
  rejectTimesheet(id: string, request: RejectTimesheetRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reject`, request).pipe(
      tap(() => {
        // Remove from pending approvals
        const pending = this.pendingApprovalsSignal();
        this.pendingApprovalsSignal.set(pending.filter(t => t.id !== id));
      })
    );
  }

  /**
   * Bulk approve timesheets
   */
  bulkApproveTimesheets(request: BulkApproveRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/bulk-approve`, request).pipe(
      tap(() => {
        // Remove approved ones from pending
        const pending = this.pendingApprovalsSignal();
        this.pendingApprovalsSignal.set(
          pending.filter(t => !request.timesheetIds.includes(t.id))
        );
      })
    );
  }

  // ==================== Admin Endpoints ====================

  /**
   * Generate timesheet for a specific employee
   */
  generateTimesheet(request: GenerateTimesheetRequest): Observable<Timesheet> {
    return this.http.post<Timesheet>(`${this.apiUrl}/generate`, request);
  }

  /**
   * Regenerate an existing timesheet
   */
  regenerateTimesheet(id: string): Observable<Timesheet> {
    return this.http.post<Timesheet>(`${this.apiUrl}/${id}/regenerate`, {});
  }

  /**
   * Get all timesheets (admin view)
   */
  getAllTimesheets(status?: number, startDate?: string, endDate?: string, limit: number = 100): Observable<Timesheet[]> {
    let params = `?limit=${limit}`;
    if (status) params += `&status=${status}`;
    if (startDate) params += `&startDate=${startDate}`;
    if (endDate) params += `&endDate=${endDate}`;

    return this.http.get<Timesheet[]>(`${this.apiUrl}/all${params}`);
  }

  /**
   * Lock a timesheet (prevent further changes)
   */
  lockTimesheet(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/lock`, {});
  }

  /**
   * Unlock a timesheet
   */
  unlockTimesheet(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/unlock`, {});
  }

  /**
   * Delete a timesheet
   */
  deleteTimesheet(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // ==================== Adjustment Endpoints ====================

  /**
   * Create an adjustment for a timesheet entry
   */
  createAdjustment(entryId: string, request: CreateAdjustmentRequest): Observable<TimesheetAdjustment> {
    return this.http.post<TimesheetAdjustment>(`${this.apiUrl}/entries/${entryId}/adjustments`, request);
  }

  /**
   * Approve an adjustment
   */
  approveAdjustment(adjustmentId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/adjustments/${adjustmentId}/approve`, {});
  }

  /**
   * Reject an adjustment
   */
  rejectAdjustment(adjustmentId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/adjustments/${adjustmentId}/reject`, { reason });
  }

  // ==================== Stats & Reports ====================

  /**
   * Get timesheet statistics
   */
  getTimesheetStats(startDate?: string, endDate?: string): Observable<TimesheetStats> {
    let params = '';
    if (startDate) params += `?startDate=${startDate}`;
    if (endDate) params += `${params ? '&' : '?'}endDate=${endDate}`;

    return this.http.get<TimesheetStats>(`${this.apiUrl}/stats${params}`).pipe(
      tap(stats => this.statsSignal.set(stats))
    );
  }

  // ==================== Utility Methods ====================

  /**
   * Clear all local state
   */
  clearState(): void {
    this.timesheetsSignal.set([]);
    this.currentTimesheetSignal.set(null);
    this.pendingApprovalsSignal.set([]);
    this.statsSignal.set(null);
    this.loadingSignal.set(false);
  }

  /**
   * Get period dates for a given period type
   */
  getPeriodDates(periodType: PeriodType, date: Date = new Date()): { start: Date, end: Date } {
    const result = { start: new Date(date), end: new Date(date) };

    switch (periodType) {
      case PeriodType.Weekly:
        // Get Monday of current week
        const day = result.start.getDay();
        const diff = result.start.getDate() - day + (day === 0 ? -6 : 1);
        result.start.setDate(diff);
        result.start.setHours(0, 0, 0, 0);

        // Get Sunday of current week
        result.end = new Date(result.start);
        result.end.setDate(result.start.getDate() + 6);
        result.end.setHours(23, 59, 59, 999);
        break;

      case PeriodType.BiWeekly:
        // Get Monday of current week
        const currentDay = result.start.getDay();
        const currentDiff = result.start.getDate() - currentDay + (currentDay === 0 ? -6 : 1);
        result.start.setDate(currentDiff);

        // Check if it's an even or odd week to align bi-weekly periods
        const weekNumber = this.getWeekNumber(result.start);
        if (weekNumber % 2 !== 0) {
          result.start.setDate(result.start.getDate() - 7);
        }
        result.start.setHours(0, 0, 0, 0);

        // End is 13 days later (2 weeks)
        result.end = new Date(result.start);
        result.end.setDate(result.start.getDate() + 13);
        result.end.setHours(23, 59, 59, 999);
        break;

      case PeriodType.Monthly:
        // First day of month
        result.start.setDate(1);
        result.start.setHours(0, 0, 0, 0);

        // Last day of month
        result.end = new Date(result.start.getFullYear(), result.start.getMonth() + 1, 0);
        result.end.setHours(23, 59, 59, 999);
        break;
    }

    return result;
  }

  /**
   * Get ISO week number
   */
  private getWeekNumber(date: Date): number {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
  }

  /**
   * Format hours for display
   */
  formatHours(hours: number): string {
    if (!hours) return '0h';
    const h = Math.floor(hours);
    const m = Math.round((hours - h) * 60);
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }
}
