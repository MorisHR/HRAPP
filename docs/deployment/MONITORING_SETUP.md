# HRMS Frontend - Monitoring & Observability Setup Guide

**Version**: 1.0
**Last Updated**: November 17, 2025
**Owner**: DevOps Team + SRE Team
**Review Cycle**: Quarterly

---

## Table of Contents

1. [Overview](#overview)
2. [Monitoring Architecture](#monitoring-architecture)
3. [Frontend Error Tracking (Sentry)](#frontend-error-tracking-sentry)
4. [Real User Monitoring (DataDog RUM)](#real-user-monitoring-datadog-rum)
5. [Performance Monitoring](#performance-monitoring)
6. [Alerting Configuration](#alerting-configuration)
7. [Dashboards](#dashboards)
8. [Log Aggregation](#log-aggregation)
9. [SLO/SLA Tracking](#slosla-tracking)

---

## Overview

This guide provides comprehensive setup instructions for monitoring the HRMS Frontend application following Fortune 500 observability best practices.

### Monitoring Objectives

1. **Detect issues before users report them**
2. **Measure user experience accurately**
3. **Track performance against SLAs**
4. **Enable rapid incident response**
5. **Provide data for capacity planning**

### Monitoring Stack

| Component | Tool | Purpose |
|-----------|------|---------|
| Error Tracking | Sentry | Client-side error tracking |
| APM | DataDog APM | Application performance monitoring |
| RUM | DataDog RUM | Real user monitoring |
| Infrastructure | Prometheus + Grafana | Server metrics |
| Logs | ELK Stack / DataDog | Log aggregation |
| Uptime | UptimeRobot | External uptime monitoring |
| Synthetic | Lighthouse CI | Automated performance tests |

---

## Monitoring Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: Client-Side Monitoring                            │
│  - Sentry (JavaScript errors)                               │
│  - DataDog RUM (User sessions, page loads)                  │
│  - Google Analytics (User behavior)                         │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: Application Performance Monitoring                │
│  - DataDog APM (API response times)                         │
│  - Lighthouse CI (Automated performance tests)              │
│  - Bundle size tracking                                     │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: Infrastructure Monitoring                         │
│  - Prometheus (Metrics collection)                          │
│  - Grafana (Visualization)                                  │
│  - Node Exporter (Server metrics)                           │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: Alerting & Incident Management                    │
│  - PagerDuty (Critical alerts)                              │
│  - Slack (Notifications)                                    │
│  - Email (Non-critical alerts)                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Frontend Error Tracking (Sentry)

### Setup Instructions

#### Step 1: Create Sentry Project

1. Navigate to https://sentry.io
2. Create new project → Select "Angular"
3. Save DSN (Data Source Name)

#### Step 2: Install Sentry SDK

```bash
cd /workspaces/HRAPP/hrms-frontend

npm install --save @sentry/angular-ivy @sentry/tracing
```

#### Step 3: Configure Sentry

**Create**: `src/app/core/services/sentry.service.ts`

```typescript
import { ErrorHandler, Injectable } from '@angular/core';
import * as Sentry from '@sentry/angular-ivy';
import { environment } from '../../../environments/environment';

@Injectable()
export class SentryErrorHandler implements ErrorHandler {
  constructor() {
    if (environment.production) {
      Sentry.init({
        dsn: 'YOUR_SENTRY_DSN',
        environment: environment.production ? 'production' : 'development',
        release: 'hrms-frontend@' + environment.version,
        integrations: [
          new Sentry.BrowserTracing({
            tracePropagationTargets: ['localhost', environment.apiUrl],
            routingInstrumentation: Sentry.routingInstrumentation,
          }),
        ],
        tracesSampleRate: environment.production ? 0.1 : 1.0,
        beforeSend(event, hint) {
          // Filter out non-critical errors
          if (event.level === 'info' || event.level === 'debug') {
            return null;
          }
          return event;
        },
      });
    }
  }

  handleError(error: any): void {
    if (environment.production) {
      Sentry.captureException(error.originalError || error);
    } else {
      console.error(error);
    }
  }
}
```

**Update**: `src/app/app.config.ts`

```typescript
import { ErrorHandler } from '@angular/core';
import { SentryErrorHandler } from './core/services/sentry.service';

export const appConfig: ApplicationConfig = {
  providers: [
    // ... existing providers
    { provide: ErrorHandler, useClass: SentryErrorHandler },
  ],
};
```

#### Step 4: Add User Context

```typescript
// In auth service after successful login
import * as Sentry from '@sentry/angular-ivy';

Sentry.setUser({
  id: user.id,
  email: user.email,
  tenantId: user.tenantId,
});
```

#### Step 5: Custom Error Tracking

```typescript
// Track custom events
Sentry.captureMessage('User completed onboarding', 'info');

// Track performance
Sentry.startTransaction({ name: 'PayrollProcessing' });
```

### Sentry Configuration Best Practices

```typescript
// Environment-based configuration
const sentryConfig = {
  development: {
    dsn: 'DEV_DSN',
    tracesSampleRate: 1.0,
    debug: true,
  },
  staging: {
    dsn: 'STAGING_DSN',
    tracesSampleRate: 0.5,
    debug: false,
  },
  production: {
    dsn: 'PROD_DSN',
    tracesSampleRate: 0.1,
    debug: false,
  },
};
```

---

## Real User Monitoring (DataDog RUM)

### Setup Instructions

#### Step 1: Create DataDog Application

1. Navigate to https://app.datadoghq.com/rum
2. Create new application → Select "Browser"
3. Save Application ID and Client Token

#### Step 2: Install DataDog SDK

```bash
npm install --save @datadog/browser-rum
```

#### Step 3: Initialize DataDog RUM

**Create**: `src/app/core/services/datadog.service.ts`

```typescript
import { Injectable } from '@angular/core';
import { datadogRum } from '@datadog/browser-rum';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DatadogService {
  constructor() {
    if (environment.production) {
      datadogRum.init({
        applicationId: 'YOUR_APPLICATION_ID',
        clientToken: 'YOUR_CLIENT_TOKEN',
        site: 'datadoghq.com',
        service: 'hrms-frontend',
        env: environment.production ? 'production' : 'staging',
        version: environment.version,
        sessionSampleRate: 100,
        sessionReplaySampleRate: 20,
        trackUserInteractions: true,
        trackResources: true,
        trackLongTasks: true,
        defaultPrivacyLevel: 'mask-user-input',
      });

      datadogRum.startSessionReplayRecording();
    }
  }

  setUser(user: { id: string; email: string; tenantId: string }) {
    datadogRum.setUser({
      id: user.id,
      email: user.email,
      tenant_id: user.tenantId,
    });
  }

  addAction(name: string, context?: object) {
    datadogRum.addAction(name, context);
  }

  addError(error: Error, context?: object) {
    datadogRum.addError(error, context);
  }
}
```

#### Step 4: Track Custom Actions

```typescript
// In components
constructor(private datadog: DatadogService) {}

submitPayroll() {
  this.datadog.addAction('payroll_submitted', {
    employee_count: this.employees.length,
    period: this.period,
  });
}
```

#### Step 5: Track Performance Metrics

```typescript
// Custom timing marks
performance.mark('payroll-calculation-start');
// ... calculations
performance.mark('payroll-calculation-end');

performance.measure(
  'payroll-calculation',
  'payroll-calculation-start',
  'payroll-calculation-end'
);
```

---

## Performance Monitoring

### Core Web Vitals Tracking

**Create**: `src/app/core/services/performance.service.ts`

```typescript
import { Injectable } from '@angular/core';
import { datadogRum } from '@datadog/browser-rum';

@Injectable({
  providedIn: 'root',
})
export class PerformanceService {
  constructor() {
    this.trackWebVitals();
  }

  private trackWebVitals() {
    // Largest Contentful Paint (LCP)
    new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        datadogRum.addTiming('lcp', entry.startTime);
      }
    }).observe({ entryTypes: ['largest-contentful-paint'] });

    // First Input Delay (FID)
    new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        datadogRum.addTiming('fid', entry.processingStart - entry.startTime);
      }
    }).observe({ entryTypes: ['first-input'] });

    // Cumulative Layout Shift (CLS)
    let clsValue = 0;
    new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (!entry.hadRecentInput) {
          clsValue += entry.value;
          datadogRum.addTiming('cls', clsValue);
        }
      }
    }).observe({ entryTypes: ['layout-shift'] });
  }

  trackRouteChange(route: string) {
    datadogRum.addAction('route_change', { route });
  }

  trackFeatureUsage(feature: string) {
    datadogRum.addAction('feature_used', { feature });
  }
}
```

### Bundle Size Tracking

**Create**: `scripts/track-bundle-size.js`

```javascript
const fs = require('fs');
const path = require('path');

const distPath = path.join(__dirname, '../dist/hrms-frontend/browser');
const statsFile = path.join(__dirname, '../bundle-stats.json');

function getFileSize(filePath) {
  const stats = fs.statSync(filePath);
  return stats.size;
}

function trackBundleSizes() {
  const files = fs.readdirSync(distPath);
  const sizes = {};

  files.forEach(file => {
    if (file.endsWith('.js')) {
      sizes[file] = getFileSize(path.join(distPath, file));
    }
  });

  const history = fs.existsSync(statsFile)
    ? JSON.parse(fs.readFileSync(statsFile))
    : [];

  history.push({
    timestamp: new Date().toISOString(),
    commit: process.env.GIT_COMMIT || 'unknown',
    sizes,
  });

  fs.writeFileSync(statsFile, JSON.stringify(history, null, 2));

  console.log('Bundle sizes tracked successfully');
}

trackBundleSizes();
```

Add to package.json:
```json
{
  "scripts": {
    "track-bundle": "node scripts/track-bundle-size.js"
  }
}
```

---

## Alerting Configuration

### Alert Thresholds

| Metric | Warning | Critical | Action |
|--------|---------|----------|--------|
| Error Rate | > 0.5% | > 1% | Investigate / Rollback |
| Response Time (p95) | > 200ms | > 500ms | Performance review |
| Response Time (p99) | > 500ms | > 1s | Immediate action |
| Availability | < 99.5% | < 99% | Emergency response |
| Failed Requests | > 10/min | > 50/min | Check API/backend |
| JavaScript Errors | > 5/min | > 20/min | Check Sentry |
| Bundle Size | > 400KB | > 500KB | Optimize bundles |

### DataDog Alerts

**Create Alert**: Error Rate Monitor

```yaml
name: "HRMS Frontend - High Error Rate"
type: metric alert
query: "sum(last_5m):sum:rum.error.count{service:hrms-frontend}.as_rate() > 0.01"
message: |
  Error rate is {{value}}% which is above the 1% threshold.

  Please investigate immediately:
  - Check Sentry for error details
  - Review recent deployments
  - Consider rollback if critical

  @slack-production-incidents
  @pagerduty-frontend
thresholds:
  critical: 0.01
  warning: 0.005
```

**Create Alert**: Slow Page Load

```yaml
name: "HRMS Frontend - Slow Page Load"
type: metric alert
query: "avg(last_10m):p95:rum.page.load_time{service:hrms-frontend} > 2000"
message: |
  Page load time (p95) is {{value}}ms which exceeds 2000ms threshold.

  Action required:
  - Check bundle sizes
  - Review API response times
  - Check CDN performance

  @slack-performance-alerts
thresholds:
  critical: 2000
  warning: 1500
```

### PagerDuty Integration

**Configure PagerDuty Service**

1. Create service: "HRMS Frontend Production"
2. Set escalation policy:
   - Level 1: On-call DevOps Engineer (immediate)
   - Level 2: DevOps Lead (after 10 minutes)
   - Level 3: Engineering Director (after 20 minutes)

3. Configure alerts:
   - Critical errors → Page immediately
   - Warning errors → Slack notification
   - Performance degradation → Email + Slack

---

## Dashboards

### DataDog Dashboard: Frontend Overview

**Create Dashboard**: HRMS Frontend - Production Overview

**Widgets**:

1. **Error Rate (Timeseries)**
   ```
   Metric: sum:rum.error.count{service:hrms-frontend}.as_rate()
   Display: Line chart
   Alert threshold: 1%
   ```

2. **Page Load Time (Heatmap)**
   ```
   Metric: p50, p75, p95, p99 of rum.page.load_time
   Display: Timeseries
   ```

3. **Active Users (Number)**
   ```
   Metric: count:rum.session.count{service:hrms-frontend}
   Display: Query value
   ```

4. **Top Errors (Table)**
   ```
   Metric: top 10 rum.error.message grouped by error.message
   Display: Top list
   ```

5. **API Response Times (Timeseries)**
   ```
   Metric: avg:rum.resource.duration by resource.url
   Display: Line chart
   ```

6. **Core Web Vitals (Score)**
   ```
   Metrics:
   - LCP (< 2.5s good)
   - FID (< 100ms good)
   - CLS (< 0.1 good)
   Display: Gauges
   ```

### Grafana Dashboard: Infrastructure

**Create Dashboard**: HRMS Frontend - Infrastructure

```yaml
title: "HRMS Frontend Infrastructure"
panels:
  - title: "Nginx Requests/sec"
    query: "rate(nginx_http_requests_total{job='nginx'}[5m])"

  - title: "Nginx Response Time"
    query: "histogram_quantile(0.95, rate(nginx_http_request_duration_seconds_bucket[5m]))"

  - title: "Server CPU Usage"
    query: "100 - (avg(rate(node_cpu_seconds_total{mode='idle'}[5m])) * 100)"

  - title: "Server Memory Usage"
    query: "(node_memory_MemTotal_bytes - node_memory_MemAvailable_bytes) / node_memory_MemTotal_bytes * 100"

  - title: "Disk I/O"
    query: "rate(node_disk_io_time_seconds_total[5m])"
```

---

## Log Aggregation

### Nginx Access Logs

**Configure**: `/etc/nginx/sites-available/hrms-frontend`

```nginx
log_format json_combined escape=json
  '{'
    '"time_local":"$time_local",'
    '"remote_addr":"$remote_addr",'
    '"request":"$request",'
    '"status":$status,'
    '"body_bytes_sent":$body_bytes_sent,'
    '"request_time":$request_time,'
    '"upstream_response_time":"$upstream_response_time",'
    '"http_referer":"$http_referer",'
    '"http_user_agent":"$http_user_agent"'
  '}';

access_log /var/log/nginx/hrms-access.log json_combined;
```

### Log Forwarding to DataDog

**Install DataDog Agent**

```bash
DD_AGENT_MAJOR_VERSION=7 DD_API_KEY=YOUR_API_KEY DD_SITE="datadoghq.com" bash -c "$(curl -L https://s3.amazonaws.com/dd-agent/scripts/install_script.sh)"
```

**Configure Log Collection**

```yaml
# /etc/datadog-agent/conf.d/nginx.d/conf.yaml
logs:
  - type: file
    path: /var/log/nginx/hrms-access.log
    service: hrms-frontend
    source: nginx

  - type: file
    path: /var/log/nginx/hrms-error.log
    service: hrms-frontend
    source: nginx
    log_processing_rules:
      - type: multi_line
        name: new_log_start_with_date
        pattern: \d{4}/\d{2}/\d{2}
```

---

## SLO/SLA Tracking

### Service Level Objectives

| SLO | Target | Measurement | Alert Threshold |
|-----|--------|-------------|-----------------|
| Availability | 99.9% | Uptime checks | < 99.5% |
| Page Load Time (p95) | < 2s | RUM data | > 2s |
| Error Rate | < 0.1% | RUM errors | > 0.5% |
| API Response Time (p95) | < 200ms | APM data | > 250ms |

### DataDog SLO Configuration

```yaml
# Create SLO: Page Load Performance
name: "HRMS Frontend - Page Load Performance"
type: metric_based
metric_query: "avg:rum.page.load_time{service:hrms-frontend} < 2000"
target_threshold: 99
warning_threshold: 99.5
timeframe: 30d
```

---

## Implementation Checklist

### Week 1: Foundation

- [ ] Set up Sentry account and configure error tracking
- [ ] Install and configure DataDog RUM
- [ ] Create basic DataDog dashboard
- [ ] Configure critical alerts (error rate, downtime)
- [ ] Set up PagerDuty integration

### Week 2: Enhanced Monitoring

- [ ] Add performance tracking (Core Web Vitals)
- [ ] Configure log aggregation
- [ ] Create Grafana infrastructure dashboard
- [ ] Set up bundle size tracking
- [ ] Configure warning-level alerts

### Week 3: Optimization

- [ ] Fine-tune alert thresholds based on baseline data
- [ ] Create custom dashboards for stakeholders
- [ ] Implement SLO tracking
- [ ] Add user journey tracking
- [ ] Create runbook for common alerts

### Week 4: Documentation & Training

- [ ] Document monitoring setup
- [ ] Create incident response procedures
- [ ] Train team on dashboards and alerts
- [ ] Conduct monitoring drill
- [ ] Review and adjust thresholds

---

**Document Version**: 1.0
**Last Updated**: November 17, 2025
**Next Review**: February 17, 2026
**Maintained By**: DevOps Team + SRE Team
