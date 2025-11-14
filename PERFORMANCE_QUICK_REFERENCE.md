# Database Performance - Quick Reference Card

## Quick Status Check (30 seconds)

```bash
psql "postgresql://postgres:postgres@localhost:5432/hrms_master" \
  -f quick_performance_check.sql
```

**Expected Output:**
- Overall Grade: **B+ FAIR - Needs Tuning**
- Table Cache: ~98%
- Index Cache: ~91%

---

## Current Performance Metrics

| Metric | Value | Grade |
|--------|-------|-------|
| Query Performance | < 1 ms | A+ ✅ |
| Table Cache | 98.04% | A ✅ |
| Index Cache | 91.53% | B ⚠️ |
| Custom Indexes | 81 deployed | A+ ✅ |
| Materialized Views | 3 active | A ✅ |
| Auto-Vacuum | Enabled | A ✅ |
| Table Bloat | 16.67% | B ✅ |

---

## Composite Indexes (Migration: 20251112)

### ✅ Verified Working

- **IX_PayrollCycles_Year_Month** - Index scan confirmed ⭐
- **IX_PayrollCycles_Status** - Bitmap scan confirmed ⭐
- **IX_Timesheets_Status** - Index scan confirmed ⭐

### ✅ Deployed and Ready

- IX_LeaveBalances_EmployeeId_Year_LeaveTypeId
- IX_Attendances_EmployeeId_Date_Status
- IX_Attendances_DeviceId_Date
- IX_Timesheets_EmployeeId_Status_PeriodStart
- IX_TimesheetEntries_TimesheetId_Date
- IX_Employees_FirstName_LastName_IsActive
- IX_LeaveApplications_EmployeeId_StartDate_EndDate

### ⏳ Pending (Table Not Created)

- IX_BiometricPunchRecords_* (3 indexes)

---

## Materialized Views

| View | Size | Status | Last Refresh |
|------|------|--------|--------------|
| AttendanceMonthlySummary | 48 kB | ✅ POPULATED | 2025-11-14 |
| EmployeeAttendanceStats | 64 kB | ✅ POPULATED | 2025-11-14 |
| DepartmentAttendanceSummary | 48 kB | ✅ POPULATED | 2025-11-14 |

**Total Supporting Indexes:** 7 (112 kB)

---

## Immediate Actions Required

### 1. Manual VACUUM (5 minutes)

```sql
VACUUM ANALYZE tenant_siraaj."AttendanceMonthlySummary";
VACUUM ANALYZE tenant_siraaj."EmployeeAttendanceStats";
VACUUM ANALYZE tenant_siraaj."DepartmentAttendanceSummary";
```

**Impact:** Reduce bloat from 100% to < 5%

### 2. Schedule Materialized View Refresh (Setup Once)

```sql
-- Option A: Concurrent refresh (non-blocking)
REFRESH MATERIALIZED VIEW CONCURRENTLY tenant_siraaj."AttendanceMonthlySummary";

-- Option B: Full refresh (faster, but locks)
REFRESH MATERIALIZED VIEW tenant_siraaj."AttendanceMonthlySummary";
```

**Frequency:** Hourly or daily based on reporting needs

---

## Production Tuning (Before Go-Live)

### PostgreSQL Settings

```sql
-- Assuming 16GB RAM server
ALTER SYSTEM SET shared_buffers = '4GB';           -- 25% of RAM
ALTER SYSTEM SET effective_cache_size = '12GB';    -- 75% of RAM
ALTER SYSTEM SET maintenance_work_mem = '512MB';   -- For VACUUM
ALTER SYSTEM SET work_mem = '16MB';                -- For sorting
ALTER SYSTEM SET random_page_cost = 1.1;           -- SSD optimization
ALTER SYSTEM SET effective_io_concurrency = 200;   -- SSD
ALTER SYSTEM SET max_parallel_workers_per_gather = 4;

-- Then restart PostgreSQL
sudo systemctl restart postgresql
```

### Enable Query Monitoring

```sql
-- Add to postgresql.conf
shared_preload_libraries = 'pg_stat_statements'

-- Then restart and enable
CREATE EXTENSION pg_stat_statements;
```

---

## Monitoring Commands

### Cache Hit Ratios

```sql
-- Quick check
SELECT
    'Table' as type,
    (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100)::numeric(5,2) as hit_ratio
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj'
UNION ALL
SELECT
    'Index' as type,
    (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100)::numeric(5,2)
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj';
```

### Index Usage

```sql
-- Find unused indexes
SELECT schemaname, tablename, indexrelname, idx_scan
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
  AND idx_scan = 0
  AND indexrelname NOT LIKE '%_pkey'
ORDER BY pg_relation_size(indexrelid) DESC;
```

### Table Bloat

```sql
-- Check bloat percentage
SELECT
    relname,
    n_live_tup,
    n_dead_tup,
    (n_dead_tup::float / NULLIF(n_live_tup, 0) * 100)::numeric(5,2) as bloat_percent
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
ORDER BY n_dead_tup DESC;
```

---

## Troubleshooting

### Cache Hit Ratio Low (<95%)

**Cause:** Insufficient shared_buffers
**Fix:** Increase shared_buffers to 25% of RAM

### Queries Still Slow

**Check:**
1. EXPLAIN ANALYZE your query
2. Verify index is being used
3. Check table statistics are up to date

```sql
ANALYZE tenant_siraaj."TableName";
```

### High Table Bloat (>20%)

**Fix:**
```sql
VACUUM ANALYZE tenant_siraaj."TableName";
```

**For severe bloat:**
```sql
VACUUM FULL tenant_siraaj."TableName";  -- Locks table
```

---

## Performance Testing Scripts

| Script | Purpose | Time |
|--------|---------|------|
| `quick_performance_check.sql` | Quick health check | 30 sec |
| `database_performance_test_v2.sql` | Full test suite | 2-3 min |

---

## Expected Performance Targets

| Query Type | Target | Current |
|------------|--------|---------|
| Employee Lookup | < 1 ms | 0.027 ms ✅ |
| Attendance Range | < 5 ms | 0.051 ms ✅ |
| Payroll Cycle | < 1 ms | 0.015 ms ✅ |
| Dashboard Aggregates | < 10 ms | 0.326 ms ✅ |

---

## Index Naming Convention

- `IX_` - Custom performance index
- `PK_` - Primary key
- `FK_` - Foreign key
- `idx_` - Legacy index

---

## Support Contacts

- **Full Report:** `DATABASE_PERFORMANCE_VERIFICATION_REPORT.md`
- **Executive Summary:** `PERFORMANCE_TEST_EXECUTIVE_SUMMARY.md`
- **Test Results:** `performance_test_results_v2.txt`

---

## Last Updated

**Date:** 2025-11-14
**Database Size:** 16 MB
**Total Indexes:** 129
**Total Tables:** 27

---

**Quick Reference Version 1.0**
