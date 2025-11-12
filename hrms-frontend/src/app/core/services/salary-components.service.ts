import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SalaryComponentDto {
  id: string;
  employeeId: string;
  componentName: string;
  componentType: 'Allowance' | 'Deduction';
  amount: number;
  isRecurring: boolean;
  effectiveDate: string;
  endDate?: string;
  isActive: boolean;
  requiresApproval: boolean;
  isApproved: boolean;
  approvedBy?: string;
  approvedAt?: string;
  description?: string;
  createdAt: string;
  createdBy: string;
}

export interface CreateSalaryComponentDto {
  employeeId: string;
  componentName: string;
  componentType: 'Allowance' | 'Deduction';
  amount: number;
  isRecurring: boolean;
  effectiveDate: string;
  endDate?: string;
  description?: string;
  requiresApproval?: boolean;
}

export interface UpdateSalaryComponentDto {
  componentName?: string;
  amount?: number;
  isRecurring?: boolean;
  effectiveDate?: string;
  endDate?: string;
  description?: string;
  isActive?: boolean;
}

export interface BulkCreateComponentsDto {
  employeeIds: string[];
  componentDetails: CreateSalaryComponentDto;
}

export interface TotalAmountResponse {
  employeeId: string;
  month: number;
  year: number;
  totalAllowances?: number;
  totalDeductions?: number;
}

@Injectable({
  providedIn: 'root'
})
export class SalaryComponentsService {
  private apiUrl = `${environment.apiUrl}/SalaryComponents`;

  constructor(private http: HttpClient) {}

  /**
   * Create a new salary component
   * POST /api/SalaryComponents
   */
  createComponent(dto: CreateSalaryComponentDto): Observable<string> {
    return this.http.post<string>(this.apiUrl, dto);
  }

  /**
   * Get salary component by ID
   * GET /api/SalaryComponents/{id}
   */
  getComponent(id: string): Observable<SalaryComponentDto> {
    return this.http.get<SalaryComponentDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get all salary components for a specific employee
   * GET /api/SalaryComponents/employee/{employeeId}
   */
  getEmployeeComponents(employeeId: string, activeOnly: boolean = true): Observable<SalaryComponentDto[]> {
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    return this.http.get<SalaryComponentDto[]>(`${this.apiUrl}/employee/${employeeId}`, { params });
  }

  /**
   * Get all salary components for all employees
   * GET /api/SalaryComponents
   */
  getAllComponents(activeOnly: boolean = true): Observable<SalaryComponentDto[]> {
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    return this.http.get<SalaryComponentDto[]>(this.apiUrl, { params });
  }

  /**
   * Update salary component
   * PUT /api/SalaryComponents/{id}
   */
  updateComponent(id: string, dto: UpdateSalaryComponentDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Deactivate salary component (soft delete)
   * POST /api/SalaryComponents/{id}/deactivate
   */
  deactivateComponent(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Delete salary component permanently
   * DELETE /api/SalaryComponents/{id}
   */
  deleteComponent(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  /**
   * Approve salary component
   * POST /api/SalaryComponents/{id}/approve
   */
  approveComponent(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/approve`, {});
  }

  /**
   * Bulk create salary components for multiple employees
   * POST /api/SalaryComponents/bulk-create
   */
  bulkCreateComponents(dto: BulkCreateComponentsDto): Observable<{ message: string; componentIds: string[] }> {
    return this.http.post<{ message: string; componentIds: string[] }>(`${this.apiUrl}/bulk-create`, dto);
  }

  /**
   * Get total allowances for an employee in a specific month
   * GET /api/SalaryComponents/employee/{employeeId}/allowances
   */
  getTotalAllowances(employeeId: string, month: number, year: number): Observable<TotalAmountResponse> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<TotalAmountResponse>(`${this.apiUrl}/employee/${employeeId}/allowances`, { params });
  }

  /**
   * Get total deductions for an employee in a specific month
   * GET /api/SalaryComponents/employee/{employeeId}/deductions
   */
  getTotalDeductions(employeeId: string, month: number, year: number): Observable<TotalAmountResponse> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<TotalAmountResponse>(`${this.apiUrl}/employee/${employeeId}/deductions`, { params });
  }
}
