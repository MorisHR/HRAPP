# Service Level Objectives (SLO) and Service Level Agreements (SLA)
## Fortune 500-Grade Reliability Commitments for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready SLO/SLA Framework

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [SLI (Service Level Indicators)](#sli-service-level-indicators)
3. [SLO (Service Level Objectives)](#slo-service-level-objectives)
4. [SLA (Service Level Agreements)](#sla-service-level-agreements)
5. [Error Budget](#error-budget)
6. [Measurement and Reporting](#measurement-and-reporting)
7. [SLA Breach Compensation](#sla-breach-compensation)

---

## Executive Summary

### Definitions

| Term | Definition | Audience | Example |
|------|------------|----------|---------|
| **SLI** | Service Level Indicator - Quantitative measure of service level | Internal (Engineering) | "99.5% of requests succeed" |
| **SLO** | Service Level Objective - Target value for an SLI | Internal (Product/Engineering) | "API availability target: 99.9%" |
| **SLA** | Service Level Agreement - Business commitment to customers | External (Customers) | "We guarantee 99.9% uptime or you get 10% credit" |

### Relationship

```
SLI (What we measure) → SLO (What we aim for) → SLA (What we promise)

Example:
SLI: "Successful API requests / Total API requests"
SLO: "99.9% of API requests succeed"
SLA: "We guarantee 99.5% uptime. If we fail, you get 25% credit."
```

### Key Principles

1. **SLOs are stricter than SLAs:** We aim for 99.9% but only promise 99.5%
2. **Error Budget:** (100% - SLO) = budget for failures, experiments, deployments
3. **Customer-Centric:** SLAs focus on what customers care about (availability, latency)
4. **Measurable:** Everything must be objectively measurable
5. **Actionable:** Breach = immediate action required

---

## SLI (Service Level Indicators)

### 1. Availability SLI

**Definition:** Percentage of successful requests

**Formula:**
```
Availability = (Successful Requests / Total Requests) × 100%

Where:
- Successful Request = HTTP status 2xx or 3xx
- Failed Request = HTTP status 5xx or timeout
- Note: 4xx (client errors) are NOT counted as failures
```

**Measurement:**

```promql
# Prometheus query
sum(rate(http_requests_total{status=~"2..|3.."}[5m])) /
sum(rate(http_requests_total[5m])) * 100
```

```sql
-- SQL query (hourly)
SELECT
    DATE_TRUNC('hour', timestamp) AS hour,
    COUNT(*) FILTER (WHERE status_code < 500) AS successful_requests,
    COUNT(*) AS total_requests,
    (COUNT(*) FILTER (WHERE status_code < 500)::float / COUNT(*)) * 100 AS availability_percent
FROM api_requests
WHERE timestamp >= NOW() - INTERVAL '24 hours'
GROUP BY hour
ORDER BY hour;
```

**Target:** 99.9% (3 nines)

---

### 2. Latency SLI

**Definition:** Percentage of requests completed within target latency

**Formula:**
```
Latency SLI = (Requests under threshold / Total Requests) × 100%

Thresholds:
- p50 (median): 50% of requests
- p95: 95% of requests
- p99: 99% of requests
```

**Why p95/p99 instead of average?**
- Average hides outliers
- p95 = "95% of users have this experience"
- p99 = "worst experience for 1% of users"

**Measurement:**

```promql
# p95 latency
histogram_quantile(0.95,
  sum(rate(http_request_duration_seconds_bucket[5m])) by (le)
)

# p99 latency
histogram_quantile(0.99,
  sum(rate(http_request_duration_seconds_bucket[5m])) by (le)
)
```

**Targets:**
- **p50 latency:** < 200ms (median user experience)
- **p95 latency:** < 500ms (95% of users)
- **p99 latency:** < 1000ms (99% of users)

---

### 3. Error Rate SLI

**Definition:** Percentage of requests that return errors

**Formula:**
```
Error Rate = (Failed Requests / Total Requests) × 100%

Failed Request = HTTP 5xx OR timeout OR exception
```

**Measurement:**

```promql
sum(rate(http_requests_total{status=~"5.."}[5m])) /
sum(rate(http_requests_total[5m])) * 100
```

**Target:** < 0.1% (99.9% success rate)

---

### 4. Throughput SLI

**Definition:** Number of requests handled per second

**Formula:**
```
Throughput = Total Requests / Time Period (seconds)
```

**Measurement:**

```promql
sum(rate(http_requests_total[5m]))
```

**Target:** Establish baseline, monitor for anomalies (>200% or <50% of baseline)

**Baseline Examples:**
- Off-peak hours (midnight - 6 AM): 50 req/sec
- Business hours (9 AM - 5 PM): 500 req/sec
- Month-end payroll (peak): 2000 req/sec

---

### 5. Data Durability SLI

**Definition:** Percentage of data operations that complete successfully without data loss

**Formula:**
```
Durability = (Successful DB writes / Total DB write attempts) × 100%
```

**Measurement:**

```sql
-- Database write success rate
SELECT
    COUNT(*) FILTER (WHERE success = true) AS successful_writes,
    COUNT(*) AS total_writes,
    (COUNT(*) FILTER (WHERE success = true)::float / COUNT(*)) * 100 AS durability_percent
FROM database_operations
WHERE operation_type = 'INSERT' OR operation_type = 'UPDATE'
  AND timestamp >= NOW() - INTERVAL '24 hours';
```

**Target:** 99.99% (4 nines) - Higher than availability because data loss is worse than downtime

---

## SLO (Service Level Objectives)

### SLO Summary Table

| SLI | SLO Target | Measurement Window | Error Budget (monthly) |
|-----|-----------|-------------------|------------------------|
| **Availability** | 99.9% | 30 days | 43.2 minutes downtime |
| **Latency (p95)** | < 500ms | 5 minutes | 5% of requests can be slower |
| **Latency (p99)** | < 1000ms | 5 minutes | 1% of requests can be slower |
| **Error Rate** | < 0.1% | 30 days | 0.1% of requests can fail |
| **Data Durability** | 99.99% | 30 days | 0.01% of writes can fail |

### SLO 1: API Availability

**Target:** 99.9% availability over 30-day rolling window

**What this means:**
- **Maximum downtime per month:** 43.2 minutes
- **Maximum downtime per year:** 8.76 hours

**Downtime Calculation:**

| Availability | Downtime per Month | Downtime per Year |
|--------------|-------------------|-------------------|
| 90% (1 nine) | 3 days | 36.5 days |
| 99% (2 nines) | 7.2 hours | 3.65 days |
| 99.9% (3 nines) | 43.2 minutes | 8.76 hours |
| 99.95% | 21.6 minutes | 4.38 hours |
| 99.99% (4 nines) | 4.32 minutes | 52.6 minutes |

**Measurement:**

```promql
# 30-day availability
sum(rate(http_requests_total{status!~"5.."}[30d])) /
sum(rate(http_requests_total[30d])) * 100
```

**Alert Threshold:** If availability < 99.9% for 30-day window, trigger P1 alert

---

### SLO 2: API Latency

**Targets:**
- **p50 latency:** < 200ms
- **p95 latency:** < 500ms
- **p99 latency:** < 1000ms

**Measurement Window:** 5-minute rolling average

**Why these targets?**
- **200ms:** Human perception threshold (feels instant)
- **500ms:** Acceptable for most operations
- **1000ms:** Maximum before timeout warnings

**Per-Endpoint Targets:**

| Endpoint Type | p95 Target | p99 Target | Justification |
|---------------|-----------|-----------|---------------|
| **Authentication** | 200ms | 500ms | Frequent operation, must be fast |
| **Employee List** | 300ms | 800ms | May involve complex queries |
| **Payroll Generation** | 5000ms | 10000ms | Heavy computation acceptable |
| **Report Export** | 10000ms | 30000ms | Large data processing |

**Alert Threshold:**
```yaml
- alert: HighLatency
  expr: |
    histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[5m])) by (le, endpoint)) > 0.5
  for: 10m
  labels:
    severity: high
```

---

### SLO 3: Database Query Performance

**Target:** 95% of queries complete in < 100ms

**Measurement:**

```sql
SELECT
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY total_exec_time) AS p95_exec_time_ms
FROM pg_stat_statements
WHERE calls > 100;  -- Only measure frequently-used queries
```

**Alert Threshold:** If p95 > 100ms for 15 minutes, investigate

---

### SLO 4: Background Job Success Rate

**Target:** 99.9% of background jobs complete successfully

**Jobs Covered:**
- Leave accrual job (monthly)
- Document expiry alerts (daily)
- Absent marking job (daily)

**Measurement:**

```sql
SELECT
    COUNT(*) FILTER (WHERE status = 'Succeeded') AS successful_jobs,
    COUNT(*) AS total_jobs,
    (COUNT(*) FILTER (WHERE status = 'Succeeded')::float / COUNT(*)) * 100 AS success_rate
FROM hangfire_jobs
WHERE created_at >= NOW() - INTERVAL '30 days';
```

---

## SLA (Service Level Agreements)

### Customer-Facing SLA

**Guaranteed Uptime:** 99.5% monthly uptime

**Note:** SLA (99.5%) is lower than SLO (99.9%) to provide buffer

**What is NOT covered by SLA:**
- Scheduled maintenance (announced 7 days in advance, max 4 hours/month)
- Force majeure (natural disasters, acts of war, etc.)
- Customer-caused issues (incorrect API usage, DDoS from customer side)
- Third-party service failures (DNS provider, cloud provider)

### SLA Tiers by Plan

| Plan Tier | Uptime SLA | Max Downtime/Month | Support Response Time |
|-----------|-----------|-------------------|----------------------|
| **Starter** | 99.0% | 7.2 hours | Next business day |
| **Professional** | 99.5% | 3.6 hours | < 4 hours |
| **Business** | 99.9% | 43.2 minutes | < 1 hour |
| **Enterprise** | 99.95% | 21.6 minutes | < 15 minutes (24/7) |

---

### SLA Measurement

**Uptime Calculation:**

```
Monthly Uptime % = (Total Minutes - Downtime Minutes) / Total Minutes × 100

Example (November 2025):
Total Minutes = 30 days × 24 hours × 60 minutes = 43,200 minutes
Downtime = 30 minutes
Uptime % = (43,200 - 30) / 43,200 × 100 = 99.93%
```

**Downtime Definition:**
- Downtime = period when service returns 5xx errors for > 50% of requests for > 1 minute
- Scheduled maintenance does NOT count as downtime

**Measurement Source:**
- Primary: Internal monitoring (Prometheus)
- Secondary: External monitoring (UptimeRobot from 5 global locations)
- Customer-reported incidents verified against monitoring data

---

### SLA Reporting

**Monthly SLA Report:**

```
HRMS Monthly SLA Report - November 2025

Overall Uptime: 99.93%
Target SLA: 99.5%
Status: ✅ SLA MET

Incident Summary:
- Total Incidents: 2
- P1 Incidents: 1 (API outage - 15 minutes)
- P2 Incidents: 1 (Slow queries - 15 minutes)
- Total Downtime: 30 minutes

Top 5 Slowest Endpoints (p95):
1. /api/reports/payroll-summary - 850ms
2. /api/employees/search - 420ms
3. /api/attendance/monthly - 380ms
4. /api/leave/applications - 250ms
5. /api/auth/login - 180ms

Recommendations:
- Optimize payroll report query (add index on employee_id, month)
- Implement caching for employee search
```

---

## SLA Breach Compensation

### Service Credits

| Uptime % | Downtime | Service Credit |
|----------|---------|---------------|
| ≥ 99.5% | < 3.6 hours | 0% (SLA met) |
| 99.0% - 99.5% | 3.6 - 7.2 hours | 10% of monthly fee |
| 95.0% - 99.0% | 7.2 - 36 hours | 25% of monthly fee |
| < 95.0% | > 36 hours | 50% of monthly fee |

**Example:**
- Customer on Professional Plan: $3,000/month
- November uptime: 99.2% (downtime: 5.8 hours)
- Credit: 10% × $3,000 = $300

**How to Claim:**
1. Customer submits support ticket within 7 days of month end
2. We verify downtime from monitoring data
3. Credit applied to next invoice (or refund if requested)

**Limitations:**
- Maximum credit: 50% of monthly fee (cannot exceed total subscription cost)
- Credits do not accumulate across months
- Must be claimed within 30 days

---

## Error Budget

### What is Error Budget?

**Error Budget = (100% - SLO) × Total Requests**

**Example:**
- SLO: 99.9% availability
- Error Budget: 0.1% of requests can fail
- Monthly requests: 100 million
- **Error Budget: 100,000 failed requests allowed**

### Error Budget Policy

**When Error Budget is Healthy (> 50% remaining):**
- ✅ Deploy new features aggressively
- ✅ Experiment with new technologies
- ✅ Perform load testing in production

**When Error Budget is Low (< 50% remaining):**
- ⚠️ Freeze non-critical feature deployments
- ⚠️ Focus on reliability improvements
- ⚠️ Increase testing rigor

**When Error Budget is Exhausted (0% remaining):**
- 🚫 Halt all feature deployments
- 🚫 Only critical bug fixes and reliability improvements
- 🚫 Mandatory postmortem for every incident

### Error Budget Tracking

```promql
# Error budget remaining (30-day window)
(
  1 - (
    sum(rate(http_requests_total{status=~"5.."}[30d])) /
    sum(rate(http_requests_total[30d]))
  )
) / 0.001 * 100

# Result:
# 100% = No errors (full budget remaining)
# 50% = Half of error budget used
# 0% = Error budget exhausted
```

**Dashboard Widget:**

```
Error Budget Status (30 days)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Budget Remaining: 87% ✅
Errors: 13,245 / 100,000
SLO Status: 99.987% (target: 99.9%)

Burn Rate: 0.5x (healthy)
Estimated Depletion: 60 days
```

---

## Measurement and Reporting

### Real-Time SLO Dashboard

**Grafana Dashboard Panels:**

1. **Availability Gauge**
   - Current 30-day availability
   - Color: Green (>99.9%), Yellow (99.5-99.9%), Red (<99.5%)

2. **Error Budget Chart**
   - Line chart showing budget burn over time
   - Alert threshold at 50% remaining

3. **Latency Heatmap**
   - p50/p95/p99 latency by hour
   - Color-coded by target (green < 500ms, yellow < 1000ms, red > 1000ms)

4. **Top Failing Endpoints**
   - Table of endpoints with highest error rates
   - Link to logs for investigation

### Weekly SLO Review

**Agenda:**
1. Review SLO status (met/missed)
2. Analyze error budget burn rate
3. Review incidents and root causes
4. Identify reliability improvements

**Template:**

```markdown
## Weekly SLO Review - Week 46, 2025

### SLO Status
- ✅ Availability: 99.94% (target: 99.9%)
- ✅ Latency p95: 420ms (target: <500ms)
- ⚠️ Latency p99: 1,200ms (target: <1000ms) - MISSED
- ✅ Error Rate: 0.03% (target: <0.1%)

### Error Budget
- Remaining: 78%
- Burn Rate: 0.8x (healthy)

### Incidents
- P2: Slow database queries on Nov 13 (15 min impact)
  - Root Cause: Missing index on attendance table
  - Fix: Index added, query time reduced from 5s to 50ms

### Action Items
- [ ] Optimize report generation (reduce p99 latency)
- [ ] Add caching to employee search endpoint
- [ ] Increase database connection pool size
```

---

### Monthly SLA Report to Customers

**Automatically generated and emailed on 1st of each month**

```
Dear [Customer Name],

Your HRMS Service Level Report for November 2025

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

UPTIME: 99.93% ✅
SLA TARGET: 99.5%
STATUS: SLA MET - No service credits applicable

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Service Metrics:
- Total API Requests: 2,458,932
- Average Response Time: 245ms
- Error Rate: 0.02%

Incidents:
- 1 incident on Nov 13, 2025 (15 minutes downtime)
- Root Cause: Database query optimization needed
- Resolution: Index added, performance restored

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Thank you for using HRMS!

View detailed metrics: https://status.hrms.com
Contact support: support@hrms.com
```

---

## Advanced SLO Concepts

### Multi-Window Multi-Burn-Rate Alerts

**Problem:** Simple threshold alerts are either too noisy or too slow

**Solution:** Alert based on burn rate over multiple time windows

**Example:**

```yaml
- alert: FastBurnRate
  expr: |
    # Burn rate over 1 hour > 14.4x (will exhaust budget in 2 days)
    (
      sum(rate(http_requests_total{status=~"5.."}[1h])) /
      sum(rate(http_requests_total[1h]))
    ) > 0.00144
  for: 5m
  labels:
    severity: critical

- alert: SlowBurnRate
  expr: |
    # Burn rate over 6 hours > 6x (will exhaust budget in 5 days)
    (
      sum(rate(http_requests_total{status=~"5.."}[6h])) /
      sum(rate(http_requests_total[6h]))
    ) > 0.0006
  for: 30m
  labels:
    severity: high
```

**Burn Rate Table:**

| Alert Window | Burn Rate | Budget Depletion | Alert Severity |
|--------------|-----------|------------------|----------------|
| 1 hour | 14.4x | 2 days | Critical |
| 6 hours | 6x | 5 days | High |
| 24 hours | 3x | 10 days | Medium |

---

### User-Journey SLO

**Concept:** Measure SLO for critical user journeys, not just individual endpoints

**Example: "Employee Leave Request Journey"**

1. Login (< 300ms)
2. Navigate to Leave Page (< 500ms)
3. Select Leave Type (< 200ms)
4. Submit Leave Request (< 1000ms)
5. Receive Confirmation (< 500ms)

**Total Journey SLO:** 95% of journeys complete in < 2.5 seconds

**Measurement:**

```javascript
// Frontend: Track journey with correlation ID
const journeyId = uuid();
const startTime = Date.now();

// Track each step
trackEvent('LeaveJourney_Start', { journeyId });
await login(); // Step 1
trackEvent('LeaveJourney_Login', { journeyId, duration: Date.now() - startTime });

// ... rest of journey

const totalDuration = Date.now() - startTime;
trackEvent('LeaveJourney_Complete', { journeyId, totalDuration });
```

---

## Cost of Reliability

### Cost vs SLO Trade-off

| Target Uptime | Cost Multiplier | Why? |
|---------------|-----------------|------|
| 90% | 1x | Single server, no redundancy |
| 99% | 2x | Active-passive failover |
| 99.9% | 5x | Active-active, multi-zone |
| 99.95% | 10x | Multi-region, advanced monitoring |
| 99.99% | 100x | Global distribution, chaos engineering |

**Reality Check:**
- 99.9% → 99.95% costs 2x more for 21 minutes/month improvement
- 99.95% → 99.99% costs 10x more for 17 minutes/month improvement

**Recommendation for HRMS:**
- **Starter/Professional:** 99.5% SLA (cost-effective, acceptable for SMEs)
- **Business:** 99.9% SLA (meets enterprise expectations)
- **Enterprise:** 99.95% SLA (for mission-critical customers)

---

## Summary

### SLO/SLA Quick Reference

```
┌─────────────────────────────────────────────────────────┐
│                   SLO TARGETS                           │
├─────────────────────────────────────────────────────────┤
│ Availability:        99.9% (43.2 min downtime/month)    │
│ Latency (p95):       < 500ms                            │
│ Latency (p99):       < 1000ms                           │
│ Error Rate:          < 0.1%                             │
│ Data Durability:     99.99%                             │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   SLA COMMITMENTS                       │
├─────────────────────────────────────────────────────────┤
│ Professional Plan:   99.5% uptime guaranteed            │
│ Business Plan:       99.9% uptime guaranteed            │
│ Enterprise Plan:     99.95% uptime guaranteed           │
│                                                          │
│ Breach Penalty:      10-50% service credit              │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   ERROR BUDGET                          │
├─────────────────────────────────────────────────────────┤
│ Monthly Allowance:   0.1% of requests (100K errors)     │
│ Budget > 50%:        Deploy freely                      │
│ Budget < 50%:        Freeze features                    │
│ Budget = 0%:         Emergency reliability fixes only   │
└─────────────────────────────────────────────────────────┘
```

---

**Document Owner:** SRE Team & Product Management
**Review Frequency:** Quarterly
**Last Review:** November 17, 2025
**Next Review:** February 17, 2026

**Approval:**
- [ ] CTO: ___________________
- [ ] VP Engineering: ___________________
- [ ] VP Product: ___________________
- [ ] Legal: ___________________
