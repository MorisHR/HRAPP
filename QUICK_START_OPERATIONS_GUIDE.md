# HRMS Database Operations - Quick Start Guide

**Last Updated:** November 14, 2025
**Purpose:** Daily, weekly, and monthly operational commands
**Audience:** DBAs, DevOps, Developers

---

## TABLE OF CONTENTS

1. [Commands to Run Today](#commands-to-run-today)
2. [Commands to Run This Week](#commands-to-run-this-week)
3. [Commands to Run Monthly](#commands-to-run-monthly)
4. [Troubleshooting Tips](#troubleshooting-tips)

---

## COMMANDS TO RUN TODAY

### 1. Start Application (Activate Automated Jobs)

**Time:** 2 minutes
**Priority:** CRITICAL

```bash
# Navigate to API project
cd /workspaces/HRAPP/src/HRMS.API

# Start application
dotnet run
```

**What This Does:**
- Activates 5 automated maintenance jobs
- Registers jobs with Hangfire scheduler
- Enables automatic database maintenance

**Verify Success:**
1. Open browser: `https://your-app-url/hangfire`
2. Click "Recurring Jobs" tab
3. Confirm you see 5 jobs:
   - daily-mv-refresh (3:00 AM UTC)
   - daily-token-cleanup (4:00 AM UTC)
   - weekly-vacuum-maintenance (Sunday 4:00 AM UTC)
   - monthly-partition-maintenance (1st of month 2:00 AM UTC)
   - daily-health-check (6:00 AM UTC)

**Expected Result:** All jobs show "Scheduled" status with next execution time

---

### 2. Run Daily Health Check

**Time:** 30 seconds
**Priority:** HIGH

```bash
# Quick health check (30 seconds)
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/quick_performance_check.sql
```

**What This Checks:**
- Cache hit ratio (target: > 99%)
- Connection usage (target: < 80%)
- Table bloat (target: < 20%)
- Index usage statistics
- Materialized view freshness
- Auto-vacuum status
- Deadlocks (target: 0)
- Database size

**Expected Output:**
```
=== HRMS DATABASE HEALTH CHECK ===
Date: 2025-11-14

Cache Hit Ratio: 99.60% ✅ EXCELLENT
Connection Usage: 8% (8/100) ✅ PLENTY OF CAPACITY
Table Bloat: 0.2% ✅ NEAR-ZERO
Materialized Views: 6 operational ✅
Deadlocks: 0 ✅ PERFECT
Database Size: 16 MB

Overall Health: A+ (EXCELLENT)
```

**Action Items:**
- If cache hit ratio < 95%: See troubleshooting section
- If bloat > 20%: Run manual VACUUM on affected tables
- If connection usage > 80%: Increase max_connections

---

### 3. Verify Hangfire Jobs (Tomorrow Morning)

**Time:** 5 minutes
**Priority:** MEDIUM
**When:** After first overnight job execution

```bash
# Open Hangfire dashboard
# Navigate to: https://your-app-url/hangfire
# Click: "Succeeded Jobs" tab
```

**What to Check:**
1. **daily-mv-refresh** (runs at 3:00 AM UTC)
   - Status: Succeeded
   - Duration: 5-10 seconds
   - Output: "Refreshed 6 materialized views"

2. **daily-token-cleanup** (runs at 4:00 AM UTC)
   - Status: Succeeded
   - Duration: 1-2 seconds
   - Output: "Deleted X expired tokens"

3. **daily-health-check** (runs at 6:00 AM UTC)
   - Status: Succeeded
   - Duration: 5 seconds
   - Output: "Health check passed - Grade: A+"

**If Jobs Failed:**
```bash
# Check application logs
cd /workspaces/HRAPP/src/HRMS.API
dotnet run --logs

# Manually trigger failed job
# In Hangfire dashboard: Click job → "Trigger Now" button

# Verify database connection
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -c "SELECT version();"
```

---

### 4. Create Performance Baseline

**Time:** 5 minutes
**Priority:** MEDIUM

```bash
# Run comprehensive performance test
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/database_performance_test_v2.sql \
> /workspaces/HRAPP/performance_baseline_$(date +%Y%m%d).txt

# Review results
less /workspaces/HRAPP/performance_baseline_$(date +%Y%m%d).txt
```

**What This Does:**
- Executes 40+ performance tests
- Measures query execution times
- Validates indexes are being used
- Checks materialized view performance

**Save This File:**
- Location: `/workspaces/HRAPP/performance_baseline_YYYYMMDD.txt`
- Purpose: Compare future performance against this baseline
- Frequency: Run monthly to track trends

---

## COMMANDS TO RUN THIS WEEK

### Day 3: Enable Advanced Query Tracking (Optional but Recommended)

**Time:** 10 minutes
**Priority:** MEDIUM
**Downtime:** 5 seconds (PostgreSQL restart)

**One-Command Setup:**
```bash
# Automated setup with safety checks
sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
```

**What This Script Does:**
1. Backs up current postgresql.conf
2. Adds `shared_preload_libraries = 'pg_stat_statements'`
3. Restarts PostgreSQL service (5 second downtime)
4. Installs extension in all databases
5. Verifies installation

**Interactive Prompts:**
```
This script will:
1. Modify postgresql.conf
2. Restart PostgreSQL (5 second downtime)
3. Install pg_stat_statements extension

Proceed? [y/N]: y

Creating backup of postgresql.conf... ✅
Updating configuration... ✅
Restarting PostgreSQL... ✅ (5 seconds)
Installing extension... ✅
Verifying installation... ✅

pg_stat_statements enabled successfully!
```

**Verify Success:**
```bash
# Run verification script
bash /workspaces/HRAPP/scripts/verify_pg_stat_statements.sh
```

**Expected Output:**
```
=== pg_stat_statements Verification ===

✅ Extension installed: YES
✅ Configuration correct: YES
✅ Tracking queries: YES (150 queries tracked)
✅ Memory usage: 6.2 MB
✅ Performance impact: < 1% CPU

Status: OPERATIONAL
```

**Rollback (if needed):**
```bash
# Safe rollback with backup selection
sudo bash /workspaces/HRAPP/scripts/rollback_pg_stat_statements.sh
```

---

### Day 5: Review Index Usage

**Time:** 10 minutes
**Priority:** MEDIUM

```bash
# Connect to database
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master
```

```sql
-- Check index usage statistics
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    idx_scan as scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
ORDER BY idx_scan DESC
LIMIT 20;
```

**What to Look For:**

**High Usage (Good):**
```
index_name                          | scans | size
------------------------------------|-------|------
IX_PayrollCycles_Year_Month         | 500   | 16 KB
IX_Attendances_EmployeeId_Date      | 350   | 24 KB
PK_Employees                        | 1200  | 32 KB
```

**Zero Usage (Review Needed):**
```
index_name                          | scans | size
------------------------------------|-------|------
IX_SomeUnusedIndex                  | 0     | 48 KB
```

**Action Items:**
- High usage indexes: Keep (they're working!)
- Zero usage indexes: Wait 30 days before dropping (need more data)
- Large unused indexes: Mark for review next month

**Note:** It's normal to see low usage in first week. Need 30 days for meaningful analysis.

---

### Day 7: Weekly Health Review

**Time:** 15 minutes
**Priority:** HIGH

```bash
# Run daily health check
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/quick_performance_check.sql

# Review Hangfire job history
# Navigate to: https://your-app-url/hangfire
# Check: Succeeded Jobs (last 7 days)
# Verify: 35 total jobs (5 jobs × 7 days)
```

**Weekly Checklist:**

- [ ] Cache hit ratio still > 99%
- [ ] No failed Hangfire jobs
- [ ] Table bloat < 5%
- [ ] Connection usage < 80%
- [ ] All materialized views refreshed daily
- [ ] No database errors in application logs

**Document Results:**
```bash
# Save weekly report
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/quick_performance_check.sql \
> /workspaces/HRAPP/weekly_health_$(date +%Y%m%d).txt
```

---

## COMMANDS TO RUN MONTHLY

### Week 1: Comprehensive Performance Test

**Time:** 10 minutes
**Priority:** HIGH

```bash
# Monthly performance audit
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/database_performance_test_v2.sql \
> /workspaces/HRAPP/performance_report_$(date +%Y%m).txt

# Compare to baseline
diff /workspaces/HRAPP/performance_baseline_*.txt \
     /workspaces/HRAPP/performance_report_$(date +%Y%m).txt
```

**What to Compare:**
- Average query times (should be similar or better)
- Cache hit ratio (should be > 99%)
- Index usage patterns (should increase over time)
- Materialized view query times (should stay < 1ms)

**Action Items:**
- Query times increased: Investigate slow queries
- Cache ratio decreased: Consider increasing shared_buffers
- New slow queries: Add indexes or optimize

---

### Week 2: Index Cleanup Review

**Time:** 20 minutes
**Priority:** MEDIUM

```bash
# Connect to database
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master
```

```sql
-- Find unused indexes (0 scans after 30+ days)
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    idx_scan as scans,
    pg_size_pretty(pg_relation_size(indexrelid)) as size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
  AND idx_scan = 0
  AND indexrelname NOT LIKE 'PK_%'  -- Keep primary keys
  AND indexrelname NOT LIKE 'FK_%'  -- Keep foreign keys
ORDER BY pg_relation_size(indexrelid) DESC;
```

**Decision Making:**

**Safe to Drop (All conditions must be true):**
- 0 scans after 30+ days
- Not a primary key or foreign key
- Large size (> 1 MB wasted space)
- You verified no application code uses it

**Keep:**
- Any index with > 0 scans
- Primary keys (PK_*)
- Foreign keys (FK_*)
- Recently created indexes (< 30 days old)
- Indexes for future features

**Dropping Unused Indexes (CAREFUL!):**
```sql
-- BACKUP FIRST!
-- pg_dump -h localhost -U postgres -d hrms_master > backup_before_index_drop.sql

-- Drop unused index (example - verify first!)
-- DROP INDEX IF EXISTS tenant_siraaj."IX_UnusedIndexName";

-- Verify no errors
-- Run application tests
-- Monitor for 7 days
```

**Rollback:**
```sql
-- Re-create index if needed
-- Check migration files for original CREATE INDEX statement
```

---

### Week 3: Slow Query Analysis (if pg_stat_statements enabled)

**Time:** 30 minutes
**Priority:** MEDIUM

```bash
# Connect to database
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master
```

```sql
-- Top 10 slowest queries (by average time)
SELECT
    SUBSTRING(query, 1, 100) as query_preview,
    calls,
    ROUND(total_exec_time::numeric, 2) as total_ms,
    ROUND(mean_exec_time::numeric, 2) as avg_ms,
    ROUND(max_exec_time::numeric, 2) as max_ms
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 10;

-- Top 10 most frequent queries
SELECT
    SUBSTRING(query, 1, 100) as query_preview,
    calls,
    ROUND(mean_exec_time::numeric, 2) as avg_ms
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY calls DESC
LIMIT 10;
```

**Optimization Targets:**

**Slow Queries (avg > 100ms):**
```sql
-- Get full query text
SELECT query FROM pg_stat_statements WHERE queryid = 'XXXXX';

-- Analyze query plan
EXPLAIN ANALYZE <your slow query here>;
```

**Common Fixes:**
- Missing index: Add appropriate index
- Sequential scan: Add WHERE clause index
- Large result set: Add LIMIT or pagination
- Complex JOIN: Consider materialized view

**Interactive Query Tester:**
```bash
# Test and analyze queries interactively
bash /workspaces/HRAPP/scripts/test_pg_stat_statements_queries.sh
```

---

### Week 4: Table Bloat Management

**Time:** 15 minutes
**Priority:** MEDIUM

```bash
# Connect to database
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master
```

```sql
-- Check table bloat
SELECT
    schemaname,
    tablename,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    CASE
        WHEN n_live_tup = 0 THEN 0
        ELSE ROUND((n_dead_tup::float / n_live_tup::float * 100)::numeric, 2)
    END as bloat_pct,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as total_size,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
  AND n_dead_tup > 0
ORDER BY bloat_pct DESC;
```

**Action Items:**

**High Bloat (> 20%):**
```sql
-- Manual VACUUM on bloated tables
VACUUM ANALYZE tenant_siraaj."TableName";

-- For persistent bloat issues
VACUUM FULL tenant_siraaj."TableName";  -- LOCKS TABLE - use carefully!
```

**Materialized Views (100-200% bloat is normal):**
```sql
-- Refresh and vacuum materialized views
REFRESH MATERIALIZED VIEW CONCURRENTLY tenant_siraaj."AttendanceMonthlySummary";
VACUUM ANALYZE tenant_siraaj."AttendanceMonthlySummary";
```

**Tune Auto-Vacuum (if bloat persists):**
```sql
-- Make auto-vacuum more aggressive
ALTER TABLE tenant_siraaj."TableName"
SET (autovacuum_vacuum_scale_factor = 0.05);  -- 5% instead of default 20%
```

---

### Month-End: Capture Performance Baseline

**Time:** 5 minutes
**Priority:** LOW

```bash
# Capture monthly baseline
bash /workspaces/HRAPP/scripts/capture-performance-baseline.sh

# Or manually:
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/database_performance_test_v2.sql \
> /workspaces/HRAPP/baselines/performance_$(date +%Y%m).txt
```

**Archive Monthly Reports:**
```bash
# Create monthly archive directory
mkdir -p /workspaces/HRAPP/monthly_reports/$(date +%Y%m)

# Move reports
mv /workspaces/HRAPP/performance_report_*.txt \
   /workspaces/HRAPP/monthly_reports/$(date +%Y%m)/

mv /workspaces/HRAPP/weekly_health_*.txt \
   /workspaces/HRAPP/monthly_reports/$(date +%Y%m)/
```

---

## TROUBLESHOOTING TIPS

### Issue: Cache Hit Ratio < 95%

**Symptoms:**
- Daily health check shows cache hit ratio < 95%
- Queries feel slower than usual
- Disk I/O increased

**Diagnosis:**
```sql
SELECT
    sum(heap_blks_read) as disk_reads,
    sum(heap_blks_hit) as cache_hits,
    ROUND((sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100)::numeric, 2) as cache_ratio
FROM pg_statio_user_tables;
```

**Solutions:**

**1. Increase shared_buffers (Recommended)**
```sql
-- Check current setting
SHOW shared_buffers;

-- Increase to 25% of RAM (requires PostgreSQL restart)
ALTER SYSTEM SET shared_buffers = '4GB';

-- Restart PostgreSQL
sudo systemctl restart postgresql
```

**2. Increase effective_cache_size**
```sql
-- No restart required
ALTER SYSTEM SET effective_cache_size = '12GB';  -- 75% of RAM
SELECT pg_reload_conf();
```

**3. Review query patterns**
```sql
-- Find tables with high disk reads
SELECT
    schemaname,
    tablename,
    heap_blks_read as disk_reads,
    heap_blks_hit as cache_hits
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj'
ORDER BY heap_blks_read DESC
LIMIT 10;
```

---

### Issue: Table Bloat > 20%

**Symptoms:**
- Health check shows bloat > 20%
- Table size growing faster than data
- Queries slower on bloated tables

**Diagnosis:**
```sql
-- Check specific table bloat
SELECT
    n_live_tup,
    n_dead_tup,
    ROUND((n_dead_tup::float / NULLIF(n_live_tup, 0)::float * 100)::numeric, 2) as bloat_pct,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
  AND tablename = 'YourTableName';
```

**Solutions:**

**1. Manual VACUUM**
```sql
-- Regular VACUUM (no table lock)
VACUUM ANALYZE tenant_siraaj."TableName";

-- Check if bloat reduced
-- (re-run diagnosis query)
```

**2. VACUUM FULL (if bloat persists)**
```sql
-- WARNING: LOCKS TABLE - use during maintenance window
VACUUM FULL tenant_siraaj."TableName";

-- Rebuilds entire table, reclaims all space
-- Can take several minutes for large tables
```

**3. Tune auto-vacuum settings**
```sql
-- Make auto-vacuum more aggressive for this table
ALTER TABLE tenant_siraaj."TableName" SET (
    autovacuum_vacuum_scale_factor = 0.05,  -- 5% instead of 20%
    autovacuum_analyze_scale_factor = 0.02   -- 2% instead of 10%
);
```

---

### Issue: Hangfire Job Failed

**Symptoms:**
- Job shows "Failed" status in Hangfire dashboard
- Error message visible in job details
- Scheduled maintenance not running

**Diagnosis:**
```bash
# 1. Check Hangfire dashboard
# Navigate to: https://your-app-url/hangfire
# Click: Failed Jobs tab
# Review: Error message and stack trace

# 2. Check application logs
cd /workspaces/HRAPP/src/HRMS.API
tail -n 100 logs/hrms-api.log | grep ERROR
```

**Solutions:**

**1. Retry the failed job**
```
# In Hangfire dashboard:
Click failed job → "Requeue" button
```

**2. Manual execution**
```bash
# Connect to database
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master
```

```sql
-- Manually refresh materialized views
REFRESH MATERIALIZED VIEW CONCURRENTLY "AttendanceMonthlySummary";
REFRESH MATERIALIZED VIEW CONCURRENTLY "EmployeeAttendanceStats";
REFRESH MATERIALIZED VIEW CONCURRENTLY "DepartmentAttendanceSummary";

-- Manually cleanup tokens
DELETE FROM "RefreshTokens"
WHERE "ExpiryDate" < NOW() - INTERVAL '7 days';

-- Manually vacuum tables
VACUUM ANALYZE "AuditLogs";
VACUUM ANALYZE "RefreshTokens";
```

**3. Check database connection**
```bash
# Verify database is accessible
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -c "SELECT 1;"

# Check connection pool
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -c "
SELECT count(*) as active_connections
FROM pg_stat_activity
WHERE datname = 'hrms_master';"
```

**4. Common error fixes:**

**Error: "Materialized view does not exist"**
```sql
-- Re-create materialized views
-- Run: /workspaces/HRAPP/database_optimization_scripts/08_materialized_views_corrected.sql
```

**Error: "Permission denied"**
```sql
-- Grant permissions
GRANT ALL ON ALL TABLES IN SCHEMA tenant_siraaj TO your_app_user;
GRANT ALL ON ALL MATERIALIZED VIEWS IN SCHEMA tenant_siraaj TO your_app_user;
```

**Error: "Connection timeout"**
```sql
-- Increase connection timeout
ALTER SYSTEM SET statement_timeout = '300s';  -- 5 minutes
SELECT pg_reload_conf();
```

---

### Issue: Slow Query Performance

**Symptoms:**
- Specific queries taking > 100ms
- User reports of slow page loads
- pg_stat_statements shows high mean_exec_time

**Diagnosis:**
```sql
-- Analyze query plan
EXPLAIN ANALYZE <your slow query>;

-- Check for sequential scans
-- Look for "Seq Scan" in output (bad for large tables)
-- Look for "Index Scan" (good)
```

**Solutions:**

**1. Add missing index**
```sql
-- Example: slow lookup by employee code
CREATE INDEX CONCURRENTLY "IX_Employees_EmployeeCode_Active"
ON tenant_siraaj."Employees" ("EmployeeCode")
WHERE "IsDeleted" = false AND "IsActive" = true;
```

**2. Update statistics**
```sql
-- PostgreSQL query planner uses statistics
ANALYZE tenant_siraaj."TableName";

-- Or full database
ANALYZE;
```

**3. Rewrite query**
```sql
-- Before (slow - OR condition)
SELECT * FROM "Employees"
WHERE "FirstName" = 'John' OR "LastName" = 'Smith';

-- After (fast - separate queries with UNION)
SELECT * FROM "Employees" WHERE "FirstName" = 'John'
UNION
SELECT * FROM "Employees" WHERE "LastName" = 'Smith';
```

**4. Use materialized view**
```sql
-- For complex aggregations, create materialized view
CREATE MATERIALIZED VIEW "MonthlySalesReport" AS
SELECT
    EXTRACT(YEAR FROM created_at) as year,
    EXTRACT(MONTH FROM created_at) as month,
    COUNT(*) as total_sales,
    SUM(amount) as total_revenue
FROM orders
GROUP BY year, month;

-- Create index on materialized view
CREATE INDEX ON "MonthlySalesReport" (year, month);

-- Auto-refresh daily
-- (Hangfire job handles this)
```

---

### Issue: Connection Pool Exhaustion

**Symptoms:**
- Error: "sorry, too many clients already"
- Application can't connect to database
- Health check shows connection usage > 90%

**Diagnosis:**
```sql
-- Check current connections
SELECT
    count(*) as active_connections,
    max_connections,
    ROUND((count(*)::float / max_connections::float * 100)::numeric, 2) as usage_pct
FROM pg_stat_activity, (SELECT setting::int as max_connections FROM pg_settings WHERE name = 'max_connections') s
GROUP BY max_connections;

-- Identify connection sources
SELECT
    datname,
    usename,
    application_name,
    count(*) as connections
FROM pg_stat_activity
GROUP BY datname, usename, application_name
ORDER BY connections DESC;
```

**Solutions:**

**1. Increase max_connections**
```sql
-- Increase connection limit (requires restart)
ALTER SYSTEM SET max_connections = 200;  -- from default 100

-- Restart PostgreSQL
sudo systemctl restart postgresql
```

**2. Kill idle connections**
```sql
-- Find long-running idle connections
SELECT
    pid,
    usename,
    application_name,
    state,
    state_change
FROM pg_stat_activity
WHERE state = 'idle'
  AND state_change < NOW() - INTERVAL '1 hour';

-- Kill idle connections (CAREFULLY!)
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE state = 'idle'
  AND state_change < NOW() - INTERVAL '1 hour';
```

**3. Configure connection pooling**
```csharp
// In appsettings.json (application side)
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hrms_master;Username=postgres;Password=postgres;Maximum Pool Size=20;Timeout=30;"
}
```

**4. Install PgBouncer (advanced)**
```bash
# Install connection pooler
sudo apt-get install pgbouncer

# Configure /etc/pgbouncer/pgbouncer.ini
# See: https://www.pgbouncer.org/config.html
```

---

### Issue: Materialized Views Not Refreshing

**Symptoms:**
- Dashboard shows stale data
- Last_refresh timestamp is old
- Hangfire job succeeds but views unchanged

**Diagnosis:**
```sql
-- Check materialized view refresh status
SELECT
    schemaname,
    matviewname,
    last_refresh
FROM pg_catalog.pg_matviews
WHERE schemaname = 'tenant_siraaj';

-- Check if views are populated
SELECT COUNT(*) FROM tenant_siraaj."AttendanceMonthlySummary";
```

**Solutions:**

**1. Manual refresh**
```sql
-- Refresh all materialized views
REFRESH MATERIALIZED VIEW CONCURRENTLY tenant_siraaj."AttendanceMonthlySummary";
REFRESH MATERIALIZED VIEW CONCURRENTLY tenant_siraaj."EmployeeAttendanceStats";
REFRESH MATERIALIZED VIEW CONCURRENTLY tenant_siraaj."DepartmentAttendanceSummary";

-- Check last_refresh updated
SELECT schemaname, matviewname, last_refresh
FROM pg_catalog.pg_matviews
WHERE schemaname = 'tenant_siraaj';
```

**2. Check Hangfire job**
```
# In Hangfire dashboard:
- Click "Recurring Jobs"
- Find "daily-mv-refresh"
- Click "Trigger Now"
- Monitor in "Processing" → "Succeeded" tabs
```

**3. Re-create views (if corrupted)**
```bash
# Run materialized view creation script
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/database_optimization_scripts/08_materialized_views_corrected.sql
```

---

### Emergency: Complete Rollback

**When to Use:** Critical database issues, need to restore to pre-optimization state

**Time:** 10-20 minutes
**Downtime:** 2-5 minutes

**STEP 1: Stop Application**
```bash
# Stop application to prevent new database changes
cd /workspaces/HRAPP/src/HRMS.API
# Press Ctrl+C to stop dotnet run

# Or if running as service
sudo systemctl stop hrms-api
```

**STEP 2: Create Emergency Backup**
```bash
# Backup current state (in case rollback fails)
pg_dump -h localhost -U postgres -d hrms_master \
> /workspaces/HRAPP/emergency_backup_$(date +%Y%m%d_%H%M%S).sql
```

**STEP 3: Restore Pre-Optimization Backup**
```bash
# Find pre-deployment backup (Nov 14, 2025)
ls -lh /workspaces/HRAPP/database_backup_*.sql

# Restore from backup
# WARNING: This will lose all data changes since Nov 14
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
< /workspaces/HRAPP/database_backup_20251114_*.sql
```

**STEP 4: Rollback Application Code**
```bash
# Revert Program.cs changes
cd /workspaces/HRAPP
git checkout HEAD -- src/HRMS.API/Program.cs

# Or manually comment out Hangfire jobs (Lines 938-944)
```

**STEP 5: Restart Application**
```bash
# Rebuild application
cd /workspaces/HRAPP/src/HRMS.API
dotnet build

# Start application
dotnet run

# Or if running as service
sudo systemctl start hrms-api
```

**STEP 6: Verify Rollback**
```bash
# Check database health
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-c "SELECT version();"

# Verify application starts
curl http://localhost:5000/health

# Check Hangfire dashboard (jobs should be gone or commented out)
```

---

## QUICK COMMAND REFERENCE

### Daily Commands

```bash
# Daily health check (30 seconds)
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/quick_performance_check.sql

# Check Hangfire dashboard
# Open: https://your-app-url/hangfire
```

### Weekly Commands

```bash
# Weekly health review
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/quick_performance_check.sql \
> /workspaces/HRAPP/weekly_health_$(date +%Y%m%d).txt

# Review index usage
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-c "SELECT * FROM pg_stat_user_indexes WHERE schemaname = 'tenant_siraaj' ORDER BY idx_scan DESC LIMIT 20;"
```

### Monthly Commands

```bash
# Comprehensive performance test
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-f /workspaces/HRAPP/database_performance_test_v2.sql \
> /workspaces/HRAPP/performance_report_$(date +%Y%m).txt

# Capture baseline
bash /workspaces/HRAPP/scripts/capture-performance-baseline.sh
```

### Emergency Commands

```bash
# Emergency backup
pg_dump -h localhost -U postgres -d hrms_master \
> /workspaces/HRAPP/emergency_backup_$(date +%Y%m%d_%H%M%S).sql

# Rollback pg_stat_statements
sudo bash /workspaces/HRAPP/scripts/rollback_pg_stat_statements.sh

# Manual VACUUM
PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master \
-c "VACUUM ANALYZE;"
```

---

## DOCUMENTATION REFERENCE

**Primary Documentation:**
- `/workspaces/HRAPP/PROJECT_HANDOFF_COMPLETE.md` - Comprehensive technical report
- `/workspaces/HRAPP/EXECUTIVE_SUMMARY_FINAL.md` - Business summary
- `/workspaces/HRAPP/DEPLOYMENT_DOCUMENTATION_INDEX.md` - Master index

**Performance & Monitoring:**
- `/workspaces/HRAPP/PERFORMANCE_QUICK_REFERENCE.md`
- `/workspaces/HRAPP/DATABASE_OPERATIONS_QUICK_REFERENCE.md`
- `/workspaces/HRAPP/PG_STAT_STATEMENTS_QUICK_REFERENCE.md`

**Detailed Guides:**
- `/workspaces/HRAPP/DATABASE_PERFORMANCE_VERIFICATION_REPORT.md` (24 KB)
- `/workspaces/HRAPP/FINAL_DEPLOYMENT_VALIDATION_REPORT.md` (33 KB)
- `/workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md` (13 KB)

---

**Last Updated:** November 14, 2025
**Version:** 1.0
**Status:** Production Ready

---

**END OF QUICK START GUIDE**
