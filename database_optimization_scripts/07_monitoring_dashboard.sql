-- =====================================================
-- DATABASE MONITORING DASHBOARD
-- =====================================================
-- Purpose: Comprehensive monitoring views and functions
-- Usage: Query these views for real-time database health
-- =====================================================

-- =====================================================
-- 1. DATABASE OVERVIEW
-- =====================================================

CREATE OR REPLACE VIEW master."DatabaseOverview" AS
SELECT
    current_database() AS database_name,
    pg_size_pretty(pg_database_size(current_database())) AS database_size,
    (SELECT version()) AS postgres_version,
    (SELECT setting FROM pg_settings WHERE name = 'max_connections') AS max_connections,
    (SELECT count(*) FROM pg_stat_activity) AS active_connections,
    (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') AS active_queries,
    (SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction') AS idle_in_transaction,
    (SELECT count(*) FROM pg_tables WHERE schemaname LIKE 'tenant_%') AS total_tables,
    (SELECT count(*) FROM pg_indexes WHERE schemaname LIKE 'tenant_%' OR schemaname = 'master') AS total_indexes,
    NOW() AS snapshot_time;

-- =====================================================
-- 2. CONNECTION STATISTICS
-- =====================================================

CREATE OR REPLACE VIEW master."ConnectionStats" AS
SELECT
    datname,
    usename,
    application_name,
    client_addr,
    state,
    COUNT(*) AS connection_count,
    MAX(state_change) AS last_state_change,
    array_agg(DISTINCT backend_type) AS backend_types
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY datname, usename, application_name, client_addr, state
ORDER BY connection_count DESC;

-- =====================================================
-- 3. ACTIVE QUERIES
-- =====================================================

CREATE OR REPLACE VIEW master."ActiveQueries" AS
SELECT
    pid,
    usename,
    application_name,
    client_addr,
    state,
    EXTRACT(EPOCH FROM (NOW() - query_start)) AS query_duration_seconds,
    EXTRACT(EPOCH FROM (NOW() - state_change)) AS state_duration_seconds,
    wait_event_type,
    wait_event,
    LEFT(query, 200) AS query_preview,
    CASE
        WHEN EXTRACT(EPOCH FROM (NOW() - query_start)) > 300 THEN 'LONG RUNNING (>5min)'
        WHEN EXTRACT(EPOCH FROM (NOW() - query_start)) > 60 THEN 'SLOW (>1min)'
        WHEN EXTRACT(EPOCH FROM (NOW() - query_start)) > 10 THEN 'NORMAL'
        ELSE 'FAST'
    END AS query_status,
    'SELECT pg_cancel_backend(' || pid || ');' AS cancel_statement,
    'SELECT pg_terminate_backend(' || pid || ');' AS terminate_statement
FROM pg_stat_activity
WHERE datname = current_database()
AND state != 'idle'
AND pid != pg_backend_pid()
ORDER BY query_start ASC;

-- =====================================================
-- 4. DATABASE CACHE HIT RATIO
-- =====================================================

CREATE OR REPLACE VIEW master."CacheHitRatio" AS
SELECT
    'Database' AS cache_type,
    datname,
    blks_hit AS cache_hits,
    blks_read AS disk_reads,
    blks_hit + blks_read AS total_reads,
    CASE
        WHEN blks_hit + blks_read = 0 THEN 0
        ELSE ROUND(100.0 * blks_hit / (blks_hit + blks_read), 2)
    END AS cache_hit_ratio_percent,
    CASE
        WHEN ROUND(100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0), 2) > 99 THEN 'EXCELLENT'
        WHEN ROUND(100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0), 2) > 95 THEN 'GOOD'
        WHEN ROUND(100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0), 2) > 90 THEN 'FAIR'
        ELSE 'POOR'
    END AS status
FROM pg_stat_database
WHERE datname = current_database()
UNION ALL
SELECT
    'Table' AS cache_type,
    schemaname || '.' || tablename AS datname,
    heap_blks_hit,
    heap_blks_read,
    heap_blks_hit + heap_blks_read,
    CASE
        WHEN heap_blks_hit + heap_blks_read = 0 THEN 0
        ELSE ROUND(100.0 * heap_blks_hit / (heap_blks_hit + heap_blks_read), 2)
    END,
    CASE
        WHEN ROUND(100.0 * heap_blks_hit / NULLIF(heap_blks_hit + heap_blks_read, 0), 2) > 99 THEN 'EXCELLENT'
        WHEN ROUND(100.0 * heap_blks_hit / NULLIF(heap_blks_hit + heap_blks_read, 0), 2) > 95 THEN 'GOOD'
        WHEN ROUND(100.0 * heap_blks_hit / NULLIF(heap_blks_hit + heap_blks_read, 0), 2) > 90 THEN 'FAIR'
        ELSE 'POOR'
    END
FROM pg_statio_user_tables
WHERE schemaname IN ('master', 'tenant_siraaj')
AND heap_blks_hit + heap_blks_read > 100
ORDER BY cache_hit_ratio_percent ASC
LIMIT 10;

-- =====================================================
-- 5. TOP TABLES BY SIZE
-- =====================================================

CREATE OR REPLACE VIEW master."TopTablesBySize" AS
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_indexes_size(schemaname||'.'||tablename)) AS indexes_size,
    pg_total_relation_size(schemaname||'.'||tablename) AS total_size_bytes,
    ROUND(100.0 * pg_indexes_size(schemaname||'.'||tablename) /
          NULLIF(pg_total_relation_size(schemaname||'.'||tablename), 0), 2) AS index_ratio_percent,
    n_live_tup AS estimated_row_count,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_tables t
JOIN pg_stat_user_tables s USING (schemaname, tablename)
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'hangfire')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 20;

-- =====================================================
-- 6. TRANSACTION STATISTICS
-- =====================================================

CREATE OR REPLACE VIEW master."TransactionStats" AS
SELECT
    datname,
    xact_commit AS commits,
    xact_rollback AS rollbacks,
    xact_commit + xact_rollback AS total_transactions,
    CASE
        WHEN xact_commit + xact_rollback = 0 THEN 0
        ELSE ROUND(100.0 * xact_rollback / (xact_commit + xact_rollback), 2)
    END AS rollback_ratio_percent,
    blks_read AS disk_blocks_read,
    blks_hit AS cache_blocks_hit,
    tup_returned AS rows_returned,
    tup_fetched AS rows_fetched,
    tup_inserted AS rows_inserted,
    tup_updated AS rows_updated,
    tup_deleted AS rows_deleted,
    conflicts AS conflicts,
    temp_files AS temp_files_created,
    pg_size_pretty(temp_bytes) AS temp_bytes,
    deadlocks,
    stats_reset
FROM pg_stat_database
WHERE datname = current_database();

-- =====================================================
-- 7. LOCK MONITORING
-- =====================================================

CREATE OR REPLACE VIEW master."CurrentLocks" AS
SELECT
    l.locktype,
    l.database,
    l.relation::regclass AS relation_name,
    l.page,
    l.tuple,
    l.virtualxid,
    l.transactionid,
    l.classid,
    l.objid,
    l.objsubid,
    l.virtualtransaction,
    l.pid,
    l.mode,
    l.granted,
    a.usename,
    a.application_name,
    a.client_addr,
    a.state,
    a.query_start,
    LEFT(a.query, 100) AS query_preview
FROM pg_locks l
LEFT JOIN pg_stat_activity a ON l.pid = a.pid
WHERE l.database = (SELECT oid FROM pg_database WHERE datname = current_database())
ORDER BY l.pid, l.granted DESC;

-- =====================================================
-- 8. BLOCKING QUERIES
-- =====================================================

CREATE OR REPLACE VIEW master."BlockingQueries" AS
SELECT
    blocking.pid AS blocking_pid,
    blocking.usename AS blocking_user,
    blocking.application_name AS blocking_app,
    blocking_activity.query AS blocking_query,
    blocked.pid AS blocked_pid,
    blocked.usename AS blocked_user,
    blocked.application_name AS blocked_app,
    blocked_activity.query AS blocked_query,
    EXTRACT(EPOCH FROM (NOW() - blocked_activity.query_start)) AS blocked_duration_seconds
FROM pg_catalog.pg_locks blocked
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked.pid
JOIN pg_catalog.pg_locks blocking ON blocking.locktype = blocked.locktype
    AND blocking.database IS NOT DISTINCT FROM blocked.database
    AND blocking.relation IS NOT DISTINCT FROM blocked.relation
    AND blocking.page IS NOT DISTINCT FROM blocked.page
    AND blocking.tuple IS NOT DISTINCT FROM blocked.tuple
    AND blocking.virtualxid IS NOT DISTINCT FROM blocked.virtualxid
    AND blocking.transactionid IS NOT DISTINCT FROM blocked.transactionid
    AND blocking.classid IS NOT DISTINCT FROM blocked.classid
    AND blocking.objid IS NOT DISTINCT FROM blocked.objid
    AND blocking.objsubid IS NOT DISTINCT FROM blocked.objsubid
    AND blocking.pid != blocked.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking.pid
WHERE NOT blocked.granted;

-- =====================================================
-- 9. DISK SPACE USAGE BY SCHEMA
-- =====================================================

CREATE OR REPLACE VIEW master."DiskSpaceBySchema" AS
SELECT
    schemaname,
    COUNT(*) AS table_count,
    pg_size_pretty(SUM(pg_total_relation_size(schemaname||'.'||tablename))) AS total_size,
    pg_size_pretty(SUM(pg_relation_size(schemaname||'.'||tablename))) AS table_size,
    pg_size_pretty(SUM(pg_indexes_size(schemaname||'.'||tablename))) AS index_size,
    SUM(pg_total_relation_size(schemaname||'.'||tablename)) AS total_bytes
FROM pg_tables
WHERE schemaname IN ('master', 'tenant_default', 'tenant_siraaj', 'tenant_testcorp', 'hangfire')
GROUP BY schemaname
ORDER BY SUM(pg_total_relation_size(schemaname||'.'||tablename)) DESC;

-- =====================================================
-- 10. HEALTH CHECK DASHBOARD
-- =====================================================

CREATE OR REPLACE FUNCTION master.database_health_check()
RETURNS TABLE(
    check_category TEXT,
    check_name TEXT,
    status TEXT,
    value TEXT,
    recommendation TEXT
) AS $$
DECLARE
    v_cache_hit_ratio NUMERIC;
    v_active_connections INTEGER;
    v_max_connections INTEGER;
    v_bloated_tables INTEGER;
    v_long_queries INTEGER;
    v_deadlocks BIGINT;
BEGIN
    -- Check 1: Cache Hit Ratio
    SELECT ROUND(100.0 * blks_hit / NULLIF(blks_hit + blks_read, 0), 2)
    INTO v_cache_hit_ratio
    FROM pg_stat_database
    WHERE datname = current_database();

    check_category := 'Performance';
    check_name := 'Cache Hit Ratio';
    value := v_cache_hit_ratio || '%';
    IF v_cache_hit_ratio > 99 THEN
        status := 'EXCELLENT';
        recommendation := 'Cache performance is optimal';
    ELSIF v_cache_hit_ratio > 95 THEN
        status := 'GOOD';
        recommendation := 'Cache performance is good';
    ELSIF v_cache_hit_ratio > 90 THEN
        status := 'WARNING';
        recommendation := 'Consider increasing shared_buffers';
    ELSE
        status := 'CRITICAL';
        recommendation := 'Increase shared_buffers immediately';
    END IF;
    RETURN NEXT;

    -- Check 2: Connection Usage
    SELECT count(*), setting::INTEGER
    INTO v_active_connections, v_max_connections
    FROM pg_stat_activity, pg_settings
    WHERE name = 'max_connections'
    GROUP BY setting;

    check_category := 'Capacity';
    check_name := 'Connection Usage';
    value := v_active_connections || ' / ' || v_max_connections;
    IF v_active_connections::FLOAT / v_max_connections < 0.5 THEN
        status := 'OK';
        recommendation := 'Connection usage is normal';
    ELSIF v_active_connections::FLOAT / v_max_connections < 0.8 THEN
        status := 'WARNING';
        recommendation := 'Monitor connection usage';
    ELSE
        status := 'CRITICAL';
        recommendation := 'Connection pool nearly exhausted. Implement connection pooling (PgBouncer)';
    END IF;
    RETURN NEXT;

    -- Check 3: Table Bloat
    SELECT COUNT(*)
    INTO v_bloated_tables
    FROM pg_stat_user_tables
    WHERE schemaname IN ('master', 'tenant_siraaj')
    AND n_dead_tup > 1000
    AND n_dead_tup::FLOAT / NULLIF(n_live_tup + n_dead_tup, 0) > 0.2;

    check_category := 'Maintenance';
    check_name := 'Bloated Tables';
    value := v_bloated_tables || ' tables';
    IF v_bloated_tables = 0 THEN
        status := 'OK';
        recommendation := 'No bloated tables detected';
    ELSIF v_bloated_tables < 5 THEN
        status := 'WARNING';
        recommendation := 'Run VACUUM on bloated tables';
    ELSE
        status := 'CRITICAL';
        recommendation := 'Immediate VACUUM required. Review autovacuum settings';
    END IF;
    RETURN NEXT;

    -- Check 4: Long Running Queries
    SELECT COUNT(*)
    INTO v_long_queries
    FROM pg_stat_activity
    WHERE state != 'idle'
    AND EXTRACT(EPOCH FROM (NOW() - query_start)) > 300; -- 5 minutes

    check_category := 'Performance';
    check_name := 'Long Running Queries';
    value := v_long_queries || ' queries';
    IF v_long_queries = 0 THEN
        status := 'OK';
        recommendation := 'No long-running queries';
    ELSIF v_long_queries < 3 THEN
        status := 'WARNING';
        recommendation := 'Review and optimize slow queries';
    ELSE
        status := 'CRITICAL';
        recommendation := 'Multiple slow queries detected. Check for missing indexes';
    END IF;
    RETURN NEXT;

    -- Check 5: Deadlocks
    SELECT deadlocks
    INTO v_deadlocks
    FROM pg_stat_database
    WHERE datname = current_database();

    check_category := 'Concurrency';
    check_name := 'Deadlocks (Total)';
    value := v_deadlocks::TEXT;
    IF v_deadlocks = 0 THEN
        status := 'EXCELLENT';
        recommendation := 'No deadlocks detected';
    ELSIF v_deadlocks < 10 THEN
        status := 'OK';
        recommendation := 'Low deadlock count';
    ELSE
        status := 'WARNING';
        recommendation := 'Review transaction ordering to reduce deadlocks';
    END IF;
    RETURN NEXT;

    -- Check 6: Database Size
    check_category := 'Capacity';
    check_name := 'Database Size';
    value := pg_size_pretty(pg_database_size(current_database()));
    status := 'INFO';
    recommendation := 'Monitor growth trends. Current size: ' || value;
    RETURN NEXT;

END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- USAGE EXAMPLES
-- =====================================================

/*
-- 1. Database overview
SELECT * FROM master."DatabaseOverview";

-- 2. Connection statistics
SELECT * FROM master."ConnectionStats";

-- 3. Active queries
SELECT * FROM master."ActiveQueries"
WHERE query_status IN ('LONG RUNNING (>5min)', 'SLOW (>1min)');

-- 4. Cache hit ratio
SELECT * FROM master."CacheHitRatio"
WHERE status IN ('FAIR', 'POOR');

-- 5. Top tables by size
SELECT * FROM master."TopTablesBySize"
LIMIT 10;

-- 6. Transaction statistics
SELECT * FROM master."TransactionStats";

-- 7. Current locks
SELECT * FROM master."CurrentLocks"
WHERE NOT granted;

-- 8. Blocking queries
SELECT * FROM master."BlockingQueries";

-- 9. Disk space by schema
SELECT * FROM master."DiskSpaceBySchema";

-- 10. Health check
SELECT * FROM master.database_health_check()
WHERE status IN ('WARNING', 'CRITICAL');

-- 11. Kill a long-running query
SELECT pg_cancel_backend(12345); -- Replace with actual PID
-- Or forcefully:
SELECT pg_terminate_backend(12345);
*/
