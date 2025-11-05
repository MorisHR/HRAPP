import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  Payroll,
  Payslip,
  PayrollCycle,
  PayrollResult,
  CalculateFromTimesheetsRequest,
  BatchCalculateFromTimesheetsRequest,
  BatchPayrollResult
} from '../models/payroll.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PayrollService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/payroll`;

  // Signals for reactive state
  private payrollsSignal = signal<Payroll[]>([]);
  private cyclesSignal = signal<PayrollCycle[]>([]);
  private loadingSignal = signal<boolean>(false);
  private currentPayrollResultSignal = signal<PayrollResult | null>(null);

  readonly payrolls = this.payrollsSignal.asReadonly();
  readonly cycles = this.cyclesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly currentPayrollResult = this.currentPayrollResultSignal.asReadonly();

  getPayrollCycles(): Observable<PayrollCycle[]> {
    this.loadingSignal.set(true);
    return this.http.get<PayrollCycle[]>(`${this.apiUrl}/cycles`).pipe(
      tap(cycles => {
        this.cyclesSignal.set(cycles);
        this.loadingSignal.set(false);
      })
    );
  }

  getPayrollByCycle(cycleId: string): Observable<Payroll[]> {
    return this.http.get<Payroll[]>(`${this.apiUrl}/cycles/${cycleId}`).pipe(
      tap(payrolls => this.payrollsSignal.set(payrolls))
    );
  }

  getEmployeePayslips(employeeId: string): Observable<Payslip[]> {
    return this.http.get<Payslip[]>(`${this.apiUrl}/employee/${employeeId}/payslips`);
  }

  getPayslip(payslipId: string): Observable<Payslip> {
    return this.http.get<Payslip>(`${this.apiUrl}/payslips/${payslipId}`);
  }

  processPayroll(cycleId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/cycles/${cycleId}/process`, {});
  }

  downloadPayslip(payslipId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/payslips/${payslipId}/download`, {
      responseType: 'blob'
    });
  }

  // Timesheet-based Payroll Calculation Methods

  /**
   * Calculate payroll from approved timesheets for a single employee
   * This is the actual calculation that could be saved to generate a payslip
   */
  calculatePayrollFromTimesheets(request: CalculateFromTimesheetsRequest): Observable<PayrollResult> {
    this.loadingSignal.set(true);
    return this.http.post<PayrollResult>(`${this.apiUrl}/calculate-from-timesheets`, request).pipe(
      tap(result => {
        this.currentPayrollResultSignal.set(result);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * Preview payroll calculation from timesheets without saving
   * Useful for showing employees what their payroll will look like
   */
  previewPayrollFromTimesheets(request: CalculateFromTimesheetsRequest): Observable<PayrollResult> {
    this.loadingSignal.set(true);
    return this.http.post<PayrollResult>(`${this.apiUrl}/preview-from-timesheets`, request).pipe(
      tap(result => {
        this.currentPayrollResultSignal.set(result);
        this.loadingSignal.set(false);
      })
    );
  }

  /**
   * Batch process payroll from timesheets for multiple employees
   * Used by HR/Admin to calculate payroll for entire departments or company
   */
  processBatchFromTimesheets(request: BatchCalculateFromTimesheetsRequest): Observable<BatchPayrollResult> {
    this.loadingSignal.set(true);
    return this.http.post<BatchPayrollResult>(`${this.apiUrl}/process-batch-from-timesheets`, request).pipe(
      tap(() => this.loadingSignal.set(false))
    );
  }

  /**
   * Clear current payroll result from state
   */
  clearCurrentPayrollResult(): void {
    this.currentPayrollResultSignal.set(null);
  }
}
