import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AttendanceMachineDto {
  id: string;
  machineName: string;
  machineId: string;              // Unique machine identifier
  ipAddress?: string;
  location?: string;              // String location, not locationId
  departmentId?: string;
  departmentName?: string;
  isActive: boolean;
  serialNumber?: string;
  model?: string;
  zkTecoDeviceId?: string;
  port?: number;
  lastSyncAt?: string;            // Changed from lastSyncTime to match backend
  createdAt: string;
  updatedAt?: string;
}

export interface CreateAttendanceMachineDto {
  machineName: string;          // Required
  machineId: string;             // Required - unique identifier for the machine
  ipAddress?: string;
  location?: string;             // String location name, not locationId (Guid)
  departmentId?: string;         // Guid as string
  serialNumber?: string;
  model?: string;
  zkTecoDeviceId?: string;
  port?: number;
  isActive?: boolean;
}

export interface UpdateAttendanceMachineDto {
  machineName: string;           // Required
  ipAddress?: string;
  location?: string;             // String location name, not locationId (Guid)
  departmentId?: string;         // Guid as string
  model?: string;
  port?: number;
  isActive: boolean;             // Required
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceMachinesService {
  private apiUrl = `${environment.apiUrl}/attendance-machines`;

  constructor(private http: HttpClient) {}

  /**
   * Create new attendance machine/device
   * POST /api/attendance-machines
   *
   * Requires: HR or Admin role
   */
  createMachine(dto: CreateAttendanceMachineDto): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(this.apiUrl, dto);
  }

  /**
   * Get all machines (active only by default)
   * GET /api/attendance-machines
   *
   * Requires: HR, Manager, or Admin role
   *
   * @param activeOnly Filter to show only active machines (default: true)
   */
  getMachines(activeOnly: boolean = true): Observable<{ total: number; data: AttendanceMachineDto[] }> {
    console.log('🔍 AttendanceMachinesService.getMachines() called with activeOnly:', activeOnly);
    console.log('🔍 API URL:', this.apiUrl);
    const params = new HttpParams().set('activeOnly', activeOnly.toString());
    console.log('🔍 Request params:', params.toString());

    return this.http.get<{ total: number; data: AttendanceMachineDto[] }>(this.apiUrl, { params });
  }

  /**
   * Get machine by ID
   * GET /api/attendance-machines/{id}
   *
   * Requires: HR, Manager, or Admin role
   */
  getMachineById(id: string): Observable<AttendanceMachineDto> {
    return this.http.get<AttendanceMachineDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Update machine details
   * PUT /api/attendance-machines/{id}
   *
   * Requires: HR or Admin role
   */
  updateMachine(id: string, dto: UpdateAttendanceMachineDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Delete machine (soft delete)
   * DELETE /api/attendance-machines/{id}
   *
   * Requires: HR or Admin role
   */
  deleteMachine(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}
