import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Employee, CreateEmployeeRequest } from '../models/employee.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/employees`;

  // Signals for reactive state
  private employeesSignal = signal<Employee[]>([]);
  private loadingSignal = signal<boolean>(false);

  readonly employees = this.employeesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  getEmployees(): Observable<Employee[]> {
    this.loadingSignal.set(true);
    return this.http.get<Employee[]>(this.apiUrl).pipe(
      tap(employees => {
        this.employeesSignal.set(employees);
        this.loadingSignal.set(false);
      })
    );
  }

  getEmployeeById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/${id}`);
  }

  createEmployee(request: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(this.apiUrl, request).pipe(
      tap(employee => {
        this.employeesSignal.update(employees => [...employees, employee]);
      })
    );
  }

  updateEmployee(id: string, employee: Partial<Employee>): Observable<Employee> {
    return this.http.put<Employee>(`${this.apiUrl}/${id}`, employee).pipe(
      tap(updatedEmployee => {
        this.employeesSignal.update(employees =>
          employees.map(e => e.id === id ? updatedEmployee : e)
        );
      })
    );
  }

  deleteEmployee(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this.employeesSignal.update(employees => employees.filter(e => e.id !== id));
      })
    );
  }
}
