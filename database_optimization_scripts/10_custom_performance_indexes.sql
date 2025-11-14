-- =====================================================
-- CUSTOM PERFORMANCE INDEXES
-- =====================================================
-- Purpose: Additional strategic indexes based on common query patterns
-- Date: 2025-11-14
-- Impact: Target specific slow queries and hot paths
-- =====================================================

-- =====================================================
-- ENABLE QUERY PERFORMANCE TRACKING (pg_stat_statements)
-- =====================================================

-- Enable pg_stat_statements extension for query tracking
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- View for top slow queries
CREATE OR REPLACE VIEW master."TopSlowQueries" AS
SELECT
    ROUND(total_exec_time::numeric, 2) AS total_time_ms,
    calls,
    ROUND(mean_exec_time::numeric, 2) AS avg_time_ms,
    ROUND((100 * total_exec_time / SUM(total_exec_time) OVER())::numeric, 2) AS percent_total,
    LEFT(query, 150) AS query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
AND query NOT LIKE '%pg_catalog%'
ORDER BY total_exec_time DESC
LIMIT 20;

-- =====================================================
-- CUSTOM INDEXES FOR COMMON QUERY PATTERNS
-- =====================================================

-- Index 1: Employee lookup by code (very common)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Employees_EmployeeCode_Active"
    ON tenant_siraaj."Employees" ("EmployeeCode")
    WHERE "IsDeleted" = false AND "IsActive" = true;

-- Index 2: Attendance range queries with employee filter
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_Date_EmployeeId"
    ON tenant_siraaj."Attendances" ("Date" DESC, "EmployeeId")
    WHERE "IsDeleted" = false;

-- Index 3: Leave applications by date range
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_LeaveApplications_Dates"
    ON tenant_siraaj."LeaveApplications" ("StartDate", "EndDate")
    WHERE "IsDeleted" = false;

-- Index 4: Active employees by department (common dashboard query)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Employees_Dept_Active_Covering"
    ON tenant_siraaj."Employees" ("DepartmentId")
    INCLUDE ("Id", "EmployeeCode", "FirstName", "LastName")
    WHERE "IsDeleted" = false AND "IsActive" = true;

-- Index 5: Department lookups (avoid sequential scans)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Departments_Name"
    ON tenant_siraaj."Departments" ("Name")
    WHERE "IsDeleted" = false;

-- Index 6: Audit log user activity tracking
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AuditLogs_UserId_Date"
    ON master."AuditLogs" ("UserId", "PerformedAt" DESC)
    WHERE "UserId" IS NOT NULL;

-- Index 7: Audit log by action type and tenant
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AuditLogs_Tenant_Action"
    ON master."AuditLogs" ("TenantId", "ActionType", "PerformedAt" DESC)
    WHERE "TenantId" IS NOT NULL;

-- Index 8: Biometric device lookups
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AttendanceMachines_Active"
    ON tenant_siraaj."AttendanceMachines" ("Id")
    WHERE "IsDeleted" = false;

-- =====================================================
-- TENANT_DEFAULT SCHEMA INDEXES
-- =====================================================

-- Apply same patterns to tenant_default
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Employees_EmployeeCode_Active"
    ON tenant_default."Employees" ("EmployeeCode")
    WHERE "IsDeleted" = false AND "IsActive" = true;

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_Date_EmployeeId"
    ON tenant_default."Attendances" ("Date" DESC, "EmployeeId")
    WHERE "IsDeleted" = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Departments_Name"
    ON tenant_default."Departments" ("Name")
    WHERE "IsDeleted" = false;

-- =====================================================
-- JUNCTION TABLE INDEXES (M2M Relationships)
-- =====================================================

-- If EmployeeDeviceAccesses is used for M2M employee-device mapping
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_EmployeeDeviceAccesses_EmployeeId"
    ON tenant_siraaj."EmployeeDeviceAccesses" ("EmployeeId")
    WHERE "IsDeleted" = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_EmployeeDeviceAccesses_DeviceId"
    ON tenant_siraaj."EmployeeDeviceAccesses" ("DeviceId")
    WHERE "IsDeleted" = false;

-- =====================================================
-- COMPOSITE INDEXES FOR COMPLEX QUERIES
-- =====================================================

-- Index 9: Payroll calculation (attendance + employee + date range)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_Payroll_Composite"
    ON tenant_siraaj."Attendances" ("EmployeeId", "Date", "Status")
    INCLUDE ("WorkingHours", "OvertimeHours")
    WHERE "IsDeleted" = false;

-- Index 10: Leave approval workflow
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_LeaveApplications_Approval"
    ON tenant_siraaj."LeaveApplications" ("ApprovedBy", "Status", "ApprovedDate")
    WHERE "IsDeleted" = false;

-- =====================================================
-- ANALYTICS & REPORTING INDEXES
-- =====================================================

-- Index 11: Monthly attendance aggregation
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_Monthly_Agg"
    ON tenant_siraaj."Attendances" (DATE_TRUNC('month', "Date"), "EmployeeId")
    WHERE "IsDeleted" = false;

-- Index 12: Department-wise employee count
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Employees_Dept_Count"
    ON tenant_siraaj."Employees" ("DepartmentId", "IsActive")
    WHERE "IsDeleted" = false;

-- =====================================================
-- VERIFICATION & MONITORING
-- =====================================================

-- Function to check index usage after deployment
CREATE OR REPLACE FUNCTION master.check_new_index_usage()
RETURNS TABLE(
    schema_name TEXT,
    table_name TEXT,
    index_name TEXT,
    scans BIGINT,
    tuples_read BIGINT,
    tuples_fetched BIGINT,
    size TEXT,
    usage_status TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        schemaname::TEXT,
        tablename::TEXT,
        indexname::TEXT,
        idx_scan,
        idx_tup_read,
        idx_tup_fetch,
        pg_size_pretty(pg_relation_size(indexrelid))::TEXT,
        CASE
            WHEN idx_scan = 0 THEN 'NOT YET USED'
            WHEN idx_scan < 10 THEN 'LOW USAGE'
            WHEN idx_scan < 100 THEN 'MODERATE USAGE'
            ELSE 'HIGH USAGE'
        END::TEXT
    FROM pg_stat_user_indexes
    WHERE schemaname IN ('master', 'tenant_siraaj', 'tenant_default')
    AND (indexname LIKE '%EmployeeCode%'
         OR indexname LIKE '%Date_EmployeeId%'
         OR indexname LIKE '%Payroll%'
         OR indexname LIKE '%Approval%'
         OR indexname LIKE '%Monthly%')
    ORDER BY idx_scan DESC;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- INDEX RECOMMENDATIONS ENGINE
-- =====================================================

CREATE OR REPLACE FUNCTION master.suggest_missing_indexes()
RETURNS TABLE(
    schema_name TEXT,
    table_name TEXT,
    column_suggestion TEXT,
    reason TEXT,
    priority TEXT
) AS $$
BEGIN
    -- Tables with high sequential scans and no appropriate index
    RETURN QUERY
    SELECT
        schemaname::TEXT,
        tablename::TEXT,
        'Consider index on frequently filtered columns'::TEXT,
        'High sequential scan count: ' || seq_scan || ', Table size: ' ||
        pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename))::TEXT,
        CASE
            WHEN seq_scan > 1000 THEN 'HIGH'
            WHEN seq_scan > 100 THEN 'MEDIUM'
            ELSE 'LOW'
        END::TEXT
    FROM pg_stat_user_tables
    WHERE schemaname IN ('master', 'tenant_siraaj')
    AND seq_scan > 100
    AND pg_total_relation_size(schemaname||'.'||tablename) > 1048576 -- > 1MB
    ORDER BY seq_scan DESC;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- SUMMARY OF DEPLOYED INDEXES
-- =====================================================

CREATE OR REPLACE FUNCTION master.get_custom_index_summary()
RETURNS TABLE(
    info_type TEXT,
    count BIGINT,
    total_size TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        'Total Custom Indexes'::TEXT,
        COUNT(*)::BIGINT,
        pg_size_pretty(SUM(pg_relation_size(indexrelid)))::TEXT
    FROM pg_stat_user_indexes
    WHERE schemaname IN ('tenant_siraaj', 'tenant_default', 'master')
    AND (indexname LIKE '%Custom%'
         OR indexname LIKE '%EmployeeCode%'
         OR indexname LIKE '%Date_EmployeeId%'
         OR indexname LIKE '%Payroll%'
         OR indexname LIKE '%Approval%')

    UNION ALL

    SELECT
        'All Performance Indexes'::TEXT,
        COUNT(*)::BIGINT,
        pg_size_pretty(SUM(pg_relation_size(indexrelid)))::TEXT
    FROM pg_stat_user_indexes
    WHERE schemaname IN ('tenant_siraaj', 'tenant_default', 'master');
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- 1. Check top slow queries (after pg_stat_statements is collecting data)
SELECT * FROM master."TopSlowQueries";

-- 2. Check new index usage (wait 24-48h for meaningful data)
SELECT * FROM master.check_new_index_usage();

-- 3. Get missing index suggestions
SELECT * FROM master.suggest_missing_indexes();

-- 4. Get custom index summary
SELECT * FROM master.get_custom_index_summary();

-- 5. Reset pg_stat_statements (to start fresh tracking)
SELECT pg_stat_statements_reset();

-- 6. Find queries not using indexes
SELECT * FROM master."TopSlowQueries"
WHERE query_preview LIKE '%Seq Scan%';
*/

-- =====================================================
-- COMPLETION MESSAGE
-- =====================================================

SELECT
    'Custom performance indexes deployed successfully!' AS status,
    COUNT(*) AS indexes_created
FROM pg_indexes
WHERE schemaname IN ('tenant_siraaj', 'tenant_default', 'master')
AND indexname LIKE '%Date%' OR indexname LIKE '%EmployeeCode%';
