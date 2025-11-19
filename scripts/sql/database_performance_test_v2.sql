-- =====================================================
-- DATABASE PERFORMANCE VERIFICATION AND TESTING SCRIPT V2
-- =====================================================
-- Purpose: Comprehensive performance testing of all database optimizations
-- Target: HRMS PostgreSQL Database (Corrected Schema)
-- Date: 2025-11-14
-- =====================================================

\timing on
\x auto

\echo ''
\echo '========================================================================='
\echo '       DATABASE PERFORMANCE VERIFICATION REPORT'
\echo '       HRMS Application - PostgreSQL Performance Analysis'
\echo '       Date: 2025-11-14'
\echo '========================================================================='
\echo ''

-- =====================================================
-- SECTION 1: DATABASE HEALTH CHECK
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 1: DATABASE HEALTH CHECK'
\echo '====================================='
\echo ''

SELECT version();

SELECT
    current_database() as database,
    pg_size_pretty(pg_database_size(current_database())) AS total_size;

SELECT
    schemaname as schema_name,
    count(*) as table_count,
    pg_size_pretty(sum(pg_total_relation_size(schemaname||'.'||tablename))::bigint) as total_size
FROM pg_tables
WHERE schemaname LIKE 'tenant_%' OR schemaname = 'public'
GROUP BY schemaname
ORDER BY sum(pg_total_relation_size(schemaname||'.'||tablename)) DESC;

-- =====================================================
-- SECTION 2: INDEX USAGE STATISTICS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 2: INDEX USAGE STATISTICS'
\echo '====================================='
\echo ''

SELECT
    schemaname,
    relname as table_name,
    indexrelname as index_name,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
ORDER BY idx_scan DESC
LIMIT 30;

-- Find unused indexes
\echo ''
\echo 'Unused Indexes (Candidates for Investigation):'
SELECT
    schemaname,
    relname as table_name,
    indexrelname as index_name,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
    AND idx_scan = 0
    AND indexrelname NOT LIKE '%_pkey'
ORDER BY pg_relation_size(indexrelid) DESC;

-- =====================================================
-- SECTION 3: CACHE HIT RATIOS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 3: CACHE HIT RATIOS'
\echo '====================================='
\echo ''

SELECT
    'Table Cache Hit Ratio' as metric,
    (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100)::numeric(5,2) as percentage,
    CASE
        WHEN (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) >= 99 THEN 'EXCELLENT'
        WHEN (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) >= 95 THEN 'GOOD'
        WHEN (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) >= 90 THEN 'FAIR'
        ELSE 'POOR'
    END as grade,
    sum(heap_blks_hit) as cache_hits,
    sum(heap_blks_read) as disk_reads
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj';

SELECT
    'Index Cache Hit Ratio' as metric,
    (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100)::numeric(5,2) as percentage,
    CASE
        WHEN (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) >= 99 THEN 'EXCELLENT'
        WHEN (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) >= 95 THEN 'GOOD'
        WHEN (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) >= 90 THEN 'FAIR'
        ELSE 'POOR'
    END as grade,
    sum(idx_blks_hit) as cache_hits,
    sum(idx_blks_read) as disk_reads
FROM pg_statio_user_tables
WHERE schemaname = 'tenant_siraaj';

-- =====================================================
-- SECTION 4: TABLE BLOAT DETECTION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 4: TABLE BLOAT ANALYSIS'
\echo '====================================='
\echo ''

SELECT
    schemaname,
    relname as table_name,
    n_live_tup as live_tuples,
    n_dead_tup as dead_tuples,
    CASE
        WHEN n_live_tup = 0 THEN 0
        ELSE (n_dead_tup::float / n_live_tup * 100)::numeric(5,2)
    END as dead_tuple_percent,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
ORDER BY n_dead_tup DESC
LIMIT 15;

-- =====================================================
-- SECTION 5: COMPOSITE INDEX VERIFICATION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 5: COMPOSITE INDEX VERIFICATION'
\echo '====================================='
\echo ''

\echo 'All indexes with composite keys (created by optimization migration):'
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(schemaname||'.'||indexname)) as index_size
FROM pg_indexes
WHERE schemaname = 'tenant_siraaj'
    AND indexname LIKE 'IX_%'
ORDER BY tablename, indexname;

-- =====================================================
-- SECTION 6: PERFORMANCE TEST - EMPLOYEE QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 6: EMPLOYEE QUERY PERFORMANCE TESTS'
\echo '====================================='
\echo ''

-- Test 1: Employee lookup by code (should use index)
\echo ''
\echo 'TEST 1: Employee Code Lookup Performance'
\echo '----------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT * FROM tenant_siraaj."Employees"
WHERE "EmployeeCode" = 'EMP001'
    AND "IsDeleted" = false
    AND "IsActive" = true;

-- Test 2: Employee name search
\echo ''
\echo 'TEST 2: Employee Name Search (Composite Index)'
\echo '-----------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT "Id", "EmployeeCode", "FirstName", "LastName", "Email"
FROM tenant_siraaj."Employees"
WHERE "FirstName" > ''
    AND "IsActive" = true
    AND "IsDeleted" = false
LIMIT 50;

-- Test 3: Active employees count
\echo ''
\echo 'TEST 3: Active Employees Aggregate Query'
\echo '-----------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    COUNT(*) as total_active_employees,
    COUNT(CASE WHEN "Gender" = 0 THEN 1 END) as male_count,
    COUNT(CASE WHEN "Gender" = 1 THEN 1 END) as female_count
FROM tenant_siraaj."Employees"
WHERE "IsActive" = true AND "IsDeleted" = false;

-- =====================================================
-- SECTION 7: PERFORMANCE TEST - ATTENDANCE QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 7: ATTENDANCE QUERY PERFORMANCE TESTS'
\echo '====================================='
\echo ''

-- Test 4: Attendance range query
\echo ''
\echo 'TEST 4: Monthly Attendance Range Query'
\echo '---------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT * FROM tenant_siraaj."Attendances"
WHERE "Date" >= '2025-11-01'::date
    AND "Date" < '2025-12-01'::date
    AND "IsDeleted" = false
ORDER BY "Date" DESC
LIMIT 100;

-- Test 5: Employee attendance history with composite index
\echo ''
\echo 'TEST 5: Employee Attendance History (Composite Index)'
\echo '------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "EmployeeId",
    "Date",
    "CheckInTime",
    "CheckOutTime",
    "Status",
    "WorkingHours"
FROM tenant_siraaj."Attendances"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND "Date" >= '2025-11-01'::date
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "Date" DESC;

-- Test 6: Device attendance lookup
\echo ''
\echo 'TEST 6: Device-Based Attendance Summary'
\echo '----------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "DeviceId",
    COUNT(*) as punch_count,
    MIN("Date") as first_punch,
    MAX("Date") as last_punch
FROM tenant_siraaj."Attendances"
WHERE "DeviceId" IS NOT NULL
    AND "Date" >= CURRENT_DATE - INTERVAL '30 days'
    AND "IsDeleted" = false
GROUP BY "DeviceId";

-- =====================================================
-- SECTION 8: PERFORMANCE TEST - PAYROLL QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 8: PAYROLL QUERY PERFORMANCE TESTS'
\echo '====================================='
\echo ''

-- Test 7: Monthly payroll cycle lookup with composite index
\echo ''
\echo 'TEST 7: Monthly Payroll Cycle Lookup (IX_PayrollCycles_Year_Month)'
\echo '-------------------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT * FROM tenant_siraaj."PayrollCycles"
WHERE "Year" = 2025
    AND "Month" = 11
    AND "IsDeleted" = false
ORDER BY "Year" DESC, "Month" DESC;

-- Test 8: Payroll status report
\echo ''
\echo 'TEST 8: Payroll Status Aggregation'
\echo '-----------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Status",
    "PaymentDate",
    COUNT(*) as cycle_count
FROM tenant_siraaj."PayrollCycles"
WHERE "Status" IN (2, 3)
    AND "PaymentDate" >= '2025-01-01'::date
    AND "IsDeleted" = false
GROUP BY "Status", "PaymentDate"
ORDER BY "PaymentDate" DESC;

-- =====================================================
-- SECTION 9: PERFORMANCE TEST - LEAVE MANAGEMENT
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 9: LEAVE MANAGEMENT PERFORMANCE TESTS'
\echo '====================================='
\echo ''

-- Test 9: Leave balance lookup with composite index
\echo ''
\echo 'TEST 9: Leave Balance Lookup (IX_LeaveBalances_EmployeeId_Year_LeaveTypeId)'
\echo '----------------------------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    lb."EmployeeId",
    lb."Year",
    lb."LeaveTypeId",
    lb."Earned",
    lb."Used",
    lb."Balance"
FROM tenant_siraaj."LeaveBalances" lb
WHERE lb."EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND lb."Year" = 2025
    AND lb."IsDeleted" = false
ORDER BY lb."EmployeeId", lb."Year" DESC;

-- Test 10: Leave applications date range
\echo ''
\echo 'TEST 10: Leave Applications Date Range (Composite Index)'
\echo '---------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "EmployeeId",
    "StartDate",
    "EndDate",
    "Status",
    "Days"
FROM tenant_siraaj."LeaveApplications"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 10
)
    AND "StartDate" >= '2025-01-01'::date
    AND "EndDate" <= '2025-12-31'::date
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "StartDate" DESC;

-- =====================================================
-- SECTION 10: PERFORMANCE TEST - TIMESHEET QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 10: TIMESHEET QUERY PERFORMANCE TESTS'
\echo '====================================='
\echo ''

-- Test 11: Timesheet approval workflow
\echo ''
\echo 'TEST 11: Timesheet Approval Workflow (IX_Timesheets_Status_PeriodStart)'
\echo '------------------------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Id",
    "EmployeeId",
    "Status",
    "PeriodStart",
    "PeriodEnd"
FROM tenant_siraaj."Timesheets"
WHERE "Status" IN (0, 1)
    AND "PeriodStart" >= '2025-11-01'::date
    AND "IsDeleted" = false
ORDER BY "Status", "PeriodStart" DESC
LIMIT 50;

-- Test 12: Employee timesheet history
\echo ''
\echo 'TEST 12: Employee Timesheet History (Composite Index)'
\echo '------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "EmployeeId",
    "Status",
    "PeriodStart",
    "PeriodEnd",
    "ApprovedBy",
    "ApprovedAt"
FROM tenant_siraaj."Timesheets"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND "Status" = 2
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "PeriodStart" DESC;

-- Test 13: Timesheet entries by date
\echo ''
\echo 'TEST 13: Timesheet Entries Date Query (IX_TimesheetEntries_TimesheetId_Date)'
\echo '-----------------------------------------------------------------------------'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    te."TimesheetId",
    te."Date",
    te."ProjectId",
    te."TaskId"
FROM tenant_siraaj."TimesheetEntries" te
INNER JOIN tenant_siraaj."Timesheets" t ON te."TimesheetId" = t."Id"
WHERE t."PeriodStart" >= '2025-11-01'::date
    AND te."IsDeleted" = false
ORDER BY te."TimesheetId", te."Date" DESC
LIMIT 100;

-- =====================================================
-- SECTION 11: AUTO-VACUUM CONFIGURATION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 11: AUTO-VACUUM CONFIGURATION'
\echo '====================================='
\echo ''

SELECT
    name,
    setting,
    unit,
    short_desc
FROM pg_settings
WHERE name IN (
    'autovacuum',
    'autovacuum_max_workers',
    'autovacuum_naptime',
    'autovacuum_vacuum_threshold',
    'autovacuum_analyze_threshold',
    'autovacuum_vacuum_scale_factor',
    'autovacuum_analyze_scale_factor'
)
ORDER BY name;

-- =====================================================
-- SECTION 12: TABLE STATISTICS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 12: TABLE SIZE STATISTICS'
\echo '====================================='
\echo ''

SELECT
    schemaname,
    relname as table_name,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||relname)) as total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||relname)) as table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||relname) -
                   pg_relation_size(schemaname||'.'||relname)) as indexes_size,
    n_live_tup as row_count,
    CASE
        WHEN pg_relation_size(schemaname||'.'||relname) = 0 THEN 0
        ELSE ((pg_total_relation_size(schemaname||'.'||relname) - pg_relation_size(schemaname||'.'||relname))::float /
              pg_relation_size(schemaname||'.'||relname) * 100)::numeric(5,2)
    END as index_to_table_ratio
FROM pg_stat_user_tables
WHERE schemaname = 'tenant_siraaj'
ORDER BY pg_total_relation_size(schemaname||'.'||relname) DESC
LIMIT 20;

-- =====================================================
-- SECTION 13: PERFORMANCE SETTINGS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 13: DATABASE PERFORMANCE SETTINGS'
\echo '====================================='
\echo ''

SELECT
    name,
    setting,
    unit,
    short_desc
FROM pg_settings
WHERE name IN (
    'max_connections',
    'shared_buffers',
    'effective_cache_size',
    'maintenance_work_mem',
    'work_mem',
    'random_page_cost',
    'effective_io_concurrency',
    'max_worker_processes',
    'max_parallel_workers_per_gather',
    'max_parallel_workers'
)
ORDER BY name;

-- =====================================================
-- SECTION 14: INDEX EFFICIENCY ANALYSIS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 14: INDEX EFFICIENCY ANALYSIS'
\echo '====================================='
\echo ''

SELECT
    schemaname,
    relname as table_name,
    indexrelname as index_name,
    idx_scan as index_scans,
    CASE
        WHEN idx_scan = 0 THEN 'NEVER USED'
        WHEN idx_scan < 100 THEN 'LOW USAGE'
        WHEN idx_scan < 1000 THEN 'MODERATE USAGE'
        ELSE 'HIGH USAGE'
    END as usage_category,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_siraaj'
ORDER BY idx_scan ASC, pg_relation_size(indexrelid) DESC
LIMIT 20;

-- =====================================================
-- SECTION 15: PERFORMANCE SUMMARY & GRADING
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 15: PERFORMANCE SUMMARY'
\echo '====================================='
\echo ''

WITH metrics AS (
    SELECT
        (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) as table_cache_hit_ratio,
        (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100) as index_cache_hit_ratio,
        (SELECT COUNT(*) FROM pg_stat_user_indexes WHERE schemaname = 'tenant_siraaj' AND idx_scan = 0 AND indexrelname NOT LIKE '%_pkey') as unused_index_count,
        (SELECT AVG(CASE WHEN n_live_tup = 0 THEN 0 ELSE (n_dead_tup::float / n_live_tup * 100) END)
         FROM pg_stat_user_tables WHERE schemaname = 'tenant_siraaj') as avg_bloat_percent,
        (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'tenant_siraaj' AND indexname LIKE 'IX_%') as total_custom_indexes,
        (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'tenant_siraaj') as total_tables
    FROM pg_statio_user_tables
    WHERE schemaname = 'tenant_siraaj'
)
SELECT
    'PERFORMANCE METRIC' as category,
    'Table Cache Hit Ratio' as metric,
    table_cache_hit_ratio::numeric(5,2) || '%' as value,
    CASE
        WHEN table_cache_hit_ratio >= 99 THEN 'A+ EXCELLENT'
        WHEN table_cache_hit_ratio >= 95 THEN 'A GOOD'
        WHEN table_cache_hit_ratio >= 90 THEN 'B FAIR'
        ELSE 'C POOR'
    END as grade
FROM metrics
UNION ALL
SELECT
    'PERFORMANCE METRIC',
    'Index Cache Hit Ratio',
    index_cache_hit_ratio::numeric(5,2) || '%',
    CASE
        WHEN index_cache_hit_ratio >= 99 THEN 'A+ EXCELLENT'
        WHEN index_cache_hit_ratio >= 95 THEN 'A GOOD'
        WHEN index_cache_hit_ratio >= 90 THEN 'B FAIR'
        ELSE 'C POOR'
    END
FROM metrics
UNION ALL
SELECT
    'INDEX OPTIMIZATION',
    'Unused Indexes',
    unused_index_count::text,
    CASE
        WHEN unused_index_count = 0 THEN 'A+ EXCELLENT'
        WHEN unused_index_count <= 3 THEN 'A GOOD'
        WHEN unused_index_count <= 7 THEN 'B FAIR'
        ELSE 'C NEEDS REVIEW'
    END
FROM metrics
UNION ALL
SELECT
    'TABLE HEALTH',
    'Average Table Bloat',
    avg_bloat_percent::numeric(5,2) || '%',
    CASE
        WHEN avg_bloat_percent < 5 THEN 'A+ EXCELLENT'
        WHEN avg_bloat_percent < 10 THEN 'A GOOD'
        WHEN avg_bloat_percent < 20 THEN 'B FAIR'
        ELSE 'C NEEDS VACUUM'
    END
FROM metrics
UNION ALL
SELECT
    'INDEX COVERAGE',
    'Custom Performance Indexes',
    total_custom_indexes::text || ' indexes deployed',
    CASE
        WHEN total_custom_indexes >= 10 THEN 'A+ EXCELLENT'
        WHEN total_custom_indexes >= 5 THEN 'A GOOD'
        ELSE 'B FAIR'
    END
FROM metrics
UNION ALL
SELECT
    'DATABASE OVERVIEW',
    'Total Tables in Schema',
    total_tables::text,
    'INFO'
FROM metrics;

\echo ''
\echo '========================================================================='
\echo '       PERFORMANCE TEST COMPLETED'
\echo '       Review the output above for detailed performance metrics'
\echo '========================================================================='
\echo ''
