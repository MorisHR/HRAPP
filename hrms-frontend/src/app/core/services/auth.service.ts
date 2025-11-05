import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, map, catchError, throwError } from 'rxjs';
import { User, LoginRequest, LoginResponse, UserRole } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

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

    // Store subdomain for tenant employees
    if (credentials.subdomain) {
      localStorage.setItem('tenant_subdomain', credentials.subdomain);
    } else {
      localStorage.removeItem('tenant_subdomain');
    }

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

  logout(): void {
    this.clearAuthState();
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<LoginResponse> {
    const refreshToken = localStorage.getItem('refresh_token');
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/refresh`, { refreshToken }).pipe(
      tap(response => this.setAuthState(response))
    );
  }

  private setAuthState(response: LoginResponse): void {
    localStorage.setItem('access_token', response.token);
    localStorage.setItem('refresh_token', response.refreshToken);
    localStorage.setItem('user', JSON.stringify(response.user));

    this.tokenSignal.set(response.token);
    this.userSignal.set(response.user);
  }

  private clearAuthState(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
    localStorage.removeItem('tenant_subdomain');

    this.tokenSignal.set(null);
    this.userSignal.set(null);
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
