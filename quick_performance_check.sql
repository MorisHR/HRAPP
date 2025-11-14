-- =====================================================
-- QUICK PERFORMANCE CHECK SCRIPT
-- =====================================================
-- Purpose: Fast 30-second database health check
-- Run this script anytime to verify database performance
-- =====================================================

\timing on
\x auto

\echo ''
\echo '========================================='
\echo '    QUICK DATABASE PERFORMANCE CHECK'
\echo '========================================='
\echo ''

-- 1. Database Version and Size
\echo '1. Database Overview:'
SELECT
    version() as postgres_version,
    current_database() as database,
    pg_size_pretty(pg_database_size(current_database())) AS size;

-- 2. Cache Hit Ratios (Target: >99%)
\echo ''
\echo '2. Cache Performance (Target: >99%):'
SELECT
    'Table Cache' as metric,
    (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100)::numeric(5,2) as hit_ratio_percent,
    CASE
        WHEN (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) >= 99 THEN '✓ EXCELLENT'
        WHEN (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) >= 95 THEN '⚠ GOOD'
        ELSE '✗ NEEDS ATTENTION'
    END as status
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj'
UNION ALL
SELECT
    'Index Cache' as metric,
    (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100)::numeric(5,2) as hit_ratio_percent,
    CASE
        WHEN (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) >= 99 THEN '✓ EXCELLENT'
        WHEN (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) >= 95 THEN '⚠ GOOD'
        ELSE '✗ NEEDS ATTENTION'
    END as status
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj';

-- 3. Index Health
\echo ''
\echo '3. Index Health:'
SELECT
    COUNT(*) as total_indexes,
    COUNT(*) FILTER (WHERE idx_scan > 0) as indexes_used,
    COUNT(*) FILTER (WHERE idx_scan = 0 AND indexrelname NOT LIKE '%_pkey') as indexes_unused,
    pg_size_pretty(SUM(pg_relation_size(indexrelid))::bigint) as total_index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj';

-- 4. Composite Index Verification
\echo ''
\echo '4. Critical Composite Indexes (from optimization migration):'
SELECT
    tablename,
    indexname,
    idx_scan as times_used,
    CASE
        WHEN idx_scan > 100 THEN '✓ HIGH USAGE'
        WHEN idx_scan > 10 THEN '✓ MODERATE'
        WHEN idx_scan > 0 THEN '⚠ LOW USAGE'
        ELSE '○ READY (NO DATA YET)'
    END as status
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
    AND indexname IN (
        'IX_PayrollCycles_Year_Month',
        'IX_PayrollCycles_Status_PaymentDate',
        'IX_LeaveBalances_EmployeeId_LeaveTypeId_Year',
        'IX_Attendances_EmployeeId_Date_Status',
        'IX_Attendances_DeviceId_Date',
        'IX_Timesheets_Status_PeriodStart',
        'IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd',
        'IX_Employees_FirstName_LastName_IsActive',
        'IX_TimesheetEntries_TimesheetId_Date',
        'IX_LeaveApplications_EmployeeId_StartDate_EndDate'
    )
ORDER BY idx_scan DESC;

-- 5. Materialized Views
\echo ''
\echo '5. Materialized Views:'
SELECT
    schemaname,
    matviewname,
    pg_size_pretty(pg_relation_size(schemaname||'.'||matviewname)) as size
FROM pg_matviews
WHERE schemaname = 'tenant_siraaj'
ORDER BY matviewname;

-- 6. Table Bloat
\echo ''
\echo '6. Table Bloat (Target: <20%):'
SELECT
    relname as table_name,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    CASE
        WHEN n_live_tup = 0 THEN 0
        ELSE (n_dead_tup::float / n_live_tup * 100)::numeric(5,2)
    END as bloat_percent,
    CASE
        WHEN n_live_tup = 0 THEN '✓ OK'
        WHEN (n_dead_tup::float / n_live_tup * 100) < 10 THEN '✓ EXCELLENT'
        WHEN (n_dead_tup::float / n_live_tup * 100) < 20 THEN '✓ GOOD'
        ELSE '⚠ NEEDS VACUUM'
    END as status,
    last_autovacuum
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
    AND n_dead_tup > 0
ORDER BY n_dead_tup DESC
LIMIT 10;

-- 7. Top 5 Largest Tables
\echo ''
\echo '7. Top 5 Largest Tables:'
SELECT
    relname as table_name,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||relname)) as total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||relname)) as data_size,
    n_live_tup as rows
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
ORDER BY pg_total_relation_size(schemaname||'.'||relname) DESC
LIMIT 5;

-- 8. Auto-Vacuum Status
\echo ''
\echo '8. Auto-Vacuum Configuration:'
SELECT
    name,
    setting,
    CASE
        WHEN name = 'autovacuum' AND setting = 'on' THEN '✓ ENABLED'
        WHEN name = 'autovacuum' THEN '✗ DISABLED'
        ELSE setting
    END as value
FROM pg_settings
WHERE name IN ('autovacuum', 'autovacuum_max_workers', 'autovacuum_naptime')
ORDER BY name;

-- 9. Performance Summary
\echo ''
\echo '9. Overall Performance Summary:'
WITH metrics AS (
    SELECT
        (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) as table_cache,
        (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) as index_cache,
        (SELECT COUNT(*) FROM pg_stat_user_indexes WHERE schemaname = 'tenant_siraaj' AND idx_scan = 0 AND indexrelname NOT LIKE '%_pkey') as unused_indexes,
        (SELECT AVG(CASE WHEN n_live_tup = 0 THEN 0 ELSE (n_dead_tup::float / n_live_tup * 100) END)
         FROM pg_stat_user_tables WHERE schemaname = 'tenant_siraaj') as avg_bloat
    FROM pg_statio_user_tables
    WHERE schemaname = 'tenant_siraaj'
)
SELECT
    CASE
        WHEN table_cache >= 99 AND index_cache >= 99 AND unused_indexes < 5 AND avg_bloat < 10 THEN '✓✓ A+ EXCELLENT'
        WHEN table_cache >= 95 AND index_cache >= 95 AND unused_indexes < 20 AND avg_bloat < 20 THEN '✓ A GOOD'
        WHEN table_cache >= 90 AND index_cache >= 90 THEN '⚠ B FAIR - Needs Tuning'
        ELSE '✗ C POOR - Immediate Action Required'
    END as overall_grade,
    table_cache::numeric(5,2) || '%' as table_cache,
    index_cache::numeric(5,2) || '%' as index_cache,
    unused_indexes::text as unused_indexes,
    avg_bloat::numeric(5,2) || '%' as avg_bloat
FROM metrics;

\echo ''
\echo '========================================='
\echo '   PERFORMANCE CHECK COMPLETE'
\echo '========================================='
\echo ''
\echo 'For detailed analysis, run: psql -f database_performance_test_v2.sql'
\echo ''
