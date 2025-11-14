-- =====================================================
-- AUTOMATED PARTITION MANAGEMENT
-- =====================================================
-- Purpose: Automatically create and manage table partitions
-- Impact: Prevents production outages due to missing partitions
-- Maintenance: Run monthly via cron or Hangfire job
-- =====================================================

-- Function 1: Create next month's partition for AuditLogs
CREATE OR REPLACE FUNCTION master.create_auditlogs_next_partition()
RETURNS TEXT AS $$
DECLARE
    v_partition_date DATE;
    v_partition_name TEXT;
    v_start_date TEXT;
    v_end_date TEXT;
    v_result TEXT;
BEGIN
    -- Calculate next month
    v_partition_date := DATE_TRUNC('month', CURRENT_DATE + INTERVAL '2 months');
    v_partition_name := 'AuditLogs_' || TO_CHAR(v_partition_date, 'YYYY_MM');
    v_start_date := v_partition_date::TEXT;
    v_end_date := (v_partition_date + INTERVAL '1 month')::TEXT;

    -- Check if partition already exists
    IF EXISTS (
        SELECT 1 FROM pg_tables
        WHERE schemaname = 'master'
        AND tablename = v_partition_name
    ) THEN
        RETURN 'Partition ' || v_partition_name || ' already exists';
    END IF;

    -- Create partition
    EXECUTE format('CREATE TABLE IF NOT EXISTS master.%I PARTITION OF master."AuditLogs"
                    FOR VALUES FROM (%L) TO (%L)',
                   v_partition_name,
                   v_start_date,
                   v_end_date);

    v_result := 'Created partition: ' || v_partition_name ||
                ' for range [' || v_start_date || ' to ' || v_end_date || ')';

    -- Log the partition creation
    RAISE NOTICE '%', v_result;

    RETURN v_result;
END;
$$ LANGUAGE plpgsql;

-- Function 2: Create next 3 months of partitions (preventive)
CREATE OR REPLACE FUNCTION master.create_auditlogs_future_partitions()
RETURNS TABLE(partition_name TEXT, date_range TEXT) AS $$
DECLARE
    v_month INTEGER;
    v_partition_date DATE;
    v_partition_name TEXT;
    v_start_date TEXT;
    v_end_date TEXT;
BEGIN
    FOR v_month IN 1..3 LOOP
        v_partition_date := DATE_TRUNC('month', CURRENT_DATE + (v_month || ' months')::INTERVAL);
        v_partition_name := 'AuditLogs_' || TO_CHAR(v_partition_date, 'YYYY_MM');
        v_start_date := v_partition_date::TEXT;
        v_end_date := (v_partition_date + INTERVAL '1 month')::TEXT;

        -- Skip if partition exists
        IF NOT EXISTS (
            SELECT 1 FROM pg_tables
            WHERE schemaname = 'master'
            AND tablename = v_partition_name
        ) THEN
            EXECUTE format('CREATE TABLE IF NOT EXISTS master.%I PARTITION OF master."AuditLogs"
                            FOR VALUES FROM (%L) TO (%L)',
                           v_partition_name,
                           v_start_date,
                           v_end_date);

            partition_name := v_partition_name;
            date_range := '[' || v_start_date || ' to ' || v_end_date || ')';
            RETURN NEXT;
        END IF;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- Function 3: Archive old AuditLogs partitions
CREATE OR REPLACE FUNCTION master.archive_old_auditlogs_partitions(
    p_months_to_keep INTEGER DEFAULT 12
)
RETURNS TABLE(
    partition_name TEXT,
    action_taken TEXT,
    row_count BIGINT,
    size_freed TEXT
) AS $$
DECLARE
    v_cutoff_date DATE;
    v_partition RECORD;
    v_row_count BIGINT;
    v_size_bytes BIGINT;
BEGIN
    v_cutoff_date := DATE_TRUNC('month', CURRENT_DATE - (p_months_to_keep || ' months')::INTERVAL);

    FOR v_partition IN
        SELECT
            schemaname,
            tablename,
            pg_total_relation_size(schemaname||'.'||tablename) AS table_size
        FROM pg_tables
        WHERE schemaname = 'master'
        AND tablename LIKE 'AuditLogs_2%'
        AND tablename NOT LIKE '%_and_earlier'
        ORDER BY tablename
    LOOP
        -- Extract date from partition name (AuditLogs_2025_11)
        DECLARE
            v_partition_date DATE;
            v_date_parts TEXT[];
        BEGIN
            v_date_parts := regexp_matches(v_partition.tablename, 'AuditLogs_(\d{4})_(\d{2})');
            IF v_date_parts IS NOT NULL THEN
                v_partition_date := (v_date_parts[1] || '-' || v_date_parts[2] || '-01')::DATE;

                IF v_partition_date < v_cutoff_date THEN
                    -- Get row count before detaching
                    EXECUTE format('SELECT COUNT(*) FROM master.%I', v_partition.tablename)
                        INTO v_row_count;

                    v_size_bytes := v_partition.table_size;

                    -- Mark as archived (update IsArchived flag)
                    EXECUTE format('UPDATE master.%I SET "IsArchived" = true, "ArchivedAt" = NOW()
                                    WHERE "IsArchived" = false', v_partition.tablename);

                    partition_name := v_partition.tablename;
                    action_taken := 'Marked as archived (IsArchived=true)';
                    row_count := v_row_count;
                    size_freed := pg_size_pretty(v_size_bytes);
                    RETURN NEXT;

                    -- Optional: Detach partition (uncomment for actual archival)
                    -- EXECUTE format('ALTER TABLE master."AuditLogs" DETACH PARTITION master.%I', v_partition.tablename);
                    -- action_taken := 'Detached from parent table';
                END IF;
            END IF;
        END;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- Function 4: Partition health check
CREATE OR REPLACE FUNCTION master.check_partition_health()
RETURNS TABLE(
    check_type TEXT,
    status TEXT,
    details TEXT
) AS $$
DECLARE
    v_missing_partitions INTEGER;
    v_largest_partition TEXT;
    v_largest_size BIGINT;
    v_next_month DATE;
BEGIN
    -- Check 1: Future partitions (should have next 3 months)
    v_next_month := DATE_TRUNC('month', CURRENT_DATE + INTERVAL '3 months');

    SELECT COUNT(*) INTO v_missing_partitions
    FROM generate_series(
        DATE_TRUNC('month', CURRENT_DATE + INTERVAL '1 month'),
        v_next_month,
        '1 month'::INTERVAL
    ) AS month
    WHERE NOT EXISTS (
        SELECT 1 FROM pg_tables
        WHERE schemaname = 'master'
        AND tablename = 'AuditLogs_' || TO_CHAR(month, 'YYYY_MM')
    );

    check_type := 'Future Partitions';
    IF v_missing_partitions = 0 THEN
        status := 'OK';
        details := 'All future partitions (next 3 months) exist';
    ELSE
        status := 'WARNING';
        details := v_missing_partitions || ' partition(s) missing for next 3 months';
    END IF;
    RETURN NEXT;

    -- Check 2: Largest partition size
    SELECT
        tablename,
        pg_total_relation_size('master.' || tablename)
    INTO v_largest_partition, v_largest_size
    FROM pg_tables
    WHERE schemaname = 'master'
    AND tablename LIKE 'AuditLogs_2%'
    ORDER BY pg_total_relation_size('master.' || tablename) DESC
    LIMIT 1;

    check_type := 'Largest Partition';
    status := 'INFO';
    details := v_largest_partition || ' - ' || pg_size_pretty(v_largest_size);
    RETURN NEXT;

    -- Check 3: Partitions older than 12 months
    SELECT COUNT(*) INTO v_missing_partitions
    FROM pg_tables
    WHERE schemaname = 'master'
    AND tablename LIKE 'AuditLogs_2%'
    AND tablename NOT LIKE '%_and_earlier'
    AND tablename < 'AuditLogs_' || TO_CHAR(DATE_TRUNC('month', CURRENT_DATE - INTERVAL '12 months'), 'YYYY_MM');

    check_type := 'Old Partitions';
    IF v_missing_partitions = 0 THEN
        status := 'OK';
        details := 'No partitions older than 12 months';
    ELSE
        status := 'NOTICE';
        details := v_missing_partitions || ' partition(s) older than 12 months (consider archiving)';
    END IF;
    RETURN NEXT;

    -- Check 4: Partition count
    SELECT COUNT(*) INTO v_missing_partitions
    FROM pg_tables
    WHERE schemaname = 'master'
    AND tablename LIKE 'AuditLogs_%';

    check_type := 'Total Partitions';
    status := 'INFO';
    details := v_missing_partitions || ' partition(s) exist';
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;

-- Function 5: Get partition statistics
CREATE OR REPLACE FUNCTION master.get_partition_stats()
RETURNS TABLE(
    partition_name TEXT,
    month DATE,
    row_count BIGINT,
    total_size TEXT,
    table_size TEXT,
    index_size TEXT,
    archived_rows BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        t.tablename::TEXT,
        (regexp_matches(t.tablename, 'AuditLogs_(\d{4})_(\d{2})'))[1]::TEXT ||
        '-' ||
        (regexp_matches(t.tablename, 'AuditLogs_(\d{4})_(\d{2})'))[2]::TEXT ||
        '-01'::TEXT AS month_date,
        (SELECT COUNT(*) FROM master."AuditLogs" p WHERE pg_partition_root(p.tableoid)::regclass::text = 'master."AuditLogs"' AND p.tableoid::regclass::text = 'master.' || t.tablename) AS row_count_est,
        pg_size_pretty(pg_total_relation_size('master.' || t.tablename))::TEXT,
        pg_size_pretty(pg_relation_size('master.' || t.tablename))::TEXT,
        pg_size_pretty(pg_indexes_size('master.' || t.tablename))::TEXT,
        COALESCE((SELECT COUNT(*) FROM master."AuditLogs" p WHERE p.tableoid::regclass::text = 'master.' || t.tablename AND p."IsArchived" = true), 0) AS archived_count
    FROM pg_tables t
    WHERE t.schemaname = 'master'
    AND t.tablename LIKE 'AuditLogs_2%'
    ORDER BY t.tablename DESC;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- MAINTENANCE PROCEDURES
-- =====================================================

-- Procedure 1: Monthly maintenance (run via cron/Hangfire)
CREATE OR REPLACE PROCEDURE master.monthly_partition_maintenance()
LANGUAGE plpgsql
AS $$
DECLARE
    v_result TEXT;
BEGIN
    RAISE NOTICE 'Starting monthly partition maintenance...';

    -- Create future partitions
    RAISE NOTICE 'Creating future partitions...';
    SELECT string_agg(partition_name || ': ' || date_range, E'\n')
    INTO v_result
    FROM master.create_auditlogs_future_partitions();

    IF v_result IS NOT NULL THEN
        RAISE NOTICE 'Created partitions: %', v_result;
    ELSE
        RAISE NOTICE 'All future partitions already exist';
    END IF;

    -- Health check
    RAISE NOTICE 'Running health check...';
    RAISE NOTICE '%', (SELECT string_agg(check_type || ': ' || status || ' - ' || details, E'\n')
                       FROM master.check_partition_health());

    RAISE NOTICE 'Monthly partition maintenance completed';
END;
$$;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- Create next month's partition
SELECT master.create_auditlogs_next_partition();

-- Create next 3 months of partitions
SELECT * FROM master.create_auditlogs_future_partitions();

-- Archive old partitions (older than 12 months)
SELECT * FROM master.archive_old_auditlogs_partitions(12);

-- Health check
SELECT * FROM master.check_partition_health();

-- Get partition statistics
SELECT * FROM master.get_partition_stats();

-- Run monthly maintenance
CALL master.monthly_partition_maintenance();
*/

-- =====================================================
-- SCHEDULED JOB SETUP (Execute after Hangfire is configured)
-- =====================================================

/*
-- Option 1: PostgreSQL pg_cron extension
CREATE EXTENSION IF NOT EXISTS pg_cron;

-- Schedule monthly maintenance (1st of every month at 2 AM)
SELECT cron.schedule(
    'auditlogs_partition_maintenance',
    '0 2 1 * *',
    $$CALL master.monthly_partition_maintenance()$$
);

-- Option 2: Hangfire recurring job (C# code)
RecurringJob.AddOrUpdate(
    "auditlogs-partition-maintenance",
    () => ExecuteSqlProcedure("CALL master.monthly_partition_maintenance()"),
    Cron.Monthly(1, 2) // 1st of month, 2 AM
);
*/
