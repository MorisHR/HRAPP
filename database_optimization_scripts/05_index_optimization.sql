-- =====================================================
-- INDEX OPTIMIZATION
-- =====================================================
-- Purpose: Add missing indexes and identify unused indexes
-- Impact: Improved query performance and reduced bloat
-- =====================================================

-- =====================================================
-- PART 1: ADD MISSING HIGH-IMPACT INDEXES
-- =====================================================

-- Function to add optimized indexes to a tenant schema
CREATE OR REPLACE FUNCTION master.add_performance_indexes(
    p_tenant_schema TEXT
)
RETURNS TEXT AS $$
DECLARE
    v_result TEXT := '';
BEGIN
    -- Index 1: Attendance date range queries (most common query pattern)
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_EmployeeId_Date_Perf"
                ON %I."Attendances" ("EmployeeId", "AttendanceDate" DESC)
                INCLUDE ("CheckInTime", "CheckOutTime", "Status", "WorkDuration")',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_Attendances_EmployeeId_Date_Perf' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Attendances_EmployeeId_Date_Perf: ' || SQLERRM || E'\n';
    END;

    -- Index 2: Payroll calculation queries (status filtering)
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Attendances_PayrollPeriod"
                ON %I."Attendances" ("EmployeeId", "AttendanceDate")
                WHERE "Status" IN (''Present'', ''Late'', ''HalfDay'')',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_Attendances_PayrollPeriod' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Attendances_PayrollPeriod: ' || SQLERRM || E'\n';
    END;

    -- Index 3: Device sync queries
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DeviceSyncLogs_DeviceId_SyncTime"
                ON %I."DeviceSyncLogs" ("DeviceId", "SyncTime" DESC)
                INCLUDE ("RecordsSynced", "Status")',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_DeviceSyncLogs_DeviceId_SyncTime' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_DeviceSyncLogs_DeviceId_SyncTime: ' || SQLERRM || E'\n';
    END;

    -- Index 4: Leave application approval workflow
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_LeaveApplications_Status_FromDate"
                ON %I."LeaveApplications" ("Status", "FromDate" DESC)
                WHERE "Status" IN (''Pending'', ''Approved'')',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_LeaveApplications_Status_FromDate' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_LeaveApplications_Status_FromDate: ' || SQLERRM || E'\n';
    END;

    -- Index 5: Employee lookup by department (covering index)
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Employees_DeptId_Active"
                ON %I."Employees" ("DepartmentId", "IsActive")
                INCLUDE ("EmployeeCode", "FirstName", "LastName", "Designation")
                WHERE "IsDeleted" = false',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_Employees_DeptId_Active' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Employees_DeptId_Active: ' || SQLERRM || E'\n';
    END;

    -- Index 6: Timesheet approval queries
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Timesheets_Status_WeekStart"
                ON %I."Timesheets" ("Status", "WeekStartDate" DESC)
                INCLUDE ("EmployeeId", "TotalHours")',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_Timesheets_Status_WeekStart' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Timesheets_Status_WeekStart: ' || SQLERRM || E'\n';
    END;

    -- Index 7: Payslip queries by period
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Payslips_EmployeeId_Period"
                ON %I."Payslips" ("EmployeeId", "PayrollCycleId")
                INCLUDE ("GrossSalary", "NetSalary", "Status")',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_Payslips_EmployeeId_Period' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Payslips_EmployeeId_Period: ' || SQLERRM || E'\n';
    END;

    -- Index 8: Attendance anomaly detection
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AttendanceAnomalies_Detected_Resolved"
                ON %I."AttendanceAnomalies" ("DetectedAt" DESC)
                WHERE "IsResolved" = false',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_AttendanceAnomalies_Detected_Resolved' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_AttendanceAnomalies_Detected_Resolved: ' || SQLERRM || E'\n';
    END;

    -- Index 9: Device API key validation (hot path)
    BEGIN
        EXECUTE format('
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DeviceApiKeys_Active_NotExpired"
                ON %I."DeviceApiKeys" ("ApiKey", "IsActive")
                WHERE "IsActive" = true AND "ExpiresAt" > NOW()',
            p_tenant_schema
        );
        v_result := v_result || 'Created IX_DeviceApiKeys_Active_NotExpired' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_DeviceApiKeys_Active_NotExpired: ' || SQLERRM || E'\n';
    END;

    RETURN v_result;
END;
$$ LANGUAGE plpgsql;

-- Add indexes for master schema
CREATE OR REPLACE FUNCTION master.add_master_performance_indexes()
RETURNS TEXT AS $$
DECLARE
    v_result TEXT := '';
BEGIN
    -- Index 1: RefreshToken cleanup queries
    BEGIN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_RefreshTokens_ExpiresAt_Active"
            ON master."RefreshTokens" ("ExpiresAt")
            WHERE "IsRevoked" = false;
        v_result := v_result || 'Created IX_RefreshTokens_ExpiresAt_Active' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_RefreshTokens_ExpiresAt_Active: ' || SQLERRM || E'\n';
    END;

    -- Index 2: Tenant lookup by status
    BEGIN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Tenants_IsActive_SubscriptionTier"
            ON master."Tenants" ("IsActive", "SubscriptionTier")
            INCLUDE ("CompanyName", "DatabaseSchemaName", "SubscriptionExpiresAt");
        v_result := v_result || 'Created IX_Tenants_IsActive_SubscriptionTier' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_Tenants_IsActive_SubscriptionTier: ' || SQLERRM || E'\n';
    END;

    -- Index 3: Security alert queries
    BEGIN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SecurityAlerts_Severity_CreatedAt"
            ON master."SecurityAlerts" ("Severity", "CreatedAt" DESC)
            WHERE "IsResolved" = false;
        v_result := v_result || 'Created IX_SecurityAlerts_Severity_CreatedAt' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_SecurityAlerts_Severity_CreatedAt: ' || SQLERRM || E'\n';
    END;

    -- Index 4: Detected anomalies - unresolved high-risk
    BEGIN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DetectedAnomalies_Unresolved_HighRisk"
            ON master."DetectedAnomalies" ("TenantId", "RiskLevel", "DetectedAt" DESC)
            WHERE "Status" IN ('New', 'UnderInvestigation');
        v_result := v_result || 'Created IX_DetectedAnomalies_Unresolved_HighRisk' || E'\n';
    EXCEPTION WHEN OTHERS THEN
        v_result := v_result || 'SKIP IX_DetectedAnomalies_Unresolved_HighRisk: ' || SQLERRM || E'\n';
    END;

    RETURN v_result;
END;
$$ LANGUAGE plpgsql;

-- Apply to all tenants
CREATE OR REPLACE FUNCTION master.add_all_performance_indexes()
RETURNS TABLE(schema_name TEXT, result TEXT) AS $$
DECLARE
    v_schema RECORD;
BEGIN
    -- Tenant schemas
    FOR v_schema IN
        SELECT nspname
        FROM pg_namespace
        WHERE nspname LIKE 'tenant_%'
        AND nspname != 'tenant_testcorp'
    LOOP
        schema_name := v_schema.nspname;
        result := master.add_performance_indexes(v_schema.nspname);
        RETURN NEXT;
    END LOOP;

    -- Master schema
    schema_name := 'master';
    result := master.add_master_performance_indexes();
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- PART 2: IDENTIFY UNUSED INDEXES
-- =====================================================

CREATE OR REPLACE VIEW master."UnusedIndexes" AS
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan AS index_scans,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    pg_relation_size(indexrelid) AS index_size_bytes,
    CASE
        WHEN idx_scan = 0 THEN 'NEVER USED'
        WHEN idx_scan < 100 THEN 'RARELY USED'
        ELSE 'OK'
    END AS usage_status,
    'DROP INDEX ' || schemaname || '.' || indexname || ';' AS drop_statement
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp')
AND idx_scan < 100 -- Adjust threshold as needed
AND indexname NOT LIKE 'PK_%' -- Don't drop primary keys
AND indexname NOT LIKE '%_pkey' -- Don't drop primary keys
ORDER BY pg_relation_size(indexrelid) DESC, idx_scan ASC;

-- Query to find duplicate indexes
CREATE OR REPLACE VIEW master."DuplicateIndexes" AS
SELECT
    schemaname,
    tablename,
    array_agg(indexname) AS duplicate_indexes,
    pg_get_indexdef(indexrelid) AS index_definition,
    COUNT(*) AS duplicate_count,
    pg_size_pretty(SUM(pg_relation_size(indexrelid))) AS total_wasted_size
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp')
GROUP BY schemaname, tablename, pg_get_indexdef(indexrelid)
HAVING COUNT(*) > 1
ORDER BY SUM(pg_relation_size(indexrelid)) DESC;

-- =====================================================
-- PART 3: INDEX BLOAT DETECTION
-- =====================================================

CREATE OR REPLACE VIEW master."IndexBloat" AS
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    CASE
        WHEN pg_relation_size(indexrelid) = 0 THEN 0
        ELSE ROUND(100.0 * pg_relation_size(indexrelid) /
                   NULLIF(pg_total_relation_size(schemaname||'.'||tablename), 0), 2)
    END AS index_size_percent,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    'REINDEX INDEX CONCURRENTLY ' || schemaname || '.' || indexname || ';' AS reindex_statement
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp')
AND pg_relation_size(indexrelid) > 1048576 -- > 1 MB
ORDER BY pg_relation_size(indexrelid) DESC;

-- =====================================================
-- PART 4: INDEX USAGE STATISTICS
-- =====================================================

CREATE OR REPLACE VIEW master."IndexUsageStats" AS
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    CASE
        WHEN idx_tup_read = 0 THEN 0
        ELSE ROUND(100.0 * idx_tup_fetch / idx_tup_read, 2)
    END AS fetch_ratio_percent,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    CASE
        WHEN idx_scan = 0 THEN 'Never used'
        WHEN idx_scan BETWEEN 1 AND 10 THEN 'Very rarely'
        WHEN idx_scan BETWEEN 11 AND 100 THEN 'Rarely'
        WHEN idx_scan BETWEEN 101 AND 1000 THEN 'Occasionally'
        WHEN idx_scan BETWEEN 1001 AND 10000 THEN 'Frequently'
        ELSE 'Very frequently'
    END AS usage_category
FROM pg_stat_user_indexes
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp')
ORDER BY idx_scan DESC;

-- =====================================================
-- PART 5: MISSING INDEX DETECTION (Based on Sequential Scans)
-- =====================================================

CREATE OR REPLACE VIEW master."PotentialMissingIndexes" AS
SELECT
    schemaname,
    tablename,
    seq_scan AS sequential_scans,
    seq_tup_read AS tuples_read,
    idx_scan AS index_scans,
    n_live_tup AS live_tuples,
    ROUND(100.0 * seq_scan / NULLIF(seq_scan + idx_scan, 0), 2) AS seq_scan_percent,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS table_size,
    CASE
        WHEN seq_scan > 1000 AND seq_scan > idx_scan THEN 'HIGH PRIORITY'
        WHEN seq_scan > 100 AND seq_scan > idx_scan THEN 'MEDIUM PRIORITY'
        WHEN seq_scan > 10 THEN 'LOW PRIORITY'
        ELSE 'OK'
    END AS priority
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp')
AND seq_scan > 0
ORDER BY seq_scan DESC;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- 1. Add all performance indexes
SELECT * FROM master.add_all_performance_indexes();

-- 2. Find unused indexes
SELECT * FROM master."UnusedIndexes"
WHERE usage_status = 'NEVER USED'
AND index_size_bytes > 1048576; -- > 1 MB

-- 3. Find duplicate indexes
SELECT * FROM master."DuplicateIndexes";

-- 4. Check index bloat
SELECT * FROM master."IndexBloat"
WHERE index_size_percent > 50;

-- 5. Reindex bloated indexes
-- REINDEX INDEX CONCURRENTLY master."IX_AuditLogs_PerformedAt";

-- 6. Check index usage statistics
SELECT * FROM master."IndexUsageStats"
WHERE usage_category IN ('Never used', 'Very rarely', 'Rarely')
ORDER BY index_size DESC;

-- 7. Find tables with many sequential scans
SELECT * FROM master."PotentialMissingIndexes"
WHERE priority IN ('HIGH PRIORITY', 'MEDIUM PRIORITY');

-- 8. Drop unused index (after verification!)
-- DROP INDEX CONCURRENTLY master."UnusedIndexName";
*/

-- =====================================================
-- MAINTENANCE PROCEDURE
-- =====================================================

CREATE OR REPLACE PROCEDURE master.analyze_index_health()
LANGUAGE plpgsql
AS $$
DECLARE
    v_unused_count INTEGER;
    v_duplicate_count INTEGER;
    v_bloated_count INTEGER;
BEGIN
    RAISE NOTICE 'Starting index health analysis...';

    -- Count unused indexes
    SELECT COUNT(*) INTO v_unused_count
    FROM master."UnusedIndexes"
    WHERE usage_status = 'NEVER USED';

    RAISE NOTICE 'Found % unused indexes', v_unused_count;

    -- Count duplicate indexes
    SELECT COUNT(*) INTO v_duplicate_count
    FROM master."DuplicateIndexes";

    RAISE NOTICE 'Found % duplicate index groups', v_duplicate_count;

    -- Count bloated indexes
    SELECT COUNT(*) INTO v_bloated_count
    FROM master."IndexBloat"
    WHERE index_size_percent > 50;

    RAISE NOTICE 'Found % potentially bloated indexes', v_bloated_count;

    -- Summary
    RAISE NOTICE 'Index health analysis completed';
    RAISE NOTICE 'Recommendations:';
    IF v_unused_count > 0 THEN
        RAISE NOTICE '  - Review and drop % unused indexes', v_unused_count;
    END IF;
    IF v_duplicate_count > 0 THEN
        RAISE NOTICE '  - Review and remove % duplicate indexes', v_duplicate_count;
    END IF;
    IF v_bloated_count > 0 THEN
        RAISE NOTICE '  - Reindex % bloated indexes', v_bloated_count;
    END IF;
END;
$$;

/*
-- Run index health analysis
CALL master.analyze_index_health();
*/
