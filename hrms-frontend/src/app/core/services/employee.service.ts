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
