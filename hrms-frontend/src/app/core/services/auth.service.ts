import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, BehaviorSubject } from 'rxjs';
import { User, LoginRequest, LoginResponse, UserRole } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // API URL - should be from environment
  private apiUrl = 'http://localhost:5000/api';

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

    // Determine endpoint based on user type
    const endpoint = credentials.tenantId
      ? `${this.apiUrl}/auth/tenant/login`
      : `${this.apiUrl}/auth/admin/login`;

    return this.http.post<LoginResponse>(endpoint, credentials).pipe(
      tap(response => {
        this.setAuthState(response);
        this.navigateBasedOnRole(response.user.role);
        this.loadingSignal.set(false);
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
