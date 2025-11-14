-- =====================================================
-- DATABASE PERFORMANCE VERIFICATION AND TESTING SCRIPT
-- =====================================================
-- Purpose: Comprehensive performance testing of all database optimizations
-- Target: HRMS PostgreSQL Database (Master + Tenant Schemas)
-- Date: 2025-11-14
-- =====================================================

\timing on
\x auto

-- =====================================================
-- SECTION 1: DATABASE HEALTH CHECK
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 1: DATABASE HEALTH CHECK'
\echo '====================================='
\echo ''

-- Check PostgreSQL version and configuration
SELECT version();

-- Check current database size
SELECT
    pg_database.datname as database_name,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size
FROM pg_database
WHERE datname = current_database();

-- Check all schemas and their sizes
SELECT
    schema_name,
    pg_size_pretty(sum(table_size)::bigint) as schema_size,
    (sum(table_size) / pg_database_size(current_database()) * 100)::numeric(5,2) as percent_of_db
FROM (
    SELECT
        schemaname as schema_name,
        pg_total_relation_size(schemaname||'.'||tablename) as table_size
    FROM pg_tables
    WHERE schemaname LIKE 'tenant_%' OR schemaname = 'public'
) t
GROUP BY schema_name
ORDER BY sum(table_size) DESC;

-- =====================================================
-- SECTION 2: INDEX USAGE STATISTICS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 2: INDEX USAGE STATISTICS'
\echo '====================================='
\echo ''

-- Check all indexes in tenant schemas
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname LIKE 'tenant_%'
ORDER BY schemaname, idx_scan DESC;

-- Find unused indexes (potential candidates for removal)
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    idx_scan as times_used
FROM pg_stat_user_indexes
WHERE schemaname LIKE 'tenant_%'
    AND idx_scan = 0
    AND indexrelname NOT LIKE '%_pkey'
ORDER BY pg_relation_size(indexrelid) DESC;

-- Index hit ratio (should be > 95%)
SELECT
    schemaname,
    tablename,
    CASE
        WHEN idx_scan + seq_scan = 0 THEN 0
        ELSE (idx_scan::float / (idx_scan + seq_scan) * 100)::numeric(5,2)
    END as index_hit_ratio_percent,
    idx_scan as index_scans,
    seq_scan as sequential_scans
FROM pg_stat_user_tables
WHERE schemaname LIKE 'tenant_%'
ORDER BY index_hit_ratio_percent ASC;

-- =====================================================
-- SECTION 3: CACHE HIT RATIOS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 3: CACHE HIT RATIOS'
\echo '====================================='
\echo ''

-- Overall cache hit ratio (should be > 99%)
SELECT
    'Cache Hit Ratio' as metric,
    (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100)::numeric(5,2) as percentage,
    sum(heap_blks_hit) as cache_hits,
    sum(heap_blks_read) as disk_reads
FROM pg_statio_user_tables
WHERE schemaname LIKE 'tenant_%';

-- Index cache hit ratio
SELECT
    'Index Cache Hit Ratio' as metric,
    (sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0) * 100)::numeric(5,2) as percentage,
    sum(idx_blks_hit) as cache_hits,
    sum(idx_blks_read) as disk_reads
FROM pg_statio_user_tables
WHERE schemaname LIKE 'tenant_%';

-- =====================================================
-- SECTION 4: TABLE BLOAT DETECTION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 4: TABLE BLOAT DETECTION'
\echo '====================================='
\echo ''

-- Check for table bloat (auto-vacuum effectiveness)
SELECT
    schemaname,
    tablename,
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
WHERE schemaname LIKE 'tenant_%'
ORDER BY n_dead_tup DESC
LIMIT 20;

-- =====================================================
-- SECTION 5: PERFORMANCE TEST - EMPLOYEE QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 5: EMPLOYEE QUERY PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 1: Employee lookup by code (should use IX_Employees_EmployeeCode_Active)
\echo 'Test 1: Employee Code Lookup (with index)'
EXPLAIN (ANALYZE, BUFFERS, TIMING, VERBOSE)
SELECT * FROM tenant_siraaj."Employees"
WHERE "EmployeeCode" = 'EMP001'
    AND "IsDeleted" = false
    AND "IsActive" = true;

-- Test 2: Employee name search (should use IX_Employees_FirstName_LastName_IsActive)
\echo 'Test 2: Employee Name Search (with index)'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT "Id", "EmployeeCode", "FirstName", "LastName", "Email"
FROM tenant_siraaj."Employees"
WHERE "FirstName" LIKE 'A%'
    AND "IsActive" = true
    AND "IsDeleted" = false
LIMIT 50;

-- Test 3: Active employees count
\echo 'Test 3: Active Employees Count'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    COUNT(*) as total_active_employees,
    COUNT(CASE WHEN "Gender" = 'Male' THEN 1 END) as male_count,
    COUNT(CASE WHEN "Gender" = 'Female' THEN 1 END) as female_count
FROM tenant_siraaj."Employees"
WHERE "IsActive" = true AND "IsDeleted" = false;

-- =====================================================
-- SECTION 6: PERFORMANCE TEST - ATTENDANCE QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 6: ATTENDANCE QUERY PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 4: Attendance range query (should use IX_Attendances_Date_EmployeeId)
\echo 'Test 4: Monthly Attendance Range Query'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT * FROM tenant_siraaj."Attendances"
WHERE "Date" >= '2025-11-01'::date
    AND "Date" < '2025-12-01'::date
    AND "IsDeleted" = false
ORDER BY "Date" DESC
LIMIT 100;

-- Test 5: Employee attendance history (should use IX_Attendances_EmployeeId_Date_Status)
\echo 'Test 5: Employee Attendance History'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Date",
    "CheckInTime",
    "CheckOutTime",
    "Status",
    "WorkHours"
FROM tenant_siraaj."Attendances"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND "Date" >= '2025-11-01'::date
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "Date" DESC;

-- Test 6: Device attendance lookup (should use IX_Attendances_DeviceId_Date)
\echo 'Test 6: Device Attendance Lookup'
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
-- SECTION 7: PERFORMANCE TEST - BIOMETRIC PUNCH RECORDS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 7: BIOMETRIC PUNCH PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 7: Unprocessed punch records (should use IX_BiometricPunchRecords_ProcessingStatus_PunchTime)
\echo 'Test 7: Unprocessed Punch Records Query'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Id",
    "EmployeeId",
    "DeviceId",
    "PunchTime",
    "ProcessingStatus"
FROM tenant_siraaj."BiometricPunchRecords"
WHERE "ProcessingStatus" = 'Pending'
    AND "IsDeleted" = false
ORDER BY "PunchTime" DESC
LIMIT 100;

-- Test 8: Employee punch history (should use IX_BiometricPunchRecords_EmployeeId_PunchTime)
\echo 'Test 8: Employee Punch History'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "PunchTime",
    "PunchType",
    "DeviceId",
    "ProcessingStatus"
FROM tenant_siraaj."BiometricPunchRecords"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 3
)
    AND "PunchTime" >= CURRENT_TIMESTAMP - INTERVAL '7 days'
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "PunchTime" DESC;

-- Test 9: Device sync query (should use IX_BiometricPunchRecords_DeviceId_PunchTime)
\echo 'Test 9: Device Sync Query'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "DeviceId",
    COUNT(*) as total_punches,
    COUNT(CASE WHEN "ProcessingStatus" = 'Processed' THEN 1 END) as processed,
    COUNT(CASE WHEN "ProcessingStatus" = 'Pending' THEN 1 END) as pending,
    MAX("PunchTime") as last_punch_time
FROM tenant_siraaj."BiometricPunchRecords"
WHERE "DeviceId" IS NOT NULL
    AND "PunchTime" >= CURRENT_DATE - INTERVAL '30 days'
    AND "IsDeleted" = false
GROUP BY "DeviceId";

-- =====================================================
-- SECTION 8: PERFORMANCE TEST - PAYROLL QUERIES
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 8: PAYROLL QUERY PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 10: Monthly payroll cycle lookup (should use IX_PayrollCycles_Year_Month)
\echo 'Test 10: Monthly Payroll Cycle Lookup'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT * FROM tenant_siraaj."PayrollCycles"
WHERE "Year" = 2025
    AND "Month" = 11
    AND "IsDeleted" = false
ORDER BY "Year" DESC, "Month" DESC;

-- Test 11: Payroll status report (should use IX_PayrollCycles_Status_PaymentDate)
\echo 'Test 11: Payroll Status Report'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Status",
    "PaymentDate",
    COUNT(*) as cycle_count,
    SUM("TotalAmount") as total_payroll
FROM tenant_siraaj."PayrollCycles"
WHERE "Status" IN ('Approved', 'Paid')
    AND "PaymentDate" >= '2025-01-01'::date
    AND "IsDeleted" = false
GROUP BY "Status", "PaymentDate"
ORDER BY "PaymentDate" DESC;

-- =====================================================
-- SECTION 9: PERFORMANCE TEST - LEAVE MANAGEMENT
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 9: LEAVE MANAGEMENT PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 12: Leave balance lookup (should use IX_LeaveBalances_EmployeeId_Year_LeaveTypeId)
\echo 'Test 12: Employee Leave Balance Lookup'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    lb."EmployeeId",
    lb."Year",
    lb."LeaveTypeId",
    lb."TotalDays",
    lb."UsedDays",
    lb."RemainingDays"
FROM tenant_siraaj."LeaveBalances" lb
WHERE lb."EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND lb."Year" = 2025
    AND lb."IsDeleted" = false
ORDER BY lb."EmployeeId", lb."Year" DESC;

-- Test 13: Leave applications date range (should use IX_LeaveApplications_EmployeeId_StartDate_EndDate)
\echo 'Test 13: Leave Applications Date Range Query'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "EmployeeId",
    "StartDate",
    "EndDate",
    "Status",
    "TotalDays"
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
\echo 'SECTION 10: TIMESHEET QUERY PERFORMANCE'
\echo '====================================='
\echo ''

-- Test 14: Timesheet approval workflow (should use IX_Timesheets_Status_PeriodStart)
\echo 'Test 14: Timesheet Approval Workflow Query'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "Id",
    "EmployeeId",
    "Status",
    "PeriodStart",
    "PeriodEnd",
    "TotalHours"
FROM tenant_siraaj."Timesheets"
WHERE "Status" IN ('Pending', 'Submitted')
    AND "PeriodStart" >= '2025-11-01'::date
    AND "IsDeleted" = false
ORDER BY "Status", "PeriodStart" DESC
LIMIT 50;

-- Test 15: Employee timesheet history (should use IX_Timesheets_EmployeeId_Status_PeriodStart)
\echo 'Test 15: Employee Timesheet History'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    "EmployeeId",
    "Status",
    "PeriodStart",
    "PeriodEnd",
    "TotalHours",
    "ApprovedBy",
    "ApprovedAt"
FROM tenant_siraaj."Timesheets"
WHERE "EmployeeId" IN (
    SELECT "Id" FROM tenant_siraaj."Employees"
    WHERE "IsActive" = true AND "IsDeleted" = false
    LIMIT 5
)
    AND "Status" = 'Approved'
    AND "IsDeleted" = false
ORDER BY "EmployeeId", "PeriodStart" DESC;

-- Test 16: Timesheet entries by date (should use IX_TimesheetEntries_TimesheetId_Date)
\echo 'Test 16: Timesheet Entries by Date'
EXPLAIN (ANALYZE, BUFFERS, TIMING)
SELECT
    te."TimesheetId",
    te."Date",
    te."Hours",
    te."ProjectId",
    te."TaskId"
FROM tenant_siraaj."TimesheetEntries" te
INNER JOIN tenant_siraaj."Timesheets" t ON te."TimesheetId" = t."Id"
WHERE t."PeriodStart" >= '2025-11-01'::date
    AND te."IsDeleted" = false
ORDER BY te."TimesheetId", te."Date" DESC
LIMIT 100;

-- =====================================================
-- SECTION 11: COMPOSITE INDEX VERIFICATION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 11: COMPOSITE INDEX VERIFICATION'
\echo '====================================='
\echo ''

-- List all composite indexes created by the migration
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_indexes
WHERE schemaname LIKE 'tenant_%'
    AND indexname LIKE 'IX_%'
    AND (
        indexname LIKE '%Year_Month%' OR
        indexname LIKE '%Status_PaymentDate%' OR
        indexname LIKE '%EmployeeId_Year_LeaveTypeId%' OR
        indexname LIKE '%EmployeeId_Date_Status%' OR
        indexname LIKE '%DeviceId_Date%' OR
        indexname LIKE '%Status_PeriodStart%' OR
        indexname LIKE '%EmployeeId_Status_PeriodStart%' OR
        indexname LIKE '%FirstName_LastName_IsActive%' OR
        indexname LIKE '%TimesheetId_Date%' OR
        indexname LIKE '%EmployeeId_StartDate_EndDate%' OR
        indexname LIKE '%ProcessingStatus_PunchTime%' OR
        indexname LIKE '%EmployeeId_PunchTime%' OR
        indexname LIKE '%DeviceId_PunchTime%'
    )
ORDER BY schemaname, tablename, indexname;

-- =====================================================
-- SECTION 12: SLOW QUERY DETECTION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 12: SLOW QUERY DETECTION'
\echo '====================================='
\echo ''

-- Note: This requires pg_stat_statements extension
-- Check if extension is available
SELECT EXISTS (
    SELECT 1 FROM pg_extension WHERE extname = 'pg_stat_statements'
) as pg_stat_statements_available;

-- If available, show top slow queries (uncomment if extension is enabled)
/*
SELECT
    substring(query, 1, 100) as short_query,
    calls,
    total_exec_time::numeric(10,2) as total_time_ms,
    mean_exec_time::numeric(10,2) as avg_time_ms,
    max_exec_time::numeric(10,2) as max_time_ms
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 20;
*/

-- =====================================================
-- SECTION 13: AUTO-VACUUM CONFIGURATION
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 13: AUTO-VACUUM CONFIGURATION'
\echo '====================================='
\echo ''

-- Check auto-vacuum settings
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
    'autovacuum_analyze_scale_factor',
    'autovacuum_vacuum_cost_delay',
    'autovacuum_vacuum_cost_limit'
)
ORDER BY name;

-- =====================================================
-- SECTION 14: TABLE STATISTICS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 14: TABLE STATISTICS'
\echo '====================================='
\echo ''

-- Show largest tables in tenant schemas
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) as table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) -
                   pg_relation_size(schemaname||'.'||tablename)) as index_size,
    n_live_tup as row_count
FROM pg_stat_user_tables
WHERE schemaname LIKE 'tenant_%'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 20;

-- =====================================================
-- SECTION 15: CONNECTION AND PERFORMANCE SETTINGS
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 15: CONNECTION & PERFORMANCE SETTINGS'
\echo '====================================='
\echo ''

-- Check important performance settings
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
-- SECTION 16: PERFORMANCE SUMMARY
-- =====================================================

\echo ''
\echo '====================================='
\echo 'SECTION 16: PERFORMANCE SUMMARY'
\echo '====================================='
\echo ''

-- Generate performance grade
WITH metrics AS (
    SELECT
        (sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) * 100) as cache_hit_ratio,
        (SELECT COUNT(*) FROM pg_stat_user_indexes WHERE schemaname LIKE 'tenant_%' AND idx_scan = 0) as unused_index_count,
        (SELECT AVG(CASE WHEN n_live_tup = 0 THEN 0 ELSE (n_dead_tup::float / n_live_tup * 100) END)
         FROM pg_stat_user_tables WHERE schemaname LIKE 'tenant_%') as avg_bloat_percent
    FROM pg_statio_user_tables
    WHERE schemaname LIKE 'tenant_%'
)
SELECT
    'Cache Hit Ratio' as metric,
    cache_hit_ratio::numeric(5,2) || '%' as value,
    CASE
        WHEN cache_hit_ratio >= 99 THEN 'EXCELLENT'
        WHEN cache_hit_ratio >= 95 THEN 'GOOD'
        WHEN cache_hit_ratio >= 90 THEN 'FAIR'
        ELSE 'POOR'
    END as grade
FROM metrics
UNION ALL
SELECT
    'Unused Indexes' as metric,
    unused_index_count::text as value,
    CASE
        WHEN unused_index_count = 0 THEN 'EXCELLENT'
        WHEN unused_index_count <= 5 THEN 'GOOD'
        WHEN unused_index_count <= 10 THEN 'FAIR'
        ELSE 'POOR'
    END as grade
FROM metrics
UNION ALL
SELECT
    'Table Bloat' as metric,
    avg_bloat_percent::numeric(5,2) || '%' as value,
    CASE
        WHEN avg_bloat_percent < 5 THEN 'EXCELLENT'
        WHEN avg_bloat_percent < 10 THEN 'GOOD'
        WHEN avg_bloat_percent < 20 THEN 'FAIR'
        ELSE 'POOR'
    END as grade
FROM metrics;

\echo ''
\echo '====================================='
\echo 'PERFORMANCE TEST COMPLETED'
\echo '====================================='
\echo ''
