# Database Monitoring Runbook

**Version:** 1.0
**Last Updated:** 2025-11-12
**Owner:** Database SRE Team
**On-Call:** See PagerDuty rotation

## Table of Contents

1. [Overview](#overview)
2. [Monitoring Infrastructure](#monitoring-infrastructure)
3. [Key Metrics to Monitor](#key-metrics-to-monitor)
4. [Alert Response Procedures](#alert-response-procedures)
5. [Migration Monitoring](#migration-monitoring)
6. [Encryption Monitoring](#encryption-monitoring)
7. [Performance Baselines](#performance-baselines)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Escalation Procedures](#escalation-procedures)
10. [Contact Information](#contact-information)

---

## Overview

This runbook provides comprehensive guidance for monitoring and maintaining the HRMS PostgreSQL database infrastructure, with special focus on:

- **4 Major Migrations**: Unique constraints, composite indexes, CHECK constraints, column-level encryption
- **Production Database**: PostgreSQL 14+ with multi-tenant architecture
- **Encryption Layer**: AES-256-GCM encryption for PII data
- **High Availability**: Connection pooling (500 max), replication monitoring

### Critical Systems

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Database | PostgreSQL 14+ | Primary data store |
| Schema | tenant_default | Tenant-isolated data |
| Encryption | AES-256-GCM | PII protection |
| Connection Pool | Npgsql | Connection management (MaxPoolSize: 500) |
| Monitoring | Prometheus + Grafana | Metrics & visualization |
| Alerting | AlertManager + PagerDuty | Incident response |

---

## Monitoring Infrastructure

### Monitoring Tools

1. **Real-time Health Monitor**
   ```bash
   # Continuous monitoring (updates every 10 seconds)
   CONTINUOUS=true /workspaces/HRAPP/scripts/monitor-database-health.sh 10 hrms_db

   # One-time health check
   /workspaces/HRAPP/scripts/monitor-database-health.sh
   ```

2. **Post-Migration Health Check**
   ```bash
   # After any migration
   /workspaces/HRAPP/scripts/post-migration-health-check.sh hrms_db tenant_default
   ```

3. **Grafana Dashboard**
   - URL: `http://grafana.morishr.com/d/hrms-database`
   - Import: `/workspaces/HRAPP/monitoring/grafana-dashboard.json`

4. **Prometheus Alerts**
   - Config: `/workspaces/HRAPP/monitoring/database-alerts.yaml`
   - AlertManager: `http://alertmanager:9093`

### Metrics Collection

Metrics are collected every **10 seconds** and retained for **90 days**.

**Key Metric Sources:**
- PostgreSQL stats views (`pg_stat_*`)
- Custom application metrics (encryption, migrations)
- System metrics (CPU, memory, disk)

---

## Key Metrics to Monitor

### 1. Database Availability

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Database Up | 1 | - | 0 | Page on-call immediately |
| Connection Success Rate | >99% | 95-99% | <95% | Check connection pool |
| Query Success Rate | >99.9% | 99-99.9% | <99% | Investigate errors |

**Queries:**
```sql
-- Check database is accepting connections
SELECT 1;

-- Check replication status
SELECT pg_is_in_recovery();
```

### 2. Connection Pool

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Active Connections | <250 | 250-350 | 350-400 | Scale or optimize queries |
| Total Connections | <350 | 350-400 | >400 | Immediate intervention |
| Idle in Transaction | <5 | 5-10 | >10 | Kill long transactions |
| Connection Wait Time | <100ms | 100-500ms | >500ms | Increase pool size |

**Monitoring Query:**
```sql
SELECT
    count(*) FILTER (WHERE state = 'active') AS active,
    count(*) FILTER (WHERE state = 'idle') AS idle,
    count(*) FILTER (WHERE state = 'idle in transaction') AS idle_in_tx,
    count(*) AS total
FROM pg_stat_activity
WHERE datname = 'hrms_db';
```

### 3. Query Performance

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Avg Query Time | <100ms | 100-500ms | >500ms | Analyze slow queries |
| P95 Query Time | <500ms | 500ms-2s | >2s | Check indexes |
| P99 Query Time | <2s | 2-5s | >5s | Immediate optimization |
| Slow Queries (>5s) | 0 | 1-5 | >5 | Kill or optimize |

**Monitoring Query:**
```sql
-- Find slow queries
SELECT
    pid,
    usename,
    EXTRACT(EPOCH FROM (now() - query_start)) AS duration_seconds,
    state,
    query
FROM pg_stat_activity
WHERE state = 'active'
AND now() - query_start > interval '5 seconds'
ORDER BY query_start ASC;
```

### 4. Lock Contention

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Blocked Queries | 0 | 1-5 | >5 | Identify blocking queries |
| Lock Wait Time | <1s | 1-10s | >10s | Kill blocking queries |
| Deadlocks/hour | 0 | 1-5 | >5 | Analyze deadlock patterns |

**Monitoring Query:**
```sql
-- Find blocked queries
SELECT
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_query,
    blocking_activity.query AS blocking_query
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
    AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;
```

### 5. Cache Performance

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Cache Hit Ratio | >95% | 90-95% | <90% | Increase shared_buffers |
| Buffer Cache Size | - | - | Full | Tune work_mem |

**Monitoring Query:**
```sql
-- Cache hit ratio
SELECT
    ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2) AS cache_hit_ratio
FROM pg_stat_database
WHERE datname = 'hrms_db';
```

### 6. Disk Usage

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Disk Usage | <70% | 70-80% | >80% | Cleanup or expand |
| Disk Free Space | >50GB | 20-50GB | <20GB | Immediate expansion |
| Database Size Growth | Predictable | Rapid | Exponential | Investigate |

**Monitoring Query:**
```sql
-- Database size
SELECT pg_size_pretty(pg_database_size('hrms_db'));

-- Table sizes
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size
FROM pg_tables
WHERE schemaname = 'tenant_default'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 10;
```

### 7. Replication Health

| Metric | Normal | Warning | Critical | Action |
|--------|--------|---------|----------|--------|
| Replication Lag | <10s | 10-60s | >60s | Check network/replica |
| Replica Connected | Yes | - | No | Page on-call |
| WAL Sender Active | Yes | - | No | Restart replication |

**Monitoring Query:**
```sql
-- Replication lag (on replica)
SELECT EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp())) AS lag_seconds;

-- Replication status (on primary)
SELECT * FROM pg_stat_replication;
```

---

## Alert Response Procedures

### CRITICAL: Database Down

**Alert:** `DatabaseDown`
**Severity:** CRITICAL
**Impact:** Complete service outage

**Immediate Actions:**
1. Check if PostgreSQL process is running
   ```bash
   systemctl status postgresql
   ```

2. Check PostgreSQL logs
   ```bash
   tail -f /var/log/postgresql/postgresql-*.log
   ```

3. Attempt to connect
   ```bash
   psql -h localhost -U postgres -d hrms_db
   ```

4. If process is down, attempt restart
   ```bash
   systemctl start postgresql
   ```

5. If restart fails:
   - Check disk space: `df -h`
   - Check memory: `free -m`
   - Check for corruption: Review logs
   - **Escalate to Level 2** immediately

6. Once recovered, run health check
   ```bash
   /workspaces/HRAPP/scripts/monitor-database-health.sh
   ```

**Escalation:** Immediate (Level 2 after 5 minutes)

---

### CRITICAL: Migration Failed

**Alert:** `MigrationFailed`
**Severity:** CRITICAL
**Impact:** Deployment blocked, potential data inconsistency

**Immediate Actions:**
1. **DO NOT PANIC** - Migrations are transactional

2. Check migration logs
   ```bash
   tail -f /var/log/hrms/hrms-*.log | grep -i "migration"
   ```

3. Identify which migration failed
   ```sql
   SELECT * FROM tenant_default."__EFMigrationsHistory"
   ORDER BY "MigrationId" DESC LIMIT 5;
   ```

4. Check for locks blocking migration
   ```sql
   SELECT * FROM pg_locks WHERE NOT granted;
   ```

5. If migration is stuck (not failed):
   - Identify blocking queries
   - Consider killing blocking sessions
   - **Get approval before killing production queries**

6. If migration truly failed:
   - Check error message carefully
   - Migration should have auto-rolled back
   - Verify database state is consistent
   - Run post-migration health check

7. Common failure causes:
   - **Duplicate data violating unique constraint**: Clean data first
   - **Long-running lock**: Retry during maintenance window
   - **Disk full**: Expand disk before retry
   - **Timeout**: Increase command timeout

**Recovery Steps:**
```bash
# Run health check
/workspaces/HRAPP/scripts/post-migration-health-check.sh hrms_db tenant_default

# Review detailed error
grep -A 20 "Migration failed" /var/log/hrms/hrms-*.log

# If constraint violation, identify problem data
# Example: Find duplicate National IDs
SELECT "NationalIdCard", COUNT(*)
FROM tenant_default."Employees"
WHERE "NationalIdCard" IS NOT NULL
AND "IsDeleted" = false
GROUP BY "NationalIdCard"
HAVING COUNT(*) > 1;
```

**Escalation:** Notify engineering team immediately

---

### CRITICAL: Encryption Key Access Failed

**Alert:** `EncryptionKeyAccessFailed`
**Severity:** CRITICAL
**Impact:** Unable to encrypt/decrypt sensitive data

**Immediate Actions:**
1. Check Google Secret Manager status
   ```bash
   gcloud secrets versions access latest --secret="ENCRYPTION_KEY_V1"
   ```

2. Verify application has access
   ```bash
   # Check service account permissions
   gcloud secrets get-iam-policy ENCRYPTION_KEY_V1
   ```

3. Check application logs for specific error
   ```bash
   grep -i "encryption" /var/log/hrms/hrms-*.log | tail -20
   ```

4. **CRITICAL**: Check if passthrough mode was activated
   ```bash
   grep -i "passthrough" /var/log/hrms/hrms-*.log
   ```

5. If passthrough mode is active:
   - **STOP ALL WRITE OPERATIONS** to sensitive tables
   - Data written in passthrough mode will NOT be encrypted
   - This is an emergency security incident

6. Temporary mitigation:
   - If read-only operations needed, passthrough allows reads
   - Do NOT allow writes to: Employees.NationalIdCard, BankAccountNumber, etc.

**Resolution:**
1. Fix Secret Manager access
2. Restart application
3. Verify encryption is working:
   ```bash
   # Check encryption service health in logs
   grep "Encryption.*initialized" /var/log/hrms/hrms-*.log
   ```

**Escalation:** Immediate to Security team and CTO

---

### CRITICAL: Connection Pool Exhausted

**Alert:** `ConnectionPoolExhausted`
**Severity:** CRITICAL
**Impact:** New requests will fail, service degradation

**Immediate Actions:**
1. Check current connection count
   ```sql
   SELECT count(*), state FROM pg_stat_activity GROUP BY state;
   ```

2. Identify idle connections
   ```sql
   SELECT
       pid,
       usename,
       application_name,
       client_addr,
       state,
       state_change
   FROM pg_stat_activity
   WHERE state = 'idle'
   AND state_change < now() - interval '5 minutes'
   ORDER BY state_change;
   ```

3. Kill idle connections (carefully!)
   ```sql
   -- Kill idle connections older than 10 minutes
   SELECT pg_terminate_backend(pid)
   FROM pg_stat_activity
   WHERE state = 'idle'
   AND state_change < now() - interval '10 minutes'
   AND datname = 'hrms_db'
   AND pid != pg_backend_pid();
   ```

4. Check for connection leaks in application
   - Review application logs for connection errors
   - Check if connections are being properly disposed

5. Temporary increase connection limit (if needed)
   ```sql
   ALTER SYSTEM SET max_connections = 600;
   SELECT pg_reload_conf();
   ```

6. Scale application instances if genuine traffic spike

**Root Cause Analysis:**
- Are connections being properly closed?
- Is there a sudden traffic spike?
- Is a specific tenant/feature leaking connections?

**Escalation:** Notify engineering team if issue persists > 15 minutes

---

### WARNING: Query Performance Degraded

**Alert:** `QueryPerformanceDegraded`
**Severity:** WARNING
**Impact:** Slower response times, user experience degradation

**Investigation Steps:**
1. Identify slow queries
   ```sql
   SELECT
       pid,
       query_start,
       EXTRACT(EPOCH FROM (now() - query_start)) AS duration,
       state,
       query
   FROM pg_stat_activity
   WHERE state = 'active'
   ORDER BY query_start
   LIMIT 10;
   ```

2. Check if new indexes are being used
   ```sql
   SELECT
       schemaname,
       tablename,
       indexname,
       idx_scan,
       idx_tup_read
   FROM pg_stat_user_indexes
   WHERE schemaname = 'tenant_default'
   AND indexname LIKE 'IX_%'
   ORDER BY idx_scan DESC;
   ```

3. Check for missing statistics
   ```sql
   SELECT schemaname, tablename, last_analyze, last_autoanalyze
   FROM pg_stat_user_tables
   WHERE schemaname = 'tenant_default'
   ORDER BY last_analyze NULLS FIRST;
   ```

4. Run ANALYZE if needed
   ```sql
   ANALYZE tenant_default."Employees";
   ANALYZE tenant_default."Attendances";
   ```

5. Check for table bloat
   ```sql
   SELECT
       schemaname,
       tablename,
       pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
       n_dead_tup
   FROM pg_stat_user_tables
   WHERE schemaname = 'tenant_default'
   ORDER BY n_dead_tup DESC;
   ```

6. If high dead tuples, consider VACUUM
   ```sql
   VACUUM ANALYZE tenant_default."Employees";
   ```

**Escalation:** If degradation continues > 1 hour

---

### WARNING: Index Not Being Used

**Alert:** `IndexNotUsed`
**Severity:** WARNING
**Impact:** Potential performance issues, wasted resources

**Investigation Steps:**
1. Identify unused index
   ```sql
   SELECT
       schemaname,
       tablename,
       indexname,
       idx_scan
   FROM pg_stat_user_indexes
   WHERE schemaname = 'tenant_default'
   AND idx_scan = 0
   AND indexrelname NOT LIKE '%_pkey'
   ORDER BY pg_relation_size(indexrelid) DESC;
   ```

2. Check if index was recently created
   - New indexes may take time to be used
   - Check if it's from a recent migration

3. Analyze query patterns
   ```sql
   -- Check if queries exist that should use this index
   SELECT query FROM pg_stat_statements
   WHERE query LIKE '%table_name%'
   ORDER BY calls DESC LIMIT 10;
   ```

4. Check if planner is avoiding index
   ```sql
   EXPLAIN ANALYZE SELECT * FROM tenant_default."Employees"
   WHERE "NationalIdCard" = 'test-id';
   ```

5. Possible reasons for non-use:
   - Index selectivity too low
   - Sequential scan is actually faster (small table)
   - Statistics are outdated
   - Wrong column order in composite index

**Resolution:**
- If truly unused after 7 days, consider dropping
- If needed but not used, investigate query planner

**Escalation:** Consult DBA team if unclear

---

## Migration Monitoring

### Before Migration

**Pre-migration Checklist:**
1. ✅ Capture performance baseline
   ```bash
   /workspaces/HRAPP/scripts/monitor-database-health.sh > baseline-$(date +%Y%m%d).log
   ```

2. ✅ Backup database
   ```bash
   pg_dump -h localhost -U postgres hrms_db > backup-pre-migration-$(date +%Y%m%d).sql
   ```

3. ✅ Check disk space
   ```bash
   df -h
   # Ensure at least 20% free space
   ```

4. ✅ Check current connections
   ```sql
   SELECT count(*) FROM pg_stat_activity;
   # Should be < 50 for maintenance window
   ```

5. ✅ Notify team of maintenance window

6. ✅ Put application in maintenance mode (if critical)

### During Migration

**Real-time Monitoring:**
```bash
# Terminal 1: Connection monitor
watch -n 2 'psql -U postgres -d hrms_db -c "SELECT count(*), state FROM pg_stat_activity GROUP BY state"'

# Terminal 2: Lock monitor
watch -n 5 'psql -U postgres -d hrms_db -c "SELECT count(*) FROM pg_locks WHERE NOT granted"'

# Terminal 3: Migration logs
tail -f /var/log/hrms/hrms-*.log | grep -i "migration"
```

**Metrics to Watch:**
- Connection count (should stay stable)
- Lock wait count (should be 0 or very low)
- Query response times (may spike during index creation)
- Disk I/O (will spike during index creation)

**Expected Duration:**
| Migration | Expected Time | Max Acceptable |
|-----------|---------------|----------------|
| Unique Constraints | 2-5 minutes | 15 minutes |
| Composite Indexes | 5-15 minutes | 30 minutes |
| CHECK Constraints | 1-3 minutes | 10 minutes |
| Column Encryption | 0 seconds* | 5 minutes |

*Encryption is application-layer only, no data migration

**Red Flags:**
- Migration stuck > 30 minutes
- Blocked queries > 10
- Connection count spiking
- Disk space decreasing rapidly

### After Migration

**Post-migration Checklist:**
1. ✅ Run comprehensive health check
   ```bash
   /workspaces/HRAPP/scripts/post-migration-health-check.sh hrms_db tenant_default
   ```

2. ✅ Verify all indexes created
   ```sql
   SELECT indexname FROM pg_indexes
   WHERE schemaname = 'tenant_default'
   AND indexname LIKE 'IX_%'
   ORDER BY indexname;
   ```

3. ✅ Verify all constraints exist
   ```sql
   SELECT conname FROM pg_constraint
   WHERE connamespace = 'tenant_default'::regnamespace
   AND conname LIKE 'chk_%'
   ORDER BY conname;
   ```

4. ✅ Test encryption (if applicable)
   ```bash
   # Check application logs for encryption initialization
   grep "Encryption.*initialized" /var/log/hrms/hrms-*.log
   ```

5. ✅ Run performance benchmarks
   ```sql
   -- Test employee lookup (should use unique index)
   EXPLAIN ANALYZE SELECT * FROM tenant_default."Employees"
   WHERE "NationalIdCard" = 'test-id' AND "IsDeleted" = false;

   -- Should show: Index Scan using IX_Employees_NationalIdCard_Unique
   ```

6. ✅ Update statistics
   ```sql
   ANALYZE;
   ```

7. ✅ Compare performance to baseline

8. ✅ Monitor for 1 hour post-migration

9. ✅ Remove application from maintenance mode

10. ✅ Document any issues or anomalies

---

## Encryption Monitoring

### Encryption Service Health

**Daily Checks:**
```bash
# Check encryption service logs
grep "Encryption" /var/log/hrms/hrms-$(date +%Y%m%d).log

# Should see:
# - "Encryption service initialized successfully"
# - No "passthrough mode" messages
# - No "failed to decrypt" errors
```

**Key Metrics:**
| Metric | Normal | Warning | Critical |
|--------|--------|---------|----------|
| Encryption Success Rate | >99.9% | 99-99.9% | <99% |
| Decryption Success Rate | >99.9% | 99-99.9% | <99% |
| Key Retrieval Latency | <100ms | 100-500ms | >500ms |
| Passthrough Mode | Never | - | Active |

### Encryption Performance

**Expected Overhead:**
- Encryption: 5-20ms per operation
- Decryption: 5-20ms per operation
- Bulk operations: ~10% overall slowdown

**Performance Query:**
```sql
-- Compare query times for encrypted vs non-encrypted columns
EXPLAIN ANALYZE SELECT "Id", "FirstName", "LastName"
FROM tenant_default."Employees"
WHERE "Email" = 'test@example.com';  -- Non-encrypted

EXPLAIN ANALYZE SELECT "Id", "FirstName", "NationalIdCard"
FROM tenant_default."Employees"
WHERE "Email" = 'test@example.com';  -- Includes encrypted
```

### Passthrough Mode Detection

**CRITICAL:** Passthrough mode should NEVER be active in production!

```bash
# Alert on any passthrough activation
grep -i "passthrough" /var/log/hrms/hrms-*.log

# If found:
# 1. Immediately alert security team
# 2. Stop all write operations
# 3. Fix encryption key access
# 4. Restart application
# 5. Verify encryption is working
```

### Encryption Key Rotation

**When to Rotate:**
- Every 90 days (compliance requirement)
- After security incident
- After personnel changes (access control)

**Rotation Process:**
1. Create new encryption key (v2)
2. Update application to decrypt with v1, encrypt with v2
3. Background job to re-encrypt all data
4. Switch to v2 for decryption
5. Archive v1 (retain for recovery)

---

## Performance Baselines

### Baseline Metrics (Before Migrations)

Capture these metrics BEFORE running migrations:

```sql
-- 1. Query response times (average)
SELECT
    AVG(EXTRACT(EPOCH FROM (now() - query_start))) AS avg_query_seconds
FROM pg_stat_activity
WHERE state = 'active';

-- 2. Cache hit ratio
SELECT
    ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2) AS cache_hit_ratio
FROM pg_stat_database WHERE datname = 'hrms_db';

-- 3. Index usage
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
ORDER BY idx_scan DESC;

-- 4. Table sizes
SELECT
    tablename,
    pg_size_pretty(pg_total_relation_size('tenant_default.' || tablename)) AS size
FROM pg_tables
WHERE schemaname = 'tenant_default'
ORDER BY pg_total_relation_size('tenant_default.' || tablename) DESC;

-- 5. Disk I/O rates
SELECT
    blks_read,
    blks_hit,
    ROUND(100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0), 2) AS hit_ratio
FROM pg_stat_database
WHERE datname = 'hrms_db';
```

### Expected Performance Changes

**After Migration 1 (Unique Constraints):**
- ✅ Employee lookups by National ID: **5-10x faster** (seq scan → index scan)
- ✅ Duplicate detection: **Near-instant** (constraint prevents at DB level)
- ⚠️ INSERT performance: **Slightly slower** (5-10ms overhead for constraint check)

**After Migration 2 (Composite Indexes):**
- ✅ Payroll cycle queries: **10-20x faster**
- ✅ Attendance reports: **5-10x faster**
- ✅ Leave balance lookups: **5-10x faster**
- ⚠️ Index maintenance overhead: **~2% on writes**

**After Migration 3 (CHECK Constraints):**
- ✅ Data integrity: **Guaranteed at DB level**
- ⚠️ INSERT/UPDATE performance: **Negligible impact (<1ms)**

**After Migration 4 (Encryption):**
- ⚠️ Encrypted column reads: **5-20ms overhead**
- ⚠️ Encrypted column writes: **5-20ms overhead**
- ⚠️ Bulk operations on encrypted data: **~10% slower**

### Performance Thresholds

| Query Type | Baseline | After Migrations | Alert If |
|------------|----------|------------------|----------|
| Employee by ID | <5ms | <5ms | >10ms |
| Employee by National ID | 50-200ms | **<5ms** ✅ | >20ms |
| Payroll cycle lookup | 100-500ms | **<50ms** ✅ | >100ms |
| 30-day attendance report | 500-2000ms | **<500ms** ✅ | >1000ms |
| Leave balance check | 50-100ms | **<20ms** ✅ | >50ms |

---

## Troubleshooting Guide

### Issue: Migration Running Too Long

**Symptoms:** Migration stuck, no progress

**Diagnosis:**
```sql
-- Check if migration is blocked
SELECT * FROM pg_locks WHERE NOT granted;

-- Check current queries
SELECT pid, state, query FROM pg_stat_activity
WHERE state != 'idle' ORDER BY query_start;
```

**Solutions:**
1. If blocked by another query:
   - Identify blocking query
   - If safe, kill blocking query: `SELECT pg_terminate_backend(pid);`
   - Migration should resume

2. If creating large index:
   - Index creation is normal, may take 10-30 minutes
   - Monitor progress via disk I/O
   - Do NOT kill unless > 60 minutes

3. If truly stuck:
   - Check application logs
   - Check PostgreSQL logs
   - Consider rollback and retry in maintenance window

---

### Issue: Constraint Violation Errors

**Symptoms:** Migration fails with constraint violation

**Diagnosis:**
```sql
-- Find duplicate National IDs
SELECT "NationalIdCard", COUNT(*)
FROM tenant_default."Employees"
WHERE "NationalIdCard" IS NOT NULL
GROUP BY "NationalIdCard"
HAVING COUNT(*) > 1;

-- Find negative salaries
SELECT COUNT(*)
FROM tenant_default."Employees"
WHERE "BasicSalary" < 0;
```

**Solutions:**
1. Clean data before migration:
   ```sql
   -- Fix duplicates (keep first, nullify others)
   UPDATE tenant_default."Employees" e1
   SET "NationalIdCard" = NULL
   WHERE "NationalIdCard" IN (
       SELECT "NationalIdCard"
       FROM tenant_default."Employees"
       GROUP BY "NationalIdCard"
       HAVING COUNT(*) > 1
   )
   AND "Id" NOT IN (
       SELECT MIN("Id")
       FROM tenant_default."Employees"
       GROUP BY "NationalIdCard"
   );
   ```

2. Re-run migration

3. Document data quality issues for follow-up

---

### Issue: Encryption Failures

**Symptoms:** "Failed to decrypt" errors in logs

**Diagnosis:**
```bash
# Check encryption service status
grep "Encryption" /var/log/hrms/hrms-*.log | tail -20

# Check Secret Manager access
gcloud secrets versions access latest --secret="ENCRYPTION_KEY_V1"
```

**Common Causes:**
1. **Secret Manager access denied**
   - Verify service account has `secretmanager.secretAccessor` role
   - Check IAM permissions

2. **Wrong encryption key version**
   - Data was encrypted with v1, app is using v2
   - Solution: Support multiple key versions in app

3. **Corrupted encrypted data**
   - Database corruption
   - Manual data editing
   - Solution: Restore from backup

4. **Passthrough mode was used**
   - Data was written unencrypted
   - Solution: Re-encrypt data

**Resolution:**
```bash
# 1. Fix Secret Manager access
gcloud secrets add-iam-policy-binding ENCRYPTION_KEY_V1 \
    --member="serviceAccount:hrms-app@project.iam.gserviceaccount.com" \
    --role="roles/secretmanager.secretAccessor"

# 2. Restart application
systemctl restart hrms-api

# 3. Verify encryption working
tail -f /var/log/hrms/hrms-*.log | grep "Encryption"
```

---

### Issue: Index Not Being Used

**Symptoms:** Slow queries despite index existing

**Diagnosis:**
```sql
-- Check if index exists
SELECT * FROM pg_indexes
WHERE schemaname = 'tenant_default'
AND indexname = 'IX_Employees_NationalIdCard_Unique';

-- Check query plan
EXPLAIN ANALYZE SELECT * FROM tenant_default."Employees"
WHERE "NationalIdCard" = '1234567890'
AND "IsDeleted" = false;
```

**Common Causes:**
1. **Statistics are outdated**
   ```sql
   ANALYZE tenant_default."Employees";
   ```

2. **Query not matching index condition**
   - Index has filter: `WHERE "IsDeleted" = false`
   - Query must include: `AND "IsDeleted" = false`

3. **Planner thinks sequential scan is faster**
   - Small table, seq scan may actually be faster
   - Normal behavior

4. **Index is corrupted**
   ```sql
   REINDEX INDEX tenant_default."IX_Employees_NationalIdCard_Unique";
   ```

---

### Issue: High Connection Count

**Symptoms:** Connection count growing, pool exhaustion

**Diagnosis:**
```sql
-- Find idle connections by application
SELECT
    application_name,
    state,
    COUNT(*),
    MAX(state_change) AS last_change
FROM pg_stat_activity
GROUP BY application_name, state
ORDER BY COUNT(*) DESC;
```

**Common Causes:**
1. **Connection leak in application**
   - Connections not being disposed
   - Exception in using block
   - Solution: Fix application code

2. **Connection pooling misconfigured**
   - MinPoolSize too high
   - ConnectionIdleLifetime too long
   - Solution: Tune pool settings

3. **Legitimate traffic spike**
   - More users than expected
   - Solution: Scale application instances

**Immediate Mitigation:**
```sql
-- Kill long-idle connections (use carefully!)
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle'
AND state_change < now() - interval '30 minutes'
AND datname = 'hrms_db'
AND pid != pg_backend_pid();
```

---

## Escalation Procedures

### Escalation Levels

**Level 1: On-Call Engineer (0-15 minutes)**
- Initial response to all critical alerts
- Basic troubleshooting and diagnostics
- Access to restart services, kill queries
- Can involve Level 2 at discretion

**Level 2: Database SRE / Manager (15-30 minutes)**
- Complex database issues
- Migration failures
- Performance optimization
- Can make configuration changes
- Can involve Level 3 if needed

**Level 3: CTO / VP Engineering (30+ minutes)**
- Major outages lasting > 30 minutes
- Security incidents (encryption failures)
- Data integrity issues
- Decision on major changes (failover, rollback)

### When to Escalate

| Scenario | Escalate To | Timeframe |
|----------|-------------|-----------|
| Database completely down | Level 2 | After 5 min |
| Migration failure | Level 2 | Immediately |
| Encryption key failure | Level 3 | Immediately |
| Connection pool exhausted | Level 2 | After 15 min |
| Data integrity violation | Level 2 | Immediately |
| Performance degradation >50% | Level 2 | After 30 min |
| Disk space critical | Level 2 | After 10 min |
| Unclear how to proceed | Level 2 | Anytime |

### Escalation Process

1. **Before Escalating:**
   - Gather diagnostic information
   - Document what you've tried
   - Have error messages ready
   - Note time of incident

2. **Escalation Methods:**
   - **PagerDuty**: Automatic for CRITICAL alerts
   - **Slack**: Post in #database-incidents
   - **Email**: critical-incidents@morishr.com
   - **Phone**: Only for Level 3, after hours

3. **Escalation Template:**
   ```
   INCIDENT: [Brief description]
   SEVERITY: CRITICAL/WARNING/INFO
   TIME: [When it started]
   IMPACT: [What's affected]
   ACTIONS TAKEN: [What you've done]
   CURRENT STATUS: [Is it ongoing?]
   NEED: [What you need help with]
   ```

---

## Contact Information

### On-Call Rotation

**Primary On-Call:** Check PagerDuty schedule
**Backup On-Call:** Check PagerDuty schedule
**PagerDuty URL:** https://morishr.pagerduty.com

### Team Contacts

| Role | Email | Slack | Phone (Emergency Only) |
|------|-------|-------|------------------------|
| Database SRE Lead | dba-lead@morishr.com | @dba-lead | +1-XXX-XXX-XXXX |
| Engineering Manager | eng-manager@morishr.com | @eng-mgr | +1-XXX-XXX-XXXX |
| CTO | cto@morishr.com | @cto | +1-XXX-XXX-XXXX |
| Security Team | security@morishr.com | #security | +1-XXX-XXX-XXXX |
| Infrastructure Team | infra@morishr.com | #infrastructure | - |

### Communication Channels

- **Slack Channels:**
  - #database-alerts (monitoring alerts)
  - #database-incidents (active incidents)
  - #critical-alerts (CRITICAL severity only)
  - #security-alerts (security-related)

- **Email Lists:**
  - database-team@morishr.com (all database team)
  - critical-incidents@morishr.com (critical alerts)
  - security@morishr.com (security incidents)

### External Vendors

| Vendor | Purpose | Support Contact |
|--------|---------|-----------------|
| Google Cloud | Secret Manager, Infrastructure | support.google.com/cloud |
| PagerDuty | Alerting, On-Call | support@pagerduty.com |
| SMTP2GO | Email Alerts | support@smtp2go.com |

---

## Appendix

### Useful Commands Cheat Sheet

```bash
# Database connection
psql -h localhost -U postgres -d hrms_db

# Health check (one-time)
/workspaces/HRAPP/scripts/monitor-database-health.sh

# Health check (continuous)
CONTINUOUS=true /workspaces/HRAPP/scripts/monitor-database-health.sh 10

# Post-migration validation
/workspaces/HRAPP/scripts/post-migration-health-check.sh hrms_db tenant_default

# View logs
tail -f /var/log/hrms/hrms-$(date +%Y%m%d).log

# Check PostgreSQL status
systemctl status postgresql

# Restart PostgreSQL (use carefully!)
systemctl restart postgresql

# Check disk space
df -h

# Check memory
free -m

# Kill a query (replace PID)
psql -c "SELECT pg_terminate_backend(12345);"
```

### SQL Snippets

```sql
-- Connection count by state
SELECT state, COUNT(*) FROM pg_stat_activity GROUP BY state;

-- Blocked queries
SELECT * FROM pg_locks WHERE NOT granted;

-- Slow queries
SELECT pid, now() - query_start AS duration, query
FROM pg_stat_activity
WHERE state = 'active' AND now() - query_start > interval '5 seconds';

-- Database size
SELECT pg_size_pretty(pg_database_size('hrms_db'));

-- Cache hit ratio
SELECT ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2)
FROM pg_stat_database WHERE datname = 'hrms_db';

-- Index usage
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
ORDER BY idx_scan DESC;

-- Replication lag
SELECT EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()));
```

---

**Document Version:** 1.0
**Last Reviewed:** 2025-11-12
**Next Review:** 2025-12-12
**Owner:** Database SRE Team
