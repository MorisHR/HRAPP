-- ============================================================================
-- Fortune 50 Monitoring Schema Creation
-- Purpose: Real-time performance monitoring infrastructure
-- Safety: Zero impact on existing schemas (separate monitoring schema)
-- Rollback: DROP SCHEMA monitoring CASCADE;
-- ============================================================================

-- Create dedicated monitoring schema (isolated from application schemas)
CREATE SCHEMA IF NOT EXISTS monitoring;

COMMENT ON SCHEMA monitoring IS 'Fortune 50-grade real-time performance monitoring - completely isolated from application data';

-- ============================================================================
-- Table 1: Performance Metrics Snapshot
-- Captures real-time performance metrics every minute
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.performance_metrics (
    id BIGSERIAL PRIMARY KEY,
    captured_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Database Performance
    cache_hit_rate NUMERIC(5,2),
    active_connections INTEGER,
    max_connections INTEGER,
    connection_utilization_pct NUMERIC(5,2),
    total_commits BIGINT,
    total_rollbacks BIGINT,
    rollback_rate_pct NUMERIC(5,2),

    -- Query Performance
    avg_query_time_ms NUMERIC(10,2),
    p95_query_time_ms NUMERIC(10,2),
    p99_query_time_ms NUMERIC(10,2),
    slow_queries_count INTEGER, -- queries >100ms

    -- Disk I/O
    blocks_read BIGINT,
    blocks_hit BIGINT,

    -- Tenant Metrics
    active_tenant_count INTEGER,
    total_tenant_count INTEGER,

    -- Indexes for time-series queries
    CONSTRAINT performance_metrics_captured_at_check CHECK (captured_at <= NOW())
);

CREATE INDEX idx_performance_metrics_captured_at ON monitoring.performance_metrics(captured_at DESC);

COMMENT ON TABLE monitoring.performance_metrics IS 'Time-series performance metrics captured every minute for trending analysis';

-- ============================================================================
-- Table 2: Health Check Results
-- Stores results of periodic health checks
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.health_checks (
    id BIGSERIAL PRIMARY KEY,
    checked_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    check_type VARCHAR(50) NOT NULL, -- database, api, cache, tenants
    status VARCHAR(20) NOT NULL, -- healthy, degraded, critical
    response_time_ms NUMERIC(10,2),
    error_message TEXT,
    details JSONB,

    CONSTRAINT health_checks_status_check CHECK (status IN ('healthy', 'degraded', 'critical'))
);

CREATE INDEX idx_health_checks_checked_at ON monitoring.health_checks(checked_at DESC);
CREATE INDEX idx_health_checks_status ON monitoring.health_checks(status) WHERE status != 'healthy';

COMMENT ON TABLE monitoring.health_checks IS 'Health check results for monitoring system availability';

-- ============================================================================
-- Table 3: API Performance Tracking
-- Tracks API endpoint performance in real-time
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.api_performance (
    id BIGSERIAL PRIMARY KEY,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    endpoint VARCHAR(200) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    tenant_subdomain VARCHAR(100),

    -- Performance metrics
    response_time_ms NUMERIC(10,2) NOT NULL,
    status_code INTEGER NOT NULL,
    is_error BOOLEAN GENERATED ALWAYS AS (status_code >= 400) STORED,

    -- Request details
    user_id UUID,
    request_size_bytes INTEGER,
    response_size_bytes INTEGER,

    -- Index for performance queries
    CONSTRAINT api_performance_response_time_check CHECK (response_time_ms >= 0)
);

CREATE INDEX idx_api_performance_recorded_at ON monitoring.api_performance(recorded_at DESC);
CREATE INDEX idx_api_performance_endpoint ON monitoring.api_performance(endpoint);
CREATE INDEX idx_api_performance_tenant ON monitoring.api_performance(tenant_subdomain);
CREATE INDEX idx_api_performance_slow_requests ON monitoring.api_performance(response_time_ms) WHERE response_time_ms > 100;
CREATE INDEX idx_api_performance_errors ON monitoring.api_performance(status_code) WHERE status_code >= 400;

COMMENT ON TABLE monitoring.api_performance IS 'API endpoint performance tracking for response time analysis';

-- ============================================================================
-- Table 4: Security Events
-- Tracks security-related events for compliance and threat detection
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.security_events (
    id BIGSERIAL PRIMARY KEY,
    detected_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    event_type VARCHAR(100) NOT NULL, -- failed_login, idor_attempt, cross_tenant_query, etc.
    severity VARCHAR(20) NOT NULL, -- info, warning, critical

    -- Event details
    user_id VARCHAR(100),
    ip_address INET,
    tenant_subdomain VARCHAR(100),
    resource_id VARCHAR(100),
    endpoint VARCHAR(200),

    -- Event metadata
    details JSONB,
    is_blocked BOOLEAN DEFAULT TRUE,

    CONSTRAINT security_events_severity_check CHECK (severity IN ('info', 'warning', 'critical'))
);

CREATE INDEX idx_security_events_detected_at ON monitoring.security_events(detected_at DESC);
CREATE INDEX idx_security_events_severity ON monitoring.security_events(severity) WHERE severity IN ('warning', 'critical');
CREATE INDEX idx_security_events_type ON monitoring.security_events(event_type);
CREATE INDEX idx_security_events_tenant ON monitoring.security_events(tenant_subdomain);

COMMENT ON TABLE monitoring.security_events IS 'Security event tracking for threat detection and compliance auditing';

-- ============================================================================
-- Table 5: Tenant Activity Metrics
-- Tracks per-tenant activity for capacity planning
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.tenant_activity (
    id BIGSERIAL PRIMARY KEY,
    measured_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    tenant_id UUID NOT NULL,
    tenant_subdomain VARCHAR(100) NOT NULL,

    -- Activity metrics
    active_users_count INTEGER DEFAULT 0,
    api_requests_count INTEGER DEFAULT 0,
    avg_response_time_ms NUMERIC(10,2),
    error_count INTEGER DEFAULT 0,
    error_rate_pct NUMERIC(5,2),

    -- Resource usage
    database_queries_count INTEGER DEFAULT 0,
    cache_hits_count INTEGER DEFAULT 0,
    cache_misses_count INTEGER DEFAULT 0,
    cache_hit_rate_pct NUMERIC(5,2),

    -- Storage metrics
    schema_size_mb NUMERIC(12,2),
    employee_count INTEGER DEFAULT 0,
    active_payroll_cycles INTEGER DEFAULT 0
);

CREATE INDEX idx_tenant_activity_measured_at ON monitoring.tenant_activity(measured_at DESC);
CREATE INDEX idx_tenant_activity_tenant_id ON monitoring.tenant_activity(tenant_id);
CREATE UNIQUE INDEX idx_tenant_activity_unique_snapshot ON monitoring.tenant_activity(tenant_id, DATE_TRUNC('minute', measured_at));

COMMENT ON TABLE monitoring.tenant_activity IS 'Per-tenant activity metrics for capacity planning and usage analysis';

-- ============================================================================
-- Table 6: Alert History
-- Tracks all triggered alerts for compliance and analysis
-- ============================================================================

CREATE TABLE IF NOT EXISTS monitoring.alert_history (
    id BIGSERIAL PRIMARY KEY,
    triggered_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    alert_name VARCHAR(100) NOT NULL,
    severity VARCHAR(20) NOT NULL, -- info, warning, critical
    metric_name VARCHAR(100) NOT NULL,
    threshold_value NUMERIC,
    actual_value NUMERIC,

    -- Alert details
    description TEXT,
    notification_sent BOOLEAN DEFAULT FALSE,
    notification_channels TEXT[], -- ['slack', 'email', 'pagerduty']
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    acknowledged_by VARCHAR(100),
    resolved_at TIMESTAMP WITH TIME ZONE,

    -- Metadata
    details JSONB,

    CONSTRAINT alert_history_severity_check CHECK (severity IN ('info', 'warning', 'critical'))
);

CREATE INDEX idx_alert_history_triggered_at ON monitoring.alert_history(triggered_at DESC);
CREATE INDEX idx_alert_history_alert_name ON monitoring.alert_history(alert_name);
CREATE INDEX idx_alert_history_severity ON monitoring.alert_history(severity);
CREATE INDEX idx_alert_history_unresolved ON monitoring.alert_history(resolved_at) WHERE resolved_at IS NULL;

COMMENT ON TABLE monitoring.alert_history IS 'Alert history for compliance tracking and incident analysis';

-- ============================================================================
-- Materialized View: Real-Time Dashboard Summary
-- Aggregated metrics refreshed every minute for fast dashboard queries
-- ============================================================================

CREATE MATERIALIZED VIEW IF NOT EXISTS monitoring.dashboard_summary AS
SELECT
    NOW() AS snapshot_time,

    -- Database Health
    (SELECT cache_hit_rate FROM monitoring.performance_metrics ORDER BY captured_at DESC LIMIT 1) AS current_cache_hit_rate,
    (SELECT active_connections FROM monitoring.performance_metrics ORDER BY captured_at DESC LIMIT 1) AS current_connections,
    (SELECT connection_utilization_pct FROM monitoring.performance_metrics ORDER BY captured_at DESC LIMIT 1) AS connection_utilization,

    -- API Performance (last 5 minutes)
    (SELECT PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY response_time_ms)
     FROM monitoring.api_performance
     WHERE recorded_at > NOW() - INTERVAL '5 minutes') AS api_p95_response_time,

    (SELECT COUNT(*) * 12 -- Extrapolate to req/min
     FROM monitoring.api_performance
     WHERE recorded_at > NOW() - INTERVAL '5 minutes') AS api_requests_per_minute,

    (SELECT ROUND(100.0 * COUNT(*) FILTER (WHERE status_code >= 400) / NULLIF(COUNT(*), 0), 2)
     FROM monitoring.api_performance
     WHERE recorded_at > NOW() - INTERVAL '5 minutes') AS api_error_rate_pct,

    -- Tenant Activity
    (SELECT COUNT(DISTINCT tenant_subdomain)
     FROM monitoring.tenant_activity
     WHERE measured_at > NOW() - INTERVAL '5 minutes') AS active_tenants_count,

    -- Security Events (last hour)
    (SELECT COUNT(*)
     FROM monitoring.security_events
     WHERE detected_at > NOW() - INTERVAL '1 hour'
     AND severity = 'critical') AS critical_security_events_1h,

    -- Active Alerts
    (SELECT COUNT(*)
     FROM monitoring.alert_history
     WHERE resolved_at IS NULL) AS active_alerts_count;

CREATE UNIQUE INDEX idx_dashboard_summary_snapshot_time ON monitoring.dashboard_summary(snapshot_time);

COMMENT ON MATERIALIZED VIEW monitoring.dashboard_summary IS 'Aggregated dashboard metrics refreshed every minute for fast querying';

-- ============================================================================
-- Function: Refresh Dashboard Summary (Called by cron/scheduler)
-- ============================================================================

CREATE OR REPLACE FUNCTION monitoring.refresh_dashboard_summary()
RETURNS VOID AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY monitoring.dashboard_summary;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION monitoring.refresh_dashboard_summary IS 'Refreshes materialized view with latest metrics - call every minute';

-- ============================================================================
-- Data Retention Policies (Automatic Cleanup)
-- ============================================================================

-- Partition tables by time for efficient cleanup (future enhancement)
-- For now, use simple DELETE policies

CREATE OR REPLACE FUNCTION monitoring.cleanup_old_data()
RETURNS TABLE(table_name TEXT, rows_deleted BIGINT) AS $$
DECLARE
    retention_days INTEGER := 90; -- 90 days default retention
BEGIN
    -- Cleanup performance metrics older than 90 days
    DELETE FROM monitoring.performance_metrics WHERE captured_at < NOW() - INTERVAL '90 days';
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'performance_metrics';
    RETURN NEXT;

    -- Cleanup health checks older than 30 days
    DELETE FROM monitoring.health_checks WHERE checked_at < NOW() - INTERVAL '30 days';
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'health_checks';
    RETURN NEXT;

    -- Cleanup API performance older than 30 days
    DELETE FROM monitoring.api_performance WHERE recorded_at < NOW() - INTERVAL '30 days';
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'api_performance';
    RETURN NEXT;

    -- Cleanup security events older than 365 days (1 year for compliance)
    DELETE FROM monitoring.security_events WHERE detected_at < NOW() - INTERVAL '365 days';
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'security_events';
    RETURN NEXT;

    -- Cleanup tenant activity older than 90 days
    DELETE FROM monitoring.tenant_activity WHERE measured_at < NOW() - INTERVAL '90 days';
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'tenant_activity';
    RETURN NEXT;

    -- Cleanup resolved alerts older than 90 days
    DELETE FROM monitoring.alert_history WHERE resolved_at < NOW() - INTERVAL '90 days' AND resolved_at IS NOT NULL;
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;
    table_name := 'alert_history';
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION monitoring.cleanup_old_data IS 'Automated cleanup of old monitoring data - run daily via cron';

-- ============================================================================
-- Grants: Allow monitoring user read-only access to application schemas
-- ============================================================================

-- Create monitoring role (read-only)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'monitoring_reader') THEN
        CREATE ROLE monitoring_reader WITH LOGIN PASSWORD 'CHANGE_ME_IN_PRODUCTION';
    END IF;
END $$;

-- Grant read-only access to monitoring schema
GRANT USAGE ON SCHEMA monitoring TO monitoring_reader;
GRANT SELECT ON ALL TABLES IN SCHEMA monitoring TO monitoring_reader;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA monitoring TO monitoring_reader;
ALTER DEFAULT PRIVILEGES IN SCHEMA monitoring GRANT SELECT ON TABLES TO monitoring_reader;

-- Grant read-only access to pg_stat views for metrics collection
GRANT pg_monitor TO monitoring_reader;

COMMENT ON ROLE monitoring_reader IS 'Read-only role for monitoring tools (Prometheus, Grafana)';

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Verify schema creation
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'monitoring'
ORDER BY tablename;

-- Verify indexes
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'monitoring'
ORDER BY tablename, indexname;

-- ============================================================================
-- Deployment Summary
-- ============================================================================

DO $$
BEGIN
    RAISE NOTICE '=================================================================';
    RAISE NOTICE 'Fortune 50 Monitoring Schema Created Successfully';
    RAISE NOTICE '=================================================================';
    RAISE NOTICE 'Schema: monitoring';
    RAISE NOTICE 'Tables Created: 6 (performance_metrics, health_checks, api_performance, security_events, tenant_activity, alert_history)';
    RAISE NOTICE 'Materialized View: 1 (dashboard_summary)';
    RAISE NOTICE 'Functions: 2 (refresh_dashboard_summary, cleanup_old_data)';
    RAISE NOTICE 'Monitoring Role: monitoring_reader (read-only access)';
    RAISE NOTICE '';
    RAISE NOTICE 'Next Steps:';
    RAISE NOTICE '1. Change monitoring_reader password in production';
    RAISE NOTICE '2. Schedule dashboard refresh: SELECT monitoring.refresh_dashboard_summary();';
    RAISE NOTICE '3. Schedule cleanup: SELECT * FROM monitoring.cleanup_old_data();';
    RAISE NOTICE '4. Configure Prometheus to scrape metrics';
    RAISE NOTICE '5. Import Grafana dashboards';
    RAISE NOTICE '=================================================================';
    RAISE NOTICE 'Status: SAFE FOR PRODUCTION (zero impact on existing schemas)';
    RAISE NOTICE '=================================================================';
END $$;
