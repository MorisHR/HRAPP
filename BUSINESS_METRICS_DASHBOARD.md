# Business Metrics Dashboard Specification
## Fortune 500-Grade Business Intelligence for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready Business Metrics Strategy

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [User Metrics](#user-metrics)
3. [Application Metrics](#application-metrics)
4. [Performance Metrics](#performance-metrics)
5. [Revenue Metrics](#revenue-metrics)
6. [Dashboard Specifications](#dashboard-specifications)
7. [Implementation Guide](#implementation-guide)

---

## Executive Summary

### Business Metrics vs Technical Metrics

| Type | Focus | Audience | Examples |
|------|-------|----------|----------|
| **Technical Metrics** | System health | DevOps, SRE | CPU usage, error rate, latency |
| **Business Metrics** | Product success | CEO, Product, Sales | DAU, revenue, feature adoption |
| **Hybrid Metrics** | User experience | Product, Engineering | Page load time, API response time |

### Why Business Metrics Matter for HRMS

- **Product-Market Fit:** Measure feature adoption and usage patterns
- **Customer Success:** Identify at-risk customers (low usage = churn risk)
- **Revenue Optimization:** Track usage tiers, upsell opportunities
- **Roadmap Planning:** Data-driven feature prioritization
- **Investor Reporting:** Growth metrics, retention, engagement

### Key Business Questions to Answer

1. **Adoption:** Are customers using the product?
2. **Engagement:** How frequently are they using it?
3. **Retention:** Are customers staying or churning?
4. **Performance:** Is the product delivering value?
5. **Growth:** Are we acquiring new customers?

---

## User Metrics

### 1. Active Users (DAU/WAU/MAU)

**Definitions:**

| Metric | Definition | Importance |
|--------|------------|------------|
| **DAU** | Daily Active Users | Day-to-day engagement |
| **WAU** | Weekly Active Users | Short-term retention |
| **MAU** | Monthly Active Users | Long-term engagement |
| **DAU/MAU Ratio** | DAU / MAU (stickiness) | Product stickiness (target: > 25%) |

**Tracking Implementation:**

```sql
-- DAU: Users who logged in today
SELECT COUNT(DISTINCT user_id) AS dau
FROM user_sessions
WHERE DATE(login_time) = CURRENT_DATE;

-- WAU: Users who logged in in the last 7 days
SELECT COUNT(DISTINCT user_id) AS wau
FROM user_sessions
WHERE login_time >= CURRENT_DATE - INTERVAL '7 days';

-- MAU: Users who logged in in the last 30 days
SELECT COUNT(DISTINCT user_id) AS mau
FROM user_sessions
WHERE login_time >= CURRENT_DATE - INTERVAL '30 days';

-- Stickiness: DAU/MAU ratio
SELECT
    (SELECT COUNT(DISTINCT user_id) FROM user_sessions WHERE DATE(login_time) = CURRENT_DATE)::float /
    (SELECT COUNT(DISTINCT user_id) FROM user_sessions WHERE login_time >= CURRENT_DATE - INTERVAL '30 days') AS stickiness_ratio;
```

**Application Insights Tracking:**

```csharp
// Track user login
appInsights.TrackEvent("UserLogin", new Dictionary<string, string>
{
    ["UserId"] = user.Id.ToString(),
    ["TenantId"] = user.TenantId.ToString(),
    ["UserRole"] = user.Role.ToString(),
    ["LoginMethod"] = "Password"
});

// Track user activity (page views, API calls)
appInsights.TrackPageView("EmployeeList", new Dictionary<string, string>
{
    ["UserId"] = user.Id.ToString(),
    ["TenantId"] = user.TenantId.ToString()
});
```

**Grafana Dashboard:**

```promql
# DAU
count(count_over_time({event="UserLogin"}[1d]) by (user_id))

# WAU
count(count_over_time({event="UserLogin"}[7d]) by (user_id))

# MAU
count(count_over_time({event="UserLogin"}[30d]) by (user_id))

# Stickiness
count(count_over_time({event="UserLogin"}[1d]) by (user_id)) / count(count_over_time({event="UserLogin"}[30d]) by (user_id))
```

**Target Metrics for HRMS:**

| User Type | Target DAU/MAU | Notes |
|-----------|----------------|-------|
| **HR Managers** | 60-80% | Daily usage expected (attendance, leave approval) |
| **Employees** | 20-30% | Monthly usage (payslips, leave requests) |
| **Admins** | 80-100% | High engagement (system management) |

---

### 2. Login Success/Failure Rate

**Why Track Login Metrics:**
- High failure rate = UX issue or security concern
- Track authentication methods (password, SSO, MFA)
- Identify brute force attacks

**Metrics:**

```sql
-- Login success rate
SELECT
    COUNT(CASE WHEN success = true THEN 1 END)::float / COUNT(*) * 100 AS success_rate,
    COUNT(CASE WHEN success = false THEN 1 END) AS failed_logins,
    COUNT(*) AS total_attempts
FROM login_attempts
WHERE attempt_time >= CURRENT_DATE - INTERVAL '1 day';

-- Failed logins by user (potential brute force)
SELECT
    user_email,
    COUNT(*) AS failed_attempts,
    MAX(attempt_time) AS last_attempt
FROM login_attempts
WHERE success = false AND attempt_time >= CURRENT_DATE - INTERVAL '1 hour'
GROUP BY user_email
HAVING COUNT(*) >= 5
ORDER BY failed_attempts DESC;

-- Login methods breakdown
SELECT
    login_method,
    COUNT(*) AS logins,
    AVG(EXTRACT(EPOCH FROM (logout_time - login_time))) AS avg_session_duration_seconds
FROM user_sessions
WHERE login_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY login_method;
```

**Tracking Implementation:**

```csharp
// Track login attempt
public async Task<AuthResponse> LoginAsync(LoginDto dto)
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            // Track failed login
            _telemetryClient.TrackEvent("LoginFailed", new Dictionary<string, string>
            {
                ["Email"] = dto.Email,
                ["Reason"] = "InvalidCredentials",
                ["IpAddress"] = _httpContext.Connection.RemoteIpAddress.ToString()
            });

            await LogLoginAttempt(dto.Email, false, "Invalid credentials");

            throw new UnauthorizedException("Invalid credentials");
        }

        // Track successful login
        _telemetryClient.TrackEvent("LoginSuccess", new Dictionary<string, string>
        {
            ["UserId"] = user.Id.ToString(),
            ["TenantId"] = user.TenantId.ToString(),
            ["UserRole"] = user.Role.ToString()
        });

        _telemetryClient.TrackMetric("LoginDuration", stopwatch.ElapsedMilliseconds);

        await LogLoginAttempt(dto.Email, true);

        return GenerateTokens(user);
    }
    catch (Exception ex)
    {
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

**Alert Configuration:**

```yaml
- alert: HighLoginFailureRate
  expr: sum(rate(login_failed_total[5m])) / sum(rate(login_attempts_total[5m])) > 0.3
  for: 10m
  labels:
    severity: medium
  annotations:
    summary: "High login failure rate detected"
    description: "Login failure rate is {{ $value | humanizePercentage }}"

- alert: PotentialBruteForceAttack
  expr: sum by (user_email) (rate(login_failed_total[1m])) > 5
  for: 5m
  labels:
    severity: high
  annotations:
    summary: "Potential brute force attack on {{ $labels.user_email }}"
```

---

### 3. Session Duration

**Why Track Session Duration:**
- Short sessions = poor UX or lack of features
- Long sessions = engaged users
- Identify power users vs casual users

**Metrics:**

```sql
-- Average session duration by user role
SELECT
    u.role,
    AVG(EXTRACT(EPOCH FROM (s.logout_time - s.login_time)) / 60) AS avg_session_minutes,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY EXTRACT(EPOCH FROM (s.logout_time - s.login_time)) / 60) AS median_session_minutes,
    COUNT(*) AS total_sessions
FROM user_sessions s
JOIN users u ON s.user_id = u.id
WHERE s.login_time >= CURRENT_DATE - INTERVAL '7 days' AND s.logout_time IS NOT NULL
GROUP BY u.role;

-- Session duration distribution
SELECT
    CASE
        WHEN duration_minutes < 5 THEN '0-5 min'
        WHEN duration_minutes < 15 THEN '5-15 min'
        WHEN duration_minutes < 30 THEN '15-30 min'
        WHEN duration_minutes < 60 THEN '30-60 min'
        ELSE '60+ min'
    END AS duration_bucket,
    COUNT(*) AS session_count,
    COUNT(*)::float / SUM(COUNT(*)) OVER () * 100 AS percentage
FROM (
    SELECT EXTRACT(EPOCH FROM (logout_time - login_time)) / 60 AS duration_minutes
    FROM user_sessions
    WHERE login_time >= CURRENT_DATE - INTERVAL '7 days' AND logout_time IS NOT NULL
) t
GROUP BY duration_bucket
ORDER BY duration_bucket;
```

**Expected Session Durations for HRMS:**

| User Type | Expected Duration | Use Cases |
|-----------|-------------------|-----------|
| **HR Manager** | 30-60 minutes | Review attendance, approve leave, generate reports |
| **Employee** | 5-10 minutes | View payslip, apply for leave, check attendance |
| **Admin** | 15-30 minutes | Tenant management, user setup, configuration |

---

### 4. User Journey Completion

**Critical User Journeys in HRMS:**

| Journey | Steps | Success Criteria |
|---------|-------|------------------|
| **Employee Onboarding** | Create employee → Add documents → Assign department → Send welcome email | 100% completion |
| **Leave Request** | Select leave type → Choose dates → Submit → Receive confirmation | > 90% completion |
| **Payslip Download** | Login → Navigate to payslips → Select month → Download PDF | > 95% completion |
| **Attendance Review** | Login → View attendance → Filter by date → Export to Excel | > 80% completion |
| **Payroll Processing** | Create cycle → Generate payslips → Review → Approve → Send emails | 100% completion |

**Tracking Implementation:**

```csharp
// Track funnel steps
public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto dto)
{
    // Step 1: Start journey
    _telemetryClient.TrackEvent("EmployeeOnboarding_Started", new Dictionary<string, string>
    {
        ["TenantId"] = _currentTenant.Id.ToString(),
        ["JourneyId"] = Guid.NewGuid().ToString()
    });

    var employee = new Employee { /* ... */ };
    await _repository.CreateAsync(employee);

    // Step 2: Employee created
    _telemetryClient.TrackEvent("EmployeeOnboarding_EmployeeCreated", new Dictionary<string, string>
    {
        ["EmployeeId"] = employee.Id.ToString(),
        ["JourneyId"] = journeyId
    });

    // Step 3: Documents uploaded
    if (dto.Documents?.Any() == true)
    {
        _telemetryClient.TrackEvent("EmployeeOnboarding_DocumentsUploaded", new Dictionary<string, string>
        {
            ["EmployeeId"] = employee.Id.ToString(),
            ["DocumentCount"] = dto.Documents.Count.ToString()
        });
    }

    // Step 4: Journey completed
    _telemetryClient.TrackEvent("EmployeeOnboarding_Completed", new Dictionary<string, string>
    {
        ["EmployeeId"] = employee.Id.ToString(),
        ["Duration"] = stopwatch.ElapsedMilliseconds.ToString()
    });

    return employee;
}
```

**Funnel Analysis Query:**

```sql
-- Leave request funnel
WITH funnel_steps AS (
    SELECT
        journey_id,
        MAX(CASE WHEN event_name = 'LeaveRequest_Started' THEN 1 ELSE 0 END) AS step1,
        MAX(CASE WHEN event_name = 'LeaveRequest_DatesSelected' THEN 1 ELSE 0 END) AS step2,
        MAX(CASE WHEN event_name = 'LeaveRequest_Submitted' THEN 1 ELSE 0 END) AS step3,
        MAX(CASE WHEN event_name = 'LeaveRequest_Confirmed' THEN 1 ELSE 0 END) AS step4
    FROM events
    WHERE event_time >= CURRENT_DATE - INTERVAL '30 days'
    GROUP BY journey_id
)
SELECT
    SUM(step1) AS started,
    SUM(step2) AS dates_selected,
    SUM(step3) AS submitted,
    SUM(step4) AS confirmed,
    SUM(step2)::float / NULLIF(SUM(step1), 0) * 100 AS step1_to_step2_conversion,
    SUM(step3)::float / NULLIF(SUM(step2), 0) * 100 AS step2_to_step3_conversion,
    SUM(step4)::float / NULLIF(SUM(step3), 0) * 100 AS step3_to_step4_conversion
FROM funnel_steps;
```

---

## Application Metrics

### 1. Employee Records Created

**Metrics to Track:**

```sql
-- Employees created over time
SELECT
    DATE_TRUNC('day', created_at) AS date,
    COUNT(*) AS employees_created,
    COUNT(*) FILTER (WHERE is_active = true) AS active_employees
FROM employees
WHERE created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY date
ORDER BY date;

-- Employee creation by tenant (top 10)
SELECT
    t.name AS tenant_name,
    COUNT(e.id) AS total_employees,
    COUNT(e.id) FILTER (WHERE e.created_at >= CURRENT_DATE - INTERVAL '30 days') AS new_this_month,
    COUNT(e.id) FILTER (WHERE e.is_active = true) AS active_employees
FROM employees e
JOIN tenants t ON e.tenant_id = t.id
GROUP BY t.id, t.name
ORDER BY total_employees DESC
LIMIT 10;

-- Employee growth rate
SELECT
    DATE_TRUNC('month', created_at) AS month,
    COUNT(*) AS employees_created,
    LAG(COUNT(*)) OVER (ORDER BY DATE_TRUNC('month', created_at)) AS previous_month,
    ((COUNT(*) - LAG(COUNT(*)) OVER (ORDER BY DATE_TRUNC('month', created_at)))::float /
     NULLIF(LAG(COUNT(*)) OVER (ORDER BY DATE_TRUNC('month', created_at)), 0)) * 100 AS growth_rate
FROM employees
WHERE created_at >= CURRENT_DATE - INTERVAL '12 months'
GROUP BY month
ORDER BY month;
```

**Business Insights:**

- **Growth Indicator:** Steady employee creation = customer growth
- **Tenant Health:** Active employee creation = engaged customers
- **Churn Indicator:** No new employees in 60 days = at-risk customer

---

### 2. Payroll Processing Time

**Why Track:**
- Critical business process (must complete on time)
- Long processing time = performance issue
- Track by tenant size (100 employees vs 1000)

**Metrics:**

```sql
-- Average payroll processing time by tenant
SELECT
    t.name AS tenant_name,
    COUNT(pc.id) AS payroll_cycles_processed,
    AVG(EXTRACT(EPOCH FROM (pc.completed_at - pc.started_at))) AS avg_processing_seconds,
    MAX(EXTRACT(EPOCH FROM (pc.completed_at - pc.started_at))) AS max_processing_seconds,
    AVG(pc.employee_count) AS avg_employees_processed
FROM payroll_cycles pc
JOIN tenants t ON pc.tenant_id = t.id
WHERE pc.status = 'Completed' AND pc.created_at >= CURRENT_DATE - INTERVAL '6 months'
GROUP BY t.id, t.name
HAVING COUNT(pc.id) >= 3  -- At least 3 payroll cycles
ORDER BY avg_processing_seconds DESC;

-- Processing time correlation with employee count
SELECT
    CASE
        WHEN employee_count < 50 THEN '1-50 employees'
        WHEN employee_count < 100 THEN '51-100 employees'
        WHEN employee_count < 250 THEN '101-250 employees'
        ELSE '250+ employees'
    END AS size_bucket,
    COUNT(*) AS payroll_runs,
    AVG(EXTRACT(EPOCH FROM (completed_at - started_at))) AS avg_processing_seconds,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY EXTRACT(EPOCH FROM (completed_at - started_at))) AS p95_processing_seconds
FROM payroll_cycles
WHERE status = 'Completed'
GROUP BY size_bucket
ORDER BY size_bucket;
```

**Application Tracking:**

```csharp
public async Task<PayrollCycle> GeneratePayrollAsync(Guid cycleId)
{
    var stopwatch = Stopwatch.StartNew();

    var cycle = await _payrollRepository.GetByIdAsync(cycleId);
    cycle.StartedAt = DateTime.UtcNow;
    cycle.Status = PayrollStatus.Processing;

    _telemetryClient.TrackEvent("PayrollProcessing_Started", new Dictionary<string, string>
    {
        ["CycleId"] = cycleId.ToString(),
        ["TenantId"] = _currentTenant.Id.ToString(),
        ["EmployeeCount"] = cycle.EmployeeCount.ToString(),
        ["Month"] = cycle.Month.ToString(),
        ["Year"] = cycle.Year.ToString()
    });

    try
    {
        // Generate payslips for all employees
        var payslips = await GeneratePayslipsAsync(cycle);

        cycle.CompletedAt = DateTime.UtcNow;
        cycle.Status = PayrollStatus.Completed;

        stopwatch.Stop();

        _telemetryClient.TrackMetric("PayrollProcessingTime", stopwatch.ElapsedMilliseconds, new Dictionary<string, string>
        {
            ["TenantId"] = _currentTenant.Id.ToString(),
            ["EmployeeCount"] = cycle.EmployeeCount.ToString()
        });

        _telemetryClient.TrackEvent("PayrollProcessing_Completed", new Dictionary<string, string>
        {
            ["CycleId"] = cycleId.ToString(),
            ["Duration"] = stopwatch.ElapsedMilliseconds.ToString(),
            ["PayslipsGenerated"] = payslips.Count.ToString()
        });

        return cycle;
    }
    catch (Exception ex)
    {
        cycle.Status = PayrollStatus.Failed;
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

**Target Metrics:**

| Employee Count | Target Processing Time | Max Acceptable |
|----------------|------------------------|----------------|
| 1-50 | < 30 seconds | 60 seconds |
| 51-100 | < 60 seconds | 120 seconds |
| 101-250 | < 120 seconds | 300 seconds |
| 250+ | < 300 seconds | 600 seconds |

---

### 3. Leave Requests Processed

**Metrics:**

```sql
-- Leave request processing metrics
SELECT
    status,
    COUNT(*) AS total_requests,
    AVG(EXTRACT(EPOCH FROM (
        CASE
            WHEN status IN ('Approved', 'Rejected') THEN approved_at
            ELSE NULL
        END - created_at
    )) / 3600) AS avg_approval_time_hours,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY EXTRACT(EPOCH FROM (approved_at - created_at)) / 3600) AS median_approval_time_hours
FROM leave_applications
WHERE created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY status;

-- Leave approval rate by approver
SELECT
    approver_id,
    u.first_name || ' ' || u.last_name AS approver_name,
    COUNT(*) AS total_reviewed,
    COUNT(*) FILTER (WHERE status = 'Approved') AS approved,
    COUNT(*) FILTER (WHERE status = 'Rejected') AS rejected,
    (COUNT(*) FILTER (WHERE status = 'Approved')::float / COUNT(*)) * 100 AS approval_rate
FROM leave_applications la
JOIN users u ON la.approver_id = u.id
WHERE la.status IN ('Approved', 'Rejected')
  AND la.approved_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY approver_id, approver_name
HAVING COUNT(*) >= 5
ORDER BY total_reviewed DESC;

-- Pending leave requests (SLA tracking)
SELECT
    COUNT(*) AS pending_requests,
    COUNT(*) FILTER (WHERE created_at < CURRENT_DATE - INTERVAL '24 hours') AS pending_over_24h,
    COUNT(*) FILTER (WHERE created_at < CURRENT_DATE - INTERVAL '48 hours') AS pending_over_48h,
    AVG(EXTRACT(EPOCH FROM (CURRENT_TIMESTAMP - created_at)) / 3600) AS avg_waiting_hours
FROM leave_applications
WHERE status = 'PendingApproval';
```

**SLA Tracking:**

```yaml
- alert: LeaveRequestsNotProcessedInTime
  expr: count(leave_requests{status="PendingApproval", age_hours > 48}) > 5
  for: 1h
  labels:
    severity: medium
  annotations:
    summary: "{{ $value }} leave requests pending > 48 hours"
```

---

### 4. Attendance Records Captured

**Metrics:**

```sql
-- Daily attendance capture rate
SELECT
    attendance_date,
    COUNT(*) AS total_records,
    COUNT(*) FILTER (WHERE status = 'Present') AS present,
    COUNT(*) FILTER (WHERE status = 'Absent') AS absent,
    COUNT(*) FILTER (WHERE status = 'OnLeave') AS on_leave,
    (COUNT(*) FILTER (WHERE status = 'Present')::float / COUNT(*)) * 100 AS attendance_rate
FROM attendance
WHERE attendance_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY attendance_date
ORDER BY attendance_date DESC;

-- Attendance capture by source
SELECT
    source,  -- 'Manual', 'BiometricImport', 'MobileApp'
    COUNT(*) AS total_records,
    (COUNT(*)::float / SUM(COUNT(*)) OVER ()) * 100 AS percentage
FROM attendance
WHERE attendance_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY source;

-- Late attendance capture (marked after 11 PM)
SELECT
    attendance_date,
    COUNT(*) AS late_entries,
    AVG(EXTRACT(EPOCH FROM (created_at - attendance_date - INTERVAL '1 day')) / 3600) AS avg_delay_hours
FROM attendance
WHERE created_at > attendance_date + INTERVAL '23 hours'
  AND attendance_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY attendance_date
ORDER BY attendance_date DESC;
```

---

### 5. API Usage by Endpoint

**Track Most Used Features:**

```sql
-- API endpoint usage (from request logs)
SELECT
    request_path,
    request_method,
    COUNT(*) AS request_count,
    AVG(response_time_ms) AS avg_response_time,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY response_time_ms) AS p95_response_time,
    COUNT(*) FILTER (WHERE status_code >= 400) AS error_count,
    (COUNT(*) FILTER (WHERE status_code >= 400)::float / COUNT(*)) * 100 AS error_rate
FROM api_request_logs
WHERE created_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY request_path, request_method
ORDER BY request_count DESC
LIMIT 20;

-- Feature adoption by tenant
SELECT
    t.name AS tenant_name,
    COUNT(DISTINCT CASE WHEN arl.request_path LIKE '%/employees%' THEN arl.id END) AS employee_api_calls,
    COUNT(DISTINCT CASE WHEN arl.request_path LIKE '%/attendance%' THEN arl.id END) AS attendance_api_calls,
    COUNT(DISTINCT CASE WHEN arl.request_path LIKE '%/leave%' THEN arl.id END) AS leave_api_calls,
    COUNT(DISTINCT CASE WHEN arl.request_path LIKE '%/payroll%' THEN arl.id END) AS payroll_api_calls
FROM api_request_logs arl
JOIN tenants t ON arl.tenant_id = t.id
WHERE arl.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY t.id, t.name
ORDER BY (employee_api_calls + attendance_api_calls + leave_api_calls + payroll_api_calls) DESC
LIMIT 20;
```

---

## Performance Metrics

### 1. Page Load Times

**Frontend Performance Tracking:**

```typescript
// Track page load performance
@Component({
  selector: 'app-employee-list',
  templateUrl: './employee-list.component.html'
})
export class EmployeeListComponent implements OnInit, AfterViewInit {

  ngAfterViewInit() {
    // Use Navigation Timing API
    const perfData = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;

    const metrics = {
      dns: perfData.domainLookupEnd - perfData.domainLookupStart,
      tcp: perfData.connectEnd - perfData.connectStart,
      request: perfData.responseStart - perfData.requestStart,
      response: perfData.responseEnd - perfData.responseStart,
      dom: perfData.domContentLoadedEventEnd - perfData.domContentLoadedEventStart,
      load: perfData.loadEventEnd - perfData.loadEventStart,
      total: perfData.loadEventEnd - perfData.fetchStart
    };

    appInsights.trackMetric('PageLoadTime_EmployeeList', metrics.total);
    appInsights.trackMetric('PageLoadTime_DOM', metrics.dom);
  }
}
```

**Aggregated Metrics:**

```sql
-- Page load performance by page
SELECT
    page_name,
    COUNT(*) AS page_views,
    AVG(load_time_ms) AS avg_load_time,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY load_time_ms) AS p50_load_time,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY load_time_ms) AS p95_load_time,
    PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY load_time_ms) AS p99_load_time
FROM page_load_metrics
WHERE created_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY page_name
ORDER BY page_views DESC;
```

---

### 2. API Response Times

**Tracked Automatically by APM:**

```promql
# p95 response time by endpoint
histogram_quantile(0.95,
  sum(rate(http_request_duration_seconds_bucket[5m])) by (le, endpoint)
)

# Average response time trend
avg(rate(http_request_duration_seconds_sum[5m]) / rate(http_request_duration_seconds_count[5m])) by (endpoint)
```

---

### 3. Database Query Times

**Already covered in Infrastructure Monitoring**

---

### 4. Error Rates by Type

**Error Classification:**

```sql
-- Error breakdown by type
SELECT
    error_type,
    COUNT(*) AS error_count,
    (COUNT(*)::float / SUM(COUNT(*)) OVER ()) * 100 AS percentage,
    COUNT(DISTINCT user_id) AS affected_users,
    COUNT(DISTINCT tenant_id) AS affected_tenants
FROM error_logs
WHERE created_at >= CURRENT_DATE - INTERVAL '24 hours'
GROUP BY error_type
ORDER BY error_count DESC;

-- Error rate over time
SELECT
    DATE_TRUNC('hour', created_at) AS hour,
    COUNT(*) AS errors,
    COUNT(DISTINCT user_id) AS affected_users
FROM error_logs
WHERE created_at >= CURRENT_DATE - INTERVAL '24 hours'
GROUP BY hour
ORDER BY hour;
```

---

## Revenue Metrics

### 1. Monthly Recurring Revenue (MRR)

**Metrics:**

```sql
-- Current MRR
SELECT
    SUM(subscription_amount) AS total_mrr,
    COUNT(*) AS active_subscriptions,
    AVG(subscription_amount) AS avg_revenue_per_tenant
FROM tenants
WHERE status = 'Active' AND subscription_end_date > CURRENT_DATE;

-- MRR by plan tier
SELECT
    subscription_plan,
    COUNT(*) AS tenant_count,
    SUM(subscription_amount) AS mrr,
    AVG(subscription_amount) AS avg_revenue
FROM tenants
WHERE status = 'Active'
GROUP BY subscription_plan
ORDER BY mrr DESC;

-- MRR growth rate
SELECT
    DATE_TRUNC('month', subscription_start_date) AS month,
    SUM(subscription_amount) AS new_mrr,
    LAG(SUM(subscription_amount)) OVER (ORDER BY DATE_TRUNC('month', subscription_start_date)) AS prev_month_mrr,
    ((SUM(subscription_amount) - LAG(SUM(subscription_amount)) OVER (ORDER BY DATE_TRUNC('month', subscription_start_date)))::float /
     NULLIF(LAG(SUM(subscription_amount)) OVER (ORDER BY DATE_TRUNC('month', subscription_start_date)), 0)) * 100 AS growth_rate
FROM tenants
WHERE subscription_start_date >= CURRENT_DATE - INTERVAL '12 months'
GROUP BY month
ORDER BY month;
```

---

### 2. Customer Lifetime Value (CLV)

```sql
-- Average customer lifetime value
SELECT
    AVG(total_revenue) AS avg_clv,
    AVG(months_subscribed) AS avg_lifetime_months,
    AVG(total_revenue / NULLIF(months_subscribed, 0)) AS avg_monthly_revenue
FROM (
    SELECT
        tenant_id,
        SUM(amount) AS total_revenue,
        EXTRACT(EPOCH FROM (MAX(payment_date) - MIN(payment_date))) / (30 * 24 * 3600) AS months_subscribed
    FROM payments
    WHERE status = 'Completed'
    GROUP BY tenant_id
) t;
```

---

### 3. Churn Rate

```sql
-- Monthly churn rate
SELECT
    DATE_TRUNC('month', churned_at) AS month,
    COUNT(*) AS churned_tenants,
    (
        SELECT COUNT(*)
        FROM tenants
        WHERE status = 'Active' AND created_at < DATE_TRUNC('month', churned_at)
    ) AS active_at_start,
    (COUNT(*)::float / (
        SELECT COUNT(*)
        FROM tenants
        WHERE status = 'Active' AND created_at < DATE_TRUNC('month', churned_at)
    )) * 100 AS churn_rate
FROM tenants
WHERE status = 'Churned' AND churned_at >= CURRENT_DATE - INTERVAL '12 months'
GROUP BY month
ORDER BY month;
```

---

## Dashboard Specifications

### Dashboard 1: Executive Overview

**Target Audience:** CEO, Executives

**Widgets:**

1. **KPI Cards (Top Row)**
   - Total Active Tenants
   - Monthly Recurring Revenue (MRR)
   - Total Active Users
   - DAU/MAU Ratio (Stickiness)

2. **Growth Chart**
   - MRR growth over last 12 months (line chart)
   - New tenants vs churned tenants (bar chart)

3. **User Engagement**
   - DAU/WAU/MAU trend (line chart)
   - Session duration by user role (bar chart)

4. **Health Metrics**
   - System uptime (gauge)
   - Average API response time (gauge)
   - Error rate (gauge)

---

### Dashboard 2: Product Analytics

**Target Audience:** Product Managers

**Widgets:**

1. **Feature Adoption**
   - API usage by module (pie chart)
   - Feature usage heatmap (calendar)

2. **User Funnels**
   - Leave request funnel (sankey diagram)
   - Employee onboarding funnel

3. **Retention Cohorts**
   - Monthly cohort retention (heatmap)

4. **Top Tenants**
   - Most active tenants by API calls (table)
   - Tenants at risk (low usage) (table)

---

### Dashboard 3: Operations Dashboard

**Target Audience:** Customer Success, Support

**Widgets:**

1. **Tenant Health**
   - Active tenants by plan (pie chart)
   - Tenants with recent errors (table)
   - Tenants with slow performance (table)

2. **Support Metrics**
   - Support tickets by priority (bar chart)
   - Average resolution time (gauge)

3. **Usage Patterns**
   - Peak usage hours (heatmap)
   - Geographic distribution (map)

---

## Implementation Guide

### Step 1: Create Database Schema for Metrics

```sql
-- User sessions table
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    login_time TIMESTAMP NOT NULL,
    logout_time TIMESTAMP,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_user_sessions_login_time ON user_sessions(login_time);
CREATE INDEX idx_user_sessions_user_id ON user_sessions(user_id);

-- Event tracking table
CREATE TABLE events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_name VARCHAR(100) NOT NULL,
    user_id UUID REFERENCES users(id),
    tenant_id UUID REFERENCES tenants(id),
    journey_id UUID,
    properties JSONB,
    event_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_events_event_name ON events(event_name);
CREATE INDEX idx_events_event_time ON events(event_time);
CREATE INDEX idx_events_journey_id ON events(journey_id);
```

---

### Step 2: Implement Event Tracking Service

```csharp
public interface IEventTrackingService
{
    Task TrackEventAsync(string eventName, Dictionary<string, string> properties);
    Task TrackUserSessionAsync(Guid userId, string action);
}

public class EventTrackingService : IEventTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly TelemetryClient _telemetryClient;

    public async Task TrackEventAsync(string eventName, Dictionary<string, string> properties)
    {
        // Track in Application Insights
        _telemetryClient.TrackEvent(eventName, properties);

        // Store in database for custom analytics
        var eventEntity = new Event
        {
            EventName = eventName,
            UserId = Guid.Parse(properties.GetValueOrDefault("UserId")),
            TenantId = Guid.Parse(properties.GetValueOrDefault("TenantId")),
            JourneyId = properties.ContainsKey("JourneyId") ? Guid.Parse(properties["JourneyId"]) : null,
            Properties = JsonSerializer.Serialize(properties),
            EventTime = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();
    }
}
```

---

### Step 3: Create Grafana Dashboard

```json
{
  "dashboard": {
    "title": "HRMS Business Metrics",
    "panels": [
      {
        "title": "Daily Active Users (DAU)",
        "targets": [
          {
            "expr": "count(count_over_time({event=\"UserLogin\"}[1d]) by (user_id))",
            "legendFormat": "DAU"
          }
        ]
      },
      {
        "title": "Monthly Recurring Revenue",
        "targets": [
          {
            "expr": "sum(tenant_subscription_amount{status=\"Active\"})"
          }
        ]
      }
    ]
  }
}
```

---

## Cost Estimate

**Monthly Business Analytics Costs:**

| Component | Tool | Cost |
|-----------|------|------|
| Application Insights | Event tracking | $100 |
| Grafana Cloud | Dashboard hosting | $49/month |
| PostgreSQL Storage | Event data (30 days) | $20/month |
| **Total** | | **$169/month** |

---

**Document Owner:** Product Team
**Review Frequency:** Monthly
**Last Review:** November 17, 2025
