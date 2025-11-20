# API Integration Guide

> **Fortune 500-Grade HRMS API Integration Documentation**
> Complete guide for integrating with the backend .NET Core API.

## Table of Contents

1. [API Overview](#api-overview)
2. [Authentication Setup](#authentication-setup)
3. [API Client Configuration](#api-client-configuration)
4. [Error Handling](#error-handling)
5. [API Endpoints](#api-endpoints)
6. [Request/Response Patterns](#requestresponse-patterns)
7. [Environment Configuration](#environment-configuration)
8. [Real-Time Communication](#real-time-communication)
9. [Testing API Integration](#testing-api-integration)

---

## API Overview

### Base Configuration

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://api.hrms.example.com/api',
  // OR for local development:
  // apiUrl: 'http://localhost:5090/api',

  appName: 'HRMS',
  version: '1.0.0',
  superAdminSecretPath: '732c44d0-d59b-494c-9fc0-bf1d65add4e5'
};
```

### API Response Format

All API responses follow a consistent format:

```typescript
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors?: string[];
  timestamp?: string;
}

// Success response
{
  "success": true,
  "data": { /* actual data */ },
  "message": "Operation completed successfully",
  "timestamp": "2025-11-17T12:00:00Z"
}

// Error response
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Email is required",
    "Password must be at least 12 characters"
  ],
  "timestamp": "2025-11-17T12:00:00Z"
}
```

---

## Authentication Setup

### 1. Login Flow

```typescript
/**
 * Tenant Login
 * POST /api/auth/tenant/login
 */
interface TenantLoginRequest {
  email: string;
  password: string;
  subdomain: string;
}

interface TenantLoginResponse {
  success: boolean;
  data: {
    token: string;           // JWT access token (15 min expiry)
    refreshToken: string;    // Refresh token (7 days expiry)
    expiresAt: string;
    employee: {
      id: number;
      email: string;
      fullName: string;
      role: string;
    }
  };
  message: string;
}

// Usage
this.authService.login({
  email: 'user@example.com',
  password: 'password123',
  subdomain: 'acme'
}).subscribe({
  next: (response) => {
    console.log('Logged in:', response.user);
    // Token automatically stored by AuthService
  },
  error: (error) => {
    console.error('Login failed:', error.message);
  }
});
```

### 2. SuperAdmin Login

```typescript
/**
 * SuperAdmin Login with MFA
 * POST /api/auth/system-{secretPath}
 */
interface SuperAdminLoginRequest {
  email: string;
  password: string;
}

interface SuperAdminLoginResponse {
  success: boolean;
  data: {
    requiresMfaSetup?: boolean;
    requiresMfaVerification?: boolean;
    userId?: string;
    qrCodeUrl?: string;
    secret?: string;
    backupCodes?: string[];
  };
}

// Step 1: Initial login
this.authService.superAdminSecretLogin({
  email: 'admin@system.com',
  password: 'securepass'
}).subscribe({
  next: (response) => {
    if (response.requiresMfaSetup) {
      // Show QR code for authenticator app
      this.showMfaSetup(response.qrCodeUrl, response.backupCodes);
    } else if (response.requiresMfaVerification) {
      // Show MFA code input
      this.showMfaVerification();
    }
  }
});

// Step 2: Complete MFA setup
this.authService.completeMfaSetup({
  userId: userId,
  totpCode: '123456',
  secret: secret,
  backupCodes: backupCodes
}).subscribe({
  next: () => {
    console.log('MFA setup complete, logged in');
  }
});

// Step 3: Verify MFA code
this.authService.verifyMfa({
  userId: userId,
  code: '123456' // Or backup code
}).subscribe({
  next: () => {
    console.log('MFA verified, logged in');
  }
});
```

### 3. Token Refresh

```typescript
/**
 * Refresh Access Token
 * POST /api/auth/tenant/refresh (for tenant users)
 * POST /api/auth/refresh (for SuperAdmin)
 *
 * Automatically handled by auth.interceptor.ts
 * Refresh token sent via HttpOnly cookie
 */

// Manual refresh (rarely needed)
this.authService.refreshToken().subscribe({
  next: (response) => {
    console.log('Token refreshed');
  },
  error: () => {
    console.log('Refresh failed, logging out');
    this.authService.logout();
  }
});
```

### 4. Logout

```typescript
/**
 * Logout & Revoke Tokens
 * POST /api/auth/revoke
 */
this.authService.logout();
// Automatically:
// - Clears local storage
// - Revokes refresh token on backend
// - Redirects to login page
```

### 5. Password Reset

```typescript
/**
 * Request Password Reset
 * POST /api/auth/forgot-password
 */
this.authService.forgotPassword('user@example.com').subscribe({
  next: () => {
    console.log('Reset email sent');
  }
});

/**
 * Reset Password with Token
 * POST /api/auth/reset-password
 */
this.authService.resetPassword({
  token: 'reset-token-from-email',
  newPassword: 'newSecurePass123',
  confirmPassword: 'newSecurePass123'
}).subscribe({
  next: () => {
    console.log('Password reset successful');
  }
});
```

---

## API Client Configuration

### HTTP Interceptors

```typescript
/**
 * app.config.ts - Register interceptors
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withInterceptors([
        authInterceptor,      // Adds Authorization header
        tenantInterceptor,    // Adds tenant context
        errorInterceptor      // Global error handling
      ])
    )
  ]
};
```

### Making API Requests

```typescript
/**
 * Standard service pattern for API calls
 */
@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // GET request with query parameters
  getEmployees(params?: EmployeeQueryParams): Observable<Employee[]> {
    const httpParams = new HttpParams()
      .set('page', params?.page || 1)
      .set('pageSize', params?.pageSize || 10)
      .set('search', params?.search || '')
      .set('department', params?.department || '');

    return this.http.get<ApiResponse<Employee[]>>(
      `${this.apiUrl}/employees`,
      { params: httpParams }
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // GET single resource
  getEmployee(id: number): Observable<Employee> {
    return this.http.get<ApiResponse<Employee>>(
      `${this.apiUrl}/employees/${id}`
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // POST request
  createEmployee(data: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<ApiResponse<Employee>>(
      `${this.apiUrl}/employees`,
      data
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // PUT request
  updateEmployee(id: number, data: UpdateEmployeeRequest): Observable<Employee> {
    return this.http.put<ApiResponse<Employee>>(
      `${this.apiUrl}/employees/${id}`,
      data
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // PATCH request (partial update)
  patchEmployee(id: number, data: Partial<Employee>): Observable<Employee> {
    return this.http.patch<ApiResponse<Employee>>(
      `${this.apiUrl}/employees/${id}`,
      data
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // DELETE request
  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<ApiResponse<void>>(
      `${this.apiUrl}/employees/${id}`
    ).pipe(
      map(() => undefined),
      catchError(this.handleError)
    );
  }

  // File upload
  uploadEmployeePhoto(id: number, file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ApiResponse<string>>(
      `${this.apiUrl}/employees/${id}/photo`,
      formData
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  // Error handler
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.error?.message || error.message;
    }

    console.error('API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
```

---

## Error Handling

### HTTP Status Codes

```typescript
/**
 * Error handling by status code
 */
catchError((error: HttpErrorResponse) => {
  switch (error.status) {
    case 400: // Bad Request
      console.error('Validation failed:', error.error.errors);
      // Show validation errors to user
      break;

    case 401: // Unauthorized
      console.error('Authentication failed');
      // Handled by auth interceptor (token refresh)
      break;

    case 403: // Forbidden
      console.error('Access denied');
      this.router.navigate(['/unauthorized']);
      break;

    case 404: // Not Found
      console.error('Resource not found');
      this.router.navigate(['/not-found']);
      break;

    case 409: // Conflict
      console.error('Resource conflict:', error.error.message);
      // E.g., duplicate email, concurrent update
      break;

    case 422: // Unprocessable Entity
      console.error('Business logic error:', error.error.message);
      break;

    case 429: // Too Many Requests
      console.error('Rate limit exceeded');
      // Show rate limit message
      break;

    case 500: // Internal Server Error
    case 502: // Bad Gateway
    case 503: // Service Unavailable
      console.error('Server error, please try again later');
      break;

    default:
      console.error('Unexpected error:', error);
  }

  return throwError(() => error);
})
```

### Global Error Handler

```typescript
/**
 * Global error handling service
 */
@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  private toastService = inject(ToastService);

  handleError(error: any, context?: string): void {
    // Log to console
    console.error(`Error in ${context}:`, error);

    // Show user-friendly message
    if (error instanceof HttpErrorResponse) {
      this.handleHttpError(error);
    } else {
      this.toastService.error('An unexpected error occurred');
    }

    // Send to error tracking service (e.g., Sentry)
    this.logToErrorTracking(error, context);
  }

  private handleHttpError(error: HttpErrorResponse): void {
    const message = error.error?.message || 'An error occurred';
    const errors = error.error?.errors || [];

    if (errors.length > 0) {
      errors.forEach((err: string) => this.toastService.error(err));
    } else {
      this.toastService.error(message);
    }
  }

  private logToErrorTracking(error: any, context?: string): void {
    // Send to monitoring service
    // E.g., Sentry, Application Insights, etc.
  }
}
```

---

## API Endpoints

### Authentication Endpoints

```typescript
// Tenant login
POST /api/auth/tenant/login
Body: { email, password, subdomain }
Response: { token, refreshToken, employee }

// SuperAdmin login
POST /api/auth/system-{secretPath}
Body: { email, password }
Response: { requiresMfaSetup, requiresMfaVerification, userId, qrCodeUrl }

// MFA setup
POST /api/auth/mfa/complete-setup
Body: { userId, totpCode, secret, backupCodes }
Response: { token, refreshToken, adminUser }

// MFA verification
POST /api/auth/mfa/verify
Body: { userId, code }
Response: { token, refreshToken, adminUser }

// Token refresh
POST /api/auth/tenant/refresh
POST /api/auth/refresh
Headers: Cookie (HttpOnly refresh token)
Response: { token, refreshToken }

// Logout
POST /api/auth/revoke
Headers: Cookie (HttpOnly refresh token)

// Password reset
POST /api/auth/forgot-password
Body: { email }

POST /api/auth/reset-password
Body: { token, newPassword, confirmPassword }

// Employee activation
POST /api/auth/employee/activate
Body: { token, password }

// Employee set password
POST /api/auth/employee/set-password
Body: { token, newPassword, confirmPassword, subdomain }
```

### Tenant Management (SuperAdmin)

```typescript
// Get all tenants
GET /api/Tenants
Query: ?page=1&pageSize=10&search=acme
Response: { data: Tenant[], totalCount, page, pageSize }

// Get tenant by ID
GET /api/Tenants/{id}
Response: { data: Tenant }

// Create tenant
POST /api/Tenants
Body: CreateTenantRequest
Response: { data: Tenant }

// Update tenant
PUT /api/Tenants/{id}
Body: UpdateTenantRequest
Response: { data: Tenant }

// Delete tenant
DELETE /api/Tenants/{id}

// Get tenant statistics
GET /api/Tenants/{id}/statistics
Response: { data: TenantStatistics }
```

### Employee Management

```typescript
// Get all employees (current tenant)
GET /api/employees
Query: ?page=1&pageSize=10&department=IT&status=Active
Response: { data: Employee[], totalCount }

// Get employee by ID
GET /api/employees/{id}
Response: { data: Employee }

// Create employee
POST /api/employees
Body: CreateEmployeeRequest
Response: { data: Employee }

// Update employee
PUT /api/employees/{id}
Body: UpdateEmployeeRequest
Response: { data: Employee }

// Delete employee
DELETE /api/employees/{id}

// Upload employee photo
POST /api/employees/{id}/photo
Body: FormData (file)
Response: { data: photoUrl }
```

### Attendance Management

```typescript
// Get attendance records
GET /api/attendance
Query: ?employeeId=123&startDate=2025-11-01&endDate=2025-11-30
Response: { data: AttendanceRecord[] }

// Clock in
POST /api/attendance/clock-in
Body: { employeeId, deviceId?, latitude?, longitude? }
Response: { data: AttendanceRecord }

// Clock out
POST /api/attendance/clock-out
Body: { employeeId, deviceId? }
Response: { data: AttendanceRecord }

// Get attendance summary
GET /api/attendance/summary
Query: ?employeeId=123&month=11&year=2025
Response: { data: AttendanceSummary }
```

### Leave Management

```typescript
// Get leave requests
GET /api/leave
Query: ?employeeId=123&status=Pending
Response: { data: LeaveRequest[] }

// Create leave request
POST /api/leave
Body: { employeeId, leaveTypeId, startDate, endDate, reason }
Response: { data: LeaveRequest }

// Approve leave
POST /api/leave/{id}/approve
Body: { approverComments? }
Response: { data: LeaveRequest }

// Reject leave
POST /api/leave/{id}/reject
Body: { reason }
Response: { data: LeaveRequest }

// Get leave balance
GET /api/leave/balance/{employeeId}
Response: { data: LeaveBalance[] }
```

### Payroll Management

```typescript
// Get payroll runs
GET /api/payroll
Query: ?year=2025&month=11
Response: { data: PayrollRun[] }

// Create payroll run
POST /api/payroll
Body: { month, year, employeeIds[] }
Response: { data: PayrollRun }

// Get payslip
GET /api/payroll/{runId}/payslips/{employeeId}
Response: { data: Payslip }

// Get salary components
GET /api/payroll/salary-components
Response: { data: SalaryComponent[] }
```

### Monitoring (SuperAdmin)

```typescript
// Get system health
GET /api/monitoring/health
Response: { data: HealthMetrics }

// Get API performance
GET /api/monitoring/api-performance
Query: ?startDate=2025-11-01&endDate=2025-11-17
Response: { data: ApiPerformanceMetrics[] }

// Get tenant activity
GET /api/monitoring/tenant-activity
Response: { data: TenantActivityMetrics[] }

// Get security events
GET /api/monitoring/security-events
Query: ?severity=High&startDate=2025-11-01
Response: { data: SecurityEvent[] }
```

---

## Request/Response Patterns

### Pagination

```typescript
// Request
GET /api/employees?page=2&pageSize=25&sortBy=name&sortOrder=asc

// Response
{
  "success": true,
  "data": [
    { id: 26, name: "John Doe", ... },
    { id: 27, name: "Jane Smith", ... }
  ],
  "pagination": {
    "currentPage": 2,
    "pageSize": 25,
    "totalPages": 4,
    "totalCount": 100,
    "hasPreviousPage": true,
    "hasNextPage": true
  }
}

// Service implementation
getPaginatedEmployees(page: number, pageSize: number): Observable<PaginatedResponse<Employee>> {
  const params = new HttpParams()
    .set('page', page)
    .set('pageSize', pageSize);

  return this.http.get<ApiResponse<Employee[]>>(
    `${this.apiUrl}/employees`,
    { params, observe: 'response' }
  ).pipe(
    map(response => ({
      data: response.body?.data || [],
      pagination: JSON.parse(response.headers.get('X-Pagination') || '{}')
    }))
  );
}
```

### Filtering

```typescript
// Request
GET /api/employees?department=IT&status=Active&search=john

// Service implementation
filterEmployees(filters: EmployeeFilters): Observable<Employee[]> {
  let params = new HttpParams();

  if (filters.department) {
    params = params.set('department', filters.department);
  }
  if (filters.status) {
    params = params.set('status', filters.status);
  }
  if (filters.search) {
    params = params.set('search', filters.search);
  }

  return this.http.get<ApiResponse<Employee[]>>(
    `${this.apiUrl}/employees`,
    { params }
  ).pipe(map(response => response.data));
}
```

### Sorting

```typescript
// Request
GET /api/employees?sortBy=lastName&sortOrder=desc

// Service implementation
sortEmployees(sortBy: string, sortOrder: 'asc' | 'desc'): Observable<Employee[]> {
  const params = new HttpParams()
    .set('sortBy', sortBy)
    .set('sortOrder', sortOrder);

  return this.http.get<ApiResponse<Employee[]>>(
    `${this.apiUrl}/employees`,
    { params }
  ).pipe(map(response => response.data));
}
```

### File Upload

```typescript
// Upload single file
uploadFile(file: File, employeeId: number): Observable<string> {
  const formData = new FormData();
  formData.append('file', file, file.name);

  return this.http.post<ApiResponse<string>>(
    `${this.apiUrl}/employees/${employeeId}/documents`,
    formData,
    {
      reportProgress: true,
      observe: 'events'
    }
  ).pipe(
    map(event => {
      if (event.type === HttpEventType.UploadProgress) {
        const progress = Math.round(100 * event.loaded / event.total!);
        console.log(`Upload progress: ${progress}%`);
      } else if (event.type === HttpEventType.Response) {
        return event.body?.data || '';
      }
      return '';
    }),
    filter(url => !!url),
    catchError(this.handleError)
  );
}
```

### Batch Operations

```typescript
// Bulk update
bulkUpdateEmployees(updates: EmployeeUpdate[]): Observable<BatchResult> {
  return this.http.post<ApiResponse<BatchResult>>(
    `${this.apiUrl}/employees/bulk-update`,
    { updates }
  ).pipe(map(response => response.data));
}

// Bulk delete
bulkDeleteEmployees(ids: number[]): Observable<BatchResult> {
  return this.http.delete<ApiResponse<BatchResult>>(
    `${this.apiUrl}/employees/bulk-delete`,
    { body: { ids } }
  ).pipe(map(response => response.data));
}
```

---

## Environment Configuration

### Development Environment

```typescript
// src/environments/environment.development.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5090/api',
  appName: 'HRMS Dev',
  version: '1.0.0-dev',

  // Enable debug features
  enableDebugLogging: true,
  enableMockData: false,

  // API timeout
  apiTimeout: 30000, // 30 seconds
};
```

### Production Environment

```typescript
// src/environments/environment.ts
export const environment = {
  production: true,
  apiUrl: 'https://api.hrms.example.com/api',
  appName: 'HRMS',
  version: '1.0.0',

  // Disable debug features
  enableDebugLogging: false,
  enableMockData: false,

  // API timeout
  apiTimeout: 15000, // 15 seconds
};
```

### Proxy Configuration (Development)

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5090",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

```json
// angular.json
{
  "serve": {
    "options": {
      "proxyConfig": "proxy.conf.json"
    }
  }
}
```

---

## Real-Time Communication

### SignalR Integration

```typescript
/**
 * Real-time attendance updates using SignalR
 */
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class AttendanceRealtimeService {
  private hubConnection?: HubConnection;
  private attendanceUpdates$ = new Subject<AttendanceUpdate>();

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/attendance`, {
        accessTokenFactory: () => localStorage.getItem('access_token') || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('AttendanceUpdated', (data: AttendanceUpdate) => {
      this.attendanceUpdates$.next(data);
    });

    this.startConnection();
  }

  private startConnection(): void {
    this.hubConnection?.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR connection error:', err));
  }

  getAttendanceUpdates(): Observable<AttendanceUpdate> {
    return this.attendanceUpdates$.asObservable();
  }

  disconnect(): void {
    this.hubConnection?.stop();
  }
}

// Component usage
export class AttendanceDashboardComponent implements OnInit, OnDestroy {
  private realtimeService = inject(AttendanceRealtimeService);
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.realtimeService.getAttendanceUpdates()
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        console.log('Real-time attendance update:', update);
        this.refreshAttendanceList();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

---

## Testing API Integration

### Mock HTTP Backend

```typescript
/**
 * Testing with HttpClientTestingModule
 */
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [EmployeeService]
    });

    service = TestBed.inject(EmployeeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Verify no outstanding requests
  });

  it('should get employees', () => {
    const mockEmployees: Employee[] = [
      { id: 1, name: 'John Doe', email: 'john@example.com' },
      { id: 2, name: 'Jane Smith', email: 'jane@example.com' }
    ];

    service.getEmployees().subscribe(employees => {
      expect(employees.length).toBe(2);
      expect(employees).toEqual(mockEmployees);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockEmployees });
  });

  it('should handle error', () => {
    service.getEmployees().subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.message).toContain('Server error');
      }
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    req.flush(
      { message: 'Server error' },
      { status: 500, statusText: 'Internal Server Error' }
    );
  });
});
```

### API Testing Checklist

- [ ] Test successful requests
- [ ] Test error handling (400, 401, 403, 404, 500)
- [ ] Test authentication flow
- [ ] Test token refresh
- [ ] Test pagination
- [ ] Test filtering and sorting
- [ ] Test file uploads
- [ ] Test batch operations
- [ ] Test concurrent requests
- [ ] Test network errors
- [ ] Test timeout handling

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Maintained By**: HRMS Development Team
