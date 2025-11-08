import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../../environments/environment';

// Department DTOs matching backend
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

export interface CreateDepartmentDto {
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  departmentHeadId?: string;
  costCenterCode?: string;
  isActive: boolean;
}

export interface UpdateDepartmentDto {
  name: string;
  code: string;
  description?: string;
  parentDepartmentId?: string;
  departmentHeadId?: string;
  costCenterCode?: string;
  isActive: boolean;
}

export interface DepartmentDropdownDto {
  id: string;
  name: string;
  code: string;
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

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/department`;

  /**
   * Get all departments
   */
  getAll(): Observable<DepartmentDto[]> {
    return this.http.get<{ success: boolean; data: DepartmentDto[] }>(this.apiUrl)
      .pipe(map(response => response.data));
  }

  /**
   * Get a single department by ID
   */
  getById(id: string): Observable<DepartmentDto> {
    return this.http.get<{ success: boolean; data: DepartmentDto }>(`${this.apiUrl}/${id}`)
      .pipe(map(response => response.data));
  }

  /**
   * Create a new department
   */
  create(department: CreateDepartmentDto): Observable<any> {
    return this.http.post(this.apiUrl, department);
  }

  /**
   * Update an existing department
   */
  update(id: string, department: UpdateDepartmentDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, department);
  }

  /**
   * Delete a department
   */
  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  /**
   * Get department hierarchy tree
   */
  getHierarchy(): Observable<DepartmentHierarchyDto[]> {
    return this.http.get<{ success: boolean; data: DepartmentHierarchyDto[] }>(`${this.apiUrl}/hierarchy`)
      .pipe(map(response => response.data));
  }

  /**
   * Get simple dropdown list
   */
  getDropdown(): Observable<DepartmentDropdownDto[]> {
    return this.http.get<{ success: boolean; data: DepartmentDropdownDto[] }>(`${this.apiUrl}/dropdown`)
      .pipe(map(response => response.data));
  }
}
