# Database Operations Quick Reference
## HRMS Database Health & Maintenance

**Last Updated:** November 14, 2025
**Database:** hrms_master (PostgreSQL 16)

---

## Daily Health Check (2 minutes)

```sql
-- Run comprehensive health check
SELECT * FROM master.database_health_check();
```

**Expected Results:**
- Cache Hit Ratio: > 95% (currently 99.60%)
- Connection Usage: < 70% (currently 8%)
- Bloated Tables: 0
- Long Running Queries: 0
- Deadlocks: 0

**If Any Issues:** See Section 6 - Troubleshooting

---

## Weekly Maintenance (5 minutes)

```sql
-- Check table bloat
SELECT
    schemaname, relname,
    n_dead_tup,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) AS bloat_percent
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_siraaj', 'tenant_default')
AND n_dead_tup > 100
ORDER BY bloat_percent DESC;
-- Expected: < 5% bloat on all tables

-- Check autovacuum activity
SELECT
    schemaname, relname,
    last_autovacuum,
    autovacuum_count
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_siraaj')
ORDER BY last_autovacuum DESC NULLS LAST
LIMIT 10;
-- Expected: Recent vacuum timestamps

-- Refresh all materialized views manually (until Hangfire configured)
SELECT * FROM master.refresh_all_materialized_views_corrected();
-- Expected: All 6 views refresh in < 200ms
```

---

## Monthly Maintenance (15 minutes)

```sql
-- Check database size growth
SELECT pg_size_pretty(pg_database_size(current_database())) AS current_size;
-- Track trend over time (currently 16 MB)

-- Check top 10 largest tables
SELECT
    schemaname, relname,
    pg_size_pretty(pg_total_relation_size(quote_ident(schemaname)||'.'||quote_ident(relname))) AS size
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_siraaj', 'tenant_default')
ORDER BY pg_total_relation_size(quote_ident(schemaname)||'.'||quote_ident(relname)) DESC
LIMIT 10;

-- Check index usage (identify unused indexes)
SELECT
    schemaname, relname, indexrelname,
    idx_scan,
    pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_siraaj')
AND idx_scan < 100
AND pg_relation_size(indexrelid) > 1000000  -- > 1 MB
ORDER BY pg_relation_size(indexrelid) DESC;
-- Review: Large unused indexes may be candidates for removal
```

---

## Emergency Commands

### If Database Becomes Slow

```sql
-- 1. Check for blocking queries
SELECT pid, usename, query_start, state, query
FROM pg_stat_activity
WHERE state != 'idle'
AND query_start < NOW() - INTERVAL '5 minutes';

-- 2. Kill specific blocking query (if needed)
SELECT pg_terminate_backend(12345);  -- Replace 12345 with actual PID

-- 3. Manual vacuum if needed
VACUUM ANALYZE master."AuditLogs";
VACUUM ANALYZE tenant_siraaj."Attendances";
```

### If Out of Connections

```sql
-- Check current connections
SELECT COUNT(*) FROM pg_stat_activity;

-- View connections by user
SELECT usename, COUNT(*)
FROM pg_stat_activity
GROUP BY usename
ORDER BY COUNT(*) DESC;

-- Increase max connections (requires restart)
ALTER SYSTEM SET max_connections = 200;
-- Then restart PostgreSQL
```

### If Disk Space Low

```sql
-- Find largest tables to archive/clean
SELECT
    schemaname, relname,
    pg_size_pretty(pg_total_relation_size(quote_ident(schemaname)||'.'||quote_ident(relname))) AS size
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(quote_ident(schemaname)||'.'||quote_ident(relname)) DESC
LIMIT 5;

-- Archive old audit logs (> 12 months)
SELECT * FROM master.archive_old_auditlogs_partitions(12);

-- Clean expired refresh tokens
CALL master.cleanup_expired_refresh_tokens();
```

---

## Backup & Restore

### Create Backup

```bash
# Full database backup
pg_dump -h localhost -U postgres -d hrms_master -Fc -f /tmp/hrms_backup_$(date +%Y%m%d).dump

# Backup specific schema
pg_dump -h localhost -U postgres -d hrms_master -Fc -n tenant_siraaj -f /tmp/tenant_siraaj_backup_$(date +%Y%m%d).dump
```

### Restore from Backup

```bash
# Full restore to new database
createdb -h localhost -U postgres hrms_master_restored
pg_restore -h localhost -U postgres -d hrms_master_restored /tmp/hrms_backup_20251114.dump

# Restore specific schema
pg_restore -h localhost -U postgres -d hrms_master --schema=tenant_siraaj /tmp/tenant_siraaj_backup_20251114.dump

# Restore specific table
pg_restore -h localhost -U postgres -d hrms_master -t "AuditLogs" /tmp/hrms_backup_20251114.dump
```

**Current Backup:** `/tmp/hrms_backup_pre_optimization.dump` (743 KB, Nov 14 2025)

---

## Monitoring Queries

### Check Materialized View Freshness

```sql
-- View last refresh times
SELECT
    schemaname,
    matviewname,
    pg_size_pretty(pg_total_relation_size(quote_ident(schemaname)||'.'||quote_ident(matviewname))) AS size
FROM pg_matviews
WHERE schemaname IN ('master', 'tenant_siraaj', 'tenant_default')
ORDER BY schemaname, matviewname;
```

### Check Index Health

```sql
-- Top 20 most-used indexes
SELECT
    schemaname, relname, indexrelname,
    idx_scan AS times_used,
    pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_siraaj')
ORDER BY idx_scan DESC
LIMIT 20;

-- Indexes never used (potential candidates for removal)
SELECT
    schemaname, relname, indexrelname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_siraaj')
AND idx_scan = 0
AND indexrelname NOT LIKE 'PK_%'  -- Exclude primary keys
ORDER BY pg_relation_size(indexrelid) DESC;
```

### Check Query Performance

```sql
-- Slowest queries (requires pg_stat_statements extension)
SELECT
    calls,
    ROUND(total_exec_time::numeric, 2) AS total_time,
    ROUND(mean_exec_time::numeric, 2) AS avg_time,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

---

## Automated Jobs (Pending Hangfire Configuration)

| Job | Schedule | Command |
|-----|----------|---------|
| **Refresh MVs** | Daily 2:00 AM | `CALL master.daily_materialized_view_refresh();` |
| **Cleanup Tokens** | Daily 4:00 AM | `CALL master.cleanup_expired_refresh_tokens();` |
| **Vacuum** | Sunday 3:00 AM | `CALL master.weekly_vacuum_maintenance();` |
| **Create Partitions** | 1st of month 1:00 AM | `CALL master.monthly_partition_maintenance();` |
| **Index Health** | 15th of month 2:00 AM | `CALL master.analyze_index_health();` |

**To Run Manually Until Hangfire Configured:**
```sql
-- Run daily
CALL master.daily_materialized_view_refresh();
CALL master.cleanup_expired_refresh_tokens();

-- Run weekly (Sundays)
CALL master.weekly_vacuum_maintenance();

-- Run monthly (1st day)
CALL master.monthly_partition_maintenance();

-- Run monthly (15th day)
CALL master.analyze_index_health();
```

---

## Performance Baselines

**Query Response Times (Target):**
- Token validation: < 10ms
- Attendance by employee: < 300ms
- Dashboard audit summary: < 500ms
- Payroll calculations: < 5s
- Monthly reports: < 2s

**Database Metrics (Target):**
- Cache hit ratio: > 95%
- Connection usage: < 70%
- Dead tuple ratio: < 5%
- Database size: Monitor growth trend

**If Metrics Below Target:**
1. Run daily health check
2. Check for slow queries
3. Check table bloat
4. Review index usage
5. Contact database administrator if issues persist

---

## Alert Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| Cache Hit Ratio | < 95% | < 90% |
| Dead Tuples | > 10% | > 20% |
| Connections | > 70 | > 85 |
| Query Duration | > 5s | > 10s |
| Database Size | > 100 MB | > 500 MB |

**Alert Actions:**
- Warning: Monitor closely, investigate if persists > 1 hour
- Critical: Immediate action required, may need DBA intervention

---

## Troubleshooting Guide

### Problem: Cache Hit Ratio Below 95%

**Diagnosis:**
```sql
SELECT
    ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2) AS cache_hit_ratio
FROM pg_stat_database
WHERE datname = current_database();
```

**Solutions:**
1. Increase shared_buffers (requires PostgreSQL restart)
2. Identify missing indexes with `SELECT * FROM master.suggest_missing_indexes();`
3. Review slow queries and optimize

### Problem: Table Bloat Above 10%

**Diagnosis:**
```sql
SELECT
    schemaname, relname,
    n_live_tup, n_dead_tup,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) AS bloat_percent
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_siraaj')
ORDER BY bloat_percent DESC;
```

**Solutions:**
1. Manual vacuum: `VACUUM ANALYZE schema."TableName";`
2. Check autovacuum settings: `SELECT * FROM pg_settings WHERE name LIKE 'autovacuum%';`
3. For severe bloat: `CALL master.vacuum_full_table('schema', 'TableName');`

### Problem: Slow Queries

**Diagnosis:**
```sql
SELECT pid, usename, query_start, state, query
FROM pg_stat_activity
WHERE state != 'idle'
AND query_start < NOW() - INTERVAL '5 minutes';
```

**Solutions:**
1. Identify missing indexes: `SELECT * FROM master.suggest_missing_indexes();`
2. Check index usage: Review monthly index usage report
3. Optimize query: Use EXPLAIN ANALYZE to understand query plan
4. Kill stuck query: `SELECT pg_terminate_backend(pid);`

---

## Contact Information

**Database Administrator:** [Your DBA Contact]
**On-Call Support:** [Your Support Contact]
**Emergency Escalation:** [Your Escalation Contact]

**Documentation:**
- Full Technical Report: `/workspaces/HRAPP/FINAL_DEPLOYMENT_VALIDATION_REPORT.md`
- Executive Summary: `/workspaces/HRAPP/EXECUTIVE_DEPLOYMENT_SUMMARY.md`
- Deployment Details: `/workspaces/HRAPP/DEPLOYMENT_SUMMARY_2025-11-14.md`

---

**Quick Reference Version:** 1.0
**Last Updated:** November 14, 2025
**Next Review:** December 14, 2025
