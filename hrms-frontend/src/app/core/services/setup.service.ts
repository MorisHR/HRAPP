import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateFirstAdminResponse {
  success: boolean;
  message: string;
  data: {
    email: string;
    temporaryPassword: string;
    firstName: string;
    lastName: string;
    isActive: boolean;
    mustChangePassword: boolean;
    sessionTimeoutMinutes: number;
    passwordExpiresInDays: number;
    securityLevel: string;
    warning: string;
    instructions: string[];
  };
}

export interface SetupStatusResponse {
  success: boolean;
  data: {
    isSetupComplete: boolean;
    adminUserCount: number;
    message: string;
  };
}

export interface ResetSystemResponse {
  success: boolean;
  message: string;
  deletedCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  private apiUrl = `${environment.apiUrl}/admin/Setup`;

  constructor(private http: HttpClient) {}

  /**
   * Create the first admin user for system bootstrap
   * POST /api/admin/setup/create-first-admin
   *
   * IMPORTANT: This endpoint creates a SuperAdmin with a cryptographically secure password
   * The password is shown ONLY ONCE and must be saved immediately
   * The admin MUST change the password on first login
   */
  createFirstAdmin(): Observable<CreateFirstAdminResponse> {
    return this.http.post<CreateFirstAdminResponse>(`${this.apiUrl}/create-first-admin`, {});
  }

  /**
   * Check system setup status
   * GET /api/admin/setup/status
   *
   * Returns whether the system has been initialized with admin users
   */
  getSetupStatus(): Observable<SetupStatusResponse> {
    return this.http.get<SetupStatusResponse>(`${this.apiUrl}/status`);
  }

  /**
   * Reset system (DANGER: Deletes all admin users)
   * DELETE /api/admin/setup/reset
   *
   * SECURITY WARNING:
   * - Requires SuperAdmin role
   * - Only works in Development environment
   * - Permanently deletes ALL admin users
   * - Use with extreme caution
   */
  resetSystem(): Observable<ResetSystemResponse> {
    return this.http.delete<ResetSystemResponse>(`${this.apiUrl}/reset`);
  }
}
