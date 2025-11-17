# Troubleshooting Guide

> **Fortune 500-Grade HRMS Troubleshooting & Debugging Guide**
> Solutions to common issues and debugging strategies.

## Table of Contents

1. [Common Build Issues](#common-build-issues)
2. [Runtime Errors](#runtime-errors)
3. [API Integration Issues](#api-integration-issues)
4. [Authentication Problems](#authentication-problems)
5. [Performance Issues](#performance-issues)
6. [Development Environment Issues](#development-environment-issues)
7. [Testing Issues](#testing-issues)
8. [Deployment Issues](#deployment-issues)
9. [Browser Compatibility](#browser-compatibility)
10. [Debugging Tools](#debugging-tools)

---

## Common Build Issues

### Issue 1: Module Not Found Error

```bash
Error: Cannot find module '@/core/services/auth.service'
```

**Solution:**

1. Check `tsconfig.json` path aliases:

```json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["src/app/*"]
    }
  }
}
```

2. Restart TypeScript server:
   - VS Code: `Ctrl+Shift+P` → "TypeScript: Restart TS Server"

3. Clear Angular cache:
```bash
rm -rf .angular/cache
npm run build
```

---

### Issue 2: Compilation Errors After Upgrade

```bash
Error: Type 'X' is not assignable to type 'Y'
```

**Solution:**

1. Update all dependencies:
```bash
npm update
```

2. Check for breaking changes in Angular release notes

3. Update TypeScript strict checks:
```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true
  }
}
```

4. Fix type errors incrementally

---

### Issue 3: Circular Dependency Warning

```bash
Warning: Circular dependency detected
```

**Solution:**

1. Identify circular imports:
```bash
npm install -g madge
madge --circular --extensions ts ./src
```

2. Refactor code:
```typescript
// Before (circular)
// service-a.ts
import { ServiceB } from './service-b';

// service-b.ts
import { ServiceA } from './service-a';

// After (resolved)
// Move shared logic to a third service
// shared.service.ts
export class SharedService {}

// service-a.ts
import { SharedService } from './shared.service';

// service-b.ts
import { SharedService } from './shared.service';
```

3. Use interfaces in separate files:
```typescript
// employee.interface.ts
export interface Employee {}

// employee.service.ts
import { Employee } from './employee.interface';
```

---

### Issue 4: Bundle Size Too Large

```bash
Warning: Initial chunk files exceeded maximum budget
```

**Solution:**

1. Analyze bundle:
```bash
npm run build -- --stats-json
npx webpack-bundle-analyzer dist/hrms-frontend/browser/stats.json
```

2. Enable lazy loading:
```typescript
// Before
import { HeavyComponent } from './heavy.component';

// After
loadComponent: () => import('./heavy.component')
  .then(m => m.HeavyComponent)
```

3. Remove unused imports

4. Update budget in `angular.json`:
```json
{
  "budgets": [
    {
      "type": "initial",
      "maximumWarning": "2MB",
      "maximumError": "3MB"
    }
  ]
}
```

---

## Runtime Errors

### Issue 1: ExpressionChangedAfterItHasBeenCheckedError

```bash
Error: ExpressionChangedAfterItHasBeenCheckedError
```

**Solution:**

1. Use `ChangeDetectorRef`:
```typescript
import { ChangeDetectorRef } from '@angular/core';

constructor(private cdr: ChangeDetectorRef) {}

ngAfterViewInit(): void {
  this.someProperty = 'value';
  this.cdr.detectChanges(); // Manually trigger change detection
}
```

2. Use setTimeout:
```typescript
ngAfterViewInit(): void {
  setTimeout(() => {
    this.someProperty = 'value';
  });
}
```

3. Use OnPush change detection:
```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush
})
```

---

### Issue 2: Memory Leaks

**Symptoms:**
- Browser becomes slow over time
- High memory usage
- Application crashes

**Solution:**

1. Unsubscribe from Observables:
```typescript
// Before (memory leak)
ngOnInit(): void {
  this.service.getData().subscribe(data => {
    this.data = data;
  });
}

// After (fixed)
private destroy$ = new Subject<void>();

ngOnInit(): void {
  this.service.getData()
    .pipe(takeUntil(this.destroy$))
    .subscribe(data => {
      this.data = data;
    });
}

ngOnDestroy(): void {
  this.destroy$.next();
  this.destroy$.complete();
}
```

2. Clear timers and intervals:
```typescript
private intervalId?: number;

ngOnInit(): void {
  this.intervalId = window.setInterval(() => {
    // Do something
  }, 1000);
}

ngOnDestroy(): void {
  if (this.intervalId) {
    clearInterval(this.intervalId);
  }
}
```

3. Detach event listeners:
```typescript
ngOnInit(): void {
  window.addEventListener('resize', this.onResize);
}

ngOnDestroy(): void {
  window.removeEventListener('resize', this.onResize);
}

onResize = (): void => {
  // Handle resize
};
```

---

### Issue 3: NullReferenceError

```bash
TypeError: Cannot read property 'X' of null
```

**Solution:**

1. Use optional chaining:
```typescript
// Before
const name = user.profile.name; // Error if user or profile is null

// After
const name = user?.profile?.name; // Safe
```

2. Use nullish coalescing:
```typescript
const name = user?.profile?.name ?? 'Unknown';
```

3. Add null checks:
```typescript
if (user && user.profile && user.profile.name) {
  const name = user.profile.name;
}
```

4. Use @if in templates:
```html
@if (user) {
  <p>{{ user.name }}</p>
}
```

---

## API Integration Issues

### Issue 1: CORS Error

```bash
Access to XMLHttpRequest at 'http://api.example.com' from origin 'http://localhost:4200'
has been blocked by CORS policy
```

**Solution:**

1. **Backend**: Configure CORS in .NET API:
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("AllowAngular");
```

2. **Frontend**: Use proxy for development:
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

```bash
# Start with proxy
ng serve --proxy-config proxy.conf.json
```

---

### Issue 2: 401 Unauthorized Loop

```bash
Multiple 401 errors causing infinite loop
```

**Solution:**

1. Check auth interceptor:
```typescript
// Add retry marker to prevent infinite loops
const RETRY_MARKER = 'X-Retry-Request';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Don't retry if already retried
  if (req.headers.has(RETRY_MARKER)) {
    return next(req).pipe(
      catchError(error => {
        if (error.status === 401) {
          authService.logout(); // Stop retrying
        }
        return throwError(() => error);
      })
    );
  }

  return next(req).pipe(
    catchError(error => {
      if (error.status === 401) {
        // Try refresh once
        return authService.refreshToken().pipe(
          switchMap(() => {
            // Retry with new token
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${authService.getToken()}`,
                [RETRY_MARKER]: 'true'
              }
            });
            return next(retryReq);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
```

2. Check token expiration:
```typescript
private isTokenExpired(token: string): boolean {
  const payload = this.decodeJwt(token);
  const now = Math.floor(Date.now() / 1000);
  return payload.exp < now;
}
```

---

### Issue 3: Request Timeout

```bash
TimeoutError: Request timeout after 30000ms
```

**Solution:**

1. Add timeout operator:
```typescript
this.http.get(url).pipe(
  timeout(15000), // 15 second timeout
  catchError(error => {
    if (error.name === 'TimeoutError') {
      console.error('Request timed out');
    }
    return throwError(() => error);
  })
).subscribe();
```

2. Configure global timeout:
```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withInterceptors([
        (req, next) => next(req).pipe(timeout(30000))
      ])
    )
  ]
};
```

---

### Issue 4: API Response Format Mismatch

```bash
Cannot read property 'data' of undefined
```

**Solution:**

1. Check response structure:
```typescript
// Log full response
this.http.get(url).pipe(
  tap(response => console.log('Full response:', response))
).subscribe();
```

2. Handle different response formats:
```typescript
getEmployees(): Observable<Employee[]> {
  return this.http.get<any>(url).pipe(
    map(response => {
      // Handle different formats
      if (response.data) {
        return response.data; // Format 1: { data: [...] }
      } else if (Array.isArray(response)) {
        return response; // Format 2: [...]
      } else {
        throw new Error('Unexpected response format');
      }
    })
  );
}
```

---

## Authentication Problems

### Issue 1: Token Not Being Sent

**Symptoms:**
- API returns 401
- Token exists in localStorage
- Authorization header missing

**Solution:**

1. Check interceptor registration:
```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withInterceptors([authInterceptor]) // Must be registered
    )
  ]
};
```

2. Verify token retrieval:
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('access_token');
  console.log('Token:', token ? 'Present' : 'Missing');

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
```

---

### Issue 2: User Logged Out Unexpectedly

**Symptoms:**
- User randomly redirected to login
- Token seems valid

**Solution:**

1. Check session timeout:
```typescript
// Disable session timeout for debugging
this.sessionManagement.stopSession();
```

2. Check token expiration:
```typescript
const token = localStorage.getItem('access_token');
if (token) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  const expiresAt = new Date(payload.exp * 1000);
  console.log('Token expires at:', expiresAt);
  console.log('Time until expiration:', expiresAt.getTime() - Date.now(), 'ms');
}
```

3. Check for multiple tabs:
```typescript
// Listen for logout in other tabs
window.addEventListener('storage', (event) => {
  if (event.key === 'access_token' && event.newValue === null) {
    console.log('Logged out in another tab');
    this.authService.logout();
  }
});
```

---

### Issue 3: Guards Not Working

**Symptoms:**
- Unauthorized users can access protected routes
- Guards not being called

**Solution:**

1. Check guard registration:
```typescript
// Correct
{
  path: 'admin',
  canActivate: [superAdminGuard], // Array of guards
  loadComponent: () => import('./admin.component')
}

// Wrong
{
  path: 'admin',
  canActivate: superAdminGuard, // Not an array
  loadComponent: () => import('./admin.component')
}
```

2. Add logging to guards:
```typescript
export const authGuard: CanActivateFn = (route, state) => {
  console.log('Auth guard called for:', state.url);
  const authService = inject(AuthService);
  const isAuthenticated = authService.isAuthenticated();
  console.log('Is authenticated:', isAuthenticated);
  return isAuthenticated;
};
```

---

## Performance Issues

### Issue 1: Slow Initial Load

**Solution:**

1. Enable lazy loading:
```typescript
// Load feature modules on demand
{
  path: 'reports',
  loadComponent: () => import('./reports/reports.component')
    .then(m => m.ReportsComponent)
}
```

2. Optimize images:
```bash
# Install imagemin
npm install imagemin imagemin-webp

# Convert images to WebP
```

3. Enable service worker:
```bash
ng add @angular/pwa
```

---

### Issue 2: Slow Table Rendering

**Solution:**

1. Use virtual scrolling:
```typescript
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Component({
  template: `
    <cdk-virtual-scroll-viewport itemSize="50" class="viewport">
      <div *cdkVirtualFor="let item of items">
        {{ item.name }}
      </div>
    </cdk-virtual-scroll-viewport>
  `
})
```

2. Use OnPush change detection:
```typescript
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmployeeListComponent {}
```

3. Add pagination:
```html
<app-paginator
  [totalItems]="totalItems"
  [pageSize]="pageSize"
  (pageChange)="loadPage($event)">
</app-paginator>
```

---

### Issue 3: High Memory Usage

**Solution:**

1. Use TrackBy in *ngFor:
```html
<!-- Before -->
<div *ngFor="let item of items">{{ item.name }}</div>

<!-- After -->
<div *ngFor="let item of items; trackBy: trackById">{{ item.name }}</div>
```

```typescript
trackById(index: number, item: any): number {
  return item.id;
}
```

2. Unsubscribe from Observables:
```typescript
private destroy$ = new Subject<void>();

ngOnDestroy(): void {
  this.destroy$.next();
  this.destroy$.complete();
}
```

---

## Development Environment Issues

### Issue 1: npm install fails

```bash
npm ERR! code ERESOLVE
npm ERR! ERESOLVE unable to resolve dependency tree
```

**Solution:**

1. Clear npm cache:
```bash
npm cache clean --force
```

2. Delete node_modules and reinstall:
```bash
rm -rf node_modules package-lock.json
npm install
```

3. Use legacy peer deps:
```bash
npm install --legacy-peer-deps
```

---

### Issue 2: ng serve not working

```bash
Port 4200 is already in use
```

**Solution:**

1. Use different port:
```bash
ng serve --port 4300
```

2. Kill process on port 4200:
```bash
# Linux/Mac
lsof -ti:4200 | xargs kill -9

# Windows
netstat -ano | findstr :4200
taskkill /PID <PID> /F
```

---

### Issue 3: Hot reload not working

**Solution:**

1. Check file watchers limit (Linux):
```bash
echo fs.inotify.max_user_watches=524288 | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

2. Restart dev server:
```bash
# Stop server (Ctrl+C)
rm -rf .angular/cache
ng serve
```

---

## Testing Issues

### Issue 1: Tests failing after upgrade

**Solution:**

1. Update test dependencies:
```bash
npm install --save-dev @angular/core@latest @angular/cli@latest
```

2. Clear Karma cache:
```bash
rm -rf .angular/cache
npm test
```

---

### Issue 2: TestBed errors

```bash
NullInjectorError: No provider for HttpClient
```

**Solution:**

```typescript
import { HttpClientTestingModule } from '@angular/common/http/testing';

TestBed.configureTestingModule({
  imports: [
    HttpClientTestingModule,
    ComponentToTest
  ]
});
```

---

## Deployment Issues

### Issue 1: Production build fails

**Solution:**

1. Check for environment-specific code:
```typescript
// Use environment files
import { environment } from '@/environments/environment';
```

2. Enable production mode:
```bash
ng build --configuration production
```

---

### Issue 2: Blank page after deployment

**Solution:**

1. Check base href:
```html
<!-- index.html -->
<base href="/">
```

2. Check server configuration:
```nginx
# nginx.conf
location / {
  try_files $uri $uri/ /index.html;
}
```

---

## Browser Compatibility

### Issue 1: Not working in older browsers

**Solution:**

1. Add polyfills in `polyfills.ts`:
```typescript
import 'core-js/es/array';
import 'core-js/es/object';
import 'core-js/es/promise';
```

2. Update browserslist:
```
# browserslist
last 2 Chrome versions
last 2 Firefox versions
last 2 Safari versions
last 2 Edge versions
```

---

## Debugging Tools

### Angular DevTools

1. Install Chrome extension: [Angular DevTools](https://chrome.google.com/webstore/detail/angular-devtools/)

2. Open DevTools → Angular tab

3. Features:
   - Component tree inspection
   - Change detection profiling
   - Injector tree
   - Performance profiling

### VS Code Debugging

1. Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "chrome",
      "request": "launch",
      "name": "Debug Angular",
      "url": "http://localhost:4200",
      "webRoot": "${workspaceFolder}",
      "sourceMapPathOverrides": {
        "webpack:/*": "${webRoot}/*"
      }
    }
  ]
}
```

2. Set breakpoints in VS Code

3. Press F5 to start debugging

### Network Debugging

```typescript
// Log all HTTP requests
export const loggingInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('Request:', req.method, req.url);
  console.log('Headers:', req.headers.keys());
  console.log('Body:', req.body);

  return next(req).pipe(
    tap(response => {
      console.log('Response:', response);
    }),
    catchError(error => {
      console.error('Error:', error);
      return throwError(() => error);
    })
  );
};
```

### Performance Profiling

```typescript
// Measure component initialization time
export class MyComponent implements OnInit {
  ngOnInit(): void {
    console.time('Component Init');
    // Component logic
    console.timeEnd('Component Init');
  }
}
```

---

## Getting Help

1. **Search Documentation**: Check this guide and other docs
2. **Console Logs**: Check browser console for errors
3. **Network Tab**: Inspect API requests/responses
4. **Angular DevTools**: Use component inspector
5. **Stack Overflow**: Search for similar issues
6. **Team Chat**: Ask team members
7. **GitHub Issues**: Search Angular repository

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Maintained By**: HRMS Development Team
