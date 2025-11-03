# HRMS Frontend - Angular 18

Modern Angular 18 frontend with standalone components, signals, and Material Design.

## 🚀 Quick Start

### Prerequisites
- Node.js 18+ and npm 9+
- Angular CLI 18+

### Installation

```bash
cd frontend

# Install Angular CLI globally
npm install -g @angular/cli@latest

# Install dependencies
npm install

# Start Admin Portal (http://localhost:4200)
npm run start:admin

# Start Tenant Portal (http://localhost:4201)
npm run start:tenant
```

### Development Servers

| Portal | URL | Port | Purpose |
|--------|-----|------|---------|
| Admin Panel | http://localhost:4200 | 4200 | Super admin tenant management |
| Tenant Portal | http://localhost:4201 | 4201 | HR managers and employees |

## 📁 Project Structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/                           # Core services (singleton)
│   │   │   ├── services/
│   │   │   │   ├── auth.service.ts        # Authentication & JWT
│   │   │   │   ├── tenant.service.ts      # Tenant context
│   │   │   │   ├── api.service.ts         # HTTP base service
│   │   │   │   └── storage.service.ts     # Local storage wrapper
│   │   │   ├── guards/
│   │   │   │   ├── auth.guard.ts          # Route protection
│   │   │   │   └── role.guard.ts          # Role-based access
│   │   │   ├── interceptors/
│   │   │   │   ├── jwt.interceptor.ts     # Add JWT to requests
│   │   │   │   ├── tenant.interceptor.ts  # Add tenant context
│   │   │   │   └── error.interceptor.ts   # Global error handling
│   │   │   └── models/
│   │   │       ├── user.model.ts
│   │   │       ├── tenant.model.ts
│   │   │       └── api-response.model.ts
│   │   │
│   │   ├── shared/                         # Shared components & utilities
│   │   │   ├── components/
│   │   │   │   ├── loading/               # Loading spinner
│   │   │   │   ├── confirm-dialog/        # Confirmation dialogs
│   │   │   │   └── toast/                 # Toast notifications
│   │   │   ├── pipes/
│   │   │   │   ├── date-format.pipe.ts
│   │   │   │   └── currency-format.pipe.ts
│   │   │   └── directives/
│   │   │       └── has-role.directive.ts
│   │   │
│   │   ├── features/                       # Feature modules
│   │   │   ├── admin/                     # Super Admin Portal
│   │   │   │   ├── login/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── tenants/
│   │   │   │   │   ├── tenant-list/
│   │   │   │   │   ├── tenant-create/
│   │   │   │   │   ├── tenant-edit/
│   │   │   │   │   └── tenant-detail/
│   │   │   │   └── sectors/
│   │   │   │
│   │   │   ├── hr-manager/                # HR Manager Portal
│   │   │   │   ├── login/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── employees/
│   │   │   │   │   ├── employee-list/
│   │   │   │   │   ├── employee-create/
│   │   │   │   │   ├── employee-detail/
│   │   │   │   │   └── employee-edit/
│   │   │   │   ├── attendance/
│   │   │   │   │   ├── daily-register/
│   │   │   │   │   ├── monthly-register/
│   │   │   │   │   ├── mark-attendance/
│   │   │   │   │   └── overtime-report/
│   │   │   │   ├── leave/
│   │   │   │   │   ├── leave-applications/
│   │   │   │   │   ├── leave-approvals/
│   │   │   │   │   ├── leave-balance/
│   │   │   │   │   └── leave-calendar/
│   │   │   │   ├── payroll/
│   │   │   │   │   ├── payroll-cycles/
│   │   │   │   │   ├── process-payroll/
│   │   │   │   │   ├── payslips/
│   │   │   │   │   └── reports/
│   │   │   │   └── reports/
│   │   │   │       ├── dashboard-reports/
│   │   │   │       ├── payroll-reports/
│   │   │   │       └── custom-reports/
│   │   │   │
│   │   │   └── employee/                  # Employee Self-Service
│   │   │       ├── login/
│   │   │       ├── dashboard/
│   │   │       ├── my-attendance/
│   │   │       ├── my-leave/
│   │   │       ├── my-payslips/
│   │   │       └── my-profile/
│   │   │
│   │   ├── layouts/                        # Layout components
│   │   │   ├── admin-layout/
│   │   │   ├── tenant-layout/
│   │   │   └── employee-layout/
│   │   │
│   │   └── app.routes.ts                   # App routing (standalone)
│   │
│   ├── assets/
│   │   ├── images/
│   │   ├── icons/
│   │   └── styles/
│   │       ├── _variables.scss
│   │       ├── _mixins.scss
│   │       └── _material-theme.scss
│   │
│   ├── environments/
│   │   ├── environment.ts                  # Development
│   │   ├── environment.admin.ts            # Admin portal
│   │   ├── environment.tenant.ts           # Tenant portal
│   │   └── environment.prod.ts             # Production
│   │
│   ├── index.html
│   ├── main.ts                             # Bootstrap (standalone)
│   └── styles.scss                         # Global styles
│
├── angular.json                            # Angular CLI config
├── tsconfig.json                           # TypeScript config
├── package.json                            # Dependencies
└── README.md                               # This file
```

## 🎨 Tech Stack

### Core
- **Angular 18** - Latest stable version
- **TypeScript 5.4** - Type safety
- **RxJS 7.8** - Reactive programming

### UI Framework
- **Angular Material 18** - Material Design components
- **Angular CDK** - Component dev kit
- **Chart.js + ng2-charts** - Data visualization

### State Management
- **Signals** - Angular 18 reactive primitives
- **RxJS** - Complex async operations

### Features
- **Standalone Components** - No NgModules
- **Built-in Control Flow** - @if, @for, @switch
- **Deferred Loading** - @defer for lazy loading
- **PWA** - Service Worker support
- **Responsive** - Mobile-first design

## 🔧 Configuration

### Environment Files

Each environment file configures the API URL and tenant context:

**environment.ts** (Development):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  tenantType: 'admin' // or 'tenant' or 'employee'
};
```

**environment.prod.ts** (Production):
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.hrms.mu/api',
  tenantType: 'admin'
};
```

### Angular Material Theme

Custom theme in `src/assets/styles/_material-theme.scss`:

```scss
@use '@angular/material' as mat;

$primary: mat.define-palette(mat.$blue-palette);
$accent: mat.define-palette(mat.$orange-palette);
$warn: mat.define-palette(mat.$red-palette);

$theme: mat.define-light-theme((
  color: (
    primary: $primary,
    accent: $accent,
    warn: $warn,
  ),
  typography: mat.define-typography-config(),
  density: 0,
));

@include mat.all-component-themes($theme);
```

## 🛣️ Routing

### Admin Portal Routes

```typescript
const adminRoutes: Routes = [
  { path: 'login', component: AdminLoginComponent },
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: AdminDashboardComponent },
      { path: 'tenants', loadComponent: () => import('./tenants/tenant-list.component') },
      { path: 'tenants/create', loadComponent: () => import('./tenants/tenant-create.component') },
      { path: 'tenants/:id', loadComponent: () => import('./tenants/tenant-detail.component') },
    ]
  }
];
```

### Tenant Portal Routes

```typescript
const tenantRoutes: Routes = [
  { path: 'login', component: TenantLoginComponent },
  {
    path: '',
    component: TenantLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: HrDashboardComponent },
      { path: 'employees', loadChildren: () => import('./employees/employees.routes') },
      { path: 'attendance', loadChildren: () => import('./attendance/attendance.routes') },
      { path: 'leave', loadChildren: () => import('./leave/leave.routes') },
      { path: 'payroll', loadChildren: () => import('./payroll/payroll.routes') },
      { path: 'reports', loadChildren: () => import('./reports/reports.routes') },
    ]
  }
];
```

## 🔐 Authentication Flow

### 1. Login

```typescript
// User enters credentials
this.authService.login(email, password).subscribe({
  next: (response) => {
    // JWT token stored in localStorage
    // User redirected to dashboard
  },
  error: (error) => {
    // Show error message
  }
});
```

### 2. JWT Interceptor

```typescript
// Automatically adds Authorization header to all requests
Authorization: Bearer <token>
```

### 3. Tenant Interceptor

```typescript
// Adds tenant context to requests
X-Tenant-ID: <tenant-id>
```

### 4. Auth Guard

```typescript
// Protects routes, redirects to login if not authenticated
canActivate(): boolean {
  if (!this.authService.isAuthenticated()) {
    this.router.navigate(['/login']);
    return false;
  }
  return true;
}
```

## 🎨 UI Patterns

### Using Signals (Angular 18)

```typescript
import { signal, computed } from '@angular/core';

export class EmployeeListComponent {
  // Reactive state with signals
  employees = signal<Employee[]>([]);
  searchTerm = signal<string>('');

  // Computed value
  filteredEmployees = computed(() => {
    const term = this.searchTerm().toLowerCase();
    return this.employees().filter(emp =>
      emp.firstName.toLowerCase().includes(term) ||
      emp.lastName.toLowerCase().includes(term)
    );
  });

  // Update signal
  addEmployee(employee: Employee) {
    this.employees.update(emps => [...emps, employee]);
  }
}
```

### Control Flow (@if, @for)

```html
<!-- Modern built-in control flow -->
@if (loading()) {
  <app-loading-spinner />
} @else if (employees().length === 0) {
  <div class="empty-state">No employees found</div>
} @else {
  <div class="employee-grid">
    @for (employee of filteredEmployees(); track employee.id) {
      <app-employee-card [employee]="employee" />
    }
  </div>
}
```

### Deferred Loading (@defer)

```html
<!-- Lazy load heavy components -->
@defer (on viewport) {
  <app-employee-chart />
} @placeholder {
  <div class="chart-skeleton"></div>
} @loading (minimum 500ms) {
  <mat-spinner />
}
```

## 📱 Responsive Design

### Breakpoints

```scss
$breakpoints: (
  xs: 0,
  sm: 600px,
  md: 960px,
  lg: 1280px,
  xl: 1920px
);

@mixin responsive($breakpoint) {
  @media (min-width: map-get($breakpoints, $breakpoint)) {
    @content;
  }
}
```

### Usage

```scss
.employee-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;

  @include responsive(md) {
    grid-template-columns: repeat(2, 1fr);
  }

  @include responsive(lg) {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

## 🧪 Testing

### Unit Tests

```bash
npm run test
```

### E2E Tests

```bash
npm run e2e
```

## 📦 Build & Deploy

### Development Build

```bash
npm run build
```

### Production Build

```bash
npm run build:prod
```

### Deploy to Netlify

```bash
# Install Netlify CLI
npm install -g netlify-cli

# Deploy
netlify deploy --prod --dir=dist/hrms-frontend/browser
```

## 🚢 Deployment Checklist

- [ ] Update environment.prod.ts with production API URL
- [ ] Enable PWA in angular.json
- [ ] Configure CORS in backend for frontend domain
- [ ] Set up SSL certificate
- [ ] Configure subdomain routing (*.hrms.mu)
- [ ] Enable Google Analytics (optional)
- [ ] Set up error tracking (Sentry, optional)

## 🎯 Next Steps

### Immediate (This Session)
1. ✅ Project setup
2. ✅ Core services & interceptors
3. ✅ Auth guard
4. ✅ Login component
5. ✅ Admin dashboard
6. ⏳ Tenant management

### Short-term (Next 20 hours)
1. Employee module (list, create, edit, detail)
2. Attendance module (register, mark, overtime)
3. Leave module (apply, approve, calendar)
4. Payroll module (cycles, payslips, reports)
5. Reports module (dashboard, filters, export)

### Long-term (Next 10 hours)
1. Employee self-service portal
2. Mobile responsive optimization
3. PWA features (offline mode, push notifications)
4. Advanced charts and analytics
5. Performance optimization
6. E2E testing

## 📚 Resources

- [Angular Docs](https://angular.dev)
- [Angular Material](https://material.angular.io)
- [RxJS Docs](https://rxjs.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs)

## 🐛 Troubleshooting

### Common Issues

**Issue:** `Cannot find module '@angular/core'`
**Fix:** Run `npm install`

**Issue:** `Port 4200 already in use`
**Fix:** Kill existing process or use different port: `ng serve --port 4300`

**Issue:** `CORS error`
**Fix:** Ensure backend CORS is configured to allow frontend origin

## 📄 License

MIT License - See backend LICENSE file

---

**Status:** 🚧 In Development
**Version:** 1.0.0
**Last Updated:** November 1, 2025
