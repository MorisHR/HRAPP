-- =====================================================
-- ATTENDANCES TABLE PARTITIONING (TENANT SCHEMAS)
-- =====================================================
-- Purpose: Partition Attendances by quarter for better performance
-- Impact: Improves payroll calculations and attendance queries
-- Estimated Performance Gain: 50-70% on date-range queries
-- =====================================================

-- This script creates a template for partitioning Attendances tables
-- It should be applied to each tenant schema individually

-- Function to partition Attendances for a specific tenant
CREATE OR REPLACE FUNCTION master.partition_tenant_attendances(
    p_tenant_schema TEXT
)
RETURNS TEXT AS $$
DECLARE
    v_result TEXT := '';
    v_sql TEXT;
BEGIN
    -- Step 1: Create partitioned table
    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_Partitioned" (
            LIKE %I."Attendances" INCLUDING ALL
        ) PARTITION BY RANGE ("AttendanceDate")',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;
    v_result := v_result || 'Created partitioned table' || E'\n';

    -- Step 2: Create partitions for 2025 (4 quarters)
    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2025_Q1" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2025-01-01'') TO (''2025-04-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2025_Q2" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2025-04-01'') TO (''2025-07-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2025_Q3" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2025-07-01'') TO (''2025-10-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2025_Q4" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2025-10-01'') TO (''2026-01-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    -- Step 3: Create partitions for 2024 (historical data)
    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2024_and_earlier" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2000-01-01'') TO (''2025-01-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    -- Step 4: Create future partitions (2026)
    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2026_Q1" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2026-01-01'') TO (''2026-04-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE TABLE IF NOT EXISTS %I."Attendances_2026_Q2" PARTITION OF %I."Attendances_Partitioned"
            FOR VALUES FROM (''2026-04-01'') TO (''2026-07-01'')',
        p_tenant_schema, p_tenant_schema
    );
    EXECUTE v_sql;

    v_result := v_result || 'Created partitions for 2024-2026' || E'\n';

    -- Step 5: Create indexes on partitioned table
    v_sql := format('
        CREATE INDEX IF NOT EXISTS "IX_Attendances_Part_TenantId_EmployeeId_Date"
            ON %I."Attendances_Partitioned" ("TenantId", "EmployeeId", "AttendanceDate" DESC)',
        p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE INDEX IF NOT EXISTS "IX_Attendances_Part_EmployeeId_Date"
            ON %I."Attendances_Partitioned" ("EmployeeId", "AttendanceDate" DESC)
            INCLUDE ("CheckInTime", "CheckOutTime", "Status")',
        p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE INDEX IF NOT EXISTS "IX_Attendances_Part_AttendanceDate"
            ON %I."Attendances_Partitioned" ("AttendanceDate" DESC)',
        p_tenant_schema
    );
    EXECUTE v_sql;

    v_sql := format('
        CREATE INDEX IF NOT EXISTS "IX_Attendances_Part_Status_Date"
            ON %I."Attendances_Partitioned" ("Status", "AttendanceDate")
            WHERE "Status" IN (''Present'', ''Late'', ''HalfDay'')',
        p_tenant_schema
    );
    EXECUTE v_sql;

    v_result := v_result || 'Created indexes' || E'\n';

    RETURN v_result || 'Partitioning completed for ' || p_tenant_schema;
END;
$$ LANGUAGE plpgsql;

-- Function to create future quarter partitions for Attendances
CREATE OR REPLACE FUNCTION master.create_attendances_next_quarter(
    p_tenant_schema TEXT
)
RETURNS TEXT AS $$
DECLARE
    v_current_quarter_start DATE;
    v_next_quarter_start DATE;
    v_next_quarter_end DATE;
    v_partition_name TEXT;
    v_year INTEGER;
    v_quarter INTEGER;
BEGIN
    -- Calculate next quarter
    v_current_quarter_start := DATE_TRUNC('quarter', CURRENT_DATE);
    v_next_quarter_start := v_current_quarter_start + INTERVAL '6 months'; -- 2 quarters ahead
    v_next_quarter_end := v_next_quarter_start + INTERVAL '3 months';

    v_year := EXTRACT(YEAR FROM v_next_quarter_start);
    v_quarter := EXTRACT(QUARTER FROM v_next_quarter_start);

    v_partition_name := format('Attendances_%s_Q%s', v_year, v_quarter);

    -- Check if partition exists
    IF EXISTS (
        SELECT 1 FROM pg_tables
        WHERE schemaname = p_tenant_schema
        AND tablename = v_partition_name
    ) THEN
        RETURN 'Partition ' || v_partition_name || ' already exists';
    END IF;

    -- Create partition
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I.%I PARTITION OF %I."Attendances_Partitioned"
                    FOR VALUES FROM (%L) TO (%L)',
                   p_tenant_schema,
                   v_partition_name,
                   p_tenant_schema,
                   v_next_quarter_start,
                   v_next_quarter_end);

    RETURN format('Created partition: %s for %s [%s to %s)',
                  v_partition_name, p_tenant_schema,
                  v_next_quarter_start, v_next_quarter_end);
END;
$$ LANGUAGE plpgsql;

-- Function to apply partitioning to all tenant schemas
CREATE OR REPLACE FUNCTION master.partition_all_tenant_attendances()
RETURNS TABLE(tenant_schema TEXT, result TEXT) AS $$
DECLARE
    v_schema RECORD;
BEGIN
    FOR v_schema IN
        SELECT nspname
        FROM pg_namespace
        WHERE nspname LIKE 'tenant_%'
        AND nspname != 'tenant_testcorp' -- Skip empty test tenant
    LOOP
        tenant_schema := v_schema.nspname;
        BEGIN
            result := master.partition_tenant_attendances(v_schema.nspname);
            RETURN NEXT;
        EXCEPTION WHEN OTHERS THEN
            result := 'ERROR: ' || SQLERRM;
            RETURN NEXT;
        END;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- MIGRATION PROCEDURE (Manual execution during maintenance)
-- =====================================================

CREATE OR REPLACE PROCEDURE master.migrate_attendances_to_partitioned(
    p_tenant_schema TEXT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_row_count BIGINT;
BEGIN
    RAISE NOTICE 'Starting migration for schema: %', p_tenant_schema;

    -- Get row count
    EXECUTE format('SELECT COUNT(*) FROM %I."Attendances"', p_tenant_schema)
        INTO v_row_count;
    RAISE NOTICE 'Total rows to migrate: %', v_row_count;

    -- Rename original table
    EXECUTE format('ALTER TABLE %I."Attendances" RENAME TO "Attendances_Old"', p_tenant_schema);
    RAISE NOTICE 'Renamed original table to Attendances_Old';

    -- Rename partitioned table
    EXECUTE format('ALTER TABLE %I."Attendances_Partitioned" RENAME TO "Attendances"', p_tenant_schema);
    RAISE NOTICE 'Renamed partitioned table to Attendances';

    -- Copy data
    EXECUTE format('INSERT INTO %I."Attendances" SELECT * FROM %I."Attendances_Old" ORDER BY "AttendanceDate"',
                   p_tenant_schema, p_tenant_schema);
    RAISE NOTICE 'Data migration completed';

    -- Verify
    EXECUTE format('SELECT COUNT(*) FROM %I."Attendances"', p_tenant_schema)
        INTO v_row_count;
    RAISE NOTICE 'Rows in new table: %', v_row_count;

    RAISE NOTICE 'Migration completed. Review and drop Attendances_Old if everything is correct.';
END;
$$;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- Step 1: Create partitioned structure for a single tenant
SELECT master.partition_tenant_attendances('tenant_siraaj');

-- Step 2: Create partitioned structure for all tenants
SELECT * FROM master.partition_all_tenant_attendances();

-- Step 3: Migrate data (during maintenance window)
CALL master.migrate_attendances_to_partitioned('tenant_siraaj');

-- Step 4: Verify partitions
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'tenant_siraaj'
AND tablename LIKE 'Attendances%'
ORDER BY tablename;

-- Step 5: Create future quarters
SELECT master.create_attendances_next_quarter('tenant_siraaj');

-- Step 6: Query with partition pruning
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM tenant_siraaj."Attendances"
WHERE "AttendanceDate" >= '2025-10-01'
  AND "AttendanceDate" < '2026-01-01';
-- Should show "Partitions pruned: X"

-- Step 7: Drop old table after verification
-- DROP TABLE tenant_siraaj."Attendances_Old";
*/

-- =====================================================
-- QUARTERLY MAINTENANCE
-- =====================================================

CREATE OR REPLACE PROCEDURE master.quarterly_attendances_maintenance()
LANGUAGE plpgsql
AS $$
DECLARE
    v_schema RECORD;
    v_result TEXT;
BEGIN
    RAISE NOTICE 'Starting quarterly Attendances partition maintenance...';

    FOR v_schema IN
        SELECT nspname
        FROM pg_namespace
        WHERE nspname LIKE 'tenant_%'
        AND nspname != 'tenant_testcorp'
    LOOP
        v_result := master.create_attendances_next_quarter(v_schema.nspname);
        RAISE NOTICE '%', v_result;
    END LOOP;

    RAISE NOTICE 'Quarterly Attendances partition maintenance completed';
END;
$$;

/*
-- Schedule quarterly maintenance
SELECT cron.schedule(
    'attendances_partition_maintenance',
    '0 2 1 1,4,7,10 *', -- 1st of Jan, Apr, Jul, Oct at 2 AM
    $$CALL master.quarterly_attendances_maintenance()$$
);
*/
