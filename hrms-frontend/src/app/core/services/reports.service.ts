import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardSummaryDto {
  totalEmployees: number;
  activeEmployees: number;
  totalDepartments: number;
  pendingLeaveRequests: number;
  todayAttendance: number;
  monthlyPayroll: number;
  // Add more dashboard metrics as needed
}

export interface MonthlyPayrollSummaryDto {
  month: number;
  year: number;
  totalGrossSalary: number;
  totalDeductions: number;
  totalNetSalary: number;
  employeeCount: number;
  payslips: PayslipSummaryDto[];
}

export interface PayslipSummaryDto {
  employeeId: string;
  employeeName: string;
  grossSalary: number;
  deductions: number;
  netSalary: number;
}

export interface AttendanceReportDto {
  month: number;
  year: number;
  employeeRecords: EmployeeAttendanceDto[];
}

export interface EmployeeAttendanceDto {
  employeeId: string;
  employeeName: string;
  presentDays: number;
  absentDays: number;
  lateDays: number;
  overtimeHours: number;
}

export interface OvertimeReportDto {
  month: number;
  year: number;
  records: OvertimeRecordDto[];
}

export interface OvertimeRecordDto {
  employeeId: string;
  employeeName: string;
  totalOvertimeHours: number;
  overtimeAmount: number;
}

export interface LeaveBalanceReportDto {
  year: number;
  employees: EmployeeLeaveBalanceDto[];
}

export interface EmployeeLeaveBalanceDto {
  employeeId: string;
  employeeName: string;
  annualLeaveBalance: number;
  sickLeaveBalance: number;
  casualLeaveBalance: number;
  totalLeavesTaken: number;
}

export interface LeaveUtilizationDto {
  year: number;
  totalLeavesTaken: number;
  averageLeavePerEmployee: number;
  mostUsedLeaveType: string;
}

export interface HeadcountReportDto {
  totalEmployees: number;
  byDepartment: DepartmentHeadcountDto[];
  byEmploymentType: { type: string; count: number; }[];
  byGender: { gender: string; count: number; }[];
}

export interface DepartmentHeadcountDto {
  departmentName: string;
  employeeCount: number;
  percentageOfTotal: number;
}

export interface ExpatriateReportDto {
  totalExpatriates: number;
  expatriates: ExpatriateDto[];
}

export interface ExpatriateDto {
  employeeId: string;
  employeeName: string;
  nationality: string;
  workPermitExpiry?: string;
  passportExpiry?: string;
  visaExpiry?: string;
  daysUntilExpiry?: number;
}

export interface TurnoverReportDto {
  month: number;
  year: number;
  totalTerminations: number;
  turnoverRate: number;
  terminationsByReason: { reason: string; count: number; }[];
}

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  private apiUrl = `${environment.apiUrl}/Reports`;

  constructor(private http: HttpClient) {}

  // ==================== DASHBOARD ====================

  /**
   * Get dashboard summary with KPIs
   * GET /api/Reports/dashboard
   */
  getDashboard(): Observable<DashboardSummaryDto> {
    return this.http.get<DashboardSummaryDto>(`${this.apiUrl}/dashboard`);
  }

  // ==================== PAYROLL REPORTS ====================

  /**
   * Get monthly payroll summary
   * GET /api/Reports/payroll/monthly-summary
   */
  getMonthlyPayrollSummary(month: number, year: number): Observable<MonthlyPayrollSummaryDto> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<MonthlyPayrollSummaryDto>(`${this.apiUrl}/payroll/monthly-summary`, { params });
  }

  /**
   * Export monthly payroll summary to Excel
   * GET /api/Reports/payroll/monthly-summary/excel
   */
  exportMonthlyPayroll(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/payroll/monthly-summary/excel`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Export statutory deductions report to Excel
   * GET /api/Reports/payroll/statutory-deductions/excel
   */
  exportStatutoryDeductions(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/payroll/statutory-deductions/excel`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Export bank transfer list to Excel
   * GET /api/Reports/payroll/bank-transfer-list/excel
   */
  exportBankTransferList(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/payroll/bank-transfer-list/excel`, {
      params,
      responseType: 'blob'
    });
  }

  // ==================== ATTENDANCE REPORTS ====================

  /**
   * Get monthly attendance report
   * GET /api/Reports/attendance/monthly-register
   */
  getMonthlyAttendanceRegister(month: number, year: number): Observable<AttendanceReportDto> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<AttendanceReportDto>(`${this.apiUrl}/attendance/monthly-register`, { params });
  }

  /**
   * Export monthly attendance register to Excel
   * GET /api/Reports/attendance/monthly-register/excel
   */
  exportAttendanceRegister(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/attendance/monthly-register/excel`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Get overtime report
   * GET /api/Reports/attendance/overtime
   */
  getOvertimeReport(month: number, year: number): Observable<OvertimeReportDto> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<OvertimeReportDto>(`${this.apiUrl}/attendance/overtime`, { params });
  }

  /**
   * Export overtime report to Excel
   * GET /api/Reports/attendance/overtime/excel
   */
  exportOvertimeReport(month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/attendance/overtime/excel`, {
      params,
      responseType: 'blob'
    });
  }

  // ==================== LEAVE REPORTS ====================

  /**
   * Get leave balance report for all employees
   * GET /api/Reports/leave/balance
   */
  getLeaveBalanceReport(year: number): Observable<LeaveBalanceReportDto> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get<LeaveBalanceReportDto>(`${this.apiUrl}/leave/balance`, { params });
  }

  /**
   * Export leave balance report to Excel
   * GET /api/Reports/leave/balance/excel
   */
  exportLeaveBalance(year: number): Observable<Blob> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get(`${this.apiUrl}/leave/balance/excel`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Get leave utilization report
   * GET /api/Reports/leave/utilization
   */
  getLeaveUtilization(year: number): Observable<LeaveUtilizationDto> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get<LeaveUtilizationDto>(`${this.apiUrl}/leave/utilization`, { params });
  }

  // ==================== EMPLOYEE REPORTS ====================

  /**
   * Get headcount report
   * GET /api/Reports/employees/headcount
   */
  getHeadcount(): Observable<HeadcountReportDto> {
    return this.http.get<HeadcountReportDto>(`${this.apiUrl}/employees/headcount`);
  }

  /**
   * Export headcount report to Excel
   * GET /api/Reports/employees/headcount/excel
   */
  exportHeadcount(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/employees/headcount/excel`, {
      responseType: 'blob'
    });
  }

  /**
   * Get expatriate report with document expiry tracking
   * GET /api/Reports/employees/expatriates
   */
  getExpatriates(): Observable<ExpatriateReportDto> {
    return this.http.get<ExpatriateReportDto>(`${this.apiUrl}/employees/expatriates`);
  }

  /**
   * Export expatriate report to Excel
   * GET /api/Reports/employees/expatriates/excel
   */
  exportExpatriates(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/employees/expatriates/excel`, {
      responseType: 'blob'
    });
  }

  /**
   * Get turnover report
   * GET /api/Reports/employees/turnover
   */
  getTurnover(month: number, year: number): Observable<TurnoverReportDto> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<TurnoverReportDto>(`${this.apiUrl}/employees/turnover`, { params });
  }

  // ==================== PDF GENERATION ====================

  /**
   * Generate payslip PDF
   * GET /api/Reports/pdf/payslip/{payslipId}
   */
  generatePayslipPdf(payslipId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/pdf/payslip/${payslipId}`, {
      responseType: 'blob'
    });
  }

  /**
   * Generate employment certificate PDF
   * GET /api/Reports/pdf/employment-certificate/{employeeId}
   */
  generateEmploymentCertificate(employeeId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/pdf/employment-certificate/${employeeId}`, {
      responseType: 'blob'
    });
  }

  /**
   * Generate attendance report PDF
   * GET /api/Reports/pdf/attendance/{employeeId}
   */
  generateAttendanceReportPdf(employeeId: string, month: number, year: number): Observable<Blob> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get(`${this.apiUrl}/pdf/attendance/${employeeId}`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Generate leave report PDF
   * GET /api/Reports/pdf/leave/{employeeId}
   */
  generateLeaveReportPdf(employeeId: string, year: number): Observable<Blob> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get(`${this.apiUrl}/pdf/leave/${employeeId}`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Generate tax certificate (Form C for MRA) PDF
   * GET /api/Reports/pdf/tax-certificate/{employeeId}
   */
  generateTaxCertificate(employeeId: string, year: number): Observable<Blob> {
    const params = new HttpParams().set('year', year.toString());
    return this.http.get(`${this.apiUrl}/pdf/tax-certificate/${employeeId}`, {
      params,
      responseType: 'blob'
    });
  }

  // ==================== HELPER METHODS ====================

  /**
   * Download blob as file (helper for Excel/PDF downloads)
   */
  downloadFile(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
