import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminUser {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  isTwoFactorEnabled: boolean;
  lastLoginDate?: string;
  lastLoginIPAddress?: string;
  sessionTimeoutMinutes: number;
  permissions: string[];
  createdAt: string;
  updatedAt?: string;
  isLocked: boolean;
  lockoutEnd?: string;
  accessFailedCount: number;
  mustChangePassword: boolean;
  lastPasswordChangeDate?: string;
  passwordExpiresAt?: string;
  isInitialSetupAccount: boolean;
  statusNotes?: string;
}

export interface CreateAdminUserRequest {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  permissions?: string[];
  sessionTimeoutMinutes?: number;
}

export interface UpdateAdminUserRequest {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  isActive?: boolean;
  permissions?: string[];
  sessionTimeoutMinutes?: number;
}

export interface ChangePasswordRequest {
  newPassword: string;
}

export interface UpdatePermissionsRequest {
  permissions: string[];
}

export interface AdminUserListResponse {
  success: boolean;
  data: AdminUser[];
  pagination: {
    pageIndex: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface AdminUserResponse {
  success: boolean;
  data: AdminUser;
  message?: string;
}

export interface AdminUserActionResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminUserService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/admin-users`;

  /**
   * Get all SuperAdmin users with pagination and filtering
   */
  getAll(
    pageIndex: number = 0,
    pageSize: number = 20,
    searchTerm?: string,
    isActive?: boolean
  ): Observable<AdminUserListResponse> {
    let params = new HttpParams()
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    if (isActive !== undefined) {
      params = params.set('isActive', isActive.toString());
    }

    return this.http.get<AdminUserListResponse>(this.apiUrl, { params });
  }

  /**
   * Get SuperAdmin user by ID
   */
  getById(id: string): Observable<AdminUserResponse> {
    return this.http.get<AdminUserResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create a new SuperAdmin user
   */
  create(request: CreateAdminUserRequest): Observable<AdminUserActionResponse> {
    return this.http.post<AdminUserActionResponse>(this.apiUrl, request);
  }

  /**
   * Update SuperAdmin user
   */
  update(id: string, request: UpdateAdminUserRequest): Observable<AdminUserActionResponse> {
    return this.http.put<AdminUserActionResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Change SuperAdmin password
   */
  changePassword(id: string, request: ChangePasswordRequest): Observable<AdminUserActionResponse> {
    return this.http.post<AdminUserActionResponse>(`${this.apiUrl}/${id}/change-password`, request);
  }

  /**
   * Delete SuperAdmin user (soft delete)
   */
  delete(id: string): Observable<AdminUserActionResponse> {
    return this.http.delete<AdminUserActionResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Update SuperAdmin permissions
   */
  updatePermissions(id: string, request: UpdatePermissionsRequest): Observable<AdminUserActionResponse> {
    return this.http.put<AdminUserActionResponse>(`${this.apiUrl}/${id}/permissions`, request);
  }

  /**
   * Unlock SuperAdmin account
   */
  unlockAccount(id: string): Observable<AdminUserActionResponse> {
    return this.http.post<AdminUserActionResponse>(`${this.apiUrl}/${id}/unlock`, {});
  }

  /**
   * Get SuperAdmin activity logs
   */
  getActivityLogs(id: string, pageIndex: number = 0, pageSize: number = 50): Observable<any> {
    const params = new HttpParams()
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<any>(`${this.apiUrl}/${id}/activity`, { params });
  }
}
