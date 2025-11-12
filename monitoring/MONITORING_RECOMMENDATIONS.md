# Database Monitoring Recommendations

**Version:** 1.0
**Date:** 2025-11-12
**For:** HRMS Database Infrastructure

## Executive Summary

This document provides recommendations for monitoring tools, infrastructure setup, and best practices for the HRMS database deployment with 4 major migrations introducing unique constraints, composite indexes, CHECK constraints, and column-level encryption.

---

## Recommended Monitoring Stack

### Option 1: Prometheus + Grafana (Recommended for Cloud/On-Premise)

**Pros:**
- Industry-standard, battle-tested
- Excellent visualization with Grafana
- Rich ecosystem of exporters
- Strong alerting capabilities
- Free and open-source
- Works great with Kubernetes

**Cons:**
- Requires setup and maintenance
- Learning curve for PromQL
- Need to host infrastructure

**Setup:**
```bash
# Install Prometheus
docker run -d -p 9090:9090 \
  -v /workspaces/HRAPP/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus

# Install Grafana
docker run -d -p 3000:3000 \
  -v grafana-storage:/var/lib/grafana \
  grafana/grafana

# Install PostgreSQL Exporter
docker run -d -p 9187:9187 \
  -e DATA_SOURCE_NAME="postgresql://postgres@localhost:5432/hrms_db?sslmode=disable" \
  prometheuscommunity/postgres-exporter
```

**Cost:** Free (infrastructure costs only)

---

### Option 2: Google Cloud Monitoring (Recommended for Google Cloud)

**Pros:**
- Native integration with Cloud SQL
- Automatic setup for GCP resources
- Built-in alerting to email/SMS/Slack
- Managed service (no maintenance)
- Excellent for multi-tenant monitoring

**Cons:**
- Google Cloud vendor lock-in
- Costs scale with metrics volume
- Limited customization vs Prometheus

**Setup:**
```bash
# Enable Cloud Monitoring API
gcloud services enable monitoring.googleapis.com

# Install Cloud Monitoring Agent on VMs
curl -sSO https://dl.google.com/cloudagents/add-google-cloud-ops-agent-repo.sh
sudo bash add-google-cloud-ops-agent-repo.sh --also-install

# Configure application metrics
# Add to Program.cs:
services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddPrometheusExporter()
        .AddGoogleCloudMonitoring());
```

**Cost:** ~$50-200/month depending on metrics volume

---

### Option 3: AWS CloudWatch (For AWS Deployments)

**Pros:**
- Native integration with RDS PostgreSQL
- Automatic setup for AWS resources
- Built-in alerting (SNS, Email, Lambda)
- Good for AWS-native deployments

**Cons:**
- AWS vendor lock-in
- Can get expensive with high-resolution metrics
- Dashboard customization limited

**Setup:**
```bash
# Enable Enhanced Monitoring on RDS
aws rds modify-db-instance \
    --db-instance-identifier hrms-db \
    --monitoring-interval 1 \
    --monitoring-role-arn arn:aws:iam::account:role/rds-monitoring-role

# Install CloudWatch Agent
wget https://s3.amazonaws.com/amazoncloudwatch-agent/ubuntu/amd64/latest/amazon-cloudwatch-agent.deb
sudo dpkg -i amazon-cloudwatch-agent.deb
```

**Cost:** ~$30-100/month

---

### Option 4: Azure Monitor (For Azure Deployments)

**Pros:**
- Native integration with Azure Database for PostgreSQL
- Built-in alerting
- Good Azure ecosystem integration

**Cons:**
- Azure vendor lock-in
- Learning curve for KQL queries

**Cost:** ~$40-120/month

---

## Our Recommendation

**For HRMS:** **Prometheus + Grafana**

**Reasoning:**
1. **Cloud-agnostic**: Not locked into one vendor
2. **Cost-effective**: Open-source with predictable costs
3. **Rich ecosystem**: PostgreSQL exporter is excellent
4. **Customization**: Full control over dashboards and alerts
5. **Industry standard**: Easy to find expertise

**Alternative for Google Cloud:** Use Cloud Monitoring for infrastructure, Prometheus for application-specific metrics

---

## Monitoring Tool Setup Guide

### 1. Install Prometheus

```yaml
# /workspaces/HRAPP/monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets:
            - alertmanager:9093

rule_files:
  - "alerts.yml"

scrape_configs:
  # PostgreSQL Exporter
  - job_name: 'postgresql'
    static_configs:
      - targets: ['postgres-exporter:9187']
        labels:
          environment: 'production'
          database: 'hrms_db'

  # Application Metrics
  - job_name: 'hrms-api'
    static_configs:
      - targets: ['hrms-api:5000']
        labels:
          environment: 'production'
```

### 2. Install PostgreSQL Exporter

```bash
# Create docker-compose.yml for monitoring stack
cat > /workspaces/HRAPP/monitoring/docker-compose.yml << 'EOF'
version: '3.8'

services:
  postgres-exporter:
    image: prometheuscommunity/postgres-exporter:latest
    environment:
      DATA_SOURCE_NAME: "postgresql://postgres:password@postgres:5432/hrms_db?sslmode=disable"
      PG_EXPORTER_EXTEND_QUERY_PATH: "/etc/postgres_exporter/queries.yaml"
    volumes:
      - ./postgres-exporter-queries.yaml:/etc/postgres_exporter/queries.yaml
    ports:
      - "9187:9187"
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - ./database-alerts.yaml:/etc/prometheus/alerts.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
    restart: unless-stopped

  alertmanager:
    image: prom/alertmanager:latest
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
    ports:
      - "9093:9093"
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana-dashboard.json:/etc/grafana/provisioning/dashboards/hrms-database.json
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_INSTALL_PLUGINS=grafana-piechart-panel
    restart: unless-stopped

volumes:
  prometheus-data:
  grafana-data:
EOF
```

### 3. Custom PostgreSQL Queries

```yaml
# /workspaces/HRAPP/monitoring/postgres-exporter-queries.yaml
# Custom queries for HRMS-specific monitoring

# Migration status
migration_status:
  query: |
    SELECT
      CASE
        WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'tenant_default' AND table_name = '__EFMigrationsHistory')
        THEN 1
        ELSE 0
      END as migration_table_exists
  metrics:
    - migration_table_exists:
        usage: "GAUGE"
        description: "Whether migration history table exists"

# Encryption health check
encryption_health:
  query: |
    SELECT
      COUNT(*) FILTER (WHERE "NationalIdCard" IS NOT NULL) as encrypted_records,
      COUNT(*) FILTER (WHERE "NationalIdCard" ~ '^[0-9]{14}$') as plaintext_records
    FROM tenant_default."Employees"
  metrics:
    - encrypted_records:
        usage: "GAUGE"
        description: "Number of employees with encrypted National ID"
    - plaintext_records:
        usage: "GAUGE"
        description: "Number of employees with plaintext National ID (should be 0)"

# Constraint violation attempts (from logs)
constraint_violations:
  query: |
    SELECT
      conname as constraint_name,
      0 as violations  -- Constraints prevent violations; track via application metrics
    FROM pg_constraint
    WHERE connamespace = 'tenant_default'::regnamespace
    AND conname LIKE 'chk_%'
  metrics:
    - violations:
        usage: "GAUGE"
        description: "Constraint violations attempted"
        labels: [constraint_name]

# Index usage for new migration indexes
index_usage:
  query: |
    SELECT
      indexrelname as index_name,
      idx_scan as scans,
      idx_tup_read as tuples_read,
      idx_tup_fetch as tuples_fetched
    FROM pg_stat_user_indexes
    WHERE schemaname = 'tenant_default'
    AND (
      indexrelname LIKE 'IX_Employees_%_Unique'
      OR indexrelname LIKE 'IX_PayrollCycles_%'
      OR indexrelname LIKE 'IX_Attendances_%'
      OR indexrelname LIKE 'IX_BiometricPunchRecords_%'
    )
  metrics:
    - scans:
        usage: "COUNTER"
        description: "Number of index scans"
        labels: [index_name]
    - tuples_read:
        usage: "COUNTER"
        description: "Number of tuples read from index"
        labels: [index_name]
    - tuples_fetched:
        usage: "COUNTER"
        description: "Number of tuples fetched via index"
        labels: [index_name]
```

### 4. AlertManager Configuration

```yaml
# /workspaces/HRAPP/monitoring/alertmanager.yml
global:
  resolve_timeout: 5m
  smtp_smarthost: 'mail.smtp2go.com:2525'
  smtp_from: 'alerts@morishr.com'
  smtp_auth_username: 'noreply@infynexsolutions.com'
  smtp_auth_password: '${SMTP_PASSWORD}'

route:
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h
  receiver: 'email-default'

  routes:
    # Critical alerts - page immediately
    - match:
        severity: CRITICAL
      receiver: 'pagerduty-critical'
      continue: true

    # Critical alerts - also send email
    - match:
        severity: CRITICAL
      receiver: 'email-critical'

    # Warning alerts - email only
    - match:
        severity: WARNING
      receiver: 'email-warnings'

    # Info alerts - Slack only
    - match:
        severity: INFO
      receiver: 'slack-info'

receivers:
  - name: 'email-default'
    email_configs:
      - to: 'engineering@morishr.com'
        headers:
          Subject: 'Database Alert: {{ .GroupLabels.alertname }}'

  - name: 'email-critical'
    email_configs:
      - to: 'security@morishr.com,admin@morishr.com,cto@morishr.com'
        headers:
          Subject: 'CRITICAL Database Alert: {{ .GroupLabels.alertname }}'

  - name: 'email-warnings'
    email_configs:
      - to: 'engineering@morishr.com'
        headers:
          Subject: 'Database Warning: {{ .GroupLabels.alertname }}'

  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: '${PAGERDUTY_SERVICE_KEY}'
        description: '{{ .GroupLabels.alertname }}: {{ .CommonAnnotations.description }}'

  - name: 'slack-info'
    slack_configs:
      - api_url: '${SLACK_WEBHOOK_URL}'
        channel: '#database-alerts'
        title: 'Database Info: {{ .GroupLabels.alertname }}'
        text: '{{ .CommonAnnotations.description }}'

inhibit_rules:
  - source_match:
      severity: 'CRITICAL'
    target_match:
      severity: 'WARNING'
    equal: ['alertname', 'database']
```

---

## Application-Level Metrics

### Add to HRMS.API Program.cs

```csharp
// Add Prometheus metrics endpoint
app.UseMetricServer(); // Exposes /metrics endpoint

// Custom metrics for encryption
var encryptionCounter = Metrics.CreateCounter(
    "encryption_operations_total",
    "Total encryption/decryption operations",
    new CounterConfiguration
    {
        LabelNames = new[] { "operation_type", "success" }
    });

var encryptionHistogram = Metrics.CreateHistogram(
    "encryption_duration_seconds",
    "Duration of encryption/decryption operations",
    new HistogramConfiguration
    {
        LabelNames = new[] { "operation_type" },
        Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
    });

// Migration metrics
var migrationGauge = Metrics.CreateGauge(
    "migration_status",
    "Current migration status (0=none, 1=running, 2=failed, 3=success)");

var constraintViolations = Metrics.CreateCounter(
    "constraint_violations_total",
    "Total constraint violation attempts",
    new CounterConfiguration
    {
        LabelNames = new[] { "constraint_name", "table_name" }
    });
```

---

## Long-Term Monitoring Plan

### Daily

- ✅ **Automated Health Check** (via cron)
  ```bash
  0 6 * * * /workspaces/HRAPP/scripts/monitor-database-health.sh > /var/log/hrms/daily-health-$(date +\%Y\%m\%d).log
  ```

- ✅ **Review Grafana Dashboard**
  - Check for anomalies
  - Review slow queries
  - Verify index usage

- ✅ **Check Alert History**
  - Any critical alerts overnight?
  - Patterns in warnings?

### Weekly

- ✅ **Performance Review**
  - Compare query times to baseline
  - Review cache hit ratios
  - Check connection pool usage

- ✅ **Capacity Planning**
  - Database size growth
  - Disk usage trends
  - Connection count trends

- ✅ **Index Maintenance**
  - Identify unused indexes
  - Check for missing indexes (via slow queries)

### Monthly

- ✅ **Comprehensive Performance Report**
  ```bash
  /workspaces/HRAPP/scripts/capture-performance-baseline.sh hrms_db monthly-report-$(date +\%Y-\%m).json
  ```

- ✅ **Security Audit**
  - Encryption service health
  - Verify passthrough mode never activated
  - Review failed decryption attempts

- ✅ **Backup Verification**
  - Test restore from backup
  - Verify backup integrity

### Quarterly

- ✅ **Disaster Recovery Drill**
  - Simulate database failure
  - Practice failover procedures
  - Update runbook based on learnings

- ✅ **Performance Optimization**
  - Review slow queries
  - Optimize indexes
  - Update statistics

- ✅ **Security Review**
  - Encryption key rotation
  - Access control audit
  - Vulnerability scanning

---

## Alerting Best Practices

### 1. Alert Fatigue Prevention

**Problem:** Too many alerts → People ignore them

**Solution:**
- Set appropriate thresholds (not too sensitive)
- Group related alerts
- Use severity levels correctly
- Suppress low-priority alerts during business hours

### 2. Alert Grouping

```yaml
# Good: Group by alertname
route:
  group_by: ['alertname', 'database']
  group_wait: 30s
  group_interval: 5m

# Bad: Too granular, too many notifications
route:
  group_by: []  # Every alert sends separately
```

### 3. Escalation Policy

| Time | Severity | Action |
|------|----------|--------|
| 0 min | CRITICAL | Page on-call engineer |
| 15 min | CRITICAL (unresolved) | Page manager + send email |
| 30 min | CRITICAL (unresolved) | Page CTO + create Zoom incident room |
| WARNING | - | Email only, no page |
| INFO | - | Slack only |

### 4. Alert Tuning

**Initially:** Set conservative thresholds to avoid missed issues
**After 2 weeks:** Review alert history and tune:
- Reduce noise (too many false positives?)
- Increase sensitivity (missed real issues?)
- Adjust timings (group_wait, repeat_interval)

---

## Cost Estimation

### Prometheus + Grafana (Self-Hosted)

| Component | Monthly Cost |
|-----------|--------------|
| VM for Prometheus (2 vCPU, 4GB RAM) | $20-40 |
| VM for Grafana (1 vCPU, 2GB RAM) | $10-20 |
| Disk for metrics storage (100GB SSD) | $10 |
| **Total** | **$40-70/month** |

### Google Cloud Monitoring

| Component | Monthly Cost |
|-----------|--------------|
| Metrics ingestion (estimated 500k data points/day) | $30-50 |
| Monitoring queries | $10-20 |
| Alerting policies | $5-10 |
| **Total** | **$45-80/month** |

### PagerDuty (Alerting)

| Plan | Monthly Cost | Features |
|------|--------------|----------|
| Professional | $21/user | Full features, unlimited alerts |
| Business | $41/user | Advanced features, analytics |

**Recommendation:** Start with 2 users on Professional plan = **$42/month**

---

## ROI Justification

### Cost of Downtime

**Assumptions:**
- 100 employees using system
- Average salary: $50/hour
- System downtime prevents work

**1 hour of downtime:** $5,000 in lost productivity
**Monitoring cost:** $100-150/month
**Monitoring ROI:** Prevents 1 hour downtime = 33-50x return

### Cost of Data Integrity Issues

- Failed payroll run: **Immeasurable impact** (legal, reputation)
- Encryption failure exposing PII: **Potentially millions** (GDPR fines, lawsuits)
- Data corruption requiring restore: **Hours of downtime**

**Conclusion:** Monitoring is not an expense, it's **essential insurance**.

---

## Quick Start Checklist

### Week 1: Setup Core Monitoring
- [ ] Deploy Prometheus + Grafana using docker-compose
- [ ] Install PostgreSQL exporter
- [ ] Import Grafana dashboard
- [ ] Configure basic alerts (database down, high connections)
- [ ] Test alerting to email

### Week 2: Capture Baseline
- [ ] Run baseline script before migrations
- [ ] Document current performance metrics
- [ ] Set up continuous monitoring script (cron)

### Week 3: Run Migrations with Monitoring
- [ ] Monitor migration 1 (Unique Constraints) in real-time
- [ ] Run post-migration health check
- [ ] Monitor migration 2 (Composite Indexes)
- [ ] Run post-migration health check
- [ ] Monitor migrations 3 & 4
- [ ] Capture post-migration baseline

### Week 4: Fine-Tune and Document
- [ ] Review alert history
- [ ] Tune alert thresholds
- [ ] Document any issues encountered
- [ ] Train team on monitoring tools
- [ ] Set up PagerDuty rotation

### Month 2+: Ongoing Operations
- [ ] Daily: Review Grafana dashboard
- [ ] Weekly: Performance review meeting
- [ ] Monthly: Generate performance report
- [ ] Quarterly: DR drill and optimization

---

## Additional Resources

### Documentation
- Prometheus: https://prometheus.io/docs/
- Grafana: https://grafana.com/docs/
- PostgreSQL Exporter: https://github.com/prometheus-community/postgres_exporter
- PostgreSQL Monitoring: https://www.postgresql.org/docs/current/monitoring-stats.html

### Training
- Prometheus Fundamentals: https://training.promlabs.com/
- Grafana Fundamentals: https://grafana.com/tutorials/
- PostgreSQL DBA Certification: https://www.enterprisedb.com/training

### Support
- HRMS Database Team: dba@morishr.com
- On-Call: Check PagerDuty schedule
- Emergency: critical-incidents@morishr.com

---

**Document Owner:** Database SRE Team
**Last Updated:** 2025-11-12
**Next Review:** 2025-12-12
