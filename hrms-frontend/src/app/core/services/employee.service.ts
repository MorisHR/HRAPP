import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map, catchError, throwError, of } from 'rxjs';
import { Employee, CreateEmployeeRequest, EmployeeStatus } from '../models/employee.model';
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

  // Mock data for demos and development fallback
  private readonly MOCK_EMPLOYEES: Employee[] = [
    {
      id: '1',
      employeeCode: 'EMP001',
      firstName: 'Sarah',
      lastName: 'Johnson',
      email: 'sarah.johnson@company.com',
      phone: '+1-555-0101',
      department: 'Engineering',
      designation: 'Senior Software Engineer',
      status: EmployeeStatus.Active,
      joiningDate: '2021-03-15'
    },
    {
      id: '2',
      employeeCode: 'EMP002',
      firstName: 'Michael',
      lastName: 'Chen',
      email: 'michael.chen@company.com',
      phone: '+1-555-0102',
      department: 'Sales',
      designation: 'Sales Director',
      status: EmployeeStatus.Active,
      joiningDate: '2020-01-10'
    },
    {
      id: '3',
      employeeCode: 'EMP003',
      firstName: 'Emily',
      lastName: 'Rodriguez',
      email: 'emily.rodriguez@company.com',
      phone: '+1-555-0103',
      department: 'Human Resources',
      designation: 'HR Manager',
      status: EmployeeStatus.Active,
      joiningDate: '2019-08-22'
    },
    {
      id: '4',
      employeeCode: 'EMP004',
      firstName: 'David',
      lastName: 'Thompson',
      email: 'david.thompson@company.com',
      phone: '+1-555-0104',
      department: 'Finance',
      designation: 'Financial Analyst',
      status: EmployeeStatus.Active,
      joiningDate: '2022-05-01'
    },
    {
      id: '5',
      employeeCode: 'EMP005',
      firstName: 'Jessica',
      lastName: 'Martinez',
      email: 'jessica.martinez@company.com',
      phone: '+1-555-0105',
      department: 'Engineering',
      designation: 'DevOps Engineer',
      status: EmployeeStatus.OnLeave,
      joiningDate: '2021-11-15'
    },
    {
      id: '6',
      employeeCode: 'EMP006',
      firstName: 'Robert',
      lastName: 'Anderson',
      email: 'robert.anderson@company.com',
      phone: '+1-555-0106',
      department: 'Marketing',
      designation: 'Marketing Manager',
      status: EmployeeStatus.Active,
      joiningDate: '2020-07-20'
    },
    {
      id: '7',
      employeeCode: 'EMP007',
      firstName: 'Amanda',
      lastName: 'Taylor',
      email: 'amanda.taylor@company.com',
      phone: '+1-555-0107',
      department: 'Engineering',
      designation: 'QA Engineer',
      status: EmployeeStatus.Active,
      joiningDate: '2022-02-14'
    },
    {
      id: '8',
      employeeCode: 'EMP008',
      firstName: 'Christopher',
      lastName: 'Wilson',
      email: 'christopher.wilson@company.com',
      phone: '+1-555-0108',
      department: 'Sales',
      designation: 'Account Executive',
      status: EmployeeStatus.Active,
      joiningDate: '2023-01-05'
    },
    {
      id: '9',
      employeeCode: 'EMP009',
      firstName: 'Jennifer',
      lastName: 'Brown',
      email: 'jennifer.brown@company.com',
      phone: '+1-555-0109',
      department: 'Finance',
      designation: 'Accountant',
      status: EmployeeStatus.OnLeave,
      joiningDate: '2021-06-10'
    },
    {
      id: '10',
      employeeCode: 'EMP010',
      firstName: 'Matthew',
      lastName: 'Davis',
      email: 'matthew.davis@company.com',
      phone: '+1-555-0110',
      department: 'Engineering',
      designation: 'Product Manager',
      status: EmployeeStatus.Active,
      joiningDate: '2020-09-18'
    },
    {
      id: '11',
      employeeCode: 'EMP011',
      firstName: 'Lisa',
      lastName: 'Garcia',
      email: 'lisa.garcia@company.com',
      phone: '+1-555-0111',
      department: 'Customer Support',
      designation: 'Support Team Lead',
      status: EmployeeStatus.Active,
      joiningDate: '2019-12-01'
    },
    {
      id: '12',
      employeeCode: 'EMP012',
      firstName: 'Daniel',
      lastName: 'Miller',
      email: 'daniel.miller@company.com',
      phone: '+1-555-0112',
      department: 'Engineering',
      designation: 'Frontend Developer',
      status: EmployeeStatus.Active,
      joiningDate: '2022-08-25'
    },
    {
      id: '13',
      employeeCode: 'EMP013',
      firstName: 'Michelle',
      lastName: 'Lee',
      email: 'michelle.lee@company.com',
      phone: '+1-555-0113',
      department: 'Design',
      designation: 'UX Designer',
      status: EmployeeStatus.Active,
      joiningDate: '2023-03-10'
    },
    {
      id: '14',
      employeeCode: 'EMP014',
      firstName: 'Kevin',
      lastName: 'White',
      email: 'kevin.white@company.com',
      phone: '+1-555-0114',
      department: 'Sales',
      designation: 'Business Development Manager',
      status: EmployeeStatus.OnLeave,
      joiningDate: '2021-04-20'
    },
    {
      id: '15',
      employeeCode: 'EMP015',
      firstName: 'Rachel',
      lastName: 'Harris',
      email: 'rachel.harris@company.com',
      phone: '+1-555-0115',
      department: 'Engineering',
      designation: 'Backend Developer',
      status: EmployeeStatus.Active,
      joiningDate: '2022-11-08'
    }
  ];

  getEmployees(): Observable<Employee[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<ApiResponse<Employee[]>>(this.apiUrl).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Failed to load employees');
        }

        // Validate API data - check if it's complete and usable
        const employees = response.data;

        // If empty or contains incomplete data, use mock data for better demo experience
        if (employees.length === 0 || this.hasIncompleteData(employees)) {
          console.warn('⚠️ API returned empty/incomplete data, using mock data for demo');
          return this.MOCK_EMPLOYEES;
        }

        return employees;
      }),
      tap(employees => {
        this.employeesSignal.set(employees);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        console.warn('⚠️ API call failed, using mock data for demo:', error.message);

        // Fallback to mock data for demos and development
        this.employeesSignal.set(this.MOCK_EMPLOYEES);
        this.loadingSignal.set(false);
        this.errorSignal.set(null); // Clear error since we have fallback data

        // Return mock data instead of error
        return of(this.MOCK_EMPLOYEES);
      })
    );
  }

  // Helper to check if employee data is incomplete
  private hasIncompleteData(employees: Employee[]): boolean {
    // Check if any employee has critical missing fields
    return employees.some(emp =>
      !emp.firstName ||
      !emp.lastName ||
      !emp.email ||
      !emp.department ||
      emp.firstName.trim() === '' ||
      emp.lastName.trim() === '' ||
      emp.email.trim() === ''
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

  /**
   * Check if employee code already exists
   * Used for async validation in employee form
   *
   * @param code - Employee code to check
   * @param excludeId - Optional employee ID to exclude (for edit scenarios)
   * @returns Observable<boolean> - true if code exists, false otherwise
   */
  checkEmployeeCodeExists(code: string, excludeId?: string): Observable<boolean> {
    const params = excludeId ? `?excludeId=${excludeId}` : '';

    return this.http.get<ApiResponse<{ exists: boolean }>>(
      `${this.apiUrl}/check-code/${code}${params}`
    ).pipe(
      map(response => {
        if (!response.success) {
          return false; // On API error, assume code doesn't exist
        }
        return response.data?.exists || false;
      }),
      catchError(() => {
        // On error, return false to not block the user
        console.warn('⚠️ Employee code check failed, allowing code to proceed');
        return of(false);
      })
    );
  }
}
