# Performance Optimization Plan - MorisHR HRMS
**Fortune 500 Enterprise Implementation Guide**

**Version:** 1.0
**Date:** November 17, 2025
**Target Completion:** 4-6 weeks
**Expected Performance Improvement:** 50-70%

---

## Table of Contents

1. [Optimization Roadmap](#optimization-roadmap)
2. [Phase 1: Critical Optimizations](#phase-1-critical-optimizations)
3. [Phase 2: Medium Impact Optimizations](#phase-2-medium-impact-optimizations)
4. [Phase 3: Long-term Enhancements](#phase-3-long-term-enhancements)
5. [Implementation Details](#implementation-details)
6. [Testing & Validation](#testing--validation)
7. [Success Metrics](#success-metrics)

---

## Optimization Roadmap

### Timeline Overview

```
Week 1-2:  Phase 1 - Critical optimizations (OnPush, trackBy)
Week 3-4:  Phase 2 - @defer, async pipes, caching
Week 5-6:  Phase 3 - Monitoring, CI integration, documentation
```

### Resource Requirements

- **Frontend Developers:** 2 developers
- **QA Engineers:** 1 tester
- **DevOps Engineer:** 0.5 FTE (for CI setup)
- **Total Effort:** 15-20 developer days

---

## Phase 1: Critical Optimizations (Week 1-2)

**Target:** 35-55% performance improvement
**Priority:** 🔴 CRITICAL
**Effort:** 4-6 days

### 1.1 Implement OnPush Change Detection Strategy

**Objective:** Reduce change detection overhead by 30-50%

**Implementation Steps:**

#### Step 1: Create Migration Script

```typescript
// scripts/migrate-to-onpush.ts
import { Project } from 'ts-morph';

const project = new Project({
  tsConfigFilePath: 'tsconfig.json'
});

const sourceFiles = project.getSourceFiles('src/app/**/*.component.ts');

sourceFiles.forEach(file => {
  const classes = file.getClasses();

  classes.forEach(cls => {
    const decorator = cls.getDecorator('Component');
    if (!decorator) return;

    const args = decorator.getArguments()[0];
    if (args && args.getKind() === SyntaxKind.ObjectLiteralExpression) {
      const obj = args as ObjectLiteralExpression;

      // Check if changeDetection already exists
      const cdProp = obj.getProperty('changeDetection');
      if (cdProp) return;

      // Add import
      file.addImportDeclaration({
        moduleSpecifier: '@angular/core',
        namedImports: ['ChangeDetectionStrategy']
      });

      // Add changeDetection property
      obj.addPropertyAssignment({
        name: 'changeDetection',
        initializer: 'ChangeDetectionStrategy.OnPush'
      });
    }
  });

  file.saveSync();
});

console.log(`✅ Migrated ${sourceFiles.length} components to OnPush`);
```

#### Step 2: Manual Component Updates

**For each component, follow this pattern:**

```typescript
// BEFORE
import { Component } from '@angular/core';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.scss']
})
export class EmployeeListComponent {
  employees: Employee[] = [];

  loadEmployees() {
    this.employeeService.getEmployees().subscribe(data => {
      this.employees = data;
    });
  }
}
```

```typescript
// AFTER
import { Component, ChangeDetectionStrategy, signal } from '@angular/core';

@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html',
  styleUrls: ['./employee-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush  // ✅ Added
})
export class EmployeeListComponent {
  employees = signal<Employee[]>([]);  // ✅ Use signal

  loadEmployees() {
    this.employeeService.getEmployees().subscribe(data => {
      this.employees.set(data);  // ✅ Trigger change detection
    });
  }
}
```

#### Step 3: Update Templates

```html
<!-- BEFORE -->
<div *ngFor="let employee of employees">
  {{ employee.name }}
</div>

<!-- AFTER -->
<div *ngFor="let employee of employees()">
  {{ employee.name }}
</div>
```

#### Step 4: Priority Component List (Top 20)

```
High Traffic Components:
1. ✅ employee-list.component.ts
2. ✅ tenant-dashboard.component.ts
3. ✅ admin-dashboard.component.ts
4. ✅ attendance-dashboard.component.ts
5. ✅ comprehensive-employee-form.component.ts
6. ✅ tenant-list.component.ts
7. ✅ department-list.component.ts
8. ✅ biometric-device-list.component.ts
9. ✅ payroll-dashboard.component.ts
10. ✅ reports-dashboard.component.ts
11. ✅ audit-logs.component.ts
12. ✅ security-alerts-dashboard.component.ts
13. ✅ monitoring-dashboard.component.ts
14. ✅ billing-overview.component.ts
15. ✅ subscription-dashboard.component.ts
16. ✅ location-list.component.ts
17. ✅ timesheet-list.component.ts
18. ✅ leave-dashboard.component.ts
19. ✅ salary-components.component.ts
20. ✅ employee-dashboard.component.ts
```

**Validation Checklist:**
- [ ] Component builds without errors
- [ ] UI updates correctly on data changes
- [ ] Forms remain interactive
- [ ] No performance regressions

---

### 1.2 Add trackBy Functions to All Lists

**Objective:** Improve list rendering performance by 40-60%

**Implementation Steps:**

#### Step 1: Create trackBy Helper Service

```typescript
// src/app/core/utils/track-by.utils.ts
export class TrackByUtils {
  /**
   * Generic trackBy function for entities with 'id' property
   */
  static byId<T extends { id: any }>(index: number, item: T): any {
    return item.id;
  }

  /**
   * trackBy for arrays with custom property
   */
  static byProperty<T>(property: keyof T) {
    return (index: number, item: T): any => item[property];
  }

  /**
   * trackBy by index (use only for static lists)
   */
  static byIndex(index: number): number {
    return index;
  }

  /**
   * Custom trackBy for nested properties
   */
  static byNestedProperty<T>(path: string) {
    return (index: number, item: T): any => {
      return path.split('.').reduce((obj, key) => obj?.[key], item);
    };
  }
}
```

#### Step 2: Update Component Classes

```typescript
// BEFORE
export class EmployeeListComponent {
  employees = signal<Employee[]>([]);
}
```

```typescript
// AFTER
import { TrackByUtils } from '@core/utils/track-by.utils';

export class EmployeeListComponent {
  employees = signal<Employee[]>([]);

  // ✅ Add trackBy function
  trackByEmployeeId = TrackByUtils.byId;

  // Or inline:
  trackByEmployeeId = (index: number, employee: Employee) => employee.id;
}
```

#### Step 3: Update Templates

```html
<!-- BEFORE: No trackBy -->
@for (employee of employees(); track employee) {
  <tr>
    <td>{{ employee.name }}</td>
    <td>{{ employee.email }}</td>
  </tr>
}

<!-- AFTER: With trackBy -->
@for (employee of employees(); track employee.id) {
  <tr>
    <td>{{ employee.name }}</td>
    <td>{{ employee.email }}</td>
  </tr>
}
```

```html
<!-- For *ngFor (legacy) -->
<!-- BEFORE -->
<tr *ngFor="let employee of employees()">
  <td>{{ employee.name }}</td>
</tr>

<!-- AFTER -->
<tr *ngFor="let employee of employees(); trackBy: trackByEmployeeId">
  <td>{{ employee.name }}</td>
</tr>
```

#### Step 4: Automated Detection Script

```bash
# scripts/find-missing-trackby.sh
#!/bin/bash

echo "🔍 Finding @for loops without trackBy..."

# Find all @for loops
grep -r "@for" src/app --include="*.html" | \
  grep -v "track.*\.id" | \
  grep -v "track.*\..*" | \
  awk '{print $1}' | \
  sed 's/:$//' | \
  sort | uniq

echo ""
echo "🔍 Finding *ngFor without trackBy..."

# Find all *ngFor without trackBy
grep -r "\*ngFor" src/app --include="*.html" | \
  grep -v "trackBy" | \
  awk '{print $1}' | \
  sed 's/:$//' | \
  sort | uniq
```

**Priority Files (Top 26):**

```
1. ✅ employee-list.component.html
2. ✅ tenant-list.component.html
3. ✅ department-list.component.html
4. ✅ attendance-dashboard.component.html (6 loops)
5. ✅ tenant-dashboard.component.html (7 loops)
6. ✅ audit-logs.component.html
7. ✅ alert-list.component.html (5 loops)
8. ✅ security-alerts-dashboard.component.html
9. ✅ api-performance.component.html (3 loops)
10. ✅ infrastructure-health.component.html (2 loops)
11. ✅ monitoring-dashboard.component.html
12. ✅ salary-components.component.html (3 loops)
13. ✅ tenant-form.component.html (3 loops)
14. ✅ biometric-device-form.component.html (3 loops)
15. ✅ payslip-detail.component.html (2 loops)
16. ✅ leave-component.html (2 loops)
17. ✅ table.html (5 loops)
18. ✅ datepicker.html (4 loops)
19. ✅ select.html
20. ✅ autocomplete.html
21. ✅ tabs.html
22. ✅ stepper.html
23. ✅ menu.html
24. ✅ paginator.html
25. ✅ toast-container.html
26. ✅ radio-group.html
```

---

### 1.3 Fix Sass Deprecation Warnings

**Objective:** Prepare for Dart Sass 3.0, eliminate build warnings

**Implementation Steps:**

#### Step 1: Install Sass Migrator

```bash
npm install -g sass-migrator
```

#### Step 2: Run Automated Migration

```bash
# Migrate @import to @use
sass-migrator module \
  --migrate-deps \
  src/styles/**/*.scss \
  src/app/**/*.scss

# Migrate darken/lighten to color.adjust
sass-migrator color \
  --migrate-deps \
  src/styles/**/*.scss \
  src/app/**/*.scss
```

#### Step 3: Manual Fixes

```scss
// BEFORE
@import '../../../../../styles/index';

$hover-color: darken($primary-color, 10%);
$light-color: lighten($primary-color, 20%);
```

```scss
// AFTER
@use '../../../../../styles' as *;
@use 'sass:color';

$hover-color: color.adjust($primary-color, $lightness: -10%);
$light-color: color.adjust($primary-color, $lightness: 20%);
```

#### Step 4: Update Common Patterns

**Pattern 1: Color Functions**
```scss
// BEFORE
background: linear-gradient(135deg, $success-color 0%, darken($success-color, 10%) 100%);

// AFTER
@use 'sass:color';
background: linear-gradient(
  135deg,
  $success-color 0%,
  color.adjust($success-color, $lightness: -10%) 100%
);
```

**Pattern 2: Import Chains**
```scss
// BEFORE: src/styles/_index.scss
@import 'typography';
@import 'colors';
@import 'spacing';

// AFTER
@forward 'typography';
@forward 'colors';
@forward 'spacing';
```

---

## Phase 2: Medium Impact Optimizations (Week 3-4)

**Target:** 15-30% additional improvement
**Priority:** 🟡 MEDIUM
**Effort:** 6-8 days

### 2.1 Implement @defer for Heavy Components

**Objective:** Reduce initial bundle load by 20-30%

#### Step 1: Identify Deferrable Components

**Criteria:**
- Below the fold content
- Charts and heavy visualizations
- Dashboard widgets not immediately visible
- Admin panels with complex forms

**Priority List:**
```typescript
High Priority:
1. Chart.js components (attendance-dashboard, monitoring-dashboard)
2. Complex forms (comprehensive-employee-form)
3. Data tables with 50+ rows
4. Dashboard widgets (analytics cards)
5. Admin panels (audit logs, security alerts)
```

#### Step 2: Implement @defer Blocks

```html
<!-- BEFORE -->
<div class="dashboard-row">
  <app-attendance-chart [data]="attendanceData()" />
  <app-leave-chart [data]="leaveData()" />
</div>
```

```html
<!-- AFTER: Defer until viewport -->
<div class="dashboard-row">
  @defer (on viewport) {
    <app-attendance-chart [data]="attendanceData()" />
  } @placeholder {
    <div class="chart-skeleton">
      <div class="skeleton-header"></div>
      <div class="skeleton-body"></div>
    </div>
  } @loading (minimum 500ms) {
    <div class="chart-loading">
      <mat-spinner diameter="40"></mat-spinner>
      <p>Loading chart...</p>
    </div>
  }

  @defer (on viewport) {
    <app-leave-chart [data]="leaveData()" />
  } @placeholder {
    <div class="chart-skeleton"></div>
  }
</div>
```

#### Step 3: Defer Strategies

**1. Viewport Strategy (Most Common)**
```html
@defer (on viewport) {
  <app-heavy-component />
}
```

**2. Interaction Strategy**
```html
@defer (on interaction) {
  <app-admin-panel />
} @placeholder {
  <button>Click to load admin panel</button>
}
```

**3. Idle Strategy**
```html
@defer (on idle) {
  <app-analytics-widget />
}
```

**4. Timer Strategy**
```html
@defer (on timer(2s)) {
  <app-secondary-content />
}
```

**5. Hybrid Strategy**
```html
@defer (on viewport; on timer(5s)) {
  <app-dashboard-widget />
}
```

#### Step 4: Create Loading Skeletons

```scss
// src/styles/_skeletons.scss
.skeleton {
  background: linear-gradient(
    90deg,
    var(--color-surface) 25%,
    var(--color-border) 50%,
    var(--color-surface) 75%
  );
  background-size: 200% 100%;
  animation: loading 1.5s ease-in-out infinite;
}

@keyframes loading {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.chart-skeleton {
  height: 300px;
  border-radius: var(--radius-md);

  .skeleton-header {
    @extend .skeleton;
    height: 40px;
    margin-bottom: 16px;
  }

  .skeleton-body {
    @extend .skeleton;
    height: 240px;
  }
}
```

---

### 2.2 Replace Manual Subscriptions with Async Pipe

**Objective:** Eliminate memory leaks, simplify code

#### Step 1: Identify Subscription Patterns

```bash
# Find manual subscriptions
grep -r "\.subscribe(" src/app --include="*.ts" | wc -l
# Result: 199 subscriptions
```

#### Step 2: Refactor Pattern

```typescript
// BEFORE: Manual subscription
export class EmployeeListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  employees: Employee[] = [];
  loading = false;

  ngOnInit() {
    this.loading = true;
    this.employeeService.getEmployees()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.employees = data;
          this.loading = false;
        },
        error: err => {
          console.error(err);
          this.loading = false;
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
```

```typescript
// AFTER: Async pipe + signals
export class EmployeeListComponent {
  // Expose observable directly
  employees$ = this.employeeService.getEmployees().pipe(
    shareReplay(1)  // Cache result
  );

  // Or use toSignal
  employees = toSignal(this.employeeService.getEmployees(), {
    initialValue: []
  });
}
```

```html
<!-- Template with async pipe -->
@if (employees$ | async; as employees) {
  @for (employee of employees; track employee.id) {
    <app-employee-card [employee]="employee" />
  }
} @else {
  <p>Loading...</p>
}

<!-- Or with signal -->
@for (employee of employees(); track employee.id) {
  <app-employee-card [employee]="employee" />
}
```

#### Step 3: Handle Complex Scenarios

**Scenario 1: Multiple Observables**

```typescript
// BEFORE
ngOnInit() {
  forkJoin({
    employees: this.employeeService.getEmployees(),
    departments: this.departmentService.getDepartments()
  })
  .pipe(takeUntil(this.destroy$))
  .subscribe(({ employees, departments }) => {
    this.employees = employees;
    this.departments = departments;
  });
}
```

```typescript
// AFTER
data = toSignal(
  forkJoin({
    employees: this.employeeService.getEmployees(),
    departments: this.departmentService.getDepartments()
  }),
  { initialValue: null }
);
```

**Scenario 2: Dependent Requests**

```typescript
// BEFORE
loadEmployee(id: string) {
  this.employeeService.getEmployee(id)
    .pipe(
      switchMap(employee =>
        this.departmentService.getDepartment(employee.departmentId)
      ),
      takeUntil(this.destroy$)
    )
    .subscribe(department => {
      this.department = department;
    });
}
```

```typescript
// AFTER
employeeId = signal<string>('');

department = toSignal(
  toObservable(this.employeeId).pipe(
    filter(id => !!id),
    switchMap(id => this.employeeService.getEmployee(id)),
    switchMap(employee =>
      this.departmentService.getDepartment(employee.departmentId)
    )
  )
);
```

---

### 2.3 Optimize Chart.js Bundle

**Objective:** Reduce bundle size by ~50KB

#### Step 1: Tree-shake Chart.js

```typescript
// BEFORE: Import entire Chart.js
import Chart from 'chart.js/auto';
```

```typescript
// AFTER: Import only needed components
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  Title,
  Tooltip,
  Legend,
  CategoryScale
} from 'chart.js';

// Register only what you need
Chart.register(
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  CategoryScale,
  Title,
  Tooltip,
  Legend
);
```

#### Step 2: Lazy Load Charts

```typescript
// src/app/core/utils/chart-loader.ts
export async function loadChartJs() {
  const { Chart, ...components } = await import('chart.js');
  Chart.register(...Object.values(components));
  return Chart;
}
```

```typescript
// In component
@defer (on viewport) {
  <app-chart-component />
}

// chart.component.ts
async ngOnInit() {
  this.Chart = await loadChartJs();
  this.renderChart();
}
```

#### Step 3: Replace ng2-charts

```typescript
// BEFORE: Using ng2-charts wrapper
import { BaseChartDirective } from 'ng2-charts';

@Component({
  imports: [BaseChartDirective],
  template: `
    <canvas baseChart [data]="chartData" [type]="'line'"></canvas>
  `
})
```

```typescript
// AFTER: Direct Chart.js integration
@Component({
  template: `
    <canvas #chartCanvas></canvas>
  `
})
export class ChartComponent implements AfterViewInit {
  @ViewChild('chartCanvas') canvas!: ElementRef<HTMLCanvasElement>;

  async ngAfterViewInit() {
    const Chart = await loadChartJs();
    new Chart(this.canvas.nativeElement, {
      type: 'line',
      data: this.chartData,
      options: this.chartOptions
    });
  }
}
```

---

### 2.4 Implement HTTP Caching Layer

**Objective:** Reduce duplicate API calls by 50-70%

#### Step 1: Create Cache Interceptor

```typescript
// src/app/core/interceptors/cache.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { of, tap, share } from 'rxjs';

interface CacheEntry {
  response: HttpResponse<any>;
  timestamp: number;
}

@Injectable()
export class CacheInterceptor implements HttpInterceptor {
  private cache = new Map<string, CacheEntry>();
  private pendingRequests = new Map<string, Observable<HttpEvent<any>>>();

  // Cache duration in milliseconds
  private readonly CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

  intercept(req: HttpRequest<any>, next: HttpHandler) {
    // Only cache GET requests
    if (req.method !== 'GET') {
      return next.handle(req);
    }

    // Check for cache bypass header
    if (req.headers.has('X-Skip-Cache')) {
      return next.handle(req);
    }

    const cacheKey = this.getCacheKey(req);

    // Return cached response if valid
    const cached = this.cache.get(cacheKey);
    if (cached && this.isCacheValid(cached)) {
      return of(cached.response.clone());
    }

    // Return pending request if exists (prevent duplicate calls)
    const pending = this.pendingRequests.get(cacheKey);
    if (pending) {
      return pending;
    }

    // Make request and cache
    const request$ = next.handle(req).pipe(
      tap(event => {
        if (event instanceof HttpResponse) {
          this.cache.set(cacheKey, {
            response: event,
            timestamp: Date.now()
          });
          this.pendingRequests.delete(cacheKey);
        }
      }),
      share()
    );

    this.pendingRequests.set(cacheKey, request$);
    return request$;
  }

  private getCacheKey(req: HttpRequest<any>): string {
    return `${req.method}:${req.urlWithParams}`;
  }

  private isCacheValid(entry: CacheEntry): boolean {
    return Date.now() - entry.timestamp < this.CACHE_DURATION;
  }

  // Public method to clear cache
  clearCache() {
    this.cache.clear();
  }

  // Clear specific cache entry
  clearCacheFor(url: string) {
    this.cache.delete(`GET:${url}`);
  }
}
```

#### Step 2: Configure Cache Service

```typescript
// src/app/core/services/cache.service.ts
@Injectable({ providedIn: 'root' })
export class CacheService {
  constructor(private cacheInterceptor: CacheInterceptor) {}

  // Clear all cache
  clearAll() {
    this.cacheInterceptor.clearCache();
  }

  // Clear cache for specific endpoint
  invalidate(url: string) {
    this.cacheInterceptor.clearCacheFor(url);
  }

  // Bypass cache for specific request
  bypassCache(req: HttpRequest<any>): HttpRequest<any> {
    return req.clone({
      headers: req.headers.set('X-Skip-Cache', 'true')
    });
  }
}
```

#### Step 3: Register Interceptor

```typescript
// src/app/app.config.ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { CacheInterceptor } from './core/interceptors/cache.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withInterceptors([CacheInterceptor])
    )
  ]
};
```

#### Step 4: Cache Invalidation Strategy

```typescript
// In services that modify data
export class EmployeeService {
  constructor(
    private http: HttpClient,
    private cacheService: CacheService
  ) {}

  updateEmployee(id: string, data: Employee) {
    return this.http.put(`/api/employees/${id}`, data).pipe(
      tap(() => {
        // Invalidate relevant cache entries
        this.cacheService.invalidate('/api/employees');
        this.cacheService.invalidate(`/api/employees/${id}`);
      })
    );
  }
}
```

---

## Phase 3: Long-term Enhancements (Week 5-6)

**Target:** Establish continuous performance monitoring
**Priority:** 🟢 LOW (but essential for maintenance)
**Effort:** 4-5 days

### 3.1 Set Up Lighthouse CI

**Objective:** Automated performance testing in CI/CD pipeline

#### Step 1: Install Dependencies

```bash
npm install --save-dev @lhci/cli lighthouse
```

#### Step 2: Configure Lighthouse CI

```javascript
// lighthouserc.js
module.exports = {
  ci: {
    collect: {
      startServerCommand: 'npm run serve:dist',
      url: ['http://localhost:4200'],
      numberOfRuns: 3,
      settings: {
        preset: 'desktop',
        chromeFlags: '--no-sandbox --disable-gpu',
        onlyCategories: ['performance', 'accessibility', 'best-practices']
      }
    },
    assert: {
      assertions: {
        'categories:performance': ['error', { minScore: 0.9 }],
        'categories:accessibility': ['error', { minScore: 0.9 }],
        'first-contentful-paint': ['error', { maxNumericValue: 1500 }],
        'largest-contentful-paint': ['error', { maxNumericValue: 2500 }],
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'total-blocking-time': ['error', { maxNumericValue: 200 }],
        'interactive': ['error', { maxNumericValue: 3500 }]
      }
    },
    upload: {
      target: 'temporary-public-storage'
      // Or configure your own storage:
      // target: 'lhci',
      // serverBaseUrl: 'https://lhci.yourcompany.com'
    }
  }
};
```

#### Step 3: Add NPM Scripts

```json
{
  "scripts": {
    "build:prod": "ng build --configuration production",
    "serve:dist": "npx http-server dist/hrms-frontend/browser -p 4200",
    "lighthouse": "lhci autorun",
    "lighthouse:local": "lighthouse http://localhost:4200 --view"
  }
}
```

#### Step 4: Integrate with CI/CD

```yaml
# .github/workflows/lighthouse-ci.yml
name: Lighthouse CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  lighthouse:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'

      - name: Install dependencies
        run: npm ci

      - name: Build production
        run: npm run build:prod

      - name: Run Lighthouse CI
        run: npm run lighthouse
        env:
          LHCI_GITHUB_APP_TOKEN: ${{ secrets.LHCI_GITHUB_APP_TOKEN }}

      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: lighthouse-results
          path: .lighthouseci
```

---

### 3.2 Implement Bundle Size Tracking

**Objective:** Monitor bundle size changes over time

#### Step 1: Create Bundle Analyzer Script

```javascript
// scripts/analyze-bundle.js
const { existsSync, readFileSync, writeFileSync } = require('fs');
const { execSync } = require('child_process');
const path = require('path');

class BundleAnalyzer {
  constructor() {
    this.distPath = path.join(__dirname, '../dist/hrms-frontend/browser');
    this.historyPath = path.join(__dirname, '../performance-history.json');
  }

  analyze() {
    if (!existsSync(this.distPath)) {
      throw new Error('Build dist folder not found. Run build first.');
    }

    const files = this.getFiles(this.distPath);
    const metrics = {
      timestamp: new Date().toISOString(),
      commit: this.getCommitHash(),
      branch: this.getBranch(),
      files: files,
      totals: this.calculateTotals(files)
    };

    this.saveHistory(metrics);
    this.generateReport(metrics);

    return metrics;
  }

  getFiles(dir) {
    const files = execSync(`find ${dir} -type f -name "*.js" -o -name "*.css"`)
      .toString()
      .split('\n')
      .filter(Boolean);

    return files.map(file => {
      const stats = require('fs').statSync(file);
      const gzipSize = this.getGzipSize(file);

      return {
        name: path.basename(file),
        path: path.relative(this.distPath, file),
        size: stats.size,
        gzipSize: gzipSize,
        compressionRatio: ((1 - gzipSize / stats.size) * 100).toFixed(2)
      };
    });
  }

  getGzipSize(file) {
    const content = readFileSync(file);
    const zlib = require('zlib');
    return zlib.gzipSync(content).length;
  }

  calculateTotals(files) {
    return {
      totalSize: files.reduce((sum, f) => sum + f.size, 0),
      totalGzipSize: files.reduce((sum, f) => sum + f.gzipSize, 0),
      jsFiles: files.filter(f => f.name.endsWith('.js')).length,
      cssFiles: files.filter(f => f.name.endsWith('.css')).length
    };
  }

  getCommitHash() {
    try {
      return execSync('git rev-parse --short HEAD').toString().trim();
    } catch {
      return 'unknown';
    }
  }

  getBranch() {
    try {
      return execSync('git branch --show-current').toString().trim();
    } catch {
      return 'unknown';
    }
  }

  saveHistory(metrics) {
    let history = [];

    if (existsSync(this.historyPath)) {
      history = JSON.parse(readFileSync(this.historyPath, 'utf8'));
    }

    history.push(metrics);

    // Keep only last 100 entries
    if (history.length > 100) {
      history = history.slice(-100);
    }

    writeFileSync(this.historyPath, JSON.stringify(history, null, 2));
  }

  generateReport(metrics) {
    console.log('\n📦 Bundle Size Report\n');
    console.log(`Commit: ${metrics.commit}`);
    console.log(`Branch: ${metrics.branch}`);
    console.log(`Date:   ${metrics.timestamp}\n`);

    console.log('Total Bundle Size:');
    console.log(`  Raw:    ${this.formatBytes(metrics.totals.totalSize)}`);
    console.log(`  Gzip:   ${this.formatBytes(metrics.totals.totalGzipSize)}`);
    console.log(`  Ratio:  ${((1 - metrics.totals.totalGzipSize / metrics.totals.totalSize) * 100).toFixed(2)}%\n`);

    console.log('Largest Files:');
    const largest = metrics.files
      .sort((a, b) => b.size - a.size)
      .slice(0, 10);

    largest.forEach(file => {
      console.log(`  ${file.name.padEnd(30)} ${this.formatBytes(file.size).padStart(10)} → ${this.formatBytes(file.gzipSize).padStart(10)}`);
    });

    this.checkBudgets(metrics);
  }

  checkBudgets(metrics) {
    const budgets = {
      totalSize: 2 * 1024 * 1024,      // 2MB
      totalGzipSize: 600 * 1024,       // 600KB
      maxFileSize: 500 * 1024,         // 500KB
      maxFileGzipSize: 200 * 1024      // 200KB
    };

    console.log('\n🎯 Budget Status:\n');

    const checks = [
      {
        name: 'Total Raw Size',
        value: metrics.totals.totalSize,
        budget: budgets.totalSize,
        pass: metrics.totals.totalSize < budgets.totalSize
      },
      {
        name: 'Total Gzip Size',
        value: metrics.totals.totalGzipSize,
        budget: budgets.totalGzipSize,
        pass: metrics.totals.totalGzipSize < budgets.totalGzipSize
      }
    ];

    checks.forEach(check => {
      const status = check.pass ? '✅' : '❌';
      const percent = ((check.value / check.budget) * 100).toFixed(0);
      console.log(`  ${status} ${check.name}: ${this.formatBytes(check.value)} / ${this.formatBytes(check.budget)} (${percent}%)`);
    });

    // Check individual file budgets
    const oversized = metrics.files.filter(f =>
      f.size > budgets.maxFileSize || f.gzipSize > budgets.maxFileGzipSize
    );

    if (oversized.length > 0) {
      console.log('\n⚠️  Oversized Files:');
      oversized.forEach(file => {
        console.log(`  ${file.name}: ${this.formatBytes(file.size)} (gzip: ${this.formatBytes(file.gzipSize)})`);
      });
    }

    // Exit with error if budgets exceeded
    if (!checks.every(c => c.pass) || oversized.length > 0) {
      process.exit(1);
    }
  }

  formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  }
}

// Run analysis
const analyzer = new BundleAnalyzer();
analyzer.analyze();
```

#### Step 2: Add to CI Pipeline

```json
{
  "scripts": {
    "analyze": "node scripts/analyze-bundle.js",
    "build:analyze": "npm run build:prod && npm run analyze"
  }
}
```

---

### 3.3 Create Performance Monitoring Dashboard

**Objective:** Visualize performance trends over time

#### Step 1: Generate Performance Report

```typescript
// scripts/generate-performance-report.ts
import { readFileSync, writeFileSync } from 'fs';
import { join } from 'path';

interface PerformanceMetric {
  timestamp: string;
  commit: string;
  bundle: {
    size: number;
    gzipSize: number;
  };
  lighthouse?: {
    performance: number;
    fcp: number;
    lcp: number;
    tbt: number;
  };
}

class PerformanceReporter {
  private historyPath = join(__dirname, '../performance-history.json');

  generateReport() {
    const history: PerformanceMetric[] = JSON.parse(
      readFileSync(this.historyPath, 'utf8')
    );

    const report = this.buildMarkdownReport(history);
    writeFileSync('PERFORMANCE_REPORT.md', report);

    console.log('✅ Performance report generated: PERFORMANCE_REPORT.md');
  }

  buildMarkdownReport(history: PerformanceMetric[]): string {
    const latest = history[history.length - 1];
    const previous = history.length > 1 ? history[history.length - 2] : null;

    let markdown = `# Performance Report\n\n`;
    markdown += `**Generated:** ${new Date().toISOString()}\n`;
    markdown += `**Commit:** ${latest.commit}\n\n`;

    // Current metrics
    markdown += `## Current Metrics\n\n`;
    markdown += `| Metric | Value | Change |\n`;
    markdown += `|--------|-------|--------|\n`;

    const metrics = [
      {
        name: 'Bundle Size',
        value: this.formatBytes(latest.bundle.size),
        change: previous ? this.calculateChange(
          latest.bundle.size,
          previous.bundle.size
        ) : 'N/A'
      },
      {
        name: 'Gzip Size',
        value: this.formatBytes(latest.bundle.gzipSize),
        change: previous ? this.calculateChange(
          latest.bundle.gzipSize,
          previous.bundle.gzipSize
        ) : 'N/A'
      }
    ];

    metrics.forEach(m => {
      markdown += `| ${m.name} | ${m.value} | ${m.change} |\n`;
    });

    // Trend chart (last 30 days)
    markdown += `\n## Bundle Size Trend\n\n`;
    markdown += this.generateAsciiChart(history.slice(-30));

    return markdown;
  }

  calculateChange(current: number, previous: number): string {
    const diff = current - previous;
    const percent = ((diff / previous) * 100).toFixed(2);
    const icon = diff > 0 ? '📈' : diff < 0 ? '📉' : '➡️';
    const sign = diff > 0 ? '+' : '';

    return `${icon} ${sign}${percent}%`;
  }

  formatBytes(bytes: number): string {
    return (bytes / 1024).toFixed(2) + ' KB';
  }

  generateAsciiChart(history: PerformanceMetric[]): string {
    // Simple ASCII chart generation
    const sizes = history.map(h => h.bundle.gzipSize);
    const max = Math.max(...sizes);
    const min = Math.min(...sizes);

    let chart = '```\n';

    sizes.forEach((size, i) => {
      const normalized = ((size - min) / (max - min)) * 50;
      const bar = '█'.repeat(Math.round(normalized));
      const date = new Date(history[i].timestamp).toLocaleDateString();
      chart += `${date} ${bar} ${this.formatBytes(size)}\n`;
    });

    chart += '```\n';

    return chart;
  }
}

const reporter = new PerformanceReporter();
reporter.generateReport();
```

---

## Implementation Details

### Code Review Checklist

Before merging optimization PRs, ensure:

**OnPush Migration:**
- [ ] `changeDetection: ChangeDetectionStrategy.OnPush` added
- [ ] All inputs are immutable or signals
- [ ] Component updates correctly on data changes
- [ ] No console errors in browser
- [ ] Forms remain interactive

**trackBy Functions:**
- [ ] trackBy function defined for all lists
- [ ] Uses entity ID or unique property
- [ ] Template updated with trackBy syntax
- [ ] List rendering works correctly

**@defer Blocks:**
- [ ] Placeholder UI implemented
- [ ] Loading state handled
- [ ] Component loads correctly
- [ ] No performance regression

**Async Pipe:**
- [ ] Manual subscriptions removed
- [ ] Observable exposed in template
- [ ] No memory leaks
- [ ] Error handling in place

---

## Testing & Validation

### Performance Testing Protocol

#### 1. Build Performance

```bash
# Test build time
time npm run build:prod

# Expected: < 40 seconds
```

#### 2. Bundle Size Validation

```bash
# Analyze bundle
npm run analyze

# Check budgets
# - Total gzipped < 600KB
# - Largest chunk < 200KB
```

#### 3. Runtime Performance

```bash
# Run Lighthouse
npm run lighthouse

# Expected scores:
# - Performance: > 90
# - FCP: < 1.5s
# - LCP: < 2.5s
# - TTI: < 3.5s
# - TBT: < 200ms
```

#### 4. Manual Testing

**Test Scenarios:**

1. **List Rendering**
   - Navigate to employee list
   - Add/remove/update employees
   - Verify smooth scrolling
   - Check memory usage in DevTools

2. **Dashboard Loading**
   - Navigate to dashboard
   - Verify widgets load progressively
   - Check Network tab for duplicate requests
   - Verify charts defer correctly

3. **Form Performance**
   - Open complex employee form
   - Type in various fields
   - Verify no lag in typing
   - Check change detection cycles

4. **Navigation**
   - Navigate between routes
   - Verify smooth transitions
   - Check lazy loading in Network tab

---

## Success Metrics

### Before vs After Comparison

| Metric | Before | Target | Success Criteria |
|--------|--------|--------|------------------|
| **Performance Score** | 83 | 95+ | ✅ > 90 |
| **Build Time** | 34s | 35s | ✅ < 40s |
| **Initial Bundle** | 185KB | 140KB | ✅ < 150KB |
| **FCP** | 1.2s | 0.8s | ✅ < 1.0s |
| **LCP** | 2.1s | 1.5s | ✅ < 1.8s |
| **TTI** | 3.2s | 2.2s | ✅ < 2.5s |
| **TBT** | 250ms | 120ms | ✅ < 150ms |
| **OnPush Adoption** | 3% | 100% | ✅ > 95% |
| **trackBy Usage** | 3% | 100% | ✅ > 95% |
| **Memory Leaks** | Medium | None | ✅ Zero leaks |

### Key Performance Indicators (KPIs)

**Week 1-2:**
- [ ] 50+ components migrated to OnPush
- [ ] 40+ trackBy functions added
- [ ] 0 Sass deprecation warnings

**Week 3-4:**
- [ ] 20+ @defer blocks implemented
- [ ] 100+ subscriptions eliminated
- [ ] HTTP caching interceptor active

**Week 5-6:**
- [ ] Lighthouse CI integrated
- [ ] Bundle tracking automated
- [ ] Performance dashboard live

---

## Risk Mitigation

### Potential Issues & Solutions

**Issue 1: OnPush breaks existing functionality**

**Solution:**
- Migrate component by component
- Test thoroughly after each migration
- Use `ChangeDetectorRef.markForCheck()` if needed
- Roll back if issues persist

**Issue 2: @defer causes layout shifts**

**Solution:**
- Implement proper placeholder dimensions
- Use skeleton loaders with exact sizes
- Test CLS score after each implementation

**Issue 3: HTTP caching causes stale data**

**Solution:**
- Implement cache invalidation strategy
- Add cache-busting for mutations
- Provide manual refresh option

**Issue 4: Team resistance to changes**

**Solution:**
- Provide training sessions
- Document patterns clearly
- Show before/after performance metrics
- Get stakeholder buy-in early

---

## Rollout Strategy

### Phased Rollout Plan

**Phase 1: Internal Testing (Week 1-2)**
- Deploy to dev environment
- Internal QA testing
- Performance baseline measurement

**Phase 2: Beta Testing (Week 3-4)**
- Deploy to staging
- Select user group testing
- Gather feedback
- Performance monitoring

**Phase 3: Production Rollout (Week 5)**
- 10% traffic (Day 1-2)
- 25% traffic (Day 3-4)
- 50% traffic (Day 5-6)
- 100% traffic (Day 7+)

**Rollback Plan:**
- Keep previous version deployed
- Monitor error rates
- Automatic rollback if error rate > 1%
- Manual rollback trigger available

---

## Maintenance & Monitoring

### Ongoing Performance Maintenance

**Daily:**
- [ ] Monitor Lighthouse CI scores
- [ ] Check bundle size reports
- [ ] Review error logs

**Weekly:**
- [ ] Analyze performance trends
- [ ] Review slow API endpoints
- [ ] Update performance dashboard

**Monthly:**
- [ ] Comprehensive performance audit
- [ ] Review and tighten budgets
- [ ] Team performance review

---

## Conclusion

This optimization plan provides a **comprehensive, Fortune 500-grade approach** to improving the MorisHR HRMS application performance. By following this plan systematically, the team can achieve:

- **50-70% performance improvement**
- **A+ (95+) Lighthouse score**
- **World-class user experience**
- **Sustainable performance culture**

**Next Steps:**
1. Review this plan with the team
2. Assign responsibilities
3. Create tracking board
4. Begin Phase 1 implementation

**Success Depends On:**
- Team commitment
- Thorough testing
- Continuous monitoring
- Stakeholder support

---

**Document Owner:** Performance Engineering Team
**Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** APPROVED FOR IMPLEMENTATION
