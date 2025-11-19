# Application Performance Monitoring (APM) Setup
## Fortune 500-Grade APM for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready Monitoring Strategy

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Frontend Monitoring (Angular)](#frontend-monitoring-angular)
3. [Backend Monitoring (.NET)](#backend-monitoring-net)
4. [APM Tool Comparison](#apm-tool-comparison)
5. [Implementation Guides](#implementation-guides)
6. [Alerting Configuration](#alerting-configuration)

---

## Executive Summary

### Why APM is Critical for HRMS

- **Multi-tenant architecture:** Performance issues affect multiple customers
- **Compliance requirements:** Payroll processing must complete on time
- **Financial impact:** Downtime = payroll delays = customer churn
- **SLA commitments:** 99.9% uptime target requires proactive monitoring

### Key Metrics to Monitor

| Category | Metrics | Target |
|----------|---------|--------|
| **Availability** | Uptime percentage | 99.9% |
| **Performance** | API response time (p95) | < 500ms |
| **Errors** | Error rate | < 0.1% |
| **User Experience** | Page load time | < 2s |
| **Throughput** | Requests per second | Monitor baseline |

---

## Frontend Monitoring (Angular)

### 1. Real User Monitoring (RUM)

**Purpose:** Track actual user experience in production

**Key Metrics:**
- Page load time
- Time to Interactive (TTI)
- First Contentful Paint (FCP)
- Largest Contentful Paint (LCP)
- Cumulative Layout Shift (CLS)
- First Input Delay (FID)

**Implementation:**

#### Option A: Azure Application Insights (Recommended for .NET stack)

```typescript
// src/app/app.module.ts
import { ApplicationInsights } from '@microsoft/applicationinsights-web';

const appInsights = new ApplicationInsights({
  config: {
    instrumentationKey: environment.appInsightsKey,
    enableAutoRouteTracking: true, // Track Angular route changes
    enableCorsCorrelation: true,   // Correlate frontend-backend requests
    enableRequestHeaderTracking: true,
    enableResponseHeaderTracking: true,
    enableAjaxErrorStatusText: true,
    enableUnhandledPromiseRejectionTracking: true
  }
});

appInsights.loadAppInsights();
appInsights.trackPageView(); // Initial page view

export { appInsights };
```

**Track Custom Events:**

```typescript
// Track user actions
appInsights.trackEvent({ name: 'PayrollGenerated', properties: { month: 11, year: 2025 } });

// Track custom metrics
appInsights.trackMetric({ name: 'PayrollProcessingTime', average: 3500 }); // 3.5s

// Track dependencies (API calls)
appInsights.trackDependencyData({
  id: 'unique-id',
  name: 'GET /api/employees',
  duration: 250,
  success: true,
  resultCode: 200
});
```

#### Option B: Google Analytics 4 + Web Vitals

```typescript
// Install: npm install web-vitals

import { getCLS, getFID, getFCP, getLCP, getTTFB } from 'web-vitals';

function sendToAnalytics({ name, delta, id }) {
  gtag('event', name, {
    event_category: 'Web Vitals',
    value: Math.round(name === 'CLS' ? delta * 1000 : delta),
    event_label: id,
    non_interaction: true,
  });
}

getCLS(sendToAnalytics);
getFID(sendToAnalytics);
getFCP(sendToAnalytics);
getLCP(sendToAnalytics);
getTTFB(sendToAnalytics);
```

#### Option C: Datadog RUM

```typescript
// Install: npm install @datadog/browser-rum

import { datadogRum } from '@datadog/browser-rum';

datadogRum.init({
  applicationId: environment.datadogAppId,
  clientToken: environment.datadogClientToken,
  site: 'datadoghq.com',
  service: 'hrms-frontend',
  env: environment.production ? 'production' : 'development',
  version: '1.0.0',
  sessionSampleRate: 100,
  sessionReplaySampleRate: 20, // Record 20% of sessions
  trackUserInteractions: true,
  trackResources: true,
  trackLongTasks: true,
  defaultPrivacyLevel: 'mask-user-input' // GDPR compliance
});

datadogRum.startSessionReplayRecording();
```

---

### 2. Core Web Vitals Tracking

**Google's User Experience Metrics:**

| Metric | Description | Good | Needs Improvement | Poor |
|--------|-------------|------|-------------------|------|
| **LCP** (Largest Contentful Paint) | Loading performance | ≤ 2.5s | 2.5s - 4s | > 4s |
| **FID** (First Input Delay) | Interactivity | ≤ 100ms | 100ms - 300ms | > 300ms |
| **CLS** (Cumulative Layout Shift) | Visual stability | ≤ 0.1 | 0.1 - 0.25 | > 0.25 |
| **FCP** (First Contentful Paint) | Perceived load speed | ≤ 1.8s | 1.8s - 3s | > 3s |
| **TTFB** (Time to First Byte) | Server response time | ≤ 600ms | 600ms - 1.5s | > 1.5s |

**Implementation:**

```typescript
// src/app/core/services/performance-monitoring.service.ts
import { Injectable } from '@angular/core';
import { getCLS, getFID, getLCP } from 'web-vitals';

@Injectable({ providedIn: 'root' })
export class PerformanceMonitoringService {

  initializeWebVitals() {
    getCLS(this.sendMetric);
    getFID(this.sendMetric);
    getLCP(this.sendMetric);
  }

  private sendMetric({ name, value, id }) {
    // Send to your APM tool
    const metric = {
      name,
      value: Math.round(value),
      id,
      timestamp: new Date().toISOString()
    };

    // Example: Send to Application Insights
    appInsights.trackMetric({ name, average: value });

    // Example: Send to custom backend
    this.http.post('/api/metrics/web-vitals', metric).subscribe();
  }
}
```

---

### 3. Error Tracking

**Purpose:** Capture and diagnose frontend errors

#### Option A: Sentry (Best-in-class error tracking)

```bash
npm install @sentry/angular-ivy @sentry/tracing
```

```typescript
// src/main.ts
import * as Sentry from "@sentry/angular-ivy";

Sentry.init({
  dsn: environment.sentryDsn,
  integrations: [
    new Sentry.BrowserTracing({
      tracePropagationTargets: ["localhost", "https://hrms.yourcompany.com"],
      routingInstrumentation: Sentry.routingInstrumentation,
    }),
  ],
  tracesSampleRate: 1.0,
  environment: environment.production ? 'production' : 'development',
  release: 'hrms-frontend@1.0.0',
  beforeSend(event, hint) {
    // Filter out sensitive data
    if (event.user) {
      delete event.user.email;
      delete event.user.ip_address;
    }
    return event;
  }
});

// src/app/app.module.ts
import { ErrorHandler } from '@angular/core';
import * as Sentry from "@sentry/angular-ivy";

@NgModule({
  providers: [
    {
      provide: ErrorHandler,
      useValue: Sentry.createErrorHandler({
        showDialog: false, // Don't show error dialogs to users
      }),
    },
  ],
})
export class AppModule { }
```

**Track Handled Errors:**

```typescript
try {
  await this.payrollService.generatePayslips(cycleId);
} catch (error) {
  Sentry.captureException(error, {
    tags: { feature: 'payroll' },
    extra: { cycleId, month: 11, year: 2025 }
  });
  this.notificationService.error('Failed to generate payslips');
}
```

#### Option B: Application Insights Error Tracking

```typescript
// Global error handler
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: Error) {
    appInsights.trackException({ exception: error, severityLevel: SeverityLevel.Error });
    console.error('Global error:', error);
  }
}

// In app.module.ts
providers: [
  { provide: ErrorHandler, useClass: GlobalErrorHandler }
]
```

---

### 4. User Session Recording

**Purpose:** Replay user sessions to understand issues

**Tools:**
- **Hotjar:** Session recording + heatmaps
- **FullStory:** Advanced session replay
- **LogRocket:** Session replay + performance monitoring
- **Datadog Session Replay:** Integrated with Datadog RUM

**Privacy Considerations:**
- Mask sensitive fields (salaries, passwords, personal data)
- Comply with GDPR/Data Protection Act
- Configure recording sample rate (e.g., 20% of sessions)

**Example: LogRocket Setup**

```typescript
import LogRocket from 'logrocket';

LogRocket.init('your-app-id', {
  dom: {
    inputSanitizer: true, // Mask all input fields
  },
  network: {
    requestSanitizer: request => {
      // Redact sensitive headers
      if (request.headers['Authorization']) {
        request.headers['Authorization'] = '[REDACTED]';
      }
      return request;
    },
  },
  console: {
    shouldAggregateConsoleErrors: true,
  }
});

// Identify users (after login)
LogRocket.identify(user.id, {
  name: user.name,
  email: user.email,
  role: user.role,
  tenantId: user.tenantId
});
```

---

### 5. Performance Metrics

**Custom Angular Metrics to Track:**

```typescript
// Track component load times
@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html'
})
export class EmployeeListComponent implements OnInit {

  ngOnInit() {
    const startTime = performance.now();

    this.employeeService.getEmployees().subscribe(data => {
      const loadTime = performance.now() - startTime;

      appInsights.trackMetric({
        name: 'EmployeeListLoadTime',
        average: loadTime
      });

      this.employees = data;
    });
  }
}
```

**Track API Call Performance:**

```typescript
// HTTP Interceptor
@Injectable()
export class PerformanceInterceptor implements HttpInterceptor {

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const startTime = Date.now();

    return next.handle(req).pipe(
      tap(
        event => {
          if (event instanceof HttpResponse) {
            const duration = Date.now() - startTime;

            appInsights.trackDependencyData({
              id: req.url,
              name: `${req.method} ${req.url}`,
              duration: duration,
              success: event.status >= 200 && event.status < 400,
              resultCode: event.status
            });
          }
        },
        error => {
          const duration = Date.now() - startTime;

          appInsights.trackDependencyData({
            id: req.url,
            name: `${req.method} ${req.url}`,
            duration: duration,
            success: false,
            resultCode: error.status
          });
        }
      )
    );
  }
}
```

---

## Backend Monitoring (.NET)

### 1. Request Tracing

**Purpose:** Track every API request through the system

#### Application Insights Integration

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

// Enable request tracking
builder.Services.AddApplicationInsightsTelemetryProcessor<TenantTelemetryProcessor>();
```

**Custom Telemetry Processor (Track Tenant Context):**

```csharp
public class TenantTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantTelemetryProcessor(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
    {
        _next = next;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var tenantId = httpContext.Items["TenantId"]?.ToString();
                if (!string.IsNullOrEmpty(tenantId))
                {
                    request.Properties["TenantId"] = tenantId;
                }

                var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    request.Properties["UserId"] = userId;
                }
            }
        }

        _next.Process(item);
    }
}
```

---

### 2. Database Query Performance

**Purpose:** Identify slow queries and N+1 problems

#### Option A: Application Insights SQL Tracking

```csharp
// Automatically tracked by Application Insights
// Configure in appsettings.json
{
  "ApplicationInsights": {
    "EnableDependencyTrackingTelemetryModule": true,
    "EnableSqlCommandTextInstrumentation": true
  }
}
```

#### Option B: MiniProfiler (Development/Staging)

```bash
dotnet add package MiniProfiler.AspNetCore
dotnet add package MiniProfiler.EntityFrameworkCore
```

```csharp
// Program.cs
builder.Services.AddMiniProfiler(options =>
{
    options.RouteBasePath = "/profiler";
    options.EnableDebugMode = builder.Environment.IsDevelopment();
}).AddEntityFramework();

app.UseMiniProfiler();
```

#### Option C: Custom Query Logging

```csharp
// Intercept EF Core queries
public class PerformanceDbCommandInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceDbCommandInterceptor> _logger;
    private readonly TelemetryClient _telemetryClient;

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration.TotalMilliseconds;

        // Log slow queries (> 1 second)
        if (duration > 1000)
        {
            _logger.LogWarning(
                "Slow query detected: {Duration}ms - {CommandText}",
                duration,
                command.CommandText
            );
        }

        // Track in Application Insights
        _telemetryClient.TrackDependency(
            "SQL",
            command.Connection.Database,
            command.CommandText,
            DateTimeOffset.UtcNow.AddMilliseconds(-duration),
            TimeSpan.FromMilliseconds(duration),
            result != null
        );

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}

// Register in DbContext
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new PerformanceDbCommandInterceptor());
}
```

---

### 3. API Endpoint Latency

**Purpose:** Track response times for each endpoint

**Custom Middleware:**

```csharp
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly TelemetryClient _telemetryClient;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        TelemetryClient telemetryClient)
    {
        _next = next;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var duration = sw.ElapsedMilliseconds;

            // Log slow requests (> 500ms)
            if (duration > 500)
            {
                _logger.LogWarning(
                    "Slow request: {Method} {Path} - {Duration}ms - Status {StatusCode}",
                    method, path, duration, context.Response.StatusCode
                );
            }

            // Track metrics
            _telemetryClient.TrackMetric(
                "ApiResponseTime",
                duration,
                new Dictionary<string, string>
                {
                    ["Endpoint"] = $"{method} {path}",
                    ["StatusCode"] = context.Response.StatusCode.ToString(),
                    ["TenantId"] = context.Items["TenantId"]?.ToString() ?? "unknown"
                }
            );

            // Track percentiles
            _telemetryClient.GetMetric("ApiResponseTime_P95", "Endpoint").TrackValue(duration, $"{method} {path}");
        }
    }
}

// Register in Program.cs
app.UseMiddleware<PerformanceMonitoringMiddleware>();
```

---

### 4. Exception Tracking

**Purpose:** Capture all exceptions with context

```csharp
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;
    private readonly TelemetryClient _telemetryClient;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var telemetry = new ExceptionTelemetry(ex)
            {
                SeverityLevel = SeverityLevel.Error,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Add context
            telemetry.Properties["Path"] = context.Request.Path;
            telemetry.Properties["Method"] = context.Request.Method;
            telemetry.Properties["TenantId"] = context.Items["TenantId"]?.ToString();
            telemetry.Properties["UserId"] = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            telemetry.Properties["UserAgent"] = context.Request.Headers["User-Agent"];

            // Add custom properties based on exception type
            if (ex is UnauthorizedAccessException)
            {
                telemetry.SeverityLevel = SeverityLevel.Warning;
            }
            else if (ex is ArgumentException || ex is InvalidOperationException)
            {
                telemetry.SeverityLevel = SeverityLevel.Information;
            }

            _telemetryClient.TrackException(telemetry);

            _logger.LogError(ex, "Unhandled exception in request pipeline");

            throw;
        }
    }
}
```

---

### 5. Memory and CPU Usage

**Purpose:** Detect memory leaks and CPU spikes

#### Built-in .NET Metrics

```csharp
// Program.cs - Add EventCounters
builder.Services.AddSingleton<IHostedService, MetricsCollectorService>();

public class MetricsCollectorService : BackgroundService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MetricsCollectorService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var process = Process.GetCurrentProcess();

            // Memory metrics
            var workingSet = process.WorkingSet64 / 1024 / 1024; // MB
            var privateMemory = process.PrivateMemorySize64 / 1024 / 1024; // MB
            var gcMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB

            _telemetryClient.TrackMetric("Memory_WorkingSet_MB", workingSet);
            _telemetryClient.TrackMetric("Memory_Private_MB", privateMemory);
            _telemetryClient.TrackMetric("Memory_GC_MB", gcMemory);

            // CPU metrics
            var cpuUsage = process.TotalProcessorTime.TotalMilliseconds;
            _telemetryClient.TrackMetric("CPU_Usage_ms", cpuUsage);

            // Thread count
            _telemetryClient.TrackMetric("ThreadCount", process.Threads.Count);

            // GC stats
            _telemetryClient.TrackMetric("GC_Gen0_Collections", GC.CollectionCount(0));
            _telemetryClient.TrackMetric("GC_Gen1_Collections", GC.CollectionCount(1));
            _telemetryClient.TrackMetric("GC_Gen2_Collections", GC.CollectionCount(2));

            // Log warnings if thresholds exceeded
            if (workingSet > 2048) // > 2GB
            {
                _logger.LogWarning("High memory usage: {WorkingSet}MB", workingSet);
            }
        }
    }
}
```

---

## APM Tool Comparison

### Recommended Tools Matrix

| Tool | Best For | Pricing (Monthly) | Strengths | Weaknesses |
|------|----------|-------------------|-----------|------------|
| **Azure Application Insights** | .NET + Azure stack | $2.88/GB ingested | - Native .NET integration<br>- Excellent Azure integration<br>- SQL query tracking<br>- Live metrics stream | - Limited for non-Azure deployments<br>- Complex pricing model |
| **New Relic** | Full-stack APM | $99/user + $0.30/GB | - Comprehensive dashboards<br>- Great alerting<br>- Multi-cloud support<br>- Excellent documentation | - Expensive for large teams<br>- Steeper learning curve |
| **Datadog** | Infrastructure + APM | $15/host + $31/APM host | - Best-in-class infrastructure monitoring<br>- Great Kubernetes support<br>- Powerful querying<br>- Session replay | - Can get expensive at scale<br>- Requires multiple products |
| **Sentry** | Error tracking | $26/month (5K errors) | - Best error tracking<br>- Source map support<br>- Release tracking<br>- Affordable | - Limited APM features<br>- Focused on errors only |
| **Elastic APM** | Self-hosted APM | Free (self-hosted) | - Open source<br>- Full control<br>- Great search capabilities<br>- ELK stack integration | - Requires infrastructure management<br>- Complex setup |

### Recommended Stack for HRMS

**Tier 1: Startup/Small (< 50 customers)**
- **APM:** Azure Application Insights ($150/month)
- **Errors:** Sentry ($26/month)
- **Logs:** Azure Log Analytics (included)
- **Total:** ~$200/month

**Tier 2: Growing (50-200 customers)**
- **APM:** Datadog APM ($500/month)
- **Errors:** Sentry ($99/month)
- **Logs:** Datadog Logs (included)
- **Total:** ~$600/month

**Tier 3: Enterprise (200+ customers)**
- **APM:** New Relic ($2,000/month)
- **Errors:** Integrated in New Relic
- **Logs:** New Relic Logs (included)
- **Infrastructure:** New Relic Infrastructure
- **Total:** ~$2,500/month

---

## Implementation Guides

### Quick Start: Application Insights (Recommended)

**1. Create Application Insights Resource in Azure**

```bash
az monitor app-insights component create \
  --app hrms-api \
  --location eastus \
  --resource-group hrms-prod \
  --kind web
```

**2. Get Instrumentation Key**

```bash
az monitor app-insights component show \
  --app hrms-api \
  --resource-group hrms-prod \
  --query instrumentationKey
```

**3. Configure Backend**

```json
// appsettings.Production.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=YOUR-KEY;IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/"
  }
}
```

**4. Configure Frontend**

```typescript
// src/environments/environment.prod.ts
export const environment = {
  production: true,
  appInsightsKey: 'YOUR-INSTRUMENTATION-KEY',
  apiUrl: 'https://api.hrms.yourcompany.com'
};
```

**5. View Metrics**

Navigate to: Azure Portal → Application Insights → hrms-api → Performance

---

### Quick Start: Sentry

**1. Create Sentry Project**

Visit: https://sentry.io → Create Project → Angular + .NET

**2. Configure Frontend**

```bash
npm install @sentry/angular-ivy @sentry/tracing
```

```typescript
Sentry.init({
  dsn: "https://YOUR-DSN@sentry.io/PROJECT-ID",
  environment: "production",
  tracesSampleRate: 1.0
});
```

**3. Configure Backend**

```bash
dotnet add package Sentry.AspNetCore
```

```csharp
// Program.cs
builder.WebHost.UseSentry(options =>
{
    options.Dsn = "https://YOUR-DSN@sentry.io/PROJECT-ID";
    options.Environment = "production";
    options.TracesSampleRate = 1.0;
});
```

---

## Alerting Configuration

### Critical Metrics to Alert On

**Backend Alerts:**

```yaml
- name: High Error Rate
  condition: error_rate > 5%
  severity: Critical
  notification: PagerDuty + Slack

- name: Slow API Response
  condition: p95_response_time > 2000ms
  severity: High
  notification: Slack

- name: High Memory Usage
  condition: memory_usage > 85%
  severity: High
  notification: Email + Slack

- name: Database Connection Failure
  condition: db_connection_failed
  severity: Critical
  notification: PagerDuty + Slack + SMS
```

**Frontend Alerts:**

```yaml
- name: Poor Core Web Vitals
  condition: LCP > 4s OR FID > 300ms
  severity: Medium
  notification: Slack

- name: High JavaScript Error Rate
  condition: js_error_rate > 2%
  severity: High
  notification: Slack + Email

- name: Slow Page Load
  condition: page_load_time > 5s
  severity: Medium
  notification: Slack
```

---

## Cost Estimate

### Monthly APM Costs (200 Customer Tenants)

| Component | Tool | Cost | Notes |
|-----------|------|------|-------|
| Backend APM | Application Insights | $300 | ~100GB ingestion |
| Frontend RUM | Application Insights | $150 | ~50GB ingestion |
| Error Tracking | Sentry | $99 | Business plan |
| Session Replay | Datadog | $200 | 20% sampling |
| **Total** | | **$749/month** | |

**Cost Optimization:**
- Use adaptive sampling (reduce to 10GB = $50/month for Application Insights)
- Self-host Elastic APM (infrastructure cost only)
- Use free tiers during development

---

## Next Steps

1. **Week 1:** Set up Application Insights for backend
2. **Week 2:** Implement frontend RUM with Web Vitals
3. **Week 3:** Configure Sentry for error tracking
4. **Week 4:** Set up alerting rules and dashboards
5. **Ongoing:** Monitor, tune thresholds, optimize sampling rates

---

**Document Owner:** SRE Team
**Review Frequency:** Quarterly
**Last Review:** November 17, 2025
