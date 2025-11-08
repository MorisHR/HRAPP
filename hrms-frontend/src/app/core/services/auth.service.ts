import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, map, catchError, throwError } from 'rxjs';
import { User, LoginRequest, LoginResponse, UserRole } from '../models/user.model';
import { SubdomainService } from './subdomain.service';
import { SessionManagementService } from './session-management.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private sessionManagement = inject(SessionManagementService);

  // API URL from environment configuration
  private apiUrl = environment.apiUrl;

  // Signals for reactive state management (Angular 20)
  private userSignal = signal<User | null>(null);
  private tokenSignal = signal<string | null>(null);
  private loadingSignal = signal<boolean>(false);

  // Computed signals
  readonly user = this.userSignal.asReadonly();
  readonly token = this.tokenSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.userSignal() && !!this.tokenSignal());
  readonly isSuperAdmin = computed(() => this.userSignal()?.role === UserRole.SuperAdmin);
  readonly isTenantAdmin = computed(() => this.userSignal()?.role === UserRole.TenantAdmin);
  readonly isHR = computed(() => this.userSignal()?.role === UserRole.HR);
  readonly isManager = computed(() => this.userSignal()?.role === UserRole.Manager);
  readonly isEmployee = computed(() => this.userSignal()?.role === UserRole.Employee);

  constructor() {
    this.loadAuthState();

    // Subscribe to session timeout logout events
    this.sessionManagement.onLogoutRequested.subscribe(() => {
      console.log('🔔 Session timeout - logging out user');
      this.logout();
    });
  }

  private loadAuthState(): void {
    const token = localStorage.getItem('access_token');
    const userJson = localStorage.getItem('user');

    if (token && userJson) {
      try {
        const user = JSON.parse(userJson) as User;
        this.tokenSignal.set(token);
        this.userSignal.set(user);
      } catch (error) {
        console.error('Error loading auth state:', error);
        this.clearAuthState();
      }
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    this.loadingSignal.set(true);

    // ✅ Subdomain is now extracted from URL by SubdomainService
    // No need to store in localStorage - proper multi-tenant architecture

    // Determine endpoint based on user type
    const endpoint = credentials.subdomain
      ? `${this.apiUrl}/auth/tenant/login`
      : `${this.apiUrl}/auth/login`;

    return this.http.post<any>(endpoint, credentials).pipe(
      map(apiResponse => {
        // Backend returns: { success, data: { token, expiresAt, adminUser/employee }, message }
        // Transform to frontend format: { token, refreshToken, user, expiresIn }
        if (!apiResponse.success || !apiResponse.data) {
          throw new Error(apiResponse.message || 'Login failed');
        }

        const backendData = apiResponse.data;
        let user: User;

        // Handle tenant employee login response
        if (backendData.employee) {
          const employee = backendData.employee;
          const nameParts = employee.fullName?.split(' ') || ['Employee'];
          user = {
            id: employee.id,
            email: employee.email,
            firstName: nameParts[0] || 'Employee',
            lastName: nameParts.slice(1).join(' ') || '',
            role: UserRole.TenantAdmin, // Tenant employees are TenantAdmins
            avatarUrl: undefined
          };
        }
        // Handle SuperAdmin login response
        else if (backendData.adminUser) {
          const adminUser = backendData.adminUser;
          user = {
            id: adminUser.id,
            email: adminUser.email,
            firstName: adminUser.userName?.split(' ')[0] || 'Admin',
            lastName: adminUser.userName?.split(' ').slice(1).join(' ') || 'User',
            role: UserRole.SuperAdmin,
            avatarUrl: undefined
          };
        } else {
          throw new Error('Invalid login response format');
        }

        const response: LoginResponse = {
          token: backendData.token,
          refreshToken: backendData.token, // Backend doesn't return separate refresh token yet
          user: user,
          expiresIn: 480 * 60 // 480 minutes in seconds (from backend config)
        };

        return response;
      }),
      tap(response => {
        this.setAuthState(response);

        // Start session management after successful login
        this.sessionManagement.startSession();
        console.log('✅ Session management started after login');

        this.navigateBasedOnRole(response.user.role);
        this.loadingSignal.set(false);
      }),
      catchError(error => {
        this.loadingSignal.set(false);
        console.error('Login error:', error);
        return throwError(() => new Error(error.error?.message || 'Login failed'));
      })
    );
  }

  /**
   * Logout user and revoke refresh token
   * UPDATED: Now calls backend to revoke refresh token and navigates based on user type
   */
  logout(): void {
    console.log('🚪 LOGOUT: Starting logout process');

    // Get user type before clearing state
    const user = this.userSignal();
    const isSuperAdmin = user?.role === UserRole.SuperAdmin;

    console.log('👤 LOGOUT: User role:', user?.role, '| SuperAdmin:', isSuperAdmin);

    // Save user role for post-logout redirect (BEFORE clearing state)
    if (user?.role) {
      this.saveLastUserRole(user.role);
    }

    // Stop session management
    this.sessionManagement.stopSession();
    console.log('⏹️ Session management stopped');

    // Call backend to revoke refresh token
    // Don't wait for response - logout immediately for better UX
    this.http.post(
      `${this.apiUrl}/auth/revoke`,
      {},
      { withCredentials: true } // Send refresh token cookie
    ).subscribe({
      next: () => console.log('✅ Refresh token revoked successfully'),
      error: (err) => console.warn('⚠️ Token revocation failed (user logged out anyway):', err)
    });

    // Clear local auth state
    this.clearAuthState();

    // Navigate based on user type using replaceUrl to prevent back button issues
    if (isSuperAdmin) {
      console.log('🔄 LOGOUT: Redirecting SuperAdmin to /auth/superadmin');
      this.router.navigate(['/auth/superadmin'], { replaceUrl: true });
    } else {
      // Tenant user - redirect to subdomain entry page
      console.log('🔄 LOGOUT: Redirecting tenant user to /auth/subdomain');
      const subdomainService = inject(SubdomainService);
      subdomainService.redirectToMainDomain('/auth/subdomain');
    }
  }

  // ============================================
  // PRODUCTION-GRADE TOKEN REFRESH
  // ============================================

  /**
   * Refreshes access token using refresh token from HttpOnly cookie
   * The refresh token is NOT sent manually - browser sends it automatically via cookie
   * Backend implements token rotation: old token revoked, new one issued
   */
  refreshToken(): Observable<LoginResponse> {
    return this.http.post<any>(
      `${this.apiUrl}/auth/refresh`,
      {}, // Empty body - refresh token comes from HttpOnly cookie
      { withCredentials: true } // CRITICAL: Sends HttpOnly cookies
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'Token refresh failed');
        }

        // Transform backend response to frontend LoginResponse format
        const loginResponse: LoginResponse = {
          token: response.data.token,
          refreshToken: response.data.refreshToken,
          user: this.userSignal()!, // User info stays the same
          expiresIn: 15 * 60 // 15 minutes in seconds
        };

        return loginResponse;
      }),
      tap(response => this.setAuthState(response))
    );
  }

  // ============================================
  // MULTI-FACTOR AUTHENTICATION (MFA) METHODS
  // ============================================

  /**
   * SuperAdmin login using secret URL
   * Returns MFA setup requirement or verification requirement
   */
  superAdminSecretLogin(credentials: { email: string; password: string }): Observable<any> {
    const secretPath = environment.superAdminSecretPath;

    return this.http.post<any>(
      `${this.apiUrl}/auth/${secretPath}`,
      credentials,
      { withCredentials: true }
    ).pipe(
      map(response => {
        if (!response.success) {
          throw new Error(response.message || 'Login failed');
        }
        return response.data;
      })
    );
  }

  /**
   * Complete MFA setup after scanning QR code
   * Verifies TOTP code and enables MFA
   */
  completeMfaSetup(request: {
    userId: string;
    totpCode: string;
    secret: string;
    backupCodes: string[];
  }): Observable<LoginResponse> {
    console.log('🔵 [AUTH SERVICE] completeMfaSetup() called');
    console.log('📋 [AUTH SERVICE] API URL:', `${this.apiUrl}/auth/mfa/complete-setup`);
    console.log('📋 [AUTH SERVICE] Request payload:', {
      userId: request.userId,
      totpCode: request.totpCode,
      secret: request.secret,
      backupCodesCount: request.backupCodes.length
    });

    return this.http.post<any>(
      `${this.apiUrl}/auth/mfa/complete-setup`,
      request,
      { withCredentials: true }
    ).pipe(
      map(response => {
        console.log('✅ [AUTH SERVICE] Received response:', response);

        if (!response.success || !response.data) {
          throw new Error(response.message || 'MFA setup failed');
        }

        const backendData = response.data;
        const adminUser = backendData.adminUser;

        const user: User = {
          id: adminUser.id,
          email: adminUser.email,
          firstName: adminUser.userName?.split(' ')[0] || 'Admin',
          lastName: adminUser.userName?.split(' ').slice(1).join(' ') || 'User',
          role: UserRole.SuperAdmin,
          avatarUrl: undefined
        };

        const loginResponse: LoginResponse = {
          token: backendData.token,
          refreshToken: backendData.refreshToken,
          user: user,
          expiresIn: 15 * 60 // 15 minutes
        };

        console.log('✅ [AUTH SERVICE] Login response created:', loginResponse);
        return loginResponse;
      }),
      tap(response => {
        console.log('✅ [AUTH SERVICE] Setting auth state and navigating...');
        this.setAuthState(response);
        this.navigateBasedOnRole(response.user.role);
      })
    );
  }

  /**
   * Verify MFA code (TOTP or backup code) during login
   */
  verifyMfa(request: { userId: string; code: string }): Observable<LoginResponse> {
    return this.http.post<any>(
      `${this.apiUrl}/auth/mfa/verify`,
      request,
      { withCredentials: true }
    ).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || 'MFA verification failed');
        }

        const backendData = response.data;
        const adminUser = backendData.adminUser;

        const user: User = {
          id: adminUser.id,
          email: adminUser.email,
          firstName: adminUser.userName?.split(' ')[0] || 'Admin',
          lastName: adminUser.userName?.split(' ').slice(1).join(' ') || 'User',
          role: UserRole.SuperAdmin,
          avatarUrl: undefined
        };

        const loginResponse: LoginResponse = {
          token: backendData.token,
          refreshToken: backendData.refreshToken,
          user: user,
          expiresIn: 15 * 60 // 15 minutes
        };

        return loginResponse;
      }),
      tap(response => {
        this.setAuthState(response);
        this.navigateBasedOnRole(response.user.role);
      })
    );
  }

  private setAuthState(response: LoginResponse): void {
    localStorage.setItem('access_token', response.token);
    localStorage.setItem('refresh_token', response.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.user));

    this.tokenSignal.set(response.token);
    this.userSignal.set(response.user);

    // Save user role for post-logout redirect
    this.saveLastUserRole(response.user.role);
  }

  private clearAuthState(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    // ✅ No longer storing tenant_subdomain in localStorage
    // NOTE: We keep hrms_last_user_role for post-logout redirect purposes

    this.tokenSignal.set(null);
    this.userSignal.set(null);
  }

  /**
   * Save the user role for post-logout redirect purposes
   * This persists after logout to redirect users to the correct login page
   */
  private saveLastUserRole(role: UserRole): void {
    localStorage.setItem('hrms_last_user_role', role);
    console.log('💾 Saved last user role for post-logout redirect:', role);
  }

  /**
   * Get the last user role (even after logout)
   * Used by guards to redirect to the correct login page
   */
  getLastUserRole(): UserRole | null {
    const role = localStorage.getItem('hrms_last_user_role');
    return role as UserRole | null;
  }

  private navigateBasedOnRole(role: UserRole): void {
    switch (role) {
      case UserRole.SuperAdmin:
        this.router.navigate(['/admin/dashboard']);
        break;
      case UserRole.TenantAdmin:
      case UserRole.HR:
      case UserRole.Manager:
        this.router.navigate(['/tenant/dashboard']);
        break;
      case UserRole.Employee:
        this.router.navigate(['/employee/dashboard']);
        break;
      default:
        this.router.navigate(['/']);
    }
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  hasRole(role: UserRole): boolean {
    return this.userSignal()?.role === role;
  }

  hasAnyRole(roles: UserRole[]): boolean {
    const userRole = this.userSignal()?.role;
    return userRole ? roles.includes(userRole) : false;
  }
}
