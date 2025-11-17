# Developer Guide

> **Fortune 500-Grade HRMS Frontend Developer Documentation**
> Comprehensive guide for developers working on the HRMS Angular application.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Environment Setup](#development-environment-setup)
3. [Project Structure](#project-structure)
4. [Development Workflow](#development-workflow)
5. [Coding Standards](#coding-standards)
6. [Git Workflow](#git-workflow)
7. [Testing Guidelines](#testing-guidelines)
8. [Code Review Checklist](#code-review-checklist)
9. [Common Tasks](#common-tasks)
10. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Prerequisites

- **Node.js**: v18.x or v20.x (LTS recommended)
- **npm**: v9.x or higher
- **Git**: v2.x or higher
- **IDE**: VS Code (recommended) with Angular Language Service extension
- **Chrome**: Latest version (for debugging and Angular DevTools)

### Quick Start

```bash
# Clone the repository
git clone <repository-url>
cd hrms-frontend

# Install dependencies
npm install

# Start development server
npm start

# Open browser to http://localhost:4200
```

---

## Development Environment Setup

### 1. Install Required Tools

```bash
# Install Node.js (using nvm - recommended)
nvm install 20
nvm use 20

# Verify installation
node --version  # Should be v20.x.x
npm --version   # Should be v9.x.x or higher

# Install Angular CLI globally (optional)
npm install -g @angular/cli@20
```

### 2. IDE Setup (VS Code)

#### Recommended Extensions

```json
{
  "recommendations": [
    "angular.ng-template",
    "esbenp.prettier-vscode",
    "dbaeumer.vscode-eslint",
    "johnpapa.angular2",
    "ms-vscode.vscode-typescript-next",
    "bradlc.vscode-tailwindcss"
  ]
}
```

#### VS Code Settings

Create `.vscode/settings.json`:

```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "typescript.tsdk": "node_modules/typescript/lib",
  "typescript.enablePromptUseWorkspaceTsdk": true,
  "[html]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[typescript]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[scss]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  }
}
```

### 3. Environment Configuration

#### Development Environment

Create `src/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5090/api',
  appName: 'HRMS Dev',
  version: '1.0.0-dev',
  superAdminSecretPath: '732c44d0-d59b-494c-9fc0-bf1d65add4e5',

  featureFlags: {
    apiEndpoint: 'http://localhost:5090/api/feature-flags',
    cacheDurationMs: 5 * 60 * 1000,
    autoRefreshIntervalMs: 10 * 60 * 1000,
    enableCaching: true,
    enableDebugLogging: true
  },

  analytics: {
    enabled: true,
    errorRateThreshold: 0.05,
    minSamplesForRollback: 10,
    enableLogging: true,
    enableBackendReporting: false, // Disable in local dev
    batchSize: 50,
    flushIntervalMs: 60000,
    apiEndpoint: 'http://localhost:5090/api/analytics'
  },

  errorTracking: {
    enabled: true,
    errorRateWindowMs: 60000,
    errorRateThreshold: 5,
    maxStoredErrors: 100,
    enableLogging: true,
    autoReportCritical: false // Disable in local dev
  }
};
```

---

## Project Structure

```
hrms-frontend/
├── src/
│   ├── app/
│   │   ├── core/                      # Core functionality (singleton services)
│   │   │   ├── guards/                # Route guards
│   │   │   │   ├── auth.guard.ts
│   │   │   │   ├── role.guard.ts
│   │   │   │   ├── subdomain.guard.ts
│   │   │   │   └── already-logged-in.guard.ts
│   │   │   ├── interceptors/          # HTTP interceptors
│   │   │   │   └── auth.interceptor.ts
│   │   │   ├── models/                # Data models and interfaces
│   │   │   │   ├── user.model.ts
│   │   │   │   ├── tenant.model.ts
│   │   │   │   ├── employee.model.ts
│   │   │   │   └── monitoring.model.ts
│   │   │   └── services/              # Core services
│   │   │       ├── auth.service.ts
│   │   │       ├── tenant.service.ts
│   │   │       ├── employee.service.ts
│   │   │       ├── monitoring.service.ts
│   │   │       └── ...
│   │   │
│   │   ├── features/                   # Feature modules
│   │   │   ├── admin/                 # Super Admin features
│   │   │   │   ├── dashboard/
│   │   │   │   ├── tenant-management/
│   │   │   │   ├── audit-logs/
│   │   │   │   ├── monitoring/
│   │   │   │   └── security-alerts/
│   │   │   ├── tenant/                # Tenant Admin features
│   │   │   │   ├── dashboard/
│   │   │   │   ├── employees/
│   │   │   │   ├── attendance/
│   │   │   │   ├── leave/
│   │   │   │   ├── payroll/
│   │   │   │   └── reports/
│   │   │   ├── employee/              # Employee features
│   │   │   │   ├── dashboard/
│   │   │   │   ├── attendance/
│   │   │   │   ├── leave/
│   │   │   │   └── payslips/
│   │   │   ├── auth/                  # Authentication pages
│   │   │   │   ├── subdomain/
│   │   │   │   ├── login/
│   │   │   │   ├── superadmin/
│   │   │   │   ├── activate/
│   │   │   │   ├── forgot-password/
│   │   │   │   └── reset-password/
│   │   │   └── marketing/             # Public marketing pages
│   │   │       └── landing-page.component.ts
│   │   │
│   │   ├── shared/                     # Shared functionality
│   │   │   ├── ui/                    # UI components
│   │   │   │   └── components/        # 29 custom UI components
│   │   │   │       ├── button/
│   │   │   │       ├── input/
│   │   │   │       ├── datepicker/
│   │   │   │       ├── select/
│   │   │   │       ├── table/
│   │   │   │       └── ...
│   │   │   ├── layouts/               # Layout components
│   │   │   │   ├── admin-layout.component.ts
│   │   │   │   └── tenant-layout.component.ts
│   │   │   ├── pipes/                 # Custom pipes
│   │   │   └── directives/            # Custom directives
│   │   │
│   │   ├── app.routes.ts              # Route configuration
│   │   └── app.config.ts              # App configuration
│   │
│   ├── environments/                   # Environment configs
│   │   ├── environment.ts
│   │   └── environment.development.ts
│   │
│   ├── styles.scss                     # Global styles
│   ├── main.ts                         # Application entry point
│   └── index.html                      # HTML entry point
│
├── public/                             # Static assets
├── node_modules/                       # Dependencies
├── angular.json                        # Angular CLI configuration
├── tsconfig.json                       # TypeScript configuration
├── package.json                        # npm dependencies and scripts
└── README.md                           # Project overview
```

### Directory Naming Conventions

- **core/**: Singleton services, guards, interceptors (never imported into lazy-loaded modules)
- **features/**: Feature-specific components organized by domain
- **shared/**: Reusable components, pipes, directives (can be imported anywhere)

---

## Development Workflow

### Daily Development Cycle

```bash
# 1. Pull latest changes
git checkout main
git pull origin main

# 2. Create feature branch
git checkout -b feature/add-employee-filter

# 3. Start development server
npm start

# 4. Make changes and test locally
# (Edit files, save, browser auto-reloads)

# 5. Run tests
npm test

# 6. Build for production
npm run build

# 7. Commit changes
git add .
git commit -m "feat: Add employee filter functionality"

# 8. Push and create PR
git push origin feature/add-employee-filter
```

### Hot Module Replacement (HMR)

The development server supports HMR - changes auto-reload without full page refresh:

```bash
# Standard dev server (with HMR)
npm start

# Dev server with specific host
ng serve --host 0.0.0.0 --port 4200

# Dev server with SSL (for testing HTTPS)
ng serve --ssl
```

### Building for Production

```bash
# Production build (optimized)
npm run build

# Output location: dist/hrms-frontend/browser/
# Verify bundle sizes in terminal output

# Analyze bundle size
npm run analyze-bundle

# Test production build locally
npx http-server dist/hrms-frontend/browser -p 8080
```

---

## Coding Standards

### TypeScript Standards

#### File Naming

```
# Components
employee-list.component.ts
employee-form.component.ts

# Services
employee.service.ts
auth.service.ts

# Models
user.model.ts
tenant.model.ts

# Guards
auth.guard.ts
role.guard.ts

# Pipes
date-format.pipe.ts
```

#### Component Structure

```typescript
import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';

/**
 * Employee list component displays all employees in a table
 * with search, filter, and pagination capabilities.
 *
 * @example
 * <app-employee-list
 *   [department]="'Engineering'"
 *   (employeeSelected)="onEmployeeSelected($event)">
 * </app-employee-list>
 */
@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss'
})
export class EmployeeListComponent implements OnInit, OnDestroy {
  // 1. Input/Output properties
  @Input() department: string = '';
  @Output() employeeSelected = new EventEmitter<Employee>();

  // 2. Public properties
  employees: Employee[] = [];
  loading: boolean = false;

  // 3. Private properties
  private destroy$ = new Subject<void>();

  // 4. Constructor with dependency injection
  constructor(
    private employeeService: EmployeeService,
    private router: Router
  ) {}

  // 5. Lifecycle hooks
  ngOnInit(): void {
    this.loadEmployees();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // 6. Public methods
  loadEmployees(): void {
    this.loading = true;
    this.employeeService.getEmployees(this.department)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.employees = data;
          this.loading = false;
        },
        error: (error) => {
          console.error('Failed to load employees:', error);
          this.loading = false;
        }
      });
  }

  onRowClick(employee: Employee): void {
    this.employeeSelected.emit(employee);
  }

  // 7. Private methods
  private formatEmployeeName(employee: Employee): string {
    return `${employee.firstName} ${employee.lastName}`;
  }
}
```

#### Service Structure

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, throwError } from 'rxjs';
import { environment } from '@/environments/environment';

/**
 * Service for managing employee data.
 * Handles CRUD operations and business logic for employees.
 */
@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  // Use inject() function for dependency injection (Angular 14+)
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Retrieves all employees for the current tenant
   * @param department Optional department filter
   * @returns Observable of employee array
   */
  getEmployees(department?: string): Observable<Employee[]> {
    const url = `${this.apiUrl}/employees`;
    const params = department ? { department } : {};

    return this.http.get<ApiResponse<Employee[]>>(url, { params }).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * Creates a new employee
   * @param employee Employee data
   * @returns Observable of created employee
   */
  createEmployee(employee: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<ApiResponse<Employee>>(
      `${this.apiUrl}/employees`,
      employee
    ).pipe(
      map(response => response.data),
      catchError(this.handleError)
    );
  }

  /**
   * Global error handler for HTTP requests
   */
  private handleError(error: any): Observable<never> {
    console.error('API Error:', error);
    const message = error.error?.message || 'An error occurred';
    return throwError(() => new Error(message));
  }
}
```

### HTML Template Standards

```html
<!--
  Employee List Component Template
  Displays searchable, filterable employee table
-->

<!-- Loading state -->
@if (loading) {
  <div class="loading-container">
    <app-progress-spinner></app-progress-spinner>
    <p>Loading employees...</p>
  </div>
}

<!-- Empty state -->
@else if (employees.length === 0) {
  <app-card>
    <div class="empty-state">
      <app-icon name="group" size="large"></app-icon>
      <h3>No employees found</h3>
      <p>Get started by adding your first employee</p>
      <app-button variant="primary" (clicked)="addEmployee()">
        Add Employee
      </app-button>
    </div>
  </app-card>
}

<!-- Data table -->
@else {
  <app-card>
    <!-- Search and filters -->
    <div class="table-toolbar">
      <app-input
        placeholder="Search employees..."
        [clearable]="true"
        [(value)]="searchQuery"
        (valueChange)="onSearchChange($event)">
      </app-input>

      <app-select
        label="Department"
        [options]="departmentOptions"
        [clearable]="true"
        [(value)]="selectedDepartment">
      </app-select>

      <app-button variant="primary" (clicked)="addEmployee()">
        <app-icon name="add"></app-icon>
        Add Employee
      </app-button>
    </div>

    <!-- Employee table -->
    <app-table
      [columns]="columns"
      [data]="filteredEmployees"
      [sortKey]="sortKey"
      [sortDirection]="sortDirection"
      [hoverable]="true"
      (rowClick)="onRowClick($event)"
      (sortChange)="onSort($event)">
    </app-table>

    <!-- Pagination -->
    <app-paginator
      [totalItems]="totalEmployees"
      [pageSize]="pageSize"
      [currentPage]="currentPage"
      (pageChange)="onPageChange($event)"
      (pageSizeChange)="onPageSizeChange($event)">
    </app-paginator>
  </app-card>
}
```

#### Template Best Practices

1. **Use @if/@else instead of *ngIf** (Angular 17+)
2. **Use @for instead of *ngFor** (Angular 17+)
3. **Add comments for complex logic**
4. **Keep templates clean** - move logic to component
5. **Use semantic HTML elements**
6. **Proper indentation** (2 spaces)

### SCSS Standards

```scss
// Component styles - scoped to component
:host {
  display: block;
  width: 100%;
}

// Use BEM naming convention
.employee-list {
  padding: 16px;

  &__toolbar {
    display: flex;
    gap: 16px;
    margin-bottom: 24px;
    align-items: flex-end;

    @media (max-width: 768px) {
      flex-direction: column;
      align-items: stretch;
    }
  }

  &__empty-state {
    text-align: center;
    padding: 48px 24px;

    h3 {
      margin: 16px 0 8px;
      color: #333;
    }

    p {
      color: #666;
      margin-bottom: 24px;
    }
  }
}

// Loading container
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;

  p {
    margin-top: 16px;
    color: #666;
  }
}

// Use CSS custom properties for theming
.theme-card {
  background: var(--card-background, #fff);
  border: 1px solid var(--card-border, #e0e0e0);
  border-radius: var(--border-radius, 4px);
  padding: var(--card-padding, 16px);
}
```

### TypeScript Naming Conventions

```typescript
// Classes: PascalCase
export class EmployeeService {}
export class UserModel {}

// Interfaces: PascalCase with 'I' prefix (optional)
export interface Employee {}
export interface ApiResponse<T> {}

// Enums: PascalCase
export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  TenantAdmin = 'TenantAdmin',
  Employee = 'Employee'
}

// Constants: UPPER_SNAKE_CASE
export const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
export const API_TIMEOUT = 30000;

// Functions and methods: camelCase
function calculateTotal() {}
loadEmployeeData() {}

// Variables: camelCase
let employeeCount = 0;
const selectedEmployee = null;

// Private properties: camelCase with underscore prefix
private _internalState = {};
private destroy$ = new Subject<void>();

// Boolean variables: is/has/should prefix
let isLoading = false;
let hasPermission = true;
let shouldValidate = true;

// Observable variables: $ suffix
employees$ = this.employeeService.getEmployees();
loading$ = new BehaviorSubject<boolean>(false);
```

---

## Git Workflow

### Branch Naming

```bash
# Feature branches
feature/add-employee-export
feature/multi-tenant-support

# Bug fixes
fix/login-validation-error
fix/datepicker-timezone-bug

# Hotfixes (production issues)
hotfix/security-token-expiry

# Refactoring
refactor/employee-service-cleanup

# Documentation
docs/update-api-documentation

# Performance improvements
perf/optimize-table-rendering

# Chores (maintenance)
chore/update-dependencies
```

### Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```bash
# Format
<type>(<scope>): <subject>

<body>

<footer>

# Types
feat:     New feature
fix:      Bug fix
docs:     Documentation changes
style:    Formatting, missing semicolons, etc.
refactor: Code refactoring
perf:     Performance improvements
test:     Adding or updating tests
chore:    Maintenance tasks

# Examples
feat(employee): Add employee export to Excel functionality

Implemented Excel export using XLSX library. Includes:
- Column selection
- Data filtering
- Custom formatting

Closes #123

---

fix(auth): Fix token refresh infinite loop

The auth interceptor was retrying failed refresh requests,
causing an infinite loop. Added retry marker to prevent this.

Fixes #456

---

docs(components): Update datepicker API documentation

Added missing input properties and usage examples
```

### Pull Request Process

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/employee-search
   ```

2. **Make Changes and Commit**
   ```bash
   git add .
   git commit -m "feat(employee): Add search functionality"
   ```

3. **Push Branch**
   ```bash
   git push origin feature/employee-search
   ```

4. **Create Pull Request**
   - Go to GitHub/GitLab
   - Click "New Pull Request"
   - Select your branch
   - Fill in PR template:
     ```markdown
     ## Description
     Adds employee search with filters for name, department, and status.

     ## Type of Change
     - [x] New feature
     - [ ] Bug fix
     - [ ] Breaking change
     - [ ] Documentation update

     ## How to Test
     1. Navigate to Employee List
     2. Enter search query in search box
     3. Apply department filter
     4. Verify filtered results

     ## Screenshots
     [Attach screenshots if UI changes]

     ## Checklist
     - [x] Code follows style guidelines
     - [x] Self-review completed
     - [x] Comments added for complex code
     - [x] Tests added/updated
     - [x] Documentation updated
     - [x] No console errors
     ```

5. **Code Review**
   - Address review comments
   - Push additional commits
   - Get approval

6. **Merge**
   - Squash and merge (preferred)
   - Delete branch after merge

---

## Testing Guidelines

### Unit Testing

#### Component Testing

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmployeeListComponent } from './employee-list.component';
import { EmployeeService } from '@/core/services/employee.service';
import { of, throwError } from 'rxjs';

describe('EmployeeListComponent', () => {
  let component: EmployeeListComponent;
  let fixture: ComponentFixture<EmployeeListComponent>;
  let employeeService: jasmine.SpyObj<EmployeeService>;

  beforeEach(async () => {
    // Create service spy
    const employeeServiceSpy = jasmine.createSpyObj('EmployeeService', ['getEmployees']);

    await TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [
        { provide: EmployeeService, useValue: employeeServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EmployeeListComponent);
    component = fixture.componentInstance;
    employeeService = TestBed.inject(EmployeeService) as jasmine.SpyObj<EmployeeService>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load employees on init', () => {
    const mockEmployees = [
      { id: 1, name: 'John Doe', department: 'IT' },
      { id: 2, name: 'Jane Smith', department: 'HR' }
    ];

    employeeService.getEmployees.and.returnValue(of(mockEmployees));

    component.ngOnInit();

    expect(employeeService.getEmployees).toHaveBeenCalled();
    expect(component.employees).toEqual(mockEmployees);
    expect(component.loading).toBe(false);
  });

  it('should handle error when loading employees', () => {
    const error = new Error('API Error');
    employeeService.getEmployees.and.returnValue(throwError(() => error));

    spyOn(console, 'error');

    component.ngOnInit();

    expect(component.loading).toBe(false);
    expect(console.error).toHaveBeenCalled();
  });

  it('should emit event when row is clicked', () => {
    const employee = { id: 1, name: 'John Doe', department: 'IT' };
    spyOn(component.employeeSelected, 'emit');

    component.onRowClick(employee);

    expect(component.employeeSelected.emit).toHaveBeenCalledWith(employee);
  });
});
```

#### Service Testing

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EmployeeService } from './employee.service';
import { environment } from '@/environments/environment';

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

  it('should fetch employees', () => {
    const mockEmployees = [
      { id: 1, name: 'John Doe' },
      { id: 2, name: 'Jane Smith' }
    ];

    service.getEmployees().subscribe(employees => {
      expect(employees.length).toBe(2);
      expect(employees).toEqual(mockEmployees);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockEmployees });
  });

  it('should create employee', () => {
    const newEmployee = { name: 'John Doe', department: 'IT' };
    const createdEmployee = { id: 1, ...newEmployee };

    service.createEmployee(newEmployee).subscribe(employee => {
      expect(employee).toEqual(createdEmployee);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newEmployee);
    req.flush({ success: true, data: createdEmployee });
  });

  it('should handle error', () => {
    service.getEmployees().subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.message).toContain('error');
      }
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/employees`);
    req.flush({ message: 'Server error' }, { status: 500, statusText: 'Server Error' });
  });
});
```

### Running Tests

```bash
# Run all tests
npm test

# Run tests in headless mode (CI)
npm test -- --browsers=ChromeHeadless --watch=false

# Run tests with coverage
npm test -- --code-coverage

# Run specific test file
npm test -- --include='**/employee.service.spec.ts'

# Watch mode (auto-rerun on changes)
npm test -- --watch
```

---

## Code Review Checklist

### Before Submitting PR

- [ ] Code compiles without errors
- [ ] All tests pass
- [ ] No console errors or warnings
- [ ] Code follows style guidelines
- [ ] Complex code has comments
- [ ] No commented-out code
- [ ] No debug statements (console.log)
- [ ] Proper error handling
- [ ] Input validation added
- [ ] Accessibility requirements met
- [ ] Responsive design works
- [ ] Browser compatibility verified

### Code Quality

- [ ] DRY (Don't Repeat Yourself) principle followed
- [ ] Single Responsibility Principle
- [ ] Proper separation of concerns
- [ ] No magic numbers (use constants)
- [ ] Meaningful variable/function names
- [ ] Functions are small and focused
- [ ] Proper TypeScript types (no 'any')
- [ ] RxJS subscriptions cleaned up
- [ ] Memory leaks prevented

### Security

- [ ] No sensitive data in code
- [ ] Input sanitization
- [ ] XSS prevention
- [ ] CSRF tokens used
- [ ] Authentication checks
- [ ] Authorization validated
- [ ] API secrets in environment files

### Performance

- [ ] Lazy loading used where appropriate
- [ ] OnPush change detection considered
- [ ] Large lists virtualized
- [ ] Images optimized
- [ ] Bundle size acceptable
- [ ] No unnecessary API calls

---

## Common Tasks

### Create New Component

```bash
# Generate component
ng generate component features/employee/employee-detail

# Or use shorthand
ng g c features/employee/employee-detail

# Generate standalone component (default in Angular 17+)
ng g c features/employee/employee-detail --standalone

# Generate component with spec file
ng g c features/employee/employee-detail --skip-tests=false
```

### Create New Service

```bash
# Generate service
ng generate service core/services/notification

# Or use shorthand
ng g s core/services/notification
```

### Create New Guard

```bash
# Generate functional guard
ng generate guard core/guards/admin

# Select guard type: CanActivate
```

### Create New Pipe

```bash
# Generate pipe
ng generate pipe shared/pipes/currency-format

# Or use shorthand
ng g p shared/pipes/currency-format
```

### Add New Route

Edit `src/app/app.routes.ts`:

```typescript
export const routes: Routes = [
  {
    path: 'tenant',
    canActivate: [hrGuard],
    loadComponent: () => import('./shared/layouts/tenant-layout.component')
      .then(m => m.TenantLayoutComponent),
    children: [
      {
        path: 'employees/:id/performance',
        loadComponent: () => import('./features/tenant/performance/performance-review.component')
          .then(m => m.PerformanceReviewComponent),
        data: { title: 'Performance Review' }
      }
    ]
  }
];
```

### Add New Environment Variable

1. Edit `src/environments/environment.ts`
2. Add your variable
3. Update `environment.development.ts` with dev value

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5090/api',
  newFeatureEnabled: true, // New variable
  maxUploadSize: 10 * 1024 * 1024 // 10MB
};
```

### Debugging Tips

```typescript
// 1. Use Angular DevTools (Chrome extension)
// Install: https://chrome.google.com/webstore/detail/angular-devtools/

// 2. Debug RxJS streams
import { tap } from 'rxjs';

this.employeeService.getEmployees().pipe(
  tap(data => console.log('Employees loaded:', data)),
  tap(data => debugger) // Breakpoint
).subscribe();

// 3. Debug change detection
import { ChangeDetectorRef } from '@angular/core';

constructor(private cdr: ChangeDetectorRef) {}

someMethod() {
  console.log('Change detection triggered');
  this.cdr.markForCheck();
}

// 4. VS Code breakpoints
// Set breakpoints in VS Code, run in debug mode

// 5. Network inspection
// Use Chrome DevTools Network tab to inspect API calls
```

---

## Troubleshooting

### Common Issues

#### 1. Module Not Found

```bash
Error: Cannot find module '@/core/services/auth.service'

# Solution: Check tsconfig.json paths
{
  "compilerOptions": {
    "paths": {
      "@/*": ["src/app/*"]
    }
  }
}
```

#### 2. Circular Dependency

```bash
Warning: Circular dependency detected

# Solution: Refactor code to remove circular imports
# Use interfaces in separate files
# Use forwardRef() if necessary
```

#### 3. Memory Leak

```bash
# Symptom: Browser becomes slow, high memory usage

# Solution: Unsubscribe from observables
private destroy$ = new Subject<void>();

ngOnDestroy(): void {
  this.destroy$.next();
  this.destroy$.complete();
}

// Use takeUntil
this.service.getData()
  .pipe(takeUntil(this.destroy$))
  .subscribe();
```

#### 4. CORS Error

```bash
Error: Access to XMLHttpRequest blocked by CORS

# Solution 1: Configure backend CORS
# Solution 2: Use proxy in development

# Create proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5090",
    "secure": false,
    "changeOrigin": true
  }
}

# Update angular.json
"serve": {
  "options": {
    "proxyConfig": "proxy.conf.json"
  }
}
```

#### 5. Build Errors

```bash
# Clear cache and rebuild
rm -rf node_modules package-lock.json
npm install
npm run build
```

#### 6. Test Failures

```bash
# Update snapshots
npm test -- --updateSnapshot

# Clear Karma cache
rm -rf .angular/cache
npm test
```

### Getting Help

1. **Check Documentation**: Review this guide and component docs
2. **Search Issues**: Look for similar issues in Git history
3. **Team Chat**: Ask in development channel
4. **Stack Overflow**: Search for Angular-specific questions
5. **Angular Documentation**: https://angular.dev

---

## Resources

### Official Documentation

- [Angular Documentation](https://angular.dev)
- [Angular CLI](https://angular.dev/cli)
- [RxJS Documentation](https://rxjs.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)

### Recommended Reading

- [Angular Style Guide](https://angular.dev/style-guide)
- [RxJS Best Practices](https://rxjs.dev/guide/overview)
- [TypeScript Best Practices](https://www.typescriptlang.org/docs/handbook/declaration-files/do-s-and-don-ts.html)

### Tools

- [Angular DevTools](https://angular.dev/tools/devtools)
- [RxJS Marbles](https://rxmarbles.com/) - Visualize RxJS operators
- [Bundle Analyzer](https://www.npmjs.com/package/webpack-bundle-analyzer)

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Maintained By**: HRMS Development Team
