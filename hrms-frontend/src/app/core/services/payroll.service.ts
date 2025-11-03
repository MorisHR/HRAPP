import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Payroll, Payslip, PayrollCycle } from '../models/payroll.model';
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

  readonly payrolls = this.payrollsSignal.asReadonly();
  readonly cycles = this.cyclesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

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
}
