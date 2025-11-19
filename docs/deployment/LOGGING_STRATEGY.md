# Logging Strategy
## Fortune 500-Grade Logging Framework for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready Logging Strategy

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Log Levels](#log-levels)
3. [Structured Logging Format](#structured-logging-format)
4. [Log Aggregation](#log-aggregation)
5. [Log Retention Policies](#log-retention-policies)
6. [Sensitive Data Masking](#sensitive-data-masking)
7. [Audit Logging](#audit-logging)
8. [Correlation IDs](#correlation-ids-distributed-tracing)
9. [Implementation Guide](#implementation-guide)

---

## Executive Summary

### Why Structured Logging Matters

**Benefits:**
- **Faster Troubleshooting:** Query logs by field (tenant_id, user_id, correlation_id)
- **Security Compliance:** Audit trail for regulatory requirements (GDPR, ISO 27001)
- **Performance Analysis:** Identify slow operations and bottlenecks
- **Cost Optimization:** Intelligent retention policies reduce storage costs
- **Root Cause Analysis:** Distributed tracing with correlation IDs

### Logging Architecture

```
┌─────────────────────────────────────────────────────┐
│             Application Layer                        │
│  (.NET 8 with Serilog Structured Logging)           │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│              Log Sinks                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │ Console  │  │   File   │  │   Seq    │          │
│  │  (Dev)   │  │ (Rolling)│  │ (Search) │          │
│  └──────────┘  └──────────┘  └──────────┘          │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│         Log Aggregation & Analysis                   │
│  ┌───────────────┐  ┌──────────────────┐           │
│  │  ELK Stack    │  │ Azure Log        │           │
│  │  (Self-hosted)│  │ Analytics        │           │
│  │               │  │ (Managed)        │           │
│  └───────────────┘  └──────────────────┘           │
└─────────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│         Visualization & Alerting                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │  Kibana  │  │  Grafana │  │   Seq    │          │
│  │ (Search) │  │ (Metrics)│  │ (Search) │          │
│  └──────────┘  └──────────┘  └──────────┘          │
└─────────────────────────────────────────────────────┘
```

### Log Volume Estimates

| Environment | Requests/Day | Logs/Day | Storage/Day | Monthly Cost |
|-------------|--------------|----------|-------------|--------------|
| **Development** | 10,000 | 100,000 | 50 MB | $5 |
| **Staging** | 100,000 | 1,000,000 | 500 MB | $50 |
| **Production** | 1,000,000 | 10,000,000 | 5 GB | $500 |

---

## Log Levels

### Standard Log Levels (RFC 5424)

| Level | Numeric | Purpose | Examples | Retention |
|-------|---------|---------|----------|-----------|
| **FATAL** | 0 | System crash | Unrecoverable errors, system shutdown | 90 days |
| **ERROR** | 1 | Operation failed | Exception thrown, API call failed | 60 days |
| **WARN** | 2 | Potential issue | Slow query (>1s), deprecated API usage | 30 days |
| **INFO** | 3 | Business events | User login, payroll generated, employee created | 30 days |
| **DEBUG** | 4 | Diagnostic info | Method entry/exit, variable values | 7 days |
| **TRACE** | 5 | Detailed flow | Loop iterations, condition checks | 1 day (dev only) |

### When to Use Each Level

**FATAL:**
```csharp
// System cannot continue operating
catch (OutOfMemoryException ex)
{
    _logger.LogCritical(ex, "OUT OF MEMORY - Application shutting down");
    Environment.Exit(1);
}
```

**ERROR:**
```csharp
// Operation failed but system continues
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to generate payroll for tenant {TenantId}, cycle {CycleId}",
        tenantId, cycleId);
    throw; // Re-throw to return 500 to client
}
```

**WARN:**
```csharp
// Something unusual happened but operation succeeded
if (queryDuration > 1000)
{
    _logger.LogWarning("Slow database query detected: {Duration}ms - {Query}",
        queryDuration, query);
}
```

**INFO:**
```csharp
// Business-level events (auditable actions)
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
_logger.LogInformation("Payroll generated for {EmployeeCount} employees in {Duration}ms",
    employeeCount, duration);
```

**DEBUG:**
```csharp
// Development/troubleshooting information
_logger.LogDebug("Fetching employees for tenant {TenantId} with filter {Filter}",
    tenantId, filter);
```

---

## Structured Logging Format

### JSON Format (Recommended)

**Standard Log Entry:**

```json
{
  "@timestamp": "2025-11-17T14:32:45.123Z",
  "@level": "Information",
  "@message": "User logged in successfully",
  "@exception": null,
  "CorrelationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "TenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "UserId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "UserEmail": "john.doe@acme.com",
  "IpAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0...",
  "RequestPath": "/api/auth/login",
  "RequestMethod": "POST",
  "StatusCode": 200,
  "Duration": 245,
  "MachineName": "hrms-api-pod-7d9f8b-xj4k2",
  "Environment": "Production",
  "Application": "HRMS.API",
  "Version": "1.2.5"
}
```

### Field Definitions

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| `@timestamp` | DateTime | ISO 8601 timestamp (UTC) | Yes |
| `@level` | String | Log level (see above) | Yes |
| `@message` | String | Human-readable message | Yes |
| `@exception` | Object | Exception details (type, message, stack trace) | No |
| `CorrelationId` | UUID | Request correlation ID | Yes (API requests) |
| `TenantId` | UUID | Multi-tenant context | Yes (tenant requests) |
| `UserId` | UUID | Authenticated user | If authenticated |
| `RequestPath` | String | API endpoint | API requests |
| `RequestMethod` | String | HTTP method | API requests |
| `StatusCode` | Integer | HTTP status code | API requests |
| `Duration` | Integer | Request duration (ms) | API requests |

---

## Log Aggregation

### Option 1: ELK Stack (Self-Hosted)

**Architecture:**

```
Application → Filebeat → Logstash → Elasticsearch → Kibana
```

**Installation (Kubernetes):**

```bash
# Add Elastic Helm repo
helm repo add elastic https://helm.elastic.co

# Install Elasticsearch
helm install elasticsearch elastic/elasticsearch \
  --namespace logging \
  --create-namespace \
  --set replicas=3 \
  --set resources.requests.memory=4Gi

# Install Kibana
helm install kibana elastic/kibana \
  --namespace logging \
  --set service.type=LoadBalancer

# Install Filebeat (log shipper)
helm install filebeat elastic/filebeat \
  --namespace logging \
  --set daemonset.enabled=true
```

**Logstash Pipeline Configuration:**

```ruby
# /etc/logstash/pipeline/hrms.conf
input {
  beats {
    port => 5044
  }
}

filter {
  # Parse JSON logs
  if [message] =~ /^\{/ {
    json {
      source => "message"
    }
  }

  # Add geolocation based on IP
  if [IpAddress] {
    geoip {
      source => "IpAddress"
      target => "geo"
    }
  }

  # Parse correlation ID
  if [CorrelationId] {
    mutate {
      add_field => { "[@metadata][correlation_id]" => "%{CorrelationId}" }
    }
  }

  # Mask sensitive data
  if [Password] {
    mutate {
      replace => { "Password" => "[REDACTED]" }
    }
  }
}

output {
  elasticsearch {
    hosts => ["elasticsearch:9200"]
    index => "hrms-logs-%{+YYYY.MM.dd}"
  }
}
```

**Cost:** ~$300/month (3-node cluster on managed Kubernetes)

---

### Option 2: Azure Log Analytics (Managed)

**Setup:**

```bash
# Create Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group hrms-prod \
  --workspace-name hrms-logs \
  --location eastus

# Get workspace ID and key
WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group hrms-prod \
  --workspace-name hrms-logs \
  --query customerId -o tsv)

WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group hrms-prod \
  --workspace-name hrms-logs \
  --query primarySharedKey -o tsv)
```

**Configure Serilog Sink:**

```csharp
// Install package
// dotnet add package Serilog.Sinks.AzureAnalytics

Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureAnalytics(
        workspaceId: Configuration["Azure:LogAnalytics:WorkspaceId"],
        authenticationId: Configuration["Azure:LogAnalytics:WorkspaceKey"],
        logName: "HRMSLogs")
    .CreateLogger();
```

**Query Logs (KQL):**

```kql
// Find all errors in the last hour
HRMSLogs
| where TimeGenerated > ago(1h)
| where Level == "Error"
| project TimeGenerated, Message, Exception, TenantId, UserId
| order by TimeGenerated desc

// Track a specific request across services
HRMSLogs
| where CorrelationId == "f47ac10b-58cc-4372-a567-0e02b2c3d479"
| project TimeGenerated, Level, Message, RequestPath, Duration
| order by TimeGenerated asc

// Error rate by tenant
HRMSLogs
| where TimeGenerated > ago(24h)
| summarize ErrorCount = countif(Level == "Error") by TenantId
| order by ErrorCount desc
```

**Cost:** ~$200/month (5 GB/day ingestion)

---

### Option 3: Seq (Developer-Friendly)

**Why Seq:**
- Beautiful UI optimized for structured logs
- Powerful query language
- Integrated alerting
- Free for single-user, $500/year for teams

**Installation (Docker):**

```bash
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -v /path/to/data:/data \
  -p 5341:80 \
  datalust/seq:latest
```

**Configure Serilog:**

```csharp
// Install package
// dotnet add package Serilog.Sinks.Seq

Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();
```

**Seq Query Examples:**

```
// Find slow API requests
Duration > 1000 and RequestPath like '/api/%'

// Track user actions
UserId = '9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d'

// Find database errors
@Level = 'Error' and @Exception like '%PostgresException%'
```

---

## Log Retention Policies

### Retention Strategy

| Log Type | Retention Period | Storage | Cost Impact |
|----------|------------------|---------|-------------|
| **FATAL/ERROR** | 90 days | Hot storage | High |
| **WARN** | 30 days | Hot storage | Medium |
| **INFO (Audit)** | 365 days | Warm storage | High |
| **INFO (General)** | 30 days | Hot storage | Medium |
| **DEBUG** | 7 days | Hot storage | Low |
| **TRACE** | 1 day (dev only) | Hot storage | Very Low |

### Implementation (Elasticsearch)

**Index Lifecycle Management (ILM) Policy:**

```json
{
  "policy": {
    "phases": {
      "hot": {
        "actions": {
          "rollover": {
            "max_size": "50GB",
            "max_age": "1d"
          }
        }
      },
      "warm": {
        "min_age": "7d",
        "actions": {
          "shrink": {
            "number_of_shards": 1
          },
          "forcemerge": {
            "max_num_segments": 1
          }
        }
      },
      "cold": {
        "min_age": "30d",
        "actions": {
          "freeze": {}
        }
      },
      "delete": {
        "min_age": "90d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}
```

**Apply Policy:**

```bash
# Create policy
curl -X PUT "localhost:9200/_ilm/policy/hrms-logs-policy" \
  -H 'Content-Type: application/json' \
  -d @ilm-policy.json

# Apply to index template
curl -X PUT "localhost:9200/_index_template/hrms-logs" \
  -H 'Content-Type: application/json' \
  -d '{
    "index_patterns": ["hrms-logs-*"],
    "template": {
      "settings": {
        "index.lifecycle.name": "hrms-logs-policy"
      }
    }
  }'
```

---

## Sensitive Data Masking

### Data Classification

| Category | Examples | Handling |
|----------|----------|----------|
| **Personally Identifiable Information (PII)** | Email, phone, address, National ID | Mask or redact |
| **Financial Data** | Salary, bank account, tax info | Mask completely |
| **Authentication** | Passwords, tokens, API keys | Never log |
| **Health Information** | Medical records | Redact |
| **Business Sensitive** | Proprietary algorithms, pricing | Redact in non-prod |

### Masking Implementation

**1. Serilog Destructuring Policy:**

```csharp
public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly string[] SensitiveProperties = new[]
    {
        "Password", "PasswordHash", "Token", "ApiKey", "Secret",
        "CreditCard", "SSN", "BankAccount", "Salary"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var type = value.GetType();

        if (type.IsClass)
        {
            var properties = type.GetProperties()
                .Select(prop =>
                {
                    var propValue = prop.GetValue(value);
                    var propName = prop.Name;

                    // Mask sensitive properties
                    if (SensitiveProperties.Contains(propName, StringComparer.OrdinalIgnoreCase))
                    {
                        propValue = "[REDACTED]";
                    }

                    // Mask email partially
                    if (propName.Equals("Email", StringComparison.OrdinalIgnoreCase) && propValue is string email)
                    {
                        propValue = MaskEmail(email);
                    }

                    return new LogEventProperty(propName, propertyValueFactory.CreatePropertyValue(propValue));
                })
                .ToList();

            result = new StructureValue(properties);
            return true;
        }

        result = null;
        return false;
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "[INVALID_EMAIL]";

        var username = parts[0];
        var domain = parts[1];

        // Keep first 2 characters, mask the rest
        var maskedUsername = username.Length > 2
            ? $"{username.Substring(0, 2)}***"
            : "***";

        return $"{maskedUsername}@{domain}";
    }
}

// Register in Program.cs
Log.Logger = new LoggerConfiguration()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .CreateLogger();
```

**2. Request/Response Logging with Masking:**

```csharp
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var request = await FormatRequest(context.Request);

        _logger.LogInformation("Incoming request: {Method} {Path} {QueryString} {Body}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            MaskSensitiveData(request));

        await _next(context);
    }

    private string MaskSensitiveData(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();

        // Mask password fields
        if (root.TryGetProperty("password", out _))
        {
            json = Regex.Replace(json, @"""password""\s*:\s*""[^""]*""", @"""password"":""[REDACTED]""", RegexOptions.IgnoreCase);
        }

        // Mask token fields
        if (root.TryGetProperty("token", out _))
        {
            json = Regex.Replace(json, @"""token""\s*:\s*""[^""]*""", @"""token"":""[REDACTED]""", RegexOptions.IgnoreCase);
        }

        return json;
    }
}
```

---

## Audit Logging

### Audit Events to Track

| Event Category | Examples | Required Fields |
|----------------|----------|-----------------|
| **Authentication** | Login, logout, password change | UserId, IpAddress, Timestamp |
| **Authorization** | Role change, permission grant | ActorUserId, TargetUserId, Action |
| **Data Access** | View employee, view payslip | UserId, ResourceType, ResourceId |
| **Data Modification** | Create employee, update salary | UserId, Action, Before, After |
| **Administrative** | Tenant creation, configuration change | AdminId, Action, Details |

### Audit Log Schema

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; }
    public string Action { get; set; }  // "Create", "Update", "Delete", "View"
    public string ResourceType { get; set; }  // "Employee", "Payroll", "Leave"
    public Guid? ResourceId { get; set; }
    public string OldValue { get; set; }  // JSON of previous state
    public string NewValue { get; set; }  // JSON of new state
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string FailureReason { get; set; }
}
```

### Audit Logging Service

```csharp
public interface IAuditService
{
    Task LogAsync(string action, string resourceType, Guid? resourceId, object oldValue = null, object newValue = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public async Task LogAsync(string action, string resourceType, Guid? resourceId, object oldValue = null, object newValue = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = httpContext.Items["TenantId"]?.ToString();

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            TenantId = Guid.Parse(tenantId),
            UserId = Guid.Parse(userId),
            UserEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            IsSuccess = true
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // Also log to structured logging
        _logger.LogInformation("Audit: {Action} {ResourceType} {ResourceId} by {UserId}",
            action, resourceType, resourceId, userId);
    }
}
```

**Usage Example:**

```csharp
public async Task<Employee> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto dto)
{
    var employee = await _repository.GetByIdAsync(id);
    var oldValue = employee.Clone();

    employee.FirstName = dto.FirstName;
    employee.LastName = dto.LastName;
    employee.BasicSalary = dto.BasicSalary;

    await _repository.UpdateAsync(employee);

    // Log audit event
    await _auditService.LogAsync("Update", "Employee", id, oldValue, employee);

    return employee;
}
```

---

## Correlation IDs (Distributed Tracing)

### Why Correlation IDs?

**Problem:** In distributed systems, a single user request may touch multiple services (API → Database → Redis → Email Service). Without correlation IDs, you can't trace the request flow.

**Solution:** Generate a unique ID for each request and pass it through all services.

### Implementation

**1. Middleware to Generate/Extract Correlation ID:**

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract from header or generate new
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store in HttpContext for easy access
        context.Items["CorrelationId"] = correlationId;

        // Add to response header
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to logging scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
```

**2. Configure Serilog to Include Correlation ID:**

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()  // Required for scope properties
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

app.UseMiddleware<CorrelationIdMiddleware>();
```

**3. Pass Correlation ID to External Services:**

```csharp
public class HttpClientWithCorrelationId
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<string> CallExternalApiAsync(string url)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

        if (!string.IsNullOrEmpty(correlationId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        }

        return await _httpClient.GetStringAsync(url);
    }
}
```

**4. Query Logs by Correlation ID:**

```kql
// Azure Log Analytics
HRMSLogs
| where CorrelationId == "f47ac10b-58cc-4372-a567-0e02b2c3d479"
| project TimeGenerated, Level, Message, RequestPath, Duration
| order by TimeGenerated asc
```

```
// Seq
CorrelationId = 'f47ac10b-58cc-4372-a567-0e02b2c3d479'
```

---

## Implementation Guide

### Complete Serilog Configuration

```csharp
// Program.cs
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "HRMS.API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        path: "logs/log-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000,  // 100 MB
        rollOnFileSizeLimit: true)
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"])
    .WriteTo.ApplicationInsights(
        builder.Configuration["ApplicationInsights:InstrumentationKey"],
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();
```

**appsettings.Production.json:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "Seq": {
    "ServerUrl": "http://seq:5341"
  },
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR-KEY"
  }
}
```

---

## Logging Best Practices

### DO:

```csharp
// ✅ Use structured logging with named parameters
_logger.LogInformation("Employee {EmployeeId} created by {UserId} in {Duration}ms",
    employee.Id, userId, duration);

// ✅ Include context
_logger.LogError(ex, "Failed to generate payroll for tenant {TenantId}, cycle {CycleId}",
    tenantId, cycleId);

// ✅ Log business events
_logger.LogInformation("Payslip generated: Employee={EmployeeId}, Month={Month}, NetPay={NetPay}",
    employeeId, month, netPay);
```

### DON'T:

```csharp
// ❌ String concatenation (not searchable)
_logger.LogInformation("Employee " + employee.Id + " created");

// ❌ Logging sensitive data
_logger.LogInformation("User logged in with password {Password}", password);

// ❌ Logging in loops (log volume explosion)
foreach (var employee in employees)
{
    _logger.LogDebug("Processing employee {EmployeeId}", employee.Id);  // DON'T!
}
// Instead:
_logger.LogInformation("Processing {EmployeeCount} employees", employees.Count);
```

---

## Cost Estimate

### Monthly Logging Costs (Production)

| Component | Tool | Volume | Cost |
|-----------|------|--------|------|
| **Log Ingestion** | Azure Log Analytics | 150 GB/month | $450 |
| **Log Storage** | Azure Storage (warm) | 500 GB | $10 |
| **Search/Visualization** | Kibana (self-hosted) | N/A | $100 (infra) |
| **Alerting** | Azure Monitor | 100 alerts/month | $10 |
| **Total** | | | **$570/month** |

**Cost Optimization:**
- Use sampling for DEBUG logs (10% sampling = 90% cost reduction)
- Compress old logs (gzip = 80% size reduction)
- Use tiered storage (hot/warm/cold)
- Set aggressive retention for non-critical logs

---

## Compliance Requirements

### GDPR (Data Protection)

**Right to Access:** Users can request all logs mentioning their data
```sql
SELECT * FROM logs WHERE UserId = 'user-guid' OR log_data LIKE '%user@email.com%';
```

**Right to Erasure:** Anonymize logs when user requests deletion
```sql
UPDATE logs
SET UserId = NULL, UserEmail = '[DELETED]', IpAddress = NULL
WHERE UserId = 'user-guid';
```

### ISO 27001 (Information Security)

**Requirement:** Retain security logs for 1 year minimum
```
Audit logs: 365 days retention
Authentication logs: 365 days retention
Authorization logs: 365 days retention
```

### SOC 2 (Service Organization Control)

**Requirement:** Immutable audit trail
- Use append-only logging
- Hash log entries to detect tampering
- Regular log integrity checks

---

**Document Owner:** Platform Team
**Review Frequency:** Quarterly
**Last Review:** November 17, 2025
**Next Review:** February 17, 2026
