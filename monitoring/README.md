# Database Monitoring & Alerting System

**Version:** 1.0
**Created:** 2025-11-12
**Status:** Production Ready

## Overview

This directory contains a comprehensive monitoring and alerting system for the HRMS PostgreSQL database, specifically designed to support the deployment of 4 major migrations:

1. **Unique Constraints** (Migration 1)
2. **Composite Indexes** (Migration 2)
3. **CHECK Constraints** (Migration 3)
4. **Column-Level Encryption** (Migration 4)

## Quick Start

### 1. Pre-Migration: Capture Baseline

```bash
# Capture current performance metrics
cd /workspaces/HRAPP/scripts
./capture-performance-baseline.sh hrms_db baseline-before-migration.json
```

### 2. During Migration: Monitor in Real-Time

```bash
# Terminal 1: Real-time health monitoring
CONTINUOUS=true ./monitor-database-health.sh 10 hrms_db

# Terminal 2: Watch migration logs
tail -f /var/log/hrms/hrms-*.log | grep -i "migration"
```

### 3. Post-Migration: Validate Health

```bash
# Run comprehensive health check
./post-migration-health-check.sh hrms_db tenant_default

# Capture post-migration baseline
./capture-performance-baseline.sh hrms_db baseline-after-migration.json
```

### 4. Compare Results

```bash
# Compare baselines
diff baseline-before-migration.json baseline-after-migration.json

# Or use jq for better comparison
jq -s '.[0].query_benchmarks[] as $before | .[1].query_benchmarks[] as $after |
  select($before.name == $after.name) |
  {name: $before.name, before: $before.duration_ms, after: $after.duration_ms,
   improvement: (($before.duration_ms - $after.duration_ms) / $before.duration_ms * 100)}' \
  baseline-before-migration.json baseline-after-migration.json
```

## Directory Structure

```
/workspaces/HRAPP/
├── scripts/
│   ├── monitor-database-health.sh           # Real-time monitoring
│   ├── post-migration-health-check.sh       # Post-migration validation
│   └── capture-performance-baseline.sh      # Baseline capture
├── monitoring/
│   ├── database-alerts.yaml                 # Alert definitions
│   ├── grafana-dashboard.json               # Grafana dashboard config
│   ├── MONITORING_RECOMMENDATIONS.md        # Tool recommendations
│   ├── docker-compose.yml                   # Monitoring stack (see recommendations)
│   ├── prometheus.yml                       # Prometheus config (see recommendations)
│   └── README.md                            # This file
└── MONITORING_RUNBOOK.md                    # Operational procedures
```

## Files Description

### Scripts

#### `monitor-database-health.sh`

**Purpose:** Real-time database health monitoring

**Features:**
- Active connection monitoring
- Lock detection and analysis
- Slow query identification
- Index usage statistics
- Cache hit ratio
- Disk usage alerts
- Replication lag monitoring

**Usage:**
```bash
# One-time health check
./monitor-database-health.sh [database_name]

# Continuous monitoring (updates every N seconds)
CONTINUOUS=true ./monitor-database-health.sh [interval] [database_name]

# Example: Monitor every 10 seconds
CONTINUOUS=true ./monitor-database-health.sh 10 hrms_db
```

**Output:** Console + log file at `/var/log/hrms/db-health-YYYYMMDD.log`

---

#### `post-migration-health-check.sh`

**Purpose:** Comprehensive validation after migrations

**Validates:**
- All unique indexes created
- All composite indexes created
- All CHECK constraints created
- No constraint violations
- Index validity
- Data integrity
- Encryption health (application-layer)
- Performance benchmarks

**Usage:**
```bash
./post-migration-health-check.sh [database_name] [schema_name]

# Example
./post-migration-health-check.sh hrms_db tenant_default
```

**Output:**
- Console with color-coded results (PASS/WARN/FAIL)
- Detailed report at `/var/log/hrms/post-migration-report-YYYYMMDD-HHMMSS.log`

**Exit Codes:**
- `0`: All checks passed
- `1`: Some warnings (review recommended)
- `2`: Critical failures (immediate action required)

---

#### `capture-performance-baseline.sh`

**Purpose:** Capture comprehensive performance baseline for comparison

**Captures:**
- Database size and growth
- Connection pool metrics
- Cache hit ratios
- Query performance benchmarks
- Index usage statistics
- Table sizes and statistics
- Configuration settings
- Migration history

**Usage:**
```bash
./capture-performance-baseline.sh [database_name] [output_file]

# Before migration
./capture-performance-baseline.sh hrms_db baseline-before.json

# After migration
./capture-performance-baseline.sh hrms_db baseline-after.json
```

**Output:** JSON file with all metrics

**Key Benchmarks:**
- Employee lookup by National ID (will improve 5-10x)
- Payroll cycle query (will improve 10-20x)
- 30-day attendance lookup (will improve 5-10x)
- Leave balance query (will improve 5-10x)

---

### Configuration Files

#### `database-alerts.yaml`

**Purpose:** Alert definitions compatible with Prometheus, CloudWatch, Azure Monitor

**Alert Categories:**

**CRITICAL (Immediate Action):**
- Database down
- Migration failure
- Constraint violations
- Encryption key access failure
- Connection pool exhausted
- Disk space critical (<10%)
- Replication lag critical (>5 min)

**WARNING (Investigate Soon):**
- Query performance degraded (>50%)
- Index not being used
- High lock wait times
- Constraint violation attempts
- Encryption overhead high
- Connection pool high (>70%)
- Disk space warning (>80%)

**INFO (FYI):**
- Migration completed
- New index created
- Constraint added
- Database stats updated
- Backup completed

**Special Sections:**
- Encryption-specific alerts
- Migration-specific alerts
- Routing and escalation policies
- Integration settings

---

#### `grafana-dashboard.json`

**Purpose:** Pre-configured Grafana dashboard for database monitoring

**Panels:**
1. Database Status (UP/DOWN)
2. Active Connections (gauge)
3. Cache Hit Ratio (gauge)
4. Database Size (stat)
5. Connections Over Time (timeseries)
6. Query Performance (95th/99th percentile)
7. Encryption Operations Rate
8. Encryption Performance
9. Lock Monitoring
10. Index Performance (top 10 by scans)
11. Slow Queries (>5s)
12. Migration Status
13. Migration Duration
14. Constraint Violations
15. Table Sizes (top 10)
16. Deadlocks & Conflicts
17. Replication Lag
18. Transaction Rate
19. Disk I/O
20. Encryption Health
21. Passthrough Mode Status (should always be disabled!)

**Import:**
```bash
# In Grafana UI:
# Dashboards -> Import -> Upload JSON file
# Select: /workspaces/HRAPP/monitoring/grafana-dashboard.json
```

---

### Documentation

#### `MONITORING_RUNBOOK.md`

**Purpose:** Complete operational runbook for on-call engineers

**Contents:**
- Alert response procedures (step-by-step)
- Troubleshooting guides
- Common issues and solutions
- Escalation procedures
- Contact information
- SQL snippets and commands
- Performance baselines and thresholds

**When to use:**
- Responding to alerts
- Investigating performance issues
- Post-mortem analysis
- Training new team members

---

#### `MONITORING_RECOMMENDATIONS.md`

**Purpose:** Recommendations for monitoring tools and infrastructure

**Contents:**
- Monitoring stack comparison (Prometheus, CloudWatch, Azure, GCP)
- Cost analysis
- Setup guides
- Application-level metrics implementation
- Long-term monitoring plan
- Best practices
- ROI justification

**When to use:**
- Planning monitoring infrastructure
- Choosing monitoring tools
- Budget planning
- Training sessions

---

## Expected Performance Improvements

After completing all 4 migrations, you should see:

| Query Type | Before | After | Improvement |
|------------|--------|-------|-------------|
| Employee by National ID | 50-200ms | <5ms | **10-40x faster** |
| Payroll cycle lookup | 100-500ms | <50ms | **2-10x faster** |
| 30-day attendance | 500-2000ms | <500ms | **Up to 4x faster** |
| Leave balance | 50-100ms | <20ms | **2-5x faster** |
| Employee search by name | 100-300ms | <50ms | **2-6x faster** |

**Data Integrity:**
- Unique constraints prevent duplicates at DB level
- CHECK constraints prevent invalid data (negative salaries, etc.)
- Encryption protects sensitive PII data

**Side Effects:**
- Slight INSERT/UPDATE overhead (5-10ms) from constraint checks
- Encryption adds 5-20ms to operations on encrypted columns
- Index maintenance overhead ~2% on writes

---

## Monitoring Metrics Explained

### Connection Pool Metrics

| Metric | Normal | Warning | Critical | Meaning |
|--------|--------|---------|----------|---------|
| Total Connections | <350 | 350-400 | >400 | Total database connections |
| Active | <250 | 250-350 | >350 | Connections actively executing queries |
| Idle | <100 | 100-200 | >200 | Connections open but not executing |
| Idle in Transaction | <5 | 5-10 | >10 | Connections with open transaction (potential locks!) |

**Why it matters:**
- MaxPoolSize = 500 connections
- At 400+, new requests will wait or fail
- High "idle in transaction" = potential deadlocks

---

### Cache Hit Ratio

| Ratio | Status | Action |
|-------|--------|--------|
| >95% | Excellent | No action |
| 90-95% | Good | Monitor |
| 85-90% | Fair | Consider increasing shared_buffers |
| <85% | Poor | Increase shared_buffers, investigate queries |

**Why it matters:**
- Cache hits = data in memory (fast)
- Disk reads = slow (100x slower than memory)
- Low ratio = queries are slow

---

### Index Usage

**Key Indexes to Monitor:**

From Migration 1 (Unique):
- `IX_Employees_NationalIdCard_Unique`
- `IX_Employees_PassportNumber_Unique`
- `IX_Employees_TaxIdNumber_Unique`

From Migration 2 (Composite):
- `IX_PayrollCycles_Year_Month`
- `IX_Attendances_EmployeeId_Date_Status`
- `IX_BiometricPunchRecords_ProcessingStatus_PunchTime`

**Expected:**
- These should have high `idx_scan` counts within days
- If `idx_scan = 0` after 1 week, investigate why

---

### Encryption Metrics

| Metric | Normal | Warning | Critical |
|--------|--------|---------|----------|
| Encryption Success Rate | >99.9% | 99-99.9% | <99% |
| Encryption Duration | 5-20ms | 20-50ms | >50ms |
| Passthrough Mode | NEVER | - | ACTIVE |

**CRITICAL:** Passthrough mode = encryption bypassed!
- Should NEVER happen in production
- If detected, stop all writes immediately
- Investigate encryption key access issue

---

## Alert Response Quick Reference

### CRITICAL: Database Down

```bash
# 1. Check status
systemctl status postgresql

# 2. Check logs
tail -f /var/log/postgresql/postgresql-*.log

# 3. Attempt restart (if safe)
systemctl start postgresql

# 4. If restart fails, escalate immediately
```

**Escalate to:** Level 2 after 5 minutes

---

### CRITICAL: Migration Failed

```bash
# 1. Check migration logs
tail -f /var/log/hrms/hrms-*.log | grep -i "migration"

# 2. Check for locks
psql -c "SELECT * FROM pg_locks WHERE NOT granted;"

# 3. Run health check
./post-migration-health-check.sh hrms_db tenant_default

# 4. Document error and escalate
```

**Escalate to:** Engineering team immediately

---

### CRITICAL: Encryption Key Access Failed

```bash
# 1. Check Secret Manager
gcloud secrets versions access latest --secret="ENCRYPTION_KEY_V1"

# 2. Check application logs
grep -i "encryption" /var/log/hrms/hrms-*.log | tail -20

# 3. Check if passthrough mode activated
grep -i "passthrough" /var/log/hrms/hrms-*.log

# If passthrough is active:
# 4. STOP ALL WRITES to sensitive tables!
# 5. Fix Secret Manager access
# 6. Restart application
```

**Escalate to:** Security team and CTO immediately

---

### WARNING: Query Performance Degraded

```bash
# 1. Identify slow queries
psql -c "SELECT pid, query_start, now() - query_start AS duration, query
FROM pg_stat_activity
WHERE state = 'active'
ORDER BY query_start LIMIT 10;"

# 2. Check index usage
psql -c "SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
ORDER BY idx_scan DESC LIMIT 20;"

# 3. Update statistics if needed
psql -c "ANALYZE;"

# 4. Monitor for 1 hour, escalate if not improved
```

---

## Automation Setup

### Daily Health Check (Cron)

```bash
# Add to crontab
crontab -e

# Run health check every day at 6 AM
0 6 * * * /workspaces/HRAPP/scripts/monitor-database-health.sh > /var/log/hrms/daily-health-$(date +\%Y\%m\%d).log 2>&1

# Cleanup old logs (keep 30 days)
0 7 * * * find /var/log/hrms -name "*.log" -mtime +30 -delete
```

### Weekly Performance Report

```bash
# Add to crontab
0 8 * * 1 /workspaces/HRAPP/scripts/capture-performance-baseline.sh hrms_db /var/log/hrms/weekly-baseline-$(date +\%Y-\%U).json 2>&1
```

### Continuous Monitoring (systemd service)

```bash
# Create service file
sudo tee /etc/systemd/system/db-monitor.service > /dev/null << 'EOF'
[Unit]
Description=HRMS Database Health Monitor
After=postgresql.service

[Service]
Type=simple
User=postgres
Environment="CONTINUOUS=true"
ExecStart=/workspaces/HRAPP/scripts/monitor-database-health.sh 30 hrms_db
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable db-monitor.service
sudo systemctl start db-monitor.service

# Check status
sudo systemctl status db-monitor.service
```

---

## Testing the Monitoring System

### 1. Test Alert on High Connections

```bash
# Generate many connections (test environment only!)
for i in {1..100}; do
  psql -h localhost -U postgres -d hrms_db -c "SELECT pg_sleep(60);" &
done

# Watch for alert
# Should trigger WARNING at 350 connections
# Should trigger CRITICAL at 400 connections

# Cleanup
killall psql
```

### 2. Test Slow Query Alert

```sql
-- Run intentionally slow query
SELECT pg_sleep(10);

-- Should appear in slow queries section of monitor
```

### 3. Test Encryption Monitoring

```bash
# Check encryption logs
grep "Encryption" /var/log/hrms/hrms-*.log

# Should see:
# - "Encryption service initialized successfully"
# - NO "passthrough mode" messages
```

### 4. Test Post-Migration Health Check

```bash
# Run health check
./post-migration-health-check.sh hrms_db tenant_default

# Expected results:
# - All indexes exist: PASS
# - All constraints exist: PASS
# - No data violations: PASS
# - Performance benchmarks: Improved after migrations
```

---

## Troubleshooting Common Issues

### Issue: Script won't run

```bash
# Make scripts executable
chmod +x /workspaces/HRAPP/scripts/*.sh

# Check PostgreSQL is accessible
psql -h localhost -U postgres -d hrms_db -c "SELECT 1;"
```

### Issue: "Permission denied" on log directory

```bash
# Create log directory
sudo mkdir -p /var/log/hrms
sudo chown $(whoami):$(whoami) /var/log/hrms
```

### Issue: Grafana dashboard not working

```bash
# Check Prometheus is scraping PostgreSQL exporter
curl http://localhost:9090/targets

# Check metrics are available
curl http://localhost:9187/metrics

# Import dashboard in Grafana UI
# Copy content of grafana-dashboard.json
```

---

## Support and Contact

### Team
- **Database SRE:** dba@morishr.com
- **Engineering:** engineering@morishr.com
- **Security:** security@morishr.com
- **On-Call:** Check PagerDuty schedule

### Escalation
- **Level 1:** On-call engineer (0-15 min)
- **Level 2:** Database SRE/Manager (15-30 min)
- **Level 3:** CTO/VP Engineering (30+ min)

### Emergency
- **Critical incidents:** critical-incidents@morishr.com
- **Security incidents:** security@morishr.com

---

## Next Steps

1. **Immediate:**
   - [ ] Run baseline capture before migrations
   - [ ] Set up monitoring stack (Prometheus + Grafana)
   - [ ] Test alerting to email
   - [ ] Review runbook with team

2. **Week 1:**
   - [ ] Monitor Migration 1 deployment
   - [ ] Run post-migration health check
   - [ ] Verify performance improvements

3. **Week 2-3:**
   - [ ] Monitor remaining migrations
   - [ ] Capture final baseline
   - [ ] Compare before/after metrics

4. **Week 4:**
   - [ ] Fine-tune alert thresholds
   - [ ] Set up PagerDuty rotation
   - [ ] Document lessons learned

5. **Ongoing:**
   - [ ] Daily: Review Grafana dashboard
   - [ ] Weekly: Performance review
   - [ ] Monthly: Generate report
   - [ ] Quarterly: DR drill

---

**Document Version:** 1.0
**Last Updated:** 2025-11-12
**Maintained by:** Database SRE Team
