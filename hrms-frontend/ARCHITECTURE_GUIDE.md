# Architecture Guide

> **Fortune 500-Grade HRMS Frontend Architecture Documentation**
> Complete architectural overview of the Angular application structure, patterns, and design decisions.

## Table of Contents

1. [System Overview](#system-overview)
2. [Application Architecture](#application-architecture)
3. [Module Structure](#module-structure)
4. [State Management](#state-management)
5. [Service Architecture](#service-architecture)
6. [Routing Architecture](#routing-architecture)
7. [Authentication & Authorization](#authentication--authorization)
8. [Multi-Tenancy Architecture](#multi-tenancy-architecture)
9. [Data Flow](#data-flow)
10. [Design Patterns](#design-patterns)
11. [Performance Optimization](#performance-optimization)
12. [Security Architecture](#security-architecture)

---

## System Overview

### Technology Stack

```
Frontend Framework: Angular 20.x (standalone components)
Language: TypeScript 5.9.x
State Management: Angular Signals
HTTP Client: Angular HttpClient with Interceptors
Styling: SCSS with BEM methodology
UI Components: 29 custom components (Material UI replacement)
Charts: Chart.js + ng2-charts
Real-time: SignalR (@microsoft/signalr)
Build Tool: Angular CLI with esbuild
Package Manager: npm
```

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Browser Layer                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Angular    │  │   Angular    │  │   Service    │          │
│  │  Components  │→ │   Services   │→ │    Worker    │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                            ↓ HTTP/WebSocket
┌─────────────────────────────────────────────────────────────────┐
│                      HTTP Interceptors                           │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────┐      │
│  │ Auth Interceptor│→│ Error Handler  │→│ Tenant Context│      │
│  └────────────────┘  └────────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                            ↓ REST API
┌─────────────────────────────────────────────────────────────────┐
│                      Backend API (.NET)                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Controllers │→ │   Services   │→ │   Database   │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

1. **Standalone Components**: Using Angular 17+ standalone components (no NgModules)
2. **Signal-Based State**: Angular Signals for reactive state management
3. **Lazy Loading**: Route-based code splitting for optimal performance
4. **Custom UI Library**: 29 custom components replacing Angular Material
5. **Multi-Tenant**: Subdomain-based tenant isolation with context service
6. **Token-Based Auth**: JWT with refresh tokens and automatic token rotation
7. **Feature-Based Structure**: Organized by domain (admin, tenant, employee)

---

## Application Architecture

### Component Hierarchy

```
app.component.ts (Root)
├── Marketing Module
│   └── landing-page.component.ts
│
├── Auth Module
│   ├── subdomain.component.ts
│   ├── login/tenant-login.component.ts
│   ├── superadmin/superadmin-login.component.ts
│   ├── activate/activate.component.ts
│   ├── forgot-password/forgot-password.component.ts
│   └── reset-password/reset-password.component.ts
│
├── Admin Portal (SuperAdmin)
│   ├── admin-layout.component.ts
│   │   ├── admin-dashboard.component.ts
│   │   ├── tenant-management/
│   │   │   ├── tenant-list.component.ts
│   │   │   ├── tenant-form.component.ts
│   │   │   └── tenant-detail.component.ts
│   │   ├── audit-logs/audit-logs.component.ts
│   │   ├── monitoring/
│   │   │   ├── monitoring-dashboard.component.ts
│   │   │   ├── infrastructure-health.component.ts
│   │   │   ├── api-performance.component.ts
│   │   │   └── tenant-activity.component.ts
│   │   └── security-alerts/
│   │       ├── security-alerts-dashboard.component.ts
│   │       └── alert-list.component.ts
│
├── Tenant Portal (HR/Manager)
│   ├── tenant-layout.component.ts
│   │   ├── tenant-dashboard.component.ts
│   │   ├── employees/
│   │   │   ├── employee-list.component.ts
│   │   │   └── comprehensive-employee-form.component.ts
│   │   ├── attendance/attendance-dashboard.component.ts
│   │   ├── leave/leave-dashboard.component.ts
│   │   ├── payroll/
│   │   │   ├── payroll-dashboard.component.ts
│   │   │   └── salary-components.component.ts
│   │   └── reports/reports-dashboard.component.ts
│
└── Employee Portal
    ├── employee-dashboard.component.ts
    ├── employee-attendance.component.ts
    ├── employee-leave.component.ts
    ├── timesheet-list.component.ts
    └── payslip-list.component.ts
```

### Core vs Feature vs Shared

```typescript
/**
 * CORE MODULE
 * - Singleton services (providedIn: 'root')
 * - Guards, interceptors, models
 * - Never imported into lazy-loaded modules
 */
src/app/core/
├── guards/
│   ├── auth.guard.ts           // Authentication check
│   ├── role.guard.ts           // Authorization check
│   ├── subdomain.guard.ts      // Tenant subdomain validation
│   └── already-logged-in.guard.ts
├── interceptors/
│   └── auth.interceptor.ts     // Token injection, refresh, error handling
├── models/
│   ├── user.model.ts
│   ├── tenant.model.ts
│   ├── employee.model.ts
│   └── monitoring.model.ts
└── services/
    ├── auth.service.ts         // Authentication & token management
    ├── tenant.service.ts       // Tenant CRUD operations
    ├── employee.service.ts     // Employee CRUD operations
    ├── tenant-context.service.ts // Multi-tenant context
    ├── session-management.service.ts // Session timeout
    └── monitoring.service.ts   // System monitoring

/**
 * FEATURES MODULE
 * - Feature-specific components
 * - Lazy-loaded by routes
 * - Domain-driven organization
 */
src/app/features/
├── admin/                      // SuperAdmin features
├── tenant/                     // Tenant Admin features
├── employee/                   // Employee features
├── auth/                       // Authentication pages
└── marketing/                  // Public pages

/**
 * SHARED MODULE
 * - Reusable components, pipes, directives
 * - Can be imported anywhere
 * - Should not depend on features
 */
src/app/shared/
├── ui/components/              // 29 custom UI components
├── layouts/                    // Layout wrappers
│   ├── admin-layout.component.ts
│   └── tenant-layout.component.ts
├── pipes/                      // Custom pipes
└── directives/                 // Custom directives
```

---

## Module Structure

### Standalone Component Architecture

Angular 17+ uses standalone components without NgModules:

```typescript
/**
 * Standalone component - no NgModule required
 * Components declare their own dependencies
 */
@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    CommonModule,
    ButtonComponent,
    InputComponent,
    TableComponent,
    PaginatorComponent
  ],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss'
})
export class EmployeeListComponent {
  // Component logic
}
```

### Route-Based Lazy Loading

```typescript
/**
 * Routes automatically lazy-load components
 * No need for loadChildren with modules
 */
export const routes: Routes = [
  {
    path: 'tenant',
    canActivate: [hrGuard],
    loadComponent: () => import('./shared/layouts/tenant-layout.component')
      .then(m => m.TenantLayoutComponent),
    children: [
      {
        path: 'employees',
        loadComponent: () => import('./features/tenant/employees/employee-list.component')
          .then(m => m.EmployeeListComponent)
      }
    ]
  }
];
```

---

## State Management

### Angular Signals (Reactive State)

```typescript
/**
 * Signal-based state management
 * Replaces BehaviorSubject/Observable patterns
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  // Private signals (writable)
  private userSignal = signal<User | null>(null);
  private tokenSignal = signal<string | null>(null);
  private loadingSignal = signal<boolean>(false);

  // Public readonly signals
  readonly user = this.userSignal.asReadonly();
  readonly token = this.tokenSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  // Computed signals (derived state)
  readonly isAuthenticated = computed(() =>
    !!this.userSignal() && !!this.tokenSignal()
  );
  readonly isSuperAdmin = computed(() =>
    this.userSignal()?.role === UserRole.SuperAdmin
  );

  // Update state
  login(credentials: LoginRequest): Observable<LoginResponse> {
    this.loadingSignal.set(true);

    return this.http.post<LoginResponse>(endpoint, credentials).pipe(
      tap(response => {
        this.userSignal.set(response.user);
        this.tokenSignal.set(response.token);
        this.loadingSignal.set(false);
      })
    );
  }
}
```

### Component State Usage

```typescript
/**
 * Components consume signals in templates
 * Automatic change detection on signal updates
 */
@Component({
  selector: 'app-header',
  template: `
    <!-- Direct signal usage in templates -->
    @if (auth.isAuthenticated()) {
      <p>Welcome, {{ auth.user()?.firstName }}</p>
      <button (click)="logout()">Logout</button>
    } @else {
      <a routerLink="/auth/login">Login</a>
    }

    @if (auth.loading()) {
      <app-progress-spinner></app-progress-spinner>
    }
  `
})
export class HeaderComponent {
  // Inject service with signals
  auth = inject(AuthService);

  logout(): void {
    this.auth.logout();
  }
}
```

### Local Component State

```typescript
/**
 * Component-level state with signals
 */
export class EmployeeListComponent {
  // Local signals
  employees = signal<Employee[]>([]);
  loading = signal<boolean>(false);
  searchQuery = signal<string>('');

  // Computed state
  filteredEmployees = computed(() => {
    const query = this.searchQuery().toLowerCase();
    return this.employees().filter(emp =>
      emp.name.toLowerCase().includes(query)
    );
  });

  // Load data
  loadEmployees(): void {
    this.loading.set(true);
    this.employeeService.getEmployees().subscribe(data => {
      this.employees.set(data);
      this.loading.set(false);
    });
  }
}
```

---

## Service Architecture

### Service Layers

```
┌─────────────────────────────────────────────┐
│          Presentation Layer                  │
│         (Components/Templates)               │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│         Application Services                 │
│  (Business Logic, State Management)          │
│  - AuthService                               │
│  - EmployeeService                           │
│  - TenantService                             │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│          Infrastructure Services             │
│  (HTTP, Caching, Error Handling)             │
│  - HttpClient                                │
│  - Interceptors                              │
│  - Error Handler                             │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│             Backend API                      │
│         (REST/WebSocket)                     │
└─────────────────────────────────────────────┘
```

### Service Design Pattern

```typescript
/**
 * Standard service structure
 * - Singleton (providedIn: 'root')
 * - Dependency injection via inject()
 * - RxJS for async operations
 * - Error handling
 */
@Injectable({ providedIn: 'root' })
export class EmployeeService {
  // Inject dependencies
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // Public API methods
  getEmployees(filters?: EmployeeFilters): Observable<Employee[]> {
    const params = this.buildParams(filters);
    return this.http.get<ApiResponse<Employee[]>>(
      `${this.apiUrl}/employees`,
      { params }
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  getEmployee(id: number): Observable<Employee> {
    return this.http.get<ApiResponse<Employee>>(
      `${this.apiUrl}/employees/${id}`
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  createEmployee(data: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<ApiResponse<Employee>>(
      `${this.apiUrl}/employees`,
      data
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  updateEmployee(id: number, data: UpdateEmployeeRequest): Observable<Employee> {
    return this.http.put<ApiResponse<Employee>>(
      `${this.apiUrl}/employees/${id}`,
      data
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/employees/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  // Private helper methods
  private buildParams(filters?: EmployeeFilters): HttpParams {
    let params = new HttpParams();
    if (filters?.department) {
      params = params.set('department', filters.department);
    }
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    return params;
  }

  private handleError(error: any): Observable<never> {
    console.error('API Error:', error);
    const message = error.error?.message || 'An error occurred';
    return throwError(() => new Error(message));
  }
}
```

### Service Communication Patterns

```typescript
/**
 * 1. Service-to-Service Communication
 */
@Injectable({ providedIn: 'root' })
export class EmployeeFormService {
  private employeeService = inject(EmployeeService);
  private departmentService = inject(DepartmentService);
  private locationService = inject(LocationService);

  // Combine data from multiple services
  getFormData(employeeId?: number): Observable<EmployeeFormData> {
    return forkJoin({
      employee: employeeId
        ? this.employeeService.getEmployee(employeeId)
        : of(null),
      departments: this.departmentService.getDepartments(),
      locations: this.locationService.getLocations()
    });
  }
}

/**
 * 2. Event Bus Pattern (via Subject)
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private notificationSubject = new Subject<Notification>();
  notifications$ = this.notificationSubject.asObservable();

  showSuccess(message: string): void {
    this.notificationSubject.next({
      type: 'success',
      message
    });
  }

  showError(message: string): void {
    this.notificationSubject.next({
      type: 'error',
      message
    });
  }
}
```

---

## Routing Architecture

### Route Configuration

```typescript
/**
 * app.routes.ts - Application routes
 * - Feature-based organization
 * - Lazy loading
 * - Guard protection
 */
export const routes: Routes = [
  // Public routes
  {
    path: '',
    loadComponent: () => import('./features/marketing/landing-page.component')
      .then(m => m.LandingPageComponent),
    pathMatch: 'full'
  },

  // Auth routes (no guard - accessible to all)
  {
    path: 'auth',
    children: [
      {
        path: 'subdomain',
        loadComponent: () => import('./features/auth/subdomain/subdomain.component')
          .then(m => m.SubdomainComponent)
      },
      {
        path: 'login',
        canActivate: [subdomainGuard, alreadyLoggedInGuard],
        loadComponent: () => import('./features/auth/login/tenant-login.component')
          .then(m => m.TenantLoginComponent)
      },
      {
        path: 'superadmin',
        loadComponent: () => import('./features/auth/superadmin/superadmin-login.component')
          .then(m => m.SuperAdminLoginComponent)
      }
    ]
  },

  // Admin portal (SuperAdmin only)
  {
    path: 'admin',
    canActivate: [superAdminGuard],
    loadComponent: () => import('./shared/layouts/admin-layout.component')
      .then(m => m.AdminLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/admin/dashboard/admin-dashboard.component')
          .then(m => m.AdminDashboardComponent)
      },
      {
        path: 'tenants',
        loadComponent: () => import('./features/admin/tenant-management/tenant-list.component')
          .then(m => m.TenantListComponent)
      },
      {
        path: 'monitoring',
        children: [
          {
            path: 'dashboard',
            loadComponent: () => import('./features/admin/monitoring/dashboard/monitoring-dashboard.component')
              .then(m => m.MonitoringDashboardComponent)
          }
        ]
      }
    ]
  },

  // Tenant portal (HR/Manager)
  {
    path: 'tenant',
    canActivate: [hrGuard],
    loadComponent: () => import('./shared/layouts/tenant-layout.component')
      .then(m => m.TenantLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/tenant/dashboard/tenant-dashboard.component')
          .then(m => m.TenantDashboardComponent)
      },
      {
        path: 'employees',
        loadComponent: () => import('./features/tenant/employees/employee-list.component')
          .then(m => m.EmployeeListComponent)
      }
    ]
  },

  // Employee portal
  {
    path: 'employee',
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/employee/dashboard/employee-dashboard.component')
          .then(m => m.EmployeeDashboardComponent)
      }
    ]
  },

  // Wildcard - redirect to subdomain entry
  {
    path: '**',
    redirectTo: '/auth/subdomain'
  }
];
```

### Route Guards

```typescript
/**
 * Auth Guard - Check if user is authenticated
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store intended URL for redirect after login
  router.navigate(['/auth/subdomain'], {
    queryParams: { returnUrl: state.url }
  });
  return false;
};

/**
 * Role Guard - Check user permissions
 */
export const superAdminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isSuperAdmin()) {
    return true;
  }

  console.warn('Access denied: SuperAdmin role required');
  router.navigate(['/auth/subdomain']);
  return false;
};

export const hrGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = [UserRole.TenantAdmin, UserRole.HR, UserRole.Manager];

  if (authService.hasAnyRole(allowedRoles)) {
    return true;
  }

  console.warn('Access denied: HR/Manager role required');
  router.navigate(['/employee/dashboard']);
  return false;
};

/**
 * Subdomain Guard - Validate tenant subdomain
 */
export const subdomainGuard: CanActivateFn = () => {
  const subdomainService = inject(SubdomainService);
  const router = inject(Router);

  const subdomain = subdomainService.getStoredSubdomain();

  if (subdomain) {
    return true;
  }

  router.navigate(['/auth/subdomain']);
  return false;
};
```

---

## Authentication & Authorization

### Authentication Flow

```
1. User enters subdomain → SubdomainComponent
   ↓
2. Subdomain stored in localStorage
   ↓
3. User enters credentials → TenantLoginComponent
   ↓
4. POST /api/auth/tenant/login
   ↓
5. Backend validates & returns JWT + refresh token
   ↓
6. AuthService stores tokens & user data
   ↓
7. Navigate to dashboard based on role
   ↓
8. Auth interceptor adds token to all requests
   ↓
9. On 401 error → Attempt token refresh
   ↓
10. If refresh fails → Logout and redirect to login
```

### Token Management

```typescript
/**
 * JWT Token Structure
 */
interface JwtPayload {
  sub: string;           // User ID
  email: string;
  role: UserRole;
  tenantId?: number;
  exp: number;           // Expiration timestamp
  iat: number;           // Issued at timestamp
}

/**
 * Token Storage
 */
localStorage.setItem('access_token', accessToken);
localStorage.setItem('refresh_token', refreshToken);
localStorage.setItem('user', JSON.stringify(user));

/**
 * Token Refresh Flow
 */
// 1. Access token expires (401 error)
// 2. Interceptor catches 401
// 3. Call refresh endpoint with refresh token
// 4. Receive new access token
// 5. Retry original request with new token
// 6. If refresh fails → logout
```

### HTTP Interceptor (Token Injection & Refresh)

```typescript
/**
 * Auth Interceptor
 * - Adds Authorization header
 * - Handles 401 errors with token refresh
 * - Prevents retry loops
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  // Add Authorization header
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      },
      withCredentials: true // Send HttpOnly cookies
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized
      if (error.status === 401) {
        // Don't retry login/refresh endpoints
        if (req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
          return throwError(() => error);
        }

        // Don't retry if already retried
        if (isRetryRequest(req)) {
          authService.logout();
          return throwError(() => error);
        }

        // Attempt token refresh
        return authService.refreshToken().pipe(
          switchMap(() => {
            // Retry original request with new token
            const newToken = authService.getToken();
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`,
                'X-Retry-Request': 'true' // Mark as retry
              }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
```

### Authorization Matrix

```typescript
/**
 * Role-based access control
 */
enum UserRole {
  SuperAdmin = 'SuperAdmin',   // Full system access
  TenantAdmin = 'TenantAdmin',  // Tenant management
  HR = 'HR',                    // HR operations
  Manager = 'Manager',          // Team management
  Employee = 'Employee'         // Self-service only
}

/**
 * Permission mapping
 */
const permissions = {
  // SuperAdmin only
  'admin:tenants': [UserRole.SuperAdmin],
  'admin:monitoring': [UserRole.SuperAdmin],
  'admin:audit-logs': [UserRole.SuperAdmin],

  // Tenant Admin + HR
  'tenant:employees': [UserRole.TenantAdmin, UserRole.HR],
  'tenant:payroll': [UserRole.TenantAdmin, UserRole.HR],
  'tenant:reports': [UserRole.TenantAdmin, UserRole.HR, UserRole.Manager],

  // All authenticated
  'employee:profile': [UserRole.TenantAdmin, UserRole.HR, UserRole.Manager, UserRole.Employee],
  'employee:attendance': [UserRole.TenantAdmin, UserRole.HR, UserRole.Manager, UserRole.Employee]
};
```

---

## Multi-Tenancy Architecture

### Tenant Isolation

```typescript
/**
 * Tenant Context Service
 * - Manages tenant identification
 * - Extracts subdomain from URL (production)
 * - Falls back to localStorage (development)
 */
@Injectable({ providedIn: 'root' })
export class TenantContextService {
  private currentSubdomain = signal<string | null>(null);

  constructor() {
    this.detectTenant();
  }

  private detectTenant(): void {
    // Production: Extract from subdomain
    const hostname = window.location.hostname;
    if (hostname.includes('.')) {
      const subdomain = hostname.split('.')[0];
      if (subdomain !== 'www' && subdomain !== 'app') {
        this.currentSubdomain.set(subdomain);
        return;
      }
    }

    // Development: Use localStorage
    const stored = localStorage.getItem('tenant_subdomain');
    if (stored) {
      this.currentSubdomain.set(stored);
    }
  }

  getTenantSubdomain(): string | null {
    return this.currentSubdomain();
  }

  setTenantSubdomain(subdomain: string): void {
    this.currentSubdomain.set(subdomain);
    localStorage.setItem('tenant_subdomain', subdomain);
  }
}
```

### Tenant-Aware API Requests

```typescript
/**
 * Interceptor adds tenant context
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const tenantContext = inject(TenantContextService);
  const subdomain = tenantContext.getTenantSubdomain();

  // Add tenant header for multi-tenant endpoints
  if (subdomain && !req.url.includes('/auth/')) {
    req = req.clone({
      setHeaders: {
        'X-Tenant-Subdomain': subdomain
      }
    });
  }

  return next(req);
};
```

---

## Data Flow

### Request/Response Flow

```
Component
   ↓ Call service method
Service
   ↓ HTTP request
Interceptors (auth, tenant, error)
   ↓ Add headers, handle errors
Backend API
   ↓ Process & return data
Interceptors (transform response)
   ↓ Extract data
Service
   ↓ Return Observable
Component
   ↓ Subscribe & update UI
```

### RxJS Data Pipelines

```typescript
/**
 * Complex data flow with RxJS operators
 */
loadEmployeeData(employeeId: number): void {
  this.loading.set(true);

  this.employeeService.getEmployee(employeeId).pipe(
    // Transform data
    map(employee => this.transformEmployee(employee)),

    // Add additional data
    switchMap(employee =>
      forkJoin({
        employee: of(employee),
        department: this.departmentService.getDepartment(employee.departmentId),
        manager: this.employeeService.getEmployee(employee.managerId)
      })
    ),

    // Error handling
    catchError(error => {
      this.toast.error('Failed to load employee data');
      return of(null);
    }),

    // Cleanup subscription
    takeUntil(this.destroy$)
  ).subscribe(data => {
    if (data) {
      this.employee.set(data.employee);
      this.department.set(data.department);
      this.manager.set(data.manager);
    }
    this.loading.set(false);
  });
}
```

---

## Design Patterns

### 1. Repository Pattern

```typescript
/**
 * Abstract data access behind service interface
 */
interface EmployeeRepository {
  getAll(): Observable<Employee[]>;
  getById(id: number): Observable<Employee>;
  create(data: CreateEmployeeRequest): Observable<Employee>;
  update(id: number, data: UpdateEmployeeRequest): Observable<Employee>;
  delete(id: number): Observable<void>;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService implements EmployeeRepository {
  // Implementation
}
```

### 2. Facade Pattern

```typescript
/**
 * Simplify complex subsystem interactions
 */
@Injectable({ providedIn: 'root' })
export class EmployeeManagementFacade {
  private employeeService = inject(EmployeeService);
  private departmentService = inject(DepartmentService);
  private payrollService = inject(PayrollService);

  // Single method combines multiple operations
  onboardEmployee(data: OnboardEmployeeRequest): Observable<OnboardResult> {
    return this.employeeService.createEmployee(data.employee).pipe(
      switchMap(employee =>
        forkJoin({
          employee: of(employee),
          department: this.departmentService.assignEmployee(employee.id, data.departmentId),
          payroll: this.payrollService.setupPayroll(employee.id, data.salary)
        })
      )
    );
  }
}
```

### 3. Observer Pattern (via RxJS)

```typescript
/**
 * Observable streams for reactive programming
 */
@Injectable({ providedIn: 'root' })
export class EmployeeStoreService {
  private employeesSubject = new BehaviorSubject<Employee[]>([]);
  employees$ = this.employeesSubject.asObservable();

  loadEmployees(): void {
    this.employeeService.getEmployees().subscribe(data => {
      this.employeesSubject.next(data);
    });
  }

  addEmployee(employee: Employee): void {
    const current = this.employeesSubject.value;
    this.employeesSubject.next([...current, employee]);
  }
}
```

---

## Performance Optimization

### 1. Lazy Loading

```typescript
// Route-based code splitting
{
  path: 'reports',
  loadComponent: () => import('./features/reports/reports.component')
    .then(m => m.ReportsComponent)
}
```

### 2. OnPush Change Detection

```typescript
@Component({
  selector: 'app-employee-card',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmployeeCardComponent {
  // Component only re-renders when inputs change
}
```

### 3. Virtual Scrolling (for large lists)

```typescript
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  template: `
    <cdk-virtual-scroll-viewport itemSize="50" class="viewport">
      <div *cdkVirtualFor="let employee of employees">
        {{ employee.name }}
      </div>
    </cdk-virtual-scroll-viewport>
  `
})
export class EmployeeListComponent {}
```

### 4. Bundle Size Optimization

- Tree-shaking with standalone components
- Lazy loading routes
- Custom UI components (no Material dependency)
- Code splitting with dynamic imports

---

## Security Architecture

### Security Layers

1. **Transport Security**: HTTPS only
2. **Authentication**: JWT with refresh tokens
3. **Authorization**: Role-based access control (RBAC)
4. **Input Validation**: Client-side + server-side
5. **XSS Prevention**: Angular sanitization
6. **CSRF Protection**: Token-based
7. **Secure Storage**: HttpOnly cookies for refresh tokens

### Security Best Practices

```typescript
// 1. No sensitive data in localStorage
// ✗ Bad
localStorage.setItem('password', password);

// ✓ Good
// Passwords never stored, only sent to backend

// 2. Sanitize user input
import { DomSanitizer } from '@angular/platform-browser';

constructor(private sanitizer: DomSanitizer) {}

safeHtml = this.sanitizer.sanitize(SecurityContext.HTML, userInput);

// 3. Validate on both client and server
// Client-side validation for UX
// Server-side validation for security
```

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Maintained By**: HRMS Development Team
