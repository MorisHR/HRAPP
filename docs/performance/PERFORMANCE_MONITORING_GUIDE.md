# Performance Monitoring Guide - MorisHR HRMS
**Continuous Performance Tracking & Alerting**

**Version:** 1.0
**Date:** November 17, 2025
**Audience:** DevOps, SRE, Development Team

---

## Table of Contents

1. [Overview](#overview)
2. [Monitoring Stack](#monitoring-stack)
3. [Key Metrics](#key-metrics)
4. [Automated Monitoring](#automated-monitoring)
5. [Real-time Monitoring](#real-time-monitoring)
6. [Performance Budgets](#performance-budgets)
7. [Alerting & Escalation](#alerting--escalation)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Performance Dashboard](#performance-dashboard)
10. [Reporting](#reporting)

---

## Overview

### Purpose

This guide establishes a **comprehensive performance monitoring system** for the MorisHR HRMS application, ensuring:

- Continuous performance tracking
- Early detection of regressions
- Data-driven optimization decisions
- SLA compliance monitoring
- User experience protection

### Performance SLAs

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| **Lighthouse Performance** | ≥ 95 | < 90 | < 80 |
| **First Contentful Paint (FCP)** | < 1.0s | > 1.5s | > 2.0s |
| **Largest Contentful Paint (LCP)** | < 1.8s | > 2.5s | > 4.0s |
| **Time to Interactive (TTI)** | < 2.5s | > 3.5s | > 5.0s |
| **Total Blocking Time (TBT)** | < 150ms | > 200ms | > 300ms |
| **Cumulative Layout Shift (CLS)** | < 0.05 | > 0.1 | > 0.25 |
| **Bundle Size (Gzipped)** | < 150KB | > 200KB | > 300KB |
| **API Response Time (P95)** | < 200ms | > 500ms | > 1000ms |

---

## Monitoring Stack

### 1. Build-time Monitoring

**Tool:** Webpack Bundle Analyzer + Custom Scripts

```json
{
  "scripts": {
    "analyze": "node scripts/analyze-bundle.js",
    "build:analyze": "ng build --stats-json && webpack-bundle-analyzer dist/hrms-frontend/stats.json"
  }
}
```

**Metrics Tracked:**
- Bundle size (raw & gzipped)
- Chunk sizes
- Dependency sizes
- Tree-shaking effectiveness

### 2. CI/CD Monitoring

**Tool:** Lighthouse CI

```yaml
# .github/workflows/performance-check.yml
name: Performance Check

on:
  pull_request:
  push:
    branches: [main, develop]

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Lighthouse CI
        run: lhci autorun
      - name: Check bundle size
        run: npm run analyze
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: performance-results
          path: .lighthouseci/
```

### 3. Runtime Monitoring

**Tool:** Google Analytics 4 + Custom Events

```typescript
// src/app/core/services/performance-monitor.service.ts
import { Injectable } from '@angular/core';

interface PerformanceMetrics {
  fcp: number;
  lcp: number;
  fid: number;
  cls: number;
  ttfb: number;
}

@Injectable({ providedIn: 'root' })
export class PerformanceMonitorService {
  constructor() {
    if (typeof window !== 'undefined') {
      this.observeWebVitals();
    }
  }

  private observeWebVitals() {
    // First Contentful Paint
    this.observeFCP();

    // Largest Contentful Paint
    this.observeLCP();

    // First Input Delay
    this.observeFID();

    // Cumulative Layout Shift
    this.observeCLS();

    // Time to First Byte
    this.observeTTFB();
  }

  private observeFCP() {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (entry.name === 'first-contentful-paint') {
          this.reportMetric('FCP', entry.startTime);
        }
      }
    });
    observer.observe({ entryTypes: ['paint'] });
  }

  private observeLCP() {
    const observer = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      const lastEntry = entries[entries.length - 1];
      this.reportMetric('LCP', lastEntry.startTime);
    });
    observer.observe({ entryTypes: ['largest-contentful-paint'] });
  }

  private observeFID() {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        this.reportMetric('FID', entry.processingStart - entry.startTime);
      }
    });
    observer.observe({ entryTypes: ['first-input'] });
  }

  private observeCLS() {
    let clsValue = 0;
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (!(entry as any).hadRecentInput) {
          clsValue += (entry as any).value;
        }
      }
      this.reportMetric('CLS', clsValue);
    });
    observer.observe({ entryTypes: ['layout-shift'] });
  }

  private observeTTFB() {
    const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
    if (navigation) {
      const ttfb = navigation.responseStart - navigation.requestStart;
      this.reportMetric('TTFB', ttfb);
    }
  }

  private reportMetric(name: string, value: number) {
    // Send to Google Analytics
    if (typeof gtag !== 'undefined') {
      gtag('event', 'web_vitals', {
        event_category: 'Performance',
        event_label: name,
        value: Math.round(value),
        non_interaction: true
      });
    }

    // Send to custom backend
    this.sendToBackend(name, value);

    // Log in console (dev only)
    if (!environment.production) {
      console.log(`[Performance] ${name}: ${value.toFixed(2)}ms`);
    }
  }

  private sendToBackend(metric: string, value: number) {
    // Send to your monitoring service
    fetch('/api/metrics/performance', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        metric,
        value,
        url: window.location.pathname,
        timestamp: Date.now(),
        userAgent: navigator.userAgent
      })
    }).catch(() => {
      // Fail silently to not impact user experience
    });
  }
}
```

### 4. Production Monitoring

**Tool:** Grafana + Prometheus

```yaml
# prometheus/performance-metrics.yml
scrape_configs:
  - job_name: 'frontend-performance'
    static_configs:
      - targets: ['app.morishr.com']
    metrics_path: '/api/metrics'
    scrape_interval: 60s
```

---

## Key Metrics

### 1. Core Web Vitals

#### First Contentful Paint (FCP)

**Definition:** Time when first content appears on screen

**Measurement:**
```typescript
const fcp = performance.getEntriesByName('first-contentful-paint')[0];
console.log('FCP:', fcp.startTime);
```

**Targets:**
- Good: < 1.0s
- Needs Improvement: 1.0s - 2.0s
- Poor: > 2.0s

**Optimization Factors:**
- Server response time
- Render-blocking resources
- Critical CSS inlining
- Font loading strategy

#### Largest Contentful Paint (LCP)

**Definition:** Time when largest content element appears

**Measurement:**
```typescript
new PerformanceObserver((list) => {
  const entries = list.getEntries();
  const lastEntry = entries[entries.length - 1];
  console.log('LCP:', lastEntry.startTime);
}).observe({ entryTypes: ['largest-contentful-paint'] });
```

**Targets:**
- Good: < 1.8s
- Needs Improvement: 1.8s - 4.0s
- Poor: > 4.0s

**Optimization Factors:**
- Image optimization
- Lazy loading strategy
- Server response time
- Resource prioritization

#### Cumulative Layout Shift (CLS)

**Definition:** Total of all unexpected layout shifts

**Measurement:**
```typescript
let clsValue = 0;
new PerformanceObserver((list) => {
  for (const entry of list.getEntries()) {
    if (!entry.hadRecentInput) {
      clsValue += entry.value;
    }
  }
  console.log('CLS:', clsValue);
}).observe({ entryTypes: ['layout-shift'] });
```

**Targets:**
- Good: < 0.05
- Needs Improvement: 0.05 - 0.25
- Poor: > 0.25

**Optimization Factors:**
- Fixed dimensions for images/videos
- No dynamically injected content above viewport
- Proper font loading with font-display
- Stable skeleton loaders

### 2. Custom Performance Metrics

#### Route Change Performance

```typescript
// src/app/core/services/route-performance.service.ts
@Injectable({ providedIn: 'root' })
export class RoutePerformanceService {
  constructor(private router: Router) {
    this.trackRouteChanges();
  }

  private trackRouteChanges() {
    let routeStart: number;

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        routeStart = performance.now();
      }

      if (event instanceof NavigationEnd) {
        const duration = performance.now() - routeStart;
        this.reportRouteChange(event.url, duration);
      }
    });
  }

  private reportRouteChange(url: string, duration: number) {
    gtag('event', 'route_change', {
      event_category: 'Navigation',
      event_label: url,
      value: Math.round(duration),
      non_interaction: true
    });
  }
}
```

#### Component Render Time

```typescript
// src/app/shared/decorators/track-performance.decorator.ts
export function TrackPerformance(componentName: string) {
  return function (target: any) {
    const originalInit = target.prototype.ngOnInit;
    const originalAfterViewInit = target.prototype.ngAfterViewInit;

    target.prototype.ngOnInit = function () {
      performance.mark(`${componentName}-init-start`);
      const result = originalInit?.apply(this, arguments);
      performance.mark(`${componentName}-init-end`);

      performance.measure(
        `${componentName}-init`,
        `${componentName}-init-start`,
        `${componentName}-init-end`
      );

      return result;
    };

    target.prototype.ngAfterViewInit = function () {
      performance.mark(`${componentName}-render-end`);

      performance.measure(
        `${componentName}-render`,
        `${componentName}-init-start`,
        `${componentName}-render-end`
      );

      const measure = performance.getEntriesByName(`${componentName}-render`)[0];
      if (measure) {
        console.log(`${componentName} render time:`, measure.duration);
      }

      return originalAfterViewInit?.apply(this, arguments);
    };
  };
}

// Usage:
@Component({...})
@TrackPerformance('EmployeeListComponent')
export class EmployeeListComponent {}
```

#### API Performance Monitoring

```typescript
// src/app/core/interceptors/performance.interceptor.ts
@Injectable()
export class PerformanceInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const started = Date.now();

    return next.handle(req).pipe(
      finalize(() => {
        const duration = Date.now() - started;

        // Report slow requests
        if (duration > 1000) {
          console.warn(`Slow API call: ${req.url} (${duration}ms)`);
        }

        // Send to analytics
        gtag('event', 'api_call', {
          event_category: 'API Performance',
          event_label: req.url,
          value: duration,
          non_interaction: true
        });
      })
    );
  }
}
```

---

## Automated Monitoring

### 1. Pre-commit Hooks

```bash
# .husky/pre-commit
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

# Check TypeScript compilation
echo "🔍 Checking TypeScript..."
npm run type-check

# Run linter
echo "🔍 Running linter..."
npm run lint

# Check for common performance anti-patterns
echo "🔍 Checking for performance issues..."
node scripts/check-performance-patterns.js
```

```javascript
// scripts/check-performance-patterns.js
const { execSync } = require('child_process');

console.log('Checking for performance anti-patterns...\n');

const checks = [
  {
    name: 'Template function calls',
    pattern: /\{\{.*\(.*\).*\}\}/,
    files: 'src/**/*.html',
    severity: 'warning'
  },
  {
    name: '*ngFor without trackBy',
    pattern: /\*ngFor.*(?!trackBy)/,
    files: 'src/**/*.html',
    severity: 'warning'
  },
  {
    name: 'Manual subscriptions without takeUntil',
    pattern: /\.subscribe\(/,
    files: 'src/**/*.ts',
    severity: 'info'
  },
  {
    name: 'Components without OnPush',
    pattern: /@Component\([^)]*\)(?!.*changeDetection)/,
    files: 'src/**/*.component.ts',
    severity: 'warning'
  }
];

let hasWarnings = false;

checks.forEach(check => {
  try {
    const result = execSync(
      `grep -r "${check.pattern}" ${check.files} | wc -l`,
      { encoding: 'utf8' }
    );

    const count = parseInt(result.trim());

    if (count > 0) {
      const icon = check.severity === 'warning' ? '⚠️' : 'ℹ️';
      console.log(`${icon} ${check.name}: ${count} occurrences`);

      if (check.severity === 'warning') {
        hasWarnings = true;
      }
    }
  } catch (error) {
    // Ignore errors
  }
});

if (hasWarnings) {
  console.log('\n⚠️  Performance warnings found. Consider addressing them.\n');
} else {
  console.log('\n✅ No performance issues detected.\n');
}
```

### 2. CI Pipeline Checks

```yaml
# .github/workflows/performance-ci.yml
name: Performance CI

on:
  pull_request:
  push:
    branches: [main, develop]

jobs:
  performance-check:
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

      - name: Check bundle size
        id: bundle-check
        run: |
          npm run analyze > bundle-report.txt
          cat bundle-report.txt

      - name: Compare with baseline
        run: |
          node scripts/compare-bundle-size.js

      - name: Run Lighthouse CI
        run: npm run lighthouse

      - name: Performance regression check
        run: |
          node scripts/check-performance-regression.js

      - name: Comment PR with results
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');
            const report = fs.readFileSync('bundle-report.txt', 'utf8');

            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## Performance Report\n\n\`\`\`\n${report}\n\`\`\``
            });

      - name: Fail if regression detected
        if: steps.bundle-check.outcome == 'failure'
        run: exit 1
```

---

## Real-time Monitoring

### 1. Performance Observer API

```typescript
// src/app/core/services/real-time-monitor.service.ts
@Injectable({ providedIn: 'root' })
export class RealTimeMonitorService {
  private metrics = signal<PerformanceMetric[]>([]);

  constructor() {
    this.setupObservers();
  }

  private setupObservers() {
    // Monitor long tasks (blocking main thread)
    this.observeLongTasks();

    // Monitor resource loading
    this.observeResources();

    // Monitor memory usage
    this.observeMemory();
  }

  private observeLongTasks() {
    if ('PerformanceLongTaskTiming' in window) {
      const observer = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (entry.duration > 50) {
            console.warn('Long task detected:', {
              duration: entry.duration,
              startTime: entry.startTime,
              name: entry.name
            });

            this.reportIssue('long-task', {
              duration: entry.duration,
              url: window.location.pathname
            });
          }
        }
      });

      observer.observe({ entryTypes: ['longtask'] });
    }
  }

  private observeResources() {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        const resource = entry as PerformanceResourceTiming;

        // Check for slow resources
        if (resource.duration > 1000) {
          console.warn('Slow resource:', {
            name: resource.name,
            duration: resource.duration,
            size: resource.transferSize
          });
        }

        // Check for large resources
        if (resource.transferSize > 500000) {
          console.warn('Large resource:', {
            name: resource.name,
            size: resource.transferSize
          });
        }
      }
    });

    observer.observe({ entryTypes: ['resource'] });
  }

  private observeMemory() {
    if ((performance as any).memory) {
      setInterval(() => {
        const memory = (performance as any).memory;
        const usedMB = memory.usedJSHeapSize / 1048576;
        const limitMB = memory.jsHeapSizeLimit / 1048576;
        const percentage = (usedMB / limitMB) * 100;

        if (percentage > 90) {
          console.error('High memory usage:', {
            used: usedMB.toFixed(2) + 'MB',
            limit: limitMB.toFixed(2) + 'MB',
            percentage: percentage.toFixed(2) + '%'
          });

          this.reportIssue('high-memory', {
            usedMB,
            limitMB,
            percentage
          });
        }
      }, 10000); // Check every 10 seconds
    }
  }

  private reportIssue(type: string, data: any) {
    fetch('/api/metrics/issues', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ type, data, timestamp: Date.now() })
    });
  }
}
```

### 2. Error Tracking Integration

```typescript
// src/app/core/services/error-monitor.service.ts
@Injectable({ providedIn: 'root' })
export class ErrorMonitorService {
  constructor() {
    this.setupErrorHandlers();
  }

  private setupErrorHandlers() {
    // Global error handler
    window.addEventListener('error', (event) => {
      this.reportError({
        type: 'error',
        message: event.message,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
        stack: event.error?.stack
      });
    });

    // Unhandled promise rejections
    window.addEventListener('unhandledrejection', (event) => {
      this.reportError({
        type: 'unhandledRejection',
        reason: event.reason,
        promise: event.promise
      });
    });

    // Resource loading errors
    window.addEventListener('error', (event) => {
      if (event.target !== window) {
        this.reportError({
          type: 'resourceError',
          target: (event.target as any)?.src || (event.target as any)?.href
        });
      }
    }, true);
  }

  private reportError(error: any) {
    console.error('[Error Monitor]', error);

    // Send to backend
    fetch('/api/metrics/errors', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...error,
        url: window.location.href,
        userAgent: navigator.userAgent,
        timestamp: Date.now()
      })
    });
  }
}
```

---

## Performance Budgets

### Budget Configuration

```json
{
  "budgets": {
    "initial": {
      "warning": 800,
      "error": 1000,
      "unit": "KB"
    },
    "gzipped": {
      "warning": 200,
      "error": 300,
      "unit": "KB"
    },
    "chunks": {
      "lazy": {
        "warning": 150,
        "error": 250,
        "unit": "KB"
      }
    },
    "vitals": {
      "fcp": {
        "good": 1000,
        "warning": 1500,
        "poor": 2000,
        "unit": "ms"
      },
      "lcp": {
        "good": 1800,
        "warning": 2500,
        "poor": 4000,
        "unit": "ms"
      },
      "cls": {
        "good": 0.05,
        "warning": 0.1,
        "poor": 0.25
      },
      "fid": {
        "good": 50,
        "warning": 100,
        "poor": 300,
        "unit": "ms"
      },
      "ttfb": {
        "good": 200,
        "warning": 500,
        "poor": 1000,
        "unit": "ms"
      }
    }
  }
}
```

### Budget Enforcement Script

```javascript
// scripts/enforce-budgets.js
const budgets = require('../performance-budgets.json');
const metrics = require('../.lighthouseci/manifest.json');

function checkBudgets() {
  const results = [];
  let failed = false;

  // Check bundle size
  const bundleSize = getBundleSize();
  if (bundleSize > budgets.budgets.gzipped.error * 1024) {
    results.push({
      name: 'Bundle Size',
      status: 'FAIL',
      value: formatBytes(bundleSize),
      budget: formatBytes(budgets.budgets.gzipped.error * 1024)
    });
    failed = true;
  } else if (bundleSize > budgets.budgets.gzipped.warning * 1024) {
    results.push({
      name: 'Bundle Size',
      status: 'WARN',
      value: formatBytes(bundleSize),
      budget: formatBytes(budgets.budgets.gzipped.warning * 1024)
    });
  } else {
    results.push({
      name: 'Bundle Size',
      status: 'PASS',
      value: formatBytes(bundleSize),
      budget: formatBytes(budgets.budgets.gzipped.warning * 1024)
    });
  }

  // Check Core Web Vitals
  const vitals = extractVitals(metrics);

  Object.keys(vitals).forEach(metric => {
    const value = vitals[metric];
    const budget = budgets.budgets.vitals[metric];

    if (value > budget.poor) {
      results.push({
        name: metric.toUpperCase(),
        status: 'FAIL',
        value: value + budget.unit,
        budget: budget.good + budget.unit
      });
      failed = true;
    } else if (value > budget.warning) {
      results.push({
        name: metric.toUpperCase(),
        status: 'WARN',
        value: value + budget.unit,
        budget: budget.good + budget.unit
      });
    } else {
      results.push({
        name: metric.toUpperCase(),
        status: 'PASS',
        value: value + budget.unit,
        budget: budget.good + budget.unit
      });
    }
  });

  // Print results
  console.log('\n📊 Performance Budget Check\n');
  console.table(results);

  if (failed) {
    console.error('\n❌ Performance budgets exceeded!\n');
    process.exit(1);
  } else {
    console.log('\n✅ All budgets within limits\n');
  }
}

checkBudgets();
```

---

## Alerting & Escalation

### Alert Configuration

```yaml
# alerts/performance-alerts.yml
alerts:
  - name: high_fcp
    condition: fcp > 2000
    severity: warning
    channels: [slack, email]
    message: "First Contentful Paint exceeded 2s"

  - name: critical_fcp
    condition: fcp > 3000
    severity: critical
    channels: [slack, email, pagerduty]
    message: "First Contentful Paint critically high (>3s)"

  - name: bundle_size_warning
    condition: bundle_size > 200KB
    severity: warning
    channels: [slack]
    message: "Bundle size exceeded 200KB"

  - name: api_performance_degradation
    condition: p95_response_time > 1000
    severity: critical
    channels: [slack, pagerduty]
    message: "API P95 response time > 1s"

  - name: memory_leak_detected
    condition: memory_growth_rate > 10MB/minute
    severity: critical
    channels: [slack, email, pagerduty]
    message: "Potential memory leak detected"
```

### Slack Integration

```typescript
// scripts/send-alert.ts
import { IncomingWebhook } from '@slack/webhook';

const webhook = new IncomingWebhook(process.env.SLACK_WEBHOOK_URL!);

interface Alert {
  name: string;
  severity: 'info' | 'warning' | 'critical';
  message: string;
  value: number;
  threshold: number;
}

async function sendAlert(alert: Alert) {
  const color = {
    info: '#36a64f',
    warning: '#ff9800',
    critical: '#f44336'
  }[alert.severity];

  const icon = {
    info: ':information_source:',
    warning: ':warning:',
    critical: ':rotating_light:'
  }[alert.severity];

  await webhook.send({
    text: `${icon} Performance Alert: ${alert.name}`,
    attachments: [
      {
        color,
        fields: [
          {
            title: 'Severity',
            value: alert.severity.toUpperCase(),
            short: true
          },
          {
            title: 'Current Value',
            value: `${alert.value}`,
            short: true
          },
          {
            title: 'Threshold',
            value: `${alert.threshold}`,
            short: true
          },
          {
            title: 'Message',
            value: alert.message,
            short: false
          }
        ],
        footer: 'MorisHR Performance Monitor',
        ts: Math.floor(Date.now() / 1000)
      }
    ]
  });
}
```

### Escalation Matrix

| Severity | Response Time | Notification | Actions |
|----------|---------------|--------------|---------|
| **Info** | 24 hours | Slack | Document, monitor |
| **Warning** | 4 hours | Slack, Email | Investigate, plan fix |
| **Critical** | 1 hour | Slack, Email, PagerDuty | Immediate investigation, hotfix if needed |
| **Emergency** | 15 minutes | All channels | War room, immediate rollback consideration |

---

## Troubleshooting Guide

### Common Performance Issues

#### Issue 1: Slow First Contentful Paint

**Symptoms:**
- FCP > 2s
- Users see blank screen for extended period

**Diagnosis:**
```bash
# Check server response time
curl -w "@curl-format.txt" -o /dev/null -s https://app.morishr.com

# Analyze render-blocking resources
lighthouse https://app.morishr.com --only-categories=performance --view
```

**Solutions:**
1. Optimize server response time (TTFB < 200ms)
2. Inline critical CSS
3. Defer non-critical JavaScript
4. Use resource hints (preconnect, prefetch)

#### Issue 2: Large Bundle Size

**Symptoms:**
- Bundle > 300KB gzipped
- Slow initial page load

**Diagnosis:**
```bash
# Analyze bundle composition
npm run build:analyze

# Check for duplicate dependencies
npx webpack-bundle-analyzer dist/hrms-frontend/stats.json
```

**Solutions:**
1. Enable tree-shaking for libraries
2. Lazy load routes and features
3. Remove unused dependencies
4. Split vendor bundles

#### Issue 3: Memory Leak

**Symptoms:**
- Memory usage grows continuously
- App becomes sluggish over time
- Browser tab crashes

**Diagnosis:**
```typescript
// Check memory usage
setInterval(() => {
  if ((performance as any).memory) {
    console.log('Memory:', {
      used: ((performance as any).memory.usedJSHeapSize / 1048576).toFixed(2) + 'MB',
      total: ((performance as any).memory.totalJSHeapSize / 1048576).toFixed(2) + 'MB',
      limit: ((performance as any).memory.jsHeapSizeLimit / 1048576).toFixed(2) + 'MB'
    });
  }
}, 5000);

// Use Chrome DevTools Memory Profiler
// 1. Open DevTools > Memory tab
// 2. Take heap snapshot
// 3. Navigate app
// 4. Take another snapshot
// 5. Compare for leaked objects
```

**Solutions:**
1. Properly unsubscribe from observables
2. Use `takeUntil` or async pipe
3. Remove event listeners in ngOnDestroy
4. Clear intervals/timeouts

#### Issue 4: High Cumulative Layout Shift

**Symptoms:**
- CLS > 0.1
- Content jumps during load
- Poor user experience

**Diagnosis:**
```javascript
// Detect layout shifts
let clsValue = 0;
const observer = new PerformanceObserver((list) => {
  for (const entry of list.getEntries()) {
    if (!entry.hadRecentInput) {
      console.log('Layout shift:', {
        value: entry.value,
        sources: entry.sources,
        time: entry.startTime
      });
      clsValue += entry.value;
    }
  }
});
observer.observe({ entryTypes: ['layout-shift'] });
```

**Solutions:**
1. Add width/height to images and videos
2. Reserve space for dynamic content
3. Use font-display: swap
4. Avoid inserting content above viewport

---

## Performance Dashboard

### Grafana Dashboard Configuration

```json
{
  "dashboard": {
    "title": "MorisHR Performance Dashboard",
    "panels": [
      {
        "title": "Core Web Vitals",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(fcp_bucket[5m]))",
            "legendFormat": "FCP (P95)"
          },
          {
            "expr": "histogram_quantile(0.95, rate(lcp_bucket[5m]))",
            "legendFormat": "LCP (P95)"
          },
          {
            "expr": "histogram_quantile(0.95, rate(cls_bucket[5m]))",
            "legendFormat": "CLS (P95)"
          }
        ]
      },
      {
        "title": "Bundle Size Trend",
        "targets": [
          {
            "expr": "bundle_size_bytes{type=\"gzipped\"}",
            "legendFormat": "Gzipped Size"
          }
        ]
      },
      {
        "title": "API Response Times",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "P95"
          },
          {
            "expr": "histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "P99"
          }
        ]
      }
    ]
  }
}
```

---

## Reporting

### Weekly Performance Report Template

```markdown
# Weekly Performance Report
**Week of:** [Date]

## Summary
- Overall Status: 🟢 Good / 🟡 Warning / 🔴 Critical
- Key Achievements:
- Issues Resolved:
- Action Items:

## Core Web Vitals
| Metric | This Week | Last Week | Change | Status |
|--------|-----------|-----------|--------|--------|
| FCP (P95) | X.Xs | X.Xs | ±X% | 🟢 |
| LCP (P95) | X.Xs | X.Xs | ±X% | 🟢 |
| CLS (P95) | 0.0X | 0.0X | ±X% | 🟢 |
| FID (P95) | XXms | XXms | ±X% | 🟢 |

## Bundle Size
- Current: XXXKB (gzipped)
- Change: ±X% vs last week
- Status: Within budget

## Issues
1. [Issue description]
   - Impact: High/Medium/Low
   - Status: Open/In Progress/Resolved

## Action Items
- [ ] Task 1
- [ ] Task 2

## Next Steps
- [Plan for next week]
```

---

**Document Owner:** Performance Engineering Team
**Last Updated:** November 17, 2025
**Next Review:** December 17, 2025
