# HRMS Frontend - Employee Module Services & Models Analysis

## Overview
Complete analysis of all frontend services and models related to the Employee module in the HRMS application. This document provides comprehensive code references and API endpoint mappings.

---

## 1. EMPLOYEE SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/employee.service.ts`

### Service Code
```typescript
import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map, catchError, throwError } from 'rxjs';
import { Employee, CreateEmployeeRequest } from '../models/employee.model';
import { environment } from '../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  count?: number;
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/employees`;

  // Signals for reactive state
  private employeesSignal = signal<Employee[]>([]);
  private loadingSignal = signal<boolean>(false);
  private errorSignal = signal<string | null>(null);

  readonly employees = this.employeesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  getEmployees(): Observable<Employee[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<ApiResponse<Employee[]>>(this.apiUrl).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load employees');
        }
        return response.data;
      }),
      tap(employees => {
        this.employeesSignal.set(employees);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        this.loadingSignal.set(false);
        this.errorSignal.set(error.message || 'Error loading employees');
        return throwError(() => error);
      })
    );
  }

  getEmployeeById(id: string): Observable<Employee> {
    return this.http.get<ApiResponse<Employee>>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load employee');
        }
        return response.data;
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  createEmployee(request: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<ApiResponse<Employee>>(this.apiUrl, request).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to create employee');
        }
        return response.data;
      }),
      tap(employee => {
        this.employeesSignal.update(employees => [...employees, employee]);
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  updateEmployee(id: string, employee: Partial<Employee>): Observable<Employee> {
    return this.http.put<ApiResponse<Employee>>(`${this.apiUrl}/${id}`, employee).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to update employee');
        }
        return response.data;
      }),
      tap(updatedEmployee => {
        this.employeesSignal.update(employees =>
          employees.map(e => e.id === id ? updatedEmployee : e)
        );
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }

  deleteEmployee(id: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Failed to delete employee');
        }
        return;
      }),
      tap(() => {
        this.employeesSignal.update(employees => employees.filter(e => e.id !== id));
      }),
      catchError(error => {
        return throwError(() => error);
      })
    );
  }
}
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/employees` | Get all employees |
| GET | `/employees/{id}` | Get employee by ID |
| POST | `/employees` | Create new employee |
| PUT | `/employees/{id}` | Update employee |
| DELETE | `/employees/{id}` | Delete employee |

### Reactive State (Signals)
- `employees`: Employee[] - List of all employees
- `loading`: boolean - Loading state
- `error`: string | null - Error messages

---

## 2. EMPLOYEE MODEL

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/models/employee.model.ts`

### Model Code
```typescript
export interface Employee {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  status: EmployeeStatus;
  avatarUrl?: string;
  managerId?: string;
  managerName?: string;
}

export enum EmployeeStatus {
  Active = 'Active',
  OnLeave = 'OnLeave',
  Suspended = 'Suspended',
  Terminated = 'Terminated'
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  managerId?: string;
}
```

---

## 3. EMPLOYEE DRAFT SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/employee-draft.service.ts`

### Purpose
Manages draft versions of employee forms, supporting both backend persistence and local offline storage.

### Key Interfaces
```typescript
export interface EmployeeDraft {
  id: string;
  formDataJson: string;
  draftName: string;
  completionPercentage: number;
  createdBy: string;
  createdByName: string;
  createdAt: Date;
  lastEditedBy?: string;
  lastEditedByName?: string;
  lastEditedAt: Date;
  expiresAt: Date;
  daysUntilExpiry: number;
  isExpired: boolean;
}

export interface SaveDraftRequest {
  id?: string;
  formDataJson: string;
  draftName: string;
  completionPercentage: number;
}

export interface SaveDraftResponse {
  success: boolean;
  message: string;
  data: EmployeeDraft;
}

export interface DraftsResponse {
  success: boolean;
  data: EmployeeDraft[];
  count: number;
  message: string;
}
```

### API Methods
```typescript
// Get all drafts for current tenant
getAllDrafts(): Observable<DraftsResponse>

// Get specific draft by ID
getDraftById(id: string): Observable<{ success: boolean; data: EmployeeDraft; message: string }>

// Save draft (create or update) - Used for both manual and auto-save
saveDraft(request: SaveDraftRequest): Observable<SaveDraftResponse>

// Finalize draft - convert to employee
finalizeDraft(draftId: string): Observable<{ success: boolean; message: string; data: any }>

// Delete draft
deleteDraft(id: string): Observable<{ success: boolean; message: string }>
```

### LocalStorage Helpers
```typescript
// Save draft to localStorage for instant feedback
saveToLocalStorage(draftId: string, formData: any): void

// Load draft from localStorage
loadFromLocalStorage(draftId: string): any | null

// Clear draft from localStorage
clearFromLocalStorage(draftId: string): void

// List all local drafts
listLocalDrafts(): Array<{ key: string; data: any }>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/employee-drafts` | Get all drafts |
| GET | `/employee-drafts/{id}` | Get draft by ID |
| POST | `/employee-drafts` | Create/update draft |
| POST | `/employee-drafts/{id}/finalize` | Convert draft to employee |
| DELETE | `/employee-drafts/{id}` | Delete draft |

---

## 4. ADDRESS SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/services/address.service.ts`

### Purpose
Provides geographic lookup functionality for address fields (districts, villages, postal codes).

### Service Code
```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DistrictDto, VillageDto, PostalCodeDto } from '../models/address.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AddressService {
  private apiUrl = `${environment.apiUrl}/address-lookup`;

  constructor(private http: HttpClient) {}

  /**
   * Get all districts for dropdown population
   */
  getDistricts(): Observable<DistrictDto[]> {
    return this.http.get<DistrictDto[]>(`${this.apiUrl}/districts`);
  }

  /**
   * Get villages by district ID for cascading dropdown
   */
  getVillagesByDistrict(districtId: number): Observable<VillageDto[]> {
    return this.http.get<VillageDto[]>(`${this.apiUrl}/districts/${districtId}/villages`);
  }

  /**
   * Get all villages for dropdown population
   */
  getVillages(): Observable<VillageDto[]> {
    return this.http.get<VillageDto[]>(`${this.apiUrl}/villages`);
  }

  /**
   * Search postal codes by code for autocomplete
   */
  searchPostalCodes(code: string): Observable<PostalCodeDto[]> {
    return this.http.get<PostalCodeDto[]>(`${this.apiUrl}/postal-codes/search?code=${code}`);
  }

  /**
   * Get all postal codes
   */
  getPostalCodes(): Observable<PostalCodeDto[]> {
    return this.http.get<PostalCodeDto[]>(`${this.apiUrl}/postal-codes`);
  }

  /**
   * Lookup postal code and auto-fill address fields
   */
  lookupPostalCode(code: string): Observable<PostalCodeDto[]> {
    return this.searchPostalCodes(code);
  }
}
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/address-lookup/districts` | Get all districts |
| GET | `/address-lookup/districts/{id}/villages` | Get villages by district |
| GET | `/address-lookup/villages` | Get all villages |
| GET | `/address-lookup/postal-codes` | Get all postal codes |
| GET | `/address-lookup/postal-codes/search?code={code}` | Search postal codes |

---

## 5. ADDRESS MODELS

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/models/address.models.ts`

### Models Code
```typescript
export interface DistrictDto {
  id: number;
  districtCode: string;
  districtName: string;
  districtNameFrench?: string;
  region: string;
  areaSqKm?: number;
  population?: number;
  displayOrder: number;
  isActive: boolean;
}

export interface VillageDto {
  id: number;
  villageCode: string;
  villageName: string;
  postalCode: string;
  districtId: number;
  displayOrder: number;
  isActive: boolean;
}

export interface PostalCodeDto {
  id: number;
  code: string;
  villageName: string;
  districtName: string;
  region: string;
  villageId: number;
  districtId: number;
  isPrimary: boolean;
  isActive: boolean;
}
```

---

## 6. DEPARTMENT SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/departments/services/department.service.ts`

### Purpose
Manages organization departments and hierarchies.

### Key DTOs
```typescript
export interface DepartmentDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  parentDepartmentName?: string;
  departmentHeadId?: string;
  departmentHeadName?: string;
  costCenterCode?: string;
  isActive: boolean;
  employeeCount: number;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface DepartmentHierarchyDto {
  id: string;
  name: string;
  code: string;
  parentDepartmentId?: string;
  departmentHeadName?: string;
  employeeCount: number;
  isActive: boolean;
  children: DepartmentHierarchyDto[];
}

export interface DepartmentDropdownDto {
  id: string;
  name: string;
  code: string;
}
```

### API Methods
```typescript
getAll(): Observable<DepartmentDto[]>
getById(id: string): Observable<DepartmentDto>
create(department: CreateDepartmentDto): Observable<any>
update(id: string, department: UpdateDepartmentDto): Observable<any>
delete(id: string): Observable<any>
getHierarchy(): Observable<DepartmentHierarchyDto[]>
getDropdown(): Observable<DepartmentDropdownDto[]>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/department` | Get all departments |
| GET | `/api/department/{id}` | Get department by ID |
| POST | `/api/department` | Create department |
| PUT | `/api/department/{id}` | Update department |
| DELETE | `/api/department/{id}` | Delete department |
| GET | `/api/department/hierarchy` | Get department hierarchy tree |
| GET | `/api/department/dropdown` | Get dropdown list |

---

## 7. LOCATION SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/locations/location.service.ts`

### Purpose
Manages physical work locations where employees are based.

### Key DTOs
```typescript
export interface LocationDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  region?: string;
  postalCode?: string;
  country: string;
  phone?: string;
  email?: string;
  workingHoursJson?: string;
  timezone: string;
  locationManagerId?: string;
  locationManagerName?: string;
  capacityHeadcount?: number;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  deviceCount: number;
  employeeCount: number;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface LocationDropdownDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;
  isActive: boolean;
}

export interface LocationSummaryDto {
  id: string;
  locationCode: string;
  locationName: string;
  locationType?: string;
  city?: string;
  region?: string;
  country: string;
  deviceCount: number;
  employeeCount: number;
  isActive: boolean;
}
```

### API Methods
```typescript
getAll(): Observable<LocationDto[]>
getById(id: string): Observable<LocationDto>
create(location: CreateLocationDto): Observable<any>
update(id: string, location: UpdateLocationDto): Observable<any>
delete(id: string): Observable<any>
getSummaries(): Observable<LocationSummaryDto[]>
getDropdown(): Observable<LocationDropdownDto[]>
getByType(locationType: string): Observable<LocationDto[]>
activate(id: string): Observable<any>
deactivate(id: string): Observable<any>
getStatistics(id: string): Observable<{ deviceCount, employeeCount, activeDeviceCount, lastSyncTime }>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/locations` | Get all locations |
| GET | `/api/locations/{id}` | Get location by ID |
| POST | `/api/locations` | Create location |
| PUT | `/api/locations/{id}` | Update location |
| DELETE | `/api/locations/{id}` | Delete location |
| GET | `/api/locations/summaries` | Get location summaries |
| GET | `/api/locations/dropdown` | Get dropdown list |
| GET | `/api/locations/by-type/{type}` | Get locations by type |
| PATCH | `/api/locations/{id}/activate` | Activate location |
| PATCH | `/api/locations/{id}/deactivate` | Deactivate location |
| GET | `/api/locations/{id}/statistics` | Get location statistics |

---

## 8. PAYROLL SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/payroll.service.ts`

### Purpose
Handles payroll cycles, payslips, and timesheet-based payroll calculations including Mauritius statutory deductions.

### Key Methods
```typescript
getPayrollCycles(): Observable<PayrollCycle[]>
getPayrollByCycle(cycleId: string): Observable<Payroll[]>
getEmployeePayslips(employeeId: string): Observable<Payslip[]>
getPayslip(payslipId: string): Observable<Payslip>
processPayroll(cycleId: string): Observable<void>
downloadPayslip(payslipId: string): Observable<Blob>

// Timesheet-based Payroll Calculation
calculatePayrollFromTimesheets(request: CalculateFromTimesheetsRequest): Observable<PayrollResult>
previewPayrollFromTimesheets(request: CalculateFromTimesheetsRequest): Observable<PayrollResult>
processBatchFromTimesheets(request: BatchCalculateFromTimesheetsRequest): Observable<BatchPayrollResult>
clearCurrentPayrollResult(): void
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/payroll/cycles` | Get payroll cycles |
| GET | `/payroll/cycles/{id}` | Get payroll by cycle |
| GET | `/payroll/employee/{id}/payslips` | Get employee payslips |
| GET | `/payroll/payslips/{id}` | Get payslip details |
| POST | `/payroll/cycles/{id}/process` | Process payroll |
| GET | `/payroll/payslips/{id}/download` | Download payslip (PDF) |
| POST | `/payroll/calculate-from-timesheets` | Calculate payroll from timesheets |
| POST | `/payroll/preview-from-timesheets` | Preview calculation |
| POST | `/payroll/process-batch-from-timesheets` | Batch process payroll |

### Reactive State (Signals)
- `payrolls`: Payroll[] - List of payrolls
- `cycles`: PayrollCycle[] - Available cycles
- `loading`: boolean - Loading state
- `currentPayrollResult`: PayrollResult | null - Current calculation result

---

## 9. PAYROLL MODEL

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/models/payroll.model.ts`

### Key Interfaces

#### Core Payroll Interfaces
```typescript
export interface Payroll {
  id: string;
  employeeId: string;
  employeeName: string;
  payPeriodStart: string;
  payPeriodEnd: string;
  basicSalary: number;
  allowances: number;
  deductions: number;
  netSalary: number;
  status: PayrollStatus;
  paymentDate?: string;
}

export interface Payslip {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  department: string;
  designation: string;
  payPeriod: string;
  payDate: string;
  earnings: PayslipItem[];
  deductions: PayslipItem[];
  grossPay: number;
  totalDeductions: number;
  netPay: number;
}

export interface PayslipItem {
  name: string;
  amount: number;
}

export interface PayrollCycle {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: PayrollStatus;
  employeeCount: number;
  totalAmount: number;
}

export enum PayrollStatus {
  Draft = 'Draft',
  Processed = 'Processed',
  Paid = 'Paid',
  Cancelled = 'Cancelled'
}
```

#### Timesheet-based Payroll Calculation
```typescript
export interface PayrollResult {
  // Employee Information
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  department: string;
  jobTitle: string;
  periodStart: string;
  periodEnd: string;

  // Basic Salary
  basicSalary: number;
  hourlyRate: number;

  // Hours Breakdown
  totalRegularHours: number;
  totalOvertimeHours: number;
  totalHolidayHours: number;
  totalLeaveHours: number;
  totalPayableHours: number;

  // Working Days
  workingDays: number;
  leaveDays: number;

  // Gross Pay Components
  regularPay: number;
  overtimePay: number;
  holidayPay: number;
  leavePay: number;

  // Allowances
  housingAllowance: number;
  transportAllowance: number;
  mealAllowance: number;
  mobileAllowance: number;
  otherAllowances: number;

  // Total Gross Salary
  totalGrossSalary: number;

  // Mauritius Statutory Deductions
  statutoryDeductions: MauritiusDeductions;

  // Other Deductions
  otherDeductions: number;
  loanDeduction: number;
  advanceDeduction: number;

  // Total Deductions
  totalDeductions: number;

  // Net Salary
  netSalary: number;

  // Timesheet References
  timesheetIds: string[];
  timesheetsProcessed: number;

  // Calculation Metadata
  calculatedAt: string;
  calculationNotes?: string;
  hasWarnings: boolean;
  warnings: string[];
}

export interface MauritiusDeductions {
  // Employee Contributions (deducted from salary)
  npF_Employee: number;
  nsF_Employee: number;
  csG_Employee: number;
  payE_Tax: number;
  totalEmployeeContributions: number;

  // Employer Contributions (recorded but not deducted from employee salary)
  npF_Employer: number;
  nsF_Employer: number;
  csG_Employer: number;
  prgF_Contribution: number;
  trainingLevy: number;
  totalEmployerContributions: number;

  // Calculation Details
  isBelowCSG_Threshold: boolean;
  csG_EmployeeRate: number;
  csG_EmployerRate: number;
  prgF_Rate: number;
  taxBracket: string;
}

export interface CalculateFromTimesheetsRequest {
  employeeId: string;
  periodStart: string;
  periodEnd: string;
}

export interface BatchCalculateFromTimesheetsRequest {
  employeeIds: string[];
  periodStart: string;
  periodEnd: string;
}

export interface BatchPayrollResult {
  results: PayrollResult[];
  totalProcessed: number;
  totalFailed: number;
  errors: string[];
  processedAt: string;
}
```

#### Helper Functions
```typescript
export function formatCurrency(amount: number): string
export function formatHours(hours: number): string
export function formatPercentage(rate: number): string
export function calculateTaxBracketLabel(annualIncome: number): string
```

---

## 10. TIMESHEET SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/timesheet.service.ts`

### Purpose
Complete timesheet management including generation, submission, approval, and adjustments.

### Employee Methods
```typescript
getMyTimesheets(status?: number, limit: number = 50): Observable<Timesheet[]>
getTimesheetById(id: string): Observable<Timesheet>
submitTimesheet(id: string): Observable<any>
updateTimesheetEntry(entryId: string, request: UpdateTimesheetEntryRequest): Observable<TimesheetEntry>
addComment(timesheetId: string, request: AddCommentRequest): Observable<TimesheetComment>
```

### Manager Methods
```typescript
getPendingApprovals(): Observable<Timesheet[]>
getEmployeeTimesheets(employeeId: string, startDate: string, endDate: string): Observable<Timesheet[]>
approveTimesheet(id: string): Observable<any>
rejectTimesheet(id: string, request: RejectTimesheetRequest): Observable<any>
bulkApproveTimesheets(request: BulkApproveRequest): Observable<any>
```

### Admin Methods
```typescript
generateTimesheet(request: GenerateTimesheetRequest): Observable<Timesheet>
regenerateTimesheet(id: string): Observable<Timesheet>
getAllTimesheets(status?: number, startDate?: string, endDate?: string, limit: number = 100): Observable<Timesheet[]>
lockTimesheet(id: string): Observable<any>
unlockTimesheet(id: string): Observable<any>
deleteTimesheet(id: string): Observable<any>
```

### Adjustment Methods
```typescript
createAdjustment(entryId: string, request: CreateAdjustmentRequest): Observable<TimesheetAdjustment>
approveAdjustment(adjustmentId: string): Observable<any>
rejectAdjustment(adjustmentId: string, reason: string): Observable<any>
```

### Stats & Utility
```typescript
getTimesheetStats(startDate?: string, endDate?: string): Observable<TimesheetStats>
clearState(): void
getPeriodDates(periodType: PeriodType, date: Date): { start: Date, end: Date }
formatHours(hours: number): string
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/timesheet/my-timesheets` | Get my timesheets |
| GET | `/timesheet/{id}` | Get timesheet by ID |
| POST | `/timesheet/{id}/submit` | Submit timesheet |
| PUT | `/timesheet/entries/{id}` | Update entry |
| POST | `/timesheet/{id}/comments` | Add comment |
| GET | `/timesheet/pending-approvals` | Get pending approvals |
| GET | `/timesheet/employee/{id}` | Get employee timesheets |
| POST | `/timesheet/{id}/approve` | Approve timesheet |
| POST | `/timesheet/{id}/reject` | Reject timesheet |
| POST | `/timesheet/bulk-approve` | Bulk approve |
| POST | `/timesheet/generate` | Generate timesheet |
| POST | `/timesheet/{id}/regenerate` | Regenerate timesheet |
| GET | `/timesheet/all` | Get all timesheets |
| POST | `/timesheet/{id}/lock` | Lock timesheet |
| POST | `/timesheet/{id}/unlock` | Unlock timesheet |
| DELETE | `/timesheet/{id}` | Delete timesheet |
| POST | `/timesheet/entries/{id}/adjustments` | Create adjustment |
| POST | `/timesheet/adjustments/{id}/approve` | Approve adjustment |
| POST | `/timesheet/adjustments/{id}/reject` | Reject adjustment |
| GET | `/timesheet/stats` | Get statistics |

### Reactive State (Signals)
- `timesheets`: Timesheet[] - List of timesheets
- `currentTimesheet`: Timesheet | null - Currently viewed timesheet
- `pendingApprovals`: Timesheet[] - Timesheets pending approval
- `stats`: TimesheetStats | null - Statistics data
- `loading`: boolean - Loading state

---

## 11. TIMESHEET MODEL

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/models/timesheet.model.ts`

### Core Interfaces
```typescript
export interface Timesheet {
  id: string;
  employeeId: string;
  employeeName?: string;
  periodType: PeriodType;
  periodStart: string;
  periodEnd: string;
  totalRegularHours: number;
  totalOvertimeHours: number;
  totalHolidayHours: number;
  totalSickLeaveHours: number;
  totalAnnualLeaveHours: number;
  totalAbsentHours: number;
  totalPayableHours: number;
  status: TimesheetStatus;
  submittedAt?: string;
  submittedBy?: string;
  approvedAt?: string;
  approvedBy?: string;
  approvedByName?: string;
  rejectedAt?: string;
  rejectedBy?: string;
  rejectionReason?: string;
  isLocked: boolean;
  lockedAt?: string;
  lockedBy?: string;
  notes?: string;
  entries?: TimesheetEntry[];
  comments?: TimesheetComment[];
  createdAt: string;
  updatedAt?: string;
}

export interface TimesheetEntry {
  id: string;
  timesheetId: string;
  date: string;
  attendanceId?: string;
  clockInTime?: string;
  clockOutTime?: string;
  breakDuration: number;
  actualHours: number;
  regularHours: number;
  overtimeHours: number;
  holidayHours: number;
  sickLeaveHours: number;
  annualLeaveHours: number;
  isAbsent: boolean;
  isHoliday: boolean;
  isWeekend: boolean;
  isOnLeave: boolean;
  dayType: DayType;
  notes?: string;
}

export interface TimesheetAdjustment {
  id: string;
  timesheetEntryId: string;
  adjustmentType: AdjustmentType;
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  reason: string;
  adjustedBy: string;
  adjustedByName?: string;
  adjustedAt: string;
  status: AdjustmentStatus;
  approvedBy?: string;
  approvedByName?: string;
  approvedAt?: string;
  rejectionReason?: string;
}

export interface TimesheetComment {
  id: string;
  timesheetId: string;
  userId: string;
  userName: string;
  comment: string;
  commentedAt: string;
}

export interface TimesheetStats {
  totalTimesheets: number;
  draftTimesheets: number;
  submittedTimesheets: number;
  approvedTimesheets: number;
  rejectedTimesheets: number;
  totalRegularHours: number;
  totalOvertimeHours: number;
  averageWorkHours: number;
}
```

### Enums
```typescript
export enum TimesheetStatus {
  Draft = 1,
  Submitted = 2,
  Approved = 3,
  Rejected = 4,
  Locked = 5
}

export enum PeriodType {
  Weekly = 1,
  BiWeekly = 2,
  Monthly = 3
}

export enum DayType {
  Regular = 1,
  Weekend = 2,
  Holiday = 3,
  SickLeave = 4,
  AnnualLeave = 5,
  CasualLeave = 6,
  UnpaidLeave = 7,
  Absent = 8
}

export enum AdjustmentType {
  ManualCorrection = 1,
  SystemCorrection = 2,
  AttendanceUpdate = 3,
  LeaveUpdate = 4
}

export enum AdjustmentStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3
}
```

### Request/Response DTOs
```typescript
export interface GenerateTimesheetRequest {
  employeeId: string;
  periodStart: string;
  periodEnd: string;
  periodType: PeriodType;
}

export interface UpdateTimesheetEntryRequest {
  clockInTime?: string;
  clockOutTime?: string;
  breakDuration?: number;
  notes?: string;
}

export interface CreateAdjustmentRequest {
  adjustmentType: AdjustmentType;
  fieldName: string;
  oldValue?: string;
  newValue?: string;
  reason: string;
}

export interface BulkApproveRequest {
  timesheetIds: string[];
}

export interface RejectTimesheetRequest {
  reason: string;
}

export interface AddCommentRequest {
  comment: string;
}
```

### Helper Functions
```typescript
export function getStatusLabel(status: TimesheetStatus): string
export function getStatusColor(status: TimesheetStatus): string
export function getDayTypeLabel(dayType: DayType): string
export function canEditTimesheet(timesheet: Timesheet): boolean
export function canSubmitTimesheet(timesheet: Timesheet): boolean
export function canApproveTimesheet(timesheet: Timesheet): boolean
```

---

## 12. BIOMETRIC DEVICE SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/features/tenant/organization/devices/biometric-device.service.ts`

### Purpose
Manages biometric devices for attendance tracking with sync capabilities.

### Key DTOs
```typescript
export interface BiometricDeviceDto {
  id: string;
  deviceCode: string;
  machineName: string;
  machineId: string;
  deviceType: string;
  model?: string;
  locationId?: string;
  locationName?: string;
  ipAddress?: string;
  port: number;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  lastSyncTime?: string;
  lastSyncStatus?: string;
  lastSyncRecordCount: number;
  deviceStatus: string;
  isActive: boolean;
  totalAttendanceRecords: number;
  authorizedEmployeeCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface DeviceSyncStatusDto {
  deviceId: string;
  deviceCode: string;
  machineName: string;
  locationName?: string;
  syncEnabled: boolean;
  syncIntervalMinutes: number;
  lastSyncTime?: string;
  lastSyncStatus?: string;
  lastSyncRecordCount: number;
  minutesSinceLastSync?: number;
  deviceStatus: string;
  isOnline: boolean;
  isOfflineAlertTriggered: boolean;
  totalSyncCount: number;
  successfulSyncCount: number;
  failedSyncCount: number;
  syncSuccessRate: number;
}
```

### Device Management Methods
```typescript
getDevices(): Observable<BiometricDeviceDto[]>
getDevice(id: string): Observable<BiometricDeviceDto>
createDevice(device: CreateBiometricDeviceDto): Observable<BiometricDeviceDto>
updateDevice(id: string, device: UpdateBiometricDeviceDto): Observable<BiometricDeviceDto>
deleteDevice(id: string): Observable<void>
testConnection(connectionData: TestConnectionDto): Observable<ConnectionTestResult>
syncDevice(id: string): Observable<SyncResult>
getSyncStatus(id: string): Observable<DeviceSyncStatusDto>
getAllSyncStatuses(): Observable<DeviceSyncStatusDto[]>
getDevicesByLocation(locationId: string): Observable<BiometricDeviceDto[]>
getDevicesByType(deviceType: string): Observable<BiometricDeviceDto[]>
activateDevice(id: string): Observable<void>
deactivateDevice(id: string): Observable<void>
enableSync(id: string): Observable<void>
disableSync(id: string): Observable<void>
getDeviceStatistics(id: string): Observable<{ totalAttendanceRecords, authorizedEmployeeCount, syncCount, lastSyncTime, averageSyncDuration }>
```

### Employee Authorization Methods
```typescript
getAuthorizedEmployees(deviceId: string): Observable<any[]>
authorizeEmployee(deviceId: string, employeeId: string): Observable<void>
revokeEmployeeAuthorization(deviceId: string, employeeId: string): Observable<void>
```

### Diagnostics & Maintenance
```typescript
getSyncLogs(deviceId: string, limit: number = 50): Observable<any[]>
getAnomalies(deviceId: string, limit: number = 50): Observable<any[]>
updateDeviceConfig(deviceId: string, config: any): Observable<void>
restartDevice(deviceId: string): Observable<void>
clearDeviceData(deviceId: string): Observable<void>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/biometric-devices` | Get all devices |
| GET | `/api/biometric-devices/{id}` | Get device by ID |
| POST | `/api/biometric-devices` | Create device |
| PUT | `/api/biometric-devices/{id}` | Update device |
| DELETE | `/api/biometric-devices/{id}` | Delete device |
| POST | `/api/biometric-devices/test-connection` | Test connection |
| POST | `/api/biometric-devices/{id}/sync` | Sync device |
| GET | `/api/biometric-devices/{id}/sync-status` | Get sync status |
| GET | `/api/biometric-devices/sync-statuses` | Get all sync statuses |
| GET | `/api/biometric-devices/by-location/{id}` | Get devices by location |
| GET | `/api/biometric-devices/by-type/{type}` | Get devices by type |
| PATCH | `/api/biometric-devices/{id}/activate` | Activate device |
| PATCH | `/api/biometric-devices/{id}/deactivate` | Deactivate device |
| PATCH | `/api/biometric-devices/{id}/enable-sync` | Enable sync |
| PATCH | `/api/biometric-devices/{id}/disable-sync` | Disable sync |
| GET | `/api/biometric-devices/{id}/statistics` | Get statistics |
| GET | `/api/biometric-devices/{id}/authorized-employees` | Get authorized employees |
| POST | `/api/biometric-devices/{id}/authorize-employee/{empId}` | Authorize employee |
| DELETE | `/api/biometric-devices/{id}/revoke-employee/{empId}` | Revoke authorization |
| GET | `/api/biometric-devices/{id}/sync-logs` | Get sync logs |
| GET | `/api/biometric-devices/{id}/anomalies` | Get anomalies |
| PATCH | `/api/biometric-devices/{id}/config` | Update config |
| POST | `/api/biometric-devices/{id}/restart` | Restart device |
| POST | `/api/biometric-devices/{id}/clear-data` | Clear device data |

---

## 13. DASHBOARD SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/dashboard.service.ts`

### Purpose
Provides comprehensive dashboard statistics, alerts, and activity feed.

### Key Interfaces
```typescript
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

export interface ActivityItem {
  id: string;
  type: string;
  icon: string;
  title: string;
  description: string;
  timestamp: Date;
  relatedId: string;
}

export interface BirthdayItem {
  employeeId: string;
  employeeName: string;
  department: string;
  birthdayDate: Date;
  daysUntil: number;
}

export interface ChartDataPoint {
  label: string;
  value: number;
}
```

### API Methods
```typescript
getStats(): Observable<DashboardStats>
getDepartments(): Observable<string[]>
getAlerts(): Observable<AlertItem[]>
getRecentActivity(limit: number = 10): Observable<ActivityItem[]>
refresh(): Observable<DashboardStats>
getDepartmentHeadcountChart(): Observable<ChartDataPoint[]>
getEmployeeGrowthChart(): Observable<ChartDataPoint[]>
getEmployeeTypeDistribution(): Observable<ChartDataPoint[]>
getUpcomingBirthdays(days: number = 7): Observable<BirthdayItem[]>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/dashboard/stats` | Get dashboard statistics |
| GET | `/dashboard/departments` | Get all departments |
| GET | `/dashboard/alerts` | Get urgent alerts |
| GET | `/dashboard/recent-activity` | Get activity feed |
| GET | `/dashboard/charts/department-headcount` | Department headcount chart |
| GET | `/dashboard/charts/employee-growth` | Employee growth chart |
| GET | `/dashboard/charts/employee-type-distribution` | Employee type distribution |
| GET | `/dashboard/upcoming-birthdays` | Get upcoming birthdays |

### Reactive State (Signals)
- `stats`: DashboardStats | null - Dashboard statistics
- `loading`: boolean - Loading state
- `error`: string | null - Error messages

---

## 14. AUTH SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/auth.service.ts`

### Purpose
Handles user authentication and authorization with role-based access control.

### Key Signals
```typescript
readonly user: Signal<User | null>
readonly token: Signal<string | null>
readonly loading: Signal<boolean>
readonly isAuthenticated: Signal<boolean>
readonly isSuperAdmin: Signal<boolean>
readonly isTenantAdmin: Signal<boolean>
readonly isHR: Signal<boolean>
readonly isManager: Signal<boolean>
readonly isEmployee: Signal<boolean>
```

### Key Methods
```typescript
login(credentials: LoginRequest): Observable<LoginResponse>
// Supports both SuperAdmin and Tenant employee login
// Stores token and user info in localStorage and signals
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/auth/login` | SuperAdmin login |
| POST | `/auth/tenant/login` | Tenant employee login |

---

## 15. TENANT SERVICE

### File Location
`/workspaces/HRAPP/hrms-frontend/src/app/core/services/tenant.service.ts`

### Purpose
Manages tenant organizations (for SuperAdmin).

### API Methods
```typescript
getTenants(): Observable<Tenant[]>
getTenantById(id: string): Observable<Tenant>
createTenant(request: CreateTenantRequest): Observable<Tenant>
updateTenant(id: string, tenant: Partial<Tenant>): Observable<Tenant>
suspendTenant(id: string): Observable<void>
deleteTenant(id: string): Observable<void>
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/tenants` | Get all tenants |
| GET | `/tenants/{id}` | Get tenant by ID |
| POST | `/tenants` | Create tenant |
| PUT | `/tenants/{id}` | Update tenant |
| PATCH | `/tenants/{id}/suspend` | Suspend tenant |
| DELETE | `/tenants/{id}` | Delete tenant |

---

## Service Integration Architecture

### Service Dependencies
```
EmployeeService
├── Uses: Employee, CreateEmployeeRequest models
├── Depends on: HttpClient, environment
└── State: Signals (employees, loading, error)

EmployeeDraftService
├── Uses: EmployeeDraft models
├── LocalStorage: employee_draft_* keys
├── Endpoints: Draft management, localStorage helpers
└── State: None (manages data manually)

AddressService
├── Uses: DistrictDto, VillageDto, PostalCodeDto
├── Endpoints: Cascading lookups
└── Features: Postal code autocomplete

PayrollService
├── Uses: Payroll, Payslip, PayrollResult models
├── Depends on: Timesheet data
├── Features: Mauritius statutory deductions
└── State: Signals (payrolls, cycles, loading, result)

TimesheetService
├── Manages: Timesheet lifecycle, approvals, adjustments
├── Uses: Timesheet, TimesheetEntry models
├── Features: Period calculations, bulk operations
└── State: Signals (timesheets, pending, stats, loading)

DepartmentService
├── Uses: DepartmentDto models
├── Features: Hierarchy tree, dropdowns
└── Endpoints: CRUD operations

LocationService
├── Uses: LocationDto models
├── Features: Summaries, statistics, cascading
└── Endpoints: CRUD operations

BiometricDeviceService
├── Uses: BiometricDeviceDto models
├── Features: Sync management, employee authorization
├── Endpoints: 24 different device operations
└── Data: Sync logs, anomalies, diagnostics

DashboardService
├── Uses: DashboardStats, AlertItem, ChartDataPoint
├── Features: Charts, alerts, activity feed
└── State: Signals (stats, loading, error)

AuthService
├── Uses: User, LoginRequest models
├── Features: Role-based access control (5 roles)
└── State: Signals (user, token, loading, computed role checks)

TenantService
├── Uses: Tenant models
└── Endpoints: Tenant CRUD operations
```

---

## API Response Format

### Standard API Response Structure
```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  count?: number;
}
```

### Error Handling Pattern
All services follow consistent error handling:
1. Map response to extract data
2. Validate success flag
3. Throw Error if !success
4. CatchError to return throwError
5. Update signals with error message

---

## Key Design Patterns

### 1. Signal-based Reactive State
- Uses Angular Signals for fine-grained reactivity
- Readonly public signals for component access
- Private signals with update operations

### 2. RxJS Operators
- `map`: Transform API response to domain model
- `tap`: Update signals without transforming
- `catchError`: Handle and propagate errors
- `throwError`: Re-throw for component handling

### 3. LocalStorage Caching
- Used in EmployeeDraftService for offline support
- Prefixed keys: `employee_draft_*`
- Manual serialization/deserialization

### 4. API Consistency
- Base URL: `environment.apiUrl`
- RESTful endpoints with resource names
- Standard HTTP methods (GET, POST, PUT, DELETE, PATCH)

### 5. DTO Pattern
- Separate create/update DTOs from response models
- Dropdown and summary variants for different views
- Type-safe interface contracts

---

## Configuration

### Environment Variables
All services use centralized configuration:
```typescript
environment.apiUrl  // Base API URL from environment.ts
```

### Standard Endpoints
- Employee: `/employees`
- Drafts: `/employee-drafts`
- Address: `/address-lookup`
- Departments: `/api/department`
- Locations: `/api/locations`
- Biometric: `/api/biometric-devices`
- Payroll: `/payroll`
- Timesheet: `/timesheet`
- Dashboard: `/dashboard`
- Auth: `/auth`
- Tenants: `/tenants`

---

## Testing Recommendations

### Service Test Patterns
1. Mock HttpClient
2. Test signal updates on success/error
3. Verify API endpoint URLs
4. Test error handling and throwError paths
5. Verify LocalStorage operations (for draft service)

### Component Integration
1. Inject services via dependency injection
2. Subscribe to signal changes using `toSignal()` or direct access
3. Use `async` pipe in templates for Observable subscriptions
4. Handle loading and error states from signals

---

## Summary Statistics

- **Total Services**: 8 core + 5 feature services = 13 services
- **Total Models/DTOs**: 40+ interfaces
- **Total API Endpoints**: 100+ endpoints
- **Reactive State Management**: 40+ signals across services
- **Enum Types**: 10+ enums for statuses, types, periods
- **DTO Variants**: 30+ specialized DTOs (create, update, dropdown, summary)

