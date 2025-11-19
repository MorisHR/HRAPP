# Alerting Strategy
## Fortune 500-Grade Incident Response for Multi-Tenant HRMS

**Document Version:** 1.0
**Last Updated:** November 17, 2025
**Status:** Production-Ready Alerting Framework

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Alert Priority Levels](#alert-priority-levels)
3. [Critical (P1) Alerts](#critical-p1-alerts)
4. [High (P2) Alerts](#high-p2-alerts)
5. [Medium (P3) Alerts](#medium-p3-alerts)
6. [Low (P4) Alerts](#low-p4-alerts)
7. [Escalation Paths](#escalation-paths)
8. [On-Call Rotation](#on-call-rotation)
9. [Incident Response Playbooks](#incident-response-playbooks)
10. [Communication Channels](#communication-channels)

---

## Executive Summary

### Alerting Philosophy

**Key Principles:**
1. **Alert Fatigue Prevention:** Only alert on actionable items
2. **Clear Ownership:** Every alert has a designated responder
3. **Escalation Automation:** Auto-escalate if not acknowledged
4. **Business Context:** Alerts include tenant impact and severity
5. **Runbook Driven:** Every alert links to resolution playbook

### Alert Quality Metrics

| Metric | Target | Current | Notes |
|--------|--------|---------|-------|
| **Alert-to-Incident Ratio** | < 1.5 | TBD | Max 1.5 alerts per real incident |
| **Mean Time to Acknowledge (MTTA)** | < 5 min | TBD | For P1 alerts |
| **Mean Time to Resolve (MTTR)** | < 30 min | TBD | For P1 alerts |
| **False Positive Rate** | < 10% | TBD | Alerts that don't require action |
| **Alert Coverage** | > 95% | TBD | % of incidents caught by alerts |

### Alert Routing Summary

```
┌─────────────────────────────────────────────────────┐
│                  Alert Source                        │
│  (Prometheus, Application Insights, Datadog)        │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────┐
│              Alert Manager                          │
│  (Route by Severity, Service, Time)                │
└────────────────────┬───────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
┌─────────────┐ ┌─────────┐ ┌─────────┐
│ PagerDuty   │ │  Slack  │ │  Email  │
│ (P1, P2)    │ │ (All)   │ │(P3, P4) │
└─────────────┘ └─────────┘ └─────────┘
        │
        ▼
┌─────────────────────────────────────┐
│     On-Call Engineer                 │
│  (Auto-escalate if no ACK in 5 min) │
└─────────────────────────────────────┘
```

---

## Alert Priority Levels

### Priority Classification

| Priority | SLA | Response Time | Escalation | Communication | Examples |
|----------|-----|---------------|------------|---------------|----------|
| **P1 - Critical** | Immediate | < 5 minutes | After 5 min | PagerDuty + Slack + Email | Complete outage, data loss |
| **P2 - High** | < 1 hour | < 15 minutes | After 30 min | Slack + Email | Partial outage, degraded performance |
| **P3 - Medium** | < 4 hours | < 1 hour | After 2 hours | Slack + Email | Performance degradation, non-critical errors |
| **P4 - Low** | < 24 hours | Next business day | None | Email | Warnings, potential issues |

### Priority Decision Tree

```
Is the system down or data at risk?
├─ Yes → P1 (Critical)
└─ No
   └─ Are customers affected right now?
      ├─ Yes
      │  └─ Is core functionality broken?
      │     ├─ Yes → P2 (High)
      │     └─ No → P3 (Medium)
      └─ No → P4 (Low)
```

---

## Critical (P1) Alerts

### P1-1: Application Down

**Condition:** API health check fails for > 2 minutes

**Impact:** Complete service outage, all customers affected

**Alert Rule (Prometheus):**

```yaml
- alert: ApplicationDown
  expr: up{job="hrms-api"} == 0
  for: 2m
  labels:
    severity: critical
    priority: P1
    service: hrms-api
  annotations:
    summary: "HRMS API is DOWN"
    description: "The HRMS API has been down for more than 2 minutes. All customers are affected."
    runbook_url: "https://wiki.company.com/runbooks/api-down"
    dashboard: "https://grafana.company.com/d/hrms-overview"
```

**Alert Rule (Application Insights):**

```kusto
requests
| where timestamp > ago(5m)
| summarize FailureRate = 100.0 * countif(success == false) / count()
| where FailureRate > 90
```

**Response Actions:**

1. **Immediate (< 2 min):**
   - Acknowledge alert in PagerDuty
   - Check status page: Is this a known issue?
   - Verify from external monitoring (UptimeRobot)

2. **Diagnosis (2-5 min):**
   ```bash
   # Check pod status
   kubectl get pods -n production

   # Check pod logs
   kubectl logs -n production deployment/hrms-api --tail=100

   # Check recent deployments
   kubectl rollout history deployment/hrms-api -n production
   ```

3. **Mitigation (5-10 min):**
   - If recent deployment: Rollback
   - If pod crash: Check resource limits
   - If database issue: Failover to replica
   - Update status page

4. **Communication:**
   - Post in #incidents Slack channel
   - Notify customer success team
   - Update status page: "Investigating outage"

**Escalation:** If not resolved in 15 minutes, escalate to Engineering Manager

**Runbook:** [Application Down Playbook](#playbook-application-down)

---

### P1-2: Database Connection Failure

**Condition:** Database connection pool exhausted OR connection errors > 10% for > 3 minutes

**Impact:** All database-dependent operations fail (most of the app)

**Alert Rule:**

```yaml
- alert: DatabaseConnectionFailure
  expr: |
    (
      sum(pg_stat_database_numbackends) / max(pg_settings_max_connections) > 0.95
    ) or (
      rate(database_connection_errors_total[5m]) > 0.1
    )
  for: 3m
  labels:
    severity: critical
    priority: P1
    service: postgresql
  annotations:
    summary: "Database connection failure"
    description: "Database connection pool exhausted or high connection error rate"
    runbook_url: "https://wiki.company.com/runbooks/db-connection-failure"
```

**Response Actions:**

```bash
# Check PostgreSQL connection status
kubectl exec -it postgres-0 -- psql -U hrms_admin -d hrms_master -c "
SELECT
    count(*) AS active_connections,
    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') AS max_connections,
    count(*) FILTER (WHERE state = 'idle in transaction') AS idle_in_transaction
FROM pg_stat_activity;
"

# Kill idle connections if needed
kubectl exec -it postgres-0 -- psql -U hrms_admin -d hrms_master -c "
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle in transaction' AND now() - query_start > interval '10 minutes';
"

# Check application connection pool settings
kubectl get configmap hrms-api-config -o yaml | grep -i connection
```

---

### P1-3: High Error Rate (> 5%)

**Condition:** Error rate exceeds 5% for > 5 minutes

**Impact:** Significant number of customer requests failing

**Alert Rule:**

```yaml
- alert: HighErrorRate
  expr: |
    sum(rate(http_requests_total{status=~"5.."}[5m])) /
    sum(rate(http_requests_total[5m])) > 0.05
  for: 5m
  labels:
    severity: critical
    priority: P1
    service: hrms-api
  annotations:
    summary: "High API error rate: {{ $value | humanizePercentage }}"
    description: "Error rate is {{ $value | humanizePercentage }} (threshold: 5%)"
    dashboard: "https://grafana.company.com/d/errors"
```

**Response Actions:**

1. **Identify error pattern:**
   ```bash
   # Check error distribution by endpoint
   kubectl logs -n production deployment/hrms-api --tail=1000 | grep "ERROR" | awk '{print $5}' | sort | uniq -c | sort -rn | head -20
   ```

2. **Check recent changes:**
   ```bash
   # Recent deployments
   kubectl rollout history deployment/hrms-api -n production

   # Recent config changes
   git log --oneline --since="2 hours ago" -- kubernetes/
   ```

3. **Immediate mitigation:**
   - If deployment-related: Rollback
   - If config-related: Revert configuration
   - If third-party API issue: Enable circuit breaker

---

### P1-4: API Response Time > 5 seconds (p95)

**Condition:** 95th percentile API response time > 5 seconds for > 10 minutes

**Impact:** Severe performance degradation, likely causing timeouts

**Alert Rule:**

```yaml
- alert: ExtremelySlowAPI
  expr: |
    histogram_quantile(0.95,
      sum(rate(http_request_duration_seconds_bucket[5m])) by (le, endpoint)
    ) > 5
  for: 10m
  labels:
    severity: critical
    priority: P1
    service: hrms-api
  annotations:
    summary: "API response time critically slow: {{ $value }}s"
    description: "P95 response time is {{ $value }}s (threshold: 5s)"
```

**Response Actions:**

1. **Identify slow endpoints:**
   ```sql
   -- In Application Insights
   requests
   | where timestamp > ago(10m)
   | summarize p95=percentile(duration, 95) by name
   | where p95 > 5000
   | order by p95 desc
   ```

2. **Check resource utilization:**
   ```bash
   kubectl top pods -n production
   kubectl top nodes
   ```

3. **Check database performance:**
   ```sql
   -- Slow queries
   SELECT query, calls, mean_exec_time, total_exec_time
   FROM pg_stat_statements
   WHERE mean_exec_time > 1000
   ORDER BY total_exec_time DESC
   LIMIT 10;
   ```

---

### P1-5: Security Breach Detected

**Condition:** Unauthorized access detected OR security scan alert

**Impact:** Data security compromise, potential data breach

**Alert Sources:**
- Failed login attempts > 100 in 5 minutes
- SQL injection attempt detected
- Unauthorized admin action
- Suspicious file access patterns

**Alert Rule:**

```yaml
- alert: PotentialSecurityBreach
  expr: |
    sum(rate(login_failed_total[5m])) > 20 or
    sum(rate(sql_injection_attempts_total[5m])) > 0 or
    sum(rate(unauthorized_access_attempts_total[5m])) > 5
  for: 2m
  labels:
    severity: critical
    priority: P1
    service: security
    escalate_to: security-team
  annotations:
    summary: "SECURITY: Potential breach detected"
    description: "Suspicious security activity detected"
    runbook_url: "https://wiki.company.com/runbooks/security-breach"
```

**Response Actions:**

1. **Immediate:**
   - Notify Security Team
   - Do NOT disable alerts or monitoring
   - Preserve logs and evidence

2. **Investigation:**
   - Review authentication logs
   - Check IP addresses and geolocation
   - Review recent user actions

3. **Containment:**
   - Block suspicious IP addresses
   - Disable compromised accounts
   - Rotate credentials if needed

---

## High (P2) Alerts

### P2-1: High Memory Usage (> 80%)

**Condition:** Memory usage > 80% for > 10 minutes

**Alert Rule:**

```yaml
- alert: HighMemoryUsage
  expr: |
    (1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) > 0.8
  for: 10m
  labels:
    severity: high
    priority: P2
  annotations:
    summary: "High memory usage on {{ $labels.node }}"
    description: "Memory usage is {{ $value | humanizePercentage }}"
```

**Response Actions:**

```bash
# Check memory usage by pod
kubectl top pods --sort-by=memory -n production

# Check for memory leaks
kubectl exec -it <pod-name> -- ps aux --sort=-%mem | head -20

# Scale if needed
kubectl scale deployment hrms-api --replicas=5 -n production
```

---

### P2-2: High CPU Usage (> 80%)

**Condition:** CPU usage > 80% for > 10 minutes

**Alert Rule:**

```yaml
- alert: HighCPUUsage
  expr: |
    100 - (avg by (node) (irate(node_cpu_seconds_total{mode="idle"}[5m])) * 100) > 80
  for: 10m
  labels:
    severity: high
    priority: P2
  annotations:
    summary: "High CPU usage on {{ $labels.node }}"
    description: "CPU usage is {{ $value }}%"
```

---

### P2-3: Slow Database Queries

**Condition:** Queries taking > 5 seconds detected

**Alert Rule:**

```yaml
- alert: SlowDatabaseQueries
  expr: |
    pg_stat_statements_mean_exec_time_seconds > 5
  for: 5m
  labels:
    severity: high
    priority: P2
  annotations:
    summary: "Slow database queries detected"
    description: "Average query time is {{ $value }}s"
```

---

### P2-4: Failed Deployments

**Condition:** Kubernetes deployment fails to rollout

**Alert Rule:**

```yaml
- alert: DeploymentFailed
  expr: |
    kube_deployment_status_replicas_unavailable > 0
  for: 10m
  labels:
    severity: high
    priority: P2
  annotations:
    summary: "Deployment {{ $labels.deployment }} has unavailable replicas"
```

**Response Actions:**

```bash
# Check deployment status
kubectl rollout status deployment/hrms-api -n production

# View deployment events
kubectl describe deployment hrms-api -n production

# Check pod errors
kubectl get pods -n production | grep Error

# Rollback if needed
kubectl rollout undo deployment/hrms-api -n production
```

---

## Medium (P3) Alerts

### P3-1: Moderate Error Rate (2-5%)

**Alert Rule:**

```yaml
- alert: ModerateErrorRate
  expr: |
    sum(rate(http_requests_total{status=~"5.."}[5m])) /
    sum(rate(http_requests_total[5m])) > 0.02
  for: 15m
  labels:
    severity: medium
    priority: P3
  annotations:
    summary: "Moderate error rate: {{ $value | humanizePercentage }}"
```

---

### P3-2: Slow API Responses (2-5s)

**Alert Rule:**

```yaml
- alert: SlowAPIResponses
  expr: |
    histogram_quantile(0.95,
      sum(rate(http_request_duration_seconds_bucket[5m])) by (le)
    ) > 2 and
    histogram_quantile(0.95,
      sum(rate(http_request_duration_seconds_bucket[5m])) by (le)
    ) < 5
  for: 15m
  labels:
    severity: medium
    priority: P3
```

---

### P3-3: Cache Hit Ratio < 70%

**Alert Rule:**

```yaml
- alert: LowCacheHitRatio
  expr: |
    rate(redis_keyspace_hits_total[5m]) /
    (rate(redis_keyspace_hits_total[5m]) + rate(redis_keyspace_misses_total[5m])) < 0.7
  for: 30m
  labels:
    severity: medium
    priority: P3
  annotations:
    summary: "Redis cache hit ratio is {{ $value | humanizePercentage }}"
```

---

## Low (P4) Alerts

### P4-1: Bundle Size Increase

**Alert Rule:**

```yaml
- alert: BundleSizeIncrease
  expr: |
    frontend_bundle_size_bytes > frontend_bundle_size_bytes offset 1w * 1.1
  for: 1h
  labels:
    severity: low
    priority: P4
  annotations:
    summary: "Frontend bundle size increased by > 10%"
```

---

### P4-2: Test Coverage Decrease

**Alert Rule:**

```yaml
- alert: TestCoverageDecrease
  expr: |
    test_coverage_percentage < test_coverage_percentage offset 1w - 5
  labels:
    severity: low
    priority: P4
  annotations:
    summary: "Test coverage decreased by {{ $value }}%"
```

---

### P4-3: Code Quality Issues

**Alert Rule:**

```yaml
- alert: CodeQualityIssues
  expr: |
    sonarqube_code_smells > 100 or
    sonarqube_bugs > 10 or
    sonarqube_security_hotspots > 5
  labels:
    severity: low
    priority: P4
```

---

## Escalation Paths

### Escalation Matrix

| Alert Priority | Primary Responder | Escalate After | Escalate To | Further Escalation |
|----------------|-------------------|----------------|-------------|-------------------|
| **P1** | On-call Engineer | 5 minutes | Engineering Manager | CTO (after 30 min) |
| **P2** | On-call Engineer | 30 minutes | Team Lead | Engineering Manager (after 2 hours) |
| **P3** | Team on rotation | 2 hours | Team Lead | None |
| **P4** | Next business day | N/A | N/A | None |

### PagerDuty Escalation Policy

```yaml
escalation_policy:
  name: "HRMS Production Escalation"
  escalation_rules:
    - escalation_delay_in_minutes: 5
      targets:
        - type: user
          id: on-call-engineer
    - escalation_delay_in_minutes: 10
      targets:
        - type: user
          id: engineering-manager
    - escalation_delay_in_minutes: 20
      targets:
        - type: user
          id: cto
```

---

## On-Call Rotation

### Rotation Schedule

**Primary On-Call:** Weekly rotation (Monday 9 AM - Monday 9 AM)

**Team:**
- Engineer 1: Week 1, 5, 9...
- Engineer 2: Week 2, 6, 10...
- Engineer 3: Week 3, 7, 11...
- Engineer 4: Week 4, 8, 12...

### On-Call Responsibilities

**Before Your Shift:**
- Review recent incidents
- Verify access to all systems
- Test PagerDuty notifications
- Review runbooks

**During Your Shift:**
- Respond to P1/P2 alerts within SLA
- Document all incidents
- Update status page for customer-facing issues
- Hand off any ongoing issues

**After Your Shift:**
- Complete incident reports
- Update runbooks if needed
- Share learnings with team

### On-Call Compensation

- **Weekday on-call:** $100/week stipend
- **Weekend on-call:** $200/weekend
- **Incident response:** 1.5x hourly rate (minimum 1 hour)

---

## Incident Response Playbooks

### Playbook: Application Down

**Symptoms:**
- Health check endpoint returns 503
- All API requests failing
- Users cannot access application

**Diagnosis Steps:**

1. **Check Kubernetes pods:**
   ```bash
   kubectl get pods -n production -l app=hrms-api
   kubectl describe pod <pod-name> -n production
   kubectl logs <pod-name> -n production --tail=100
   ```

2. **Check recent deployments:**
   ```bash
   kubectl rollout history deployment/hrms-api -n production
   ```

3. **Check database connectivity:**
   ```bash
   kubectl exec -it <api-pod> -n production -- nc -zv postgres-service 5432
   ```

**Resolution Steps:**

**Scenario 1: Recent Deployment Issue**
```bash
# Rollback to previous version
kubectl rollout undo deployment/hrms-api -n production

# Verify rollback
kubectl rollout status deployment/hrms-api -n production
```

**Scenario 2: Database Connection Issue**
```bash
# Check database pod
kubectl get pods -n production -l app=postgresql

# Restart database connection pool
kubectl rollout restart deployment/hrms-api -n production
```

**Scenario 3: Resource Exhaustion**
```bash
# Scale up
kubectl scale deployment hrms-api --replicas=10 -n production

# Check resource limits
kubectl describe pod <pod-name> | grep -A 5 Limits
```

**Verification:**
```bash
# Test health endpoint
curl https://api.hrms.com/health

# Monitor error rate
# Check Grafana dashboard
```

**Communication:**
```
[Slack #incidents]
🔴 RESOLVED: Application Down Incident

Duration: 12 minutes
Root Cause: Recent deployment introduced null reference exception
Resolution: Rolled back to v1.2.3
Impact: ~500 users affected, 12 minutes downtime
Next Steps: Fix bug in v1.2.4, add integration test
```

---

### Playbook: High Database Load

**Symptoms:**
- Slow query performance
- Connection pool exhaustion
- API response times degraded

**Diagnosis:**

```sql
-- Find slow queries
SELECT
    query,
    calls,
    mean_exec_time,
    total_exec_time
FROM pg_stat_statements
WHERE mean_exec_time > 1000
ORDER BY total_exec_time DESC
LIMIT 20;

-- Find blocking queries
SELECT
    blocked.pid AS blocked_pid,
    blocking.pid AS blocking_pid,
    blocked.query AS blocked_query,
    blocking.query AS blocking_query
FROM pg_stat_activity AS blocked
JOIN pg_stat_activity AS blocking ON blocking.pid = ANY(pg_blocking_pids(blocked.pid))
WHERE blocked.wait_event_type = 'Lock';
```

**Resolution:**

```sql
-- Kill long-running queries
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'active' AND now() - query_start > interval '5 minutes';

-- Reset stats if needed
SELECT pg_stat_statements_reset();
```

---

## Communication Channels

### Channel Configuration

| Channel | Use Case | Alerts Routed | Response Expected |
|---------|----------|---------------|-------------------|
| **PagerDuty** | P1, P2 alerts | Critical incidents | Immediate acknowledgment |
| **Slack #incidents** | All incidents | P1, P2, P3 | Team awareness, collaboration |
| **Slack #alerts** | Non-urgent alerts | P3, P4 | Review during business hours |
| **Email** | Low priority | P4 | Review next business day |
| **Status Page** | Customer communication | P1, P2 (customer-facing) | Update every 30 minutes |

### Slack Integration

**AlertManager Slack Webhook:**

```yaml
receivers:
  - name: 'slack-critical'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'
        channel: '#incidents'
        title: '{{ .GroupLabels.alertname }}'
        text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
        color: 'danger'

  - name: 'slack-alerts'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK/URL'
        channel: '#alerts'
        title: '{{ .GroupLabels.alertname }}'
        text: '{{ range .Alerts }}{{ .Annotations.description }}{{ end }}'
        color: 'warning'
```

### Status Page Template

**Incident Update Format:**

```
[Nov 17, 2025 - 14:35 UTC] Investigating
We are investigating reports of slow API response times.
Affected services: Employee Management, Payroll

[Nov 17, 2025 - 14:50 UTC] Identified
We have identified the issue as a slow database query.
Our team is working on a fix.

[Nov 17, 2025 - 15:10 UTC] Resolved
The issue has been resolved. All services are operating normally.
Root cause: Unoptimized query in payroll calculation.
Post-mortem will be published within 24 hours.
```

---

## Alert Configuration Examples

### Complete AlertManager Configuration

```yaml
global:
  resolve_timeout: 5m
  slack_api_url: 'https://hooks.slack.com/services/YOUR/WEBHOOK'
  pagerduty_url: 'https://events.pagerduty.com/v2/enqueue'

route:
  receiver: 'default'
  group_by: ['alertname', 'cluster', 'service']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h

  routes:
    # Critical alerts
    - match:
        priority: P1
      receiver: 'pagerduty-critical'
      continue: true

    - match:
        priority: P1
      receiver: 'slack-critical'

    # High priority
    - match:
        priority: P2
      receiver: 'pagerduty-high'
      continue: true

    - match:
        priority: P2
      receiver: 'slack-incidents'

    # Medium/Low priority
    - match:
        priority: P3
      receiver: 'slack-alerts'

    - match:
        priority: P4
      receiver: 'email-notifications'

receivers:
  - name: 'default'
    slack_configs:
      - channel: '#alerts'

  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: 'YOUR-PAGERDUTY-KEY'
        severity: 'critical'

  - name: 'slack-critical'
    slack_configs:
      - channel: '#incidents'
        color: 'danger'
        title: '🚨 CRITICAL: {{ .GroupLabels.alertname }}'

  - name: 'email-notifications'
    email_configs:
      - to: 'team@company.com'
        from: 'alerts@company.com'
```

---

## Cost Estimate

### Monthly Alerting Infrastructure Costs

| Component | Tool | Cost |
|-----------|------|------|
| PagerDuty | Professional plan (10 users) | $249/month |
| Slack | Standard plan | $8/user/month = $80 |
| Status Page | Statuspage.io | $79/month |
| **Total** | | **$408/month** |

**Alternative (Budget):**
- Use Alertmanager (free) + Email/Slack (free tier) = $0/month
- Use OpsGenie instead of PagerDuty = $99/month

---

## Success Metrics

### Alert Quality KPIs (Review Monthly)

```sql
-- Alert effectiveness report
WITH alert_metrics AS (
    SELECT
        COUNT(*) AS total_alerts,
        COUNT(DISTINCT incident_id) AS unique_incidents,
        AVG(EXTRACT(EPOCH FROM (acknowledged_at - triggered_at))) AS avg_ack_time_seconds,
        AVG(EXTRACT(EPOCH FROM (resolved_at - triggered_at))) AS avg_resolution_time_seconds,
        COUNT(*) FILTER (WHERE was_actionable = false) AS false_positives
    FROM alerts
    WHERE triggered_at >= CURRENT_DATE - INTERVAL '30 days'
)
SELECT
    total_alerts,
    unique_incidents,
    total_alerts::float / NULLIF(unique_incidents, 0) AS alert_to_incident_ratio,
    avg_ack_time_seconds / 60 AS mtta_minutes,
    avg_resolution_time_seconds / 60 AS mttr_minutes,
    (false_positives::float / total_alerts) * 100 AS false_positive_rate
FROM alert_metrics;
```

**Target KPIs:**
- Alert-to-Incident Ratio: < 1.5
- MTTA (P1): < 5 minutes
- MTTR (P1): < 30 minutes
- False Positive Rate: < 10%

---

**Document Owner:** SRE Team
**Review Frequency:** Monthly
**Last Review:** November 17, 2025
**Next Review:** December 17, 2025
