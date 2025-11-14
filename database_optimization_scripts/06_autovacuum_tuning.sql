-- =====================================================
-- AUTO-VACUUM TUNING FOR HIGH-CHURN TABLES
-- =====================================================
-- Purpose: Optimize auto-vacuum settings for tables with frequent updates
-- Impact: Reduces table bloat, improves query performance
-- Target Tables: AuditLogs, RefreshTokens, Attendances
-- =====================================================

-- =====================================================
-- PART 1: CONFIGURE AUTO-VACUUM FOR AUDIT LOGS
-- =====================================================

-- AuditLogs: High INSERT rate, no UPDATEs (insert-only table)
ALTER TABLE master."AuditLogs" SET (
    -- Vacuum more aggressively to reclaim dead tuples from failed inserts
    autovacuum_vacuum_scale_factor = 0.05, -- Vacuum when 5% of table has dead tuples (default: 20%)
    autovacuum_analyze_scale_factor = 0.02, -- Analyze when 2% changes (default: 10%)

    -- Increase cost limit for faster vacuum
    autovacuum_vacuum_cost_limit = 2000, -- Higher limit for faster vacuum (default: 200)
    autovacuum_vacuum_cost_delay = 10, -- 10ms delay (default: 20ms)

    -- More aggressive freeze to prevent transaction ID wraparound
    autovacuum_freeze_min_age = 50000000, -- Earlier freeze (default: 50M)
    autovacuum_freeze_max_age = 200000000, -- Earlier forced vacuum (default: 200M)

    -- Statistics target for better query plans
    autovacuum_analyze_threshold = 100 -- Analyze after 100 row changes
);

-- =====================================================
-- PART 2: CONFIGURE AUTO-VACUUM FOR REFRESH TOKENS
-- =====================================================

-- RefreshTokens: High INSERT/DELETE rate (token rotation)
ALTER TABLE master."RefreshTokens" SET (
    -- Very aggressive vacuum due to high churn
    autovacuum_vacuum_scale_factor = 0.1, -- Vacuum when 10% dead tuples
    autovacuum_analyze_scale_factor = 0.05, -- Analyze when 5% changes

    -- Fast vacuum execution
    autovacuum_vacuum_cost_limit = 3000,
    autovacuum_vacuum_cost_delay = 5, -- 5ms delay for faster cleanup

    -- Lower threshold for triggering vacuum
    autovacuum_vacuum_threshold = 50 -- Vacuum after 50 dead tuples
);

-- =====================================================
-- PART 3: CONFIGURE AUTO-VACUUM FOR TENANT ATTENDANCES
-- =====================================================

CREATE OR REPLACE FUNCTION master.tune_tenant_autovacuum(
    p_tenant_schema TEXT
)
RETURNS TEXT AS $$
DECLARE
    v_result TEXT := '';
BEGIN
    -- Attendances: Moderate INSERT rate, occasional UPDATEs
    EXECUTE format('
        ALTER TABLE %I."Attendances" SET (
            autovacuum_vacuum_scale_factor = 0.15,
            autovacuum_analyze_scale_factor = 0.1,
            autovacuum_vacuum_cost_limit = 1500,
            autovacuum_vacuum_cost_delay = 10
        )',
        p_tenant_schema
    );
    v_result := v_result || 'Tuned Attendances' || E'\n';

    -- AttendanceCorrections: Low frequency but important
    EXECUTE format('
        ALTER TABLE %I."AttendanceCorrections" SET (
            autovacuum_vacuum_scale_factor = 0.2,
            autovacuum_analyze_scale_factor = 0.1
        )',
        p_tenant_schema
    );
    v_result := v_result || 'Tuned AttendanceCorrections' || E'\n';

    -- LeaveApplications: Moderate churn during peak leave periods
    EXECUTE format('
        ALTER TABLE %I."LeaveApplications" SET (
            autovacuum_vacuum_scale_factor = 0.2,
            autovacuum_analyze_scale_factor = 0.1,
            autovacuum_vacuum_cost_limit = 1000
        )',
        p_tenant_schema
    );
    v_result := v_result || 'Tuned LeaveApplications' || E'\n';

    -- Timesheets: Weekly INSERT, occasional UPDATEs during approval
    EXECUTE format('
        ALTER TABLE %I."Timesheets" SET (
            autovacuum_vacuum_scale_factor = 0.2,
            autovacuum_analyze_scale_factor = 0.15
        )',
        p_tenant_schema
    );
    v_result := v_result || 'Tuned Timesheets' || E'\n';

    -- DeviceSyncLogs: High INSERT rate during device sync
    EXECUTE format('
        ALTER TABLE %I."DeviceSyncLogs" SET (
            autovacuum_vacuum_scale_factor = 0.1,
            autovacuum_analyze_scale_factor = 0.05,
            autovacuum_vacuum_cost_limit = 2000,
            autovacuum_vacuum_cost_delay = 10
        )',
        p_tenant_schema
    );
    v_result := v_result || 'Tuned DeviceSyncLogs' || E'\n';

    RETURN v_result;
END;
$$ LANGUAGE plpgsql;

-- Apply to all tenant schemas
CREATE OR REPLACE FUNCTION master.tune_all_tenant_autovacuum()
RETURNS TABLE(tenant_schema TEXT, result TEXT) AS $$
DECLARE
    v_schema RECORD;
BEGIN
    FOR v_schema IN
        SELECT nspname
        FROM pg_namespace
        WHERE nspname LIKE 'tenant_%'
        AND nspname != 'tenant_testcorp'
    LOOP
        tenant_schema := v_schema.nspname;
        result := master.tune_tenant_autovacuum(v_schema.nspname);
        RETURN NEXT;
    END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- PART 4: HANGFIRE SCHEMA AUTO-VACUUM TUNING
-- =====================================================

-- Hangfire jobs: Very high churn (jobs created and deleted frequently)
ALTER TABLE hangfire.job SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_cost_limit = 2000,
    autovacuum_vacuum_cost_delay = 10
);

ALTER TABLE hangfire.state SET (
    autovacuum_vacuum_scale_factor = 0.1,
    autovacuum_analyze_scale_factor = 0.05,
    autovacuum_vacuum_cost_limit = 2000
);

ALTER TABLE hangfire.jobparameter SET (
    autovacuum_vacuum_scale_factor = 0.1,
    autovacuum_analyze_scale_factor = 0.05
);

ALTER TABLE hangfire.jobqueue SET (
    autovacuum_vacuum_scale_factor = 0.1,
    autovacuum_analyze_scale_factor = 0.05,
    autovacuum_vacuum_cost_limit = 2000
);

-- =====================================================
-- PART 5: TABLE BLOAT MONITORING
-- =====================================================

CREATE OR REPLACE VIEW master."TableBloat" AS
SELECT
    schemaname,
    tablename,
    n_live_tup AS live_tuples,
    n_dead_tup AS dead_tuples,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) AS dead_tuple_percent,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze,
    CASE
        WHEN n_dead_tup > 1000 AND (n_dead_tup::FLOAT / NULLIF(n_live_tup + n_dead_tup, 0)) > 0.2 THEN 'HIGH BLOAT'
        WHEN n_dead_tup > 500 AND (n_dead_tup::FLOAT / NULLIF(n_live_tup + n_dead_tup, 0)) > 0.1 THEN 'MEDIUM BLOAT'
        WHEN n_dead_tup > 100 THEN 'LOW BLOAT'
        ELSE 'OK'
    END AS bloat_status,
    'VACUUM ANALYZE ' || schemaname || '.' || tablename || ';' AS vacuum_statement
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp', 'hangfire')
ORDER BY n_dead_tup DESC;

-- =====================================================
-- PART 6: AUTO-VACUUM ACTIVITY MONITORING
-- =====================================================

CREATE OR REPLACE VIEW master."AutoVacuumActivity" AS
SELECT
    schemaname,
    tablename,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze,
    vacuum_count,
    autovacuum_count,
    analyze_count,
    autoanalyze_count,
    EXTRACT(EPOCH FROM (NOW() - last_autovacuum)) / 3600 AS hours_since_autovacuum,
    EXTRACT(EPOCH FROM (NOW() - last_autoanalyze)) / 3600 AS hours_since_autoanalyze,
    n_dead_tup,
    n_live_tup,
    CASE
        WHEN last_autovacuum IS NULL THEN 'NEVER VACUUMED'
        WHEN EXTRACT(EPOCH FROM (NOW() - last_autovacuum)) > 86400 THEN 'STALE (>24h)'
        WHEN EXTRACT(EPOCH FROM (NOW() - last_autovacuum)) > 43200 THEN 'OLD (>12h)'
        ELSE 'RECENT'
    END AS vacuum_status
FROM pg_stat_user_tables
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp', 'hangfire')
ORDER BY last_autovacuum NULLS FIRST;

-- =====================================================
-- PART 7: MANUAL VACUUM PROCEDURES
-- =====================================================

CREATE OR REPLACE PROCEDURE master.vacuum_bloated_tables(
    p_bloat_threshold_percent NUMERIC DEFAULT 20.0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_table RECORD;
BEGIN
    RAISE NOTICE 'Starting manual vacuum of bloated tables (threshold: %%)...', p_bloat_threshold_percent;

    FOR v_table IN
        SELECT
            schemaname,
            tablename,
            dead_tuple_percent
        FROM master."TableBloat"
        WHERE dead_tuple_percent > p_bloat_threshold_percent
        AND bloat_status IN ('HIGH BLOAT', 'MEDIUM BLOAT')
        ORDER BY dead_tuple_percent DESC
    LOOP
        RAISE NOTICE 'Vacuuming %.% (%.% dead tuples)...',
            v_table.schemaname, v_table.tablename, v_table.dead_tuple_percent;

        EXECUTE format('VACUUM ANALYZE %I.%I', v_table.schemaname, v_table.tablename);

        RAISE NOTICE 'Completed %.%', v_table.schemaname, v_table.tablename;
    END LOOP;

    RAISE NOTICE 'Manual vacuum completed';
END;
$$;

-- Full vacuum procedure (more aggressive, requires exclusive lock)
CREATE OR REPLACE PROCEDURE master.vacuum_full_table(
    p_schema TEXT,
    p_table TEXT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE NOTICE 'Starting VACUUM FULL on %.% (this will lock the table)...', p_schema, p_table;
    RAISE NOTICE 'Table size before: %',
        (SELECT pg_size_pretty(pg_total_relation_size(p_schema||'.'||p_table)));

    EXECUTE format('VACUUM FULL ANALYZE %I.%I', p_schema, p_table);

    RAISE NOTICE 'Table size after: %',
        (SELECT pg_size_pretty(pg_total_relation_size(p_schema||'.'||p_table)));
    RAISE NOTICE 'VACUUM FULL completed';
END;
$$;

-- =====================================================
-- PART 8: REFRESH TOKEN CLEANUP PROCEDURE
-- =====================================================

CREATE OR REPLACE PROCEDURE master.cleanup_expired_refresh_tokens()
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_count INTEGER;
BEGIN
    RAISE NOTICE 'Starting expired refresh token cleanup...';

    -- Delete expired tokens
    WITH deleted AS (
        DELETE FROM master."RefreshTokens"
        WHERE "ExpiresAt" < NOW()
        OR "IsRevoked" = true
        RETURNING *
    )
    SELECT COUNT(*) INTO v_deleted_count FROM deleted;

    RAISE NOTICE 'Deleted % expired/revoked tokens', v_deleted_count;

    -- Vacuum the table
    VACUUM ANALYZE master."RefreshTokens";

    RAISE NOTICE 'Cleanup completed';
END;
$$;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- 1. Apply autovacuum tuning to all tenants
SELECT * FROM master.tune_all_tenant_autovacuum();

-- 2. Check table bloat
SELECT * FROM master."TableBloat"
WHERE bloat_status IN ('HIGH BLOAT', 'MEDIUM BLOAT');

-- 3. Check autovacuum activity
SELECT * FROM master."AutoVacuumActivity"
WHERE vacuum_status IN ('NEVER VACUUMED', 'STALE (>24h)');

-- 4. Manually vacuum bloated tables
CALL master.vacuum_bloated_tables(20.0);

-- 5. Full vacuum a specific table (during maintenance window)
CALL master.vacuum_full_table('master', 'AuditLogs');

-- 6. Cleanup expired refresh tokens
CALL master.cleanup_expired_refresh_tokens();

-- 7. View current autovacuum settings for a table
SELECT
    relname,
    reloptions
FROM pg_class
WHERE relnamespace = 'master'::regnamespace
AND relname = 'AuditLogs';
*/

-- =====================================================
-- SCHEDULED MAINTENANCE
-- =====================================================

CREATE OR REPLACE PROCEDURE master.weekly_vacuum_maintenance()
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE NOTICE 'Starting weekly vacuum maintenance...';

    -- Cleanup expired tokens
    CALL master.cleanup_expired_refresh_tokens();

    -- Vacuum bloated tables
    CALL master.vacuum_bloated_tables(15.0);

    RAISE NOTICE 'Weekly vacuum maintenance completed';
END;
$$;

/*
-- Schedule weekly vacuum maintenance
SELECT cron.schedule(
    'weekly_vacuum_maintenance',
    '0 4 * * 0', -- Sundays at 4 AM
    $$CALL master.weekly_vacuum_maintenance()$$
);
*/
