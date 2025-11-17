#!/bin/bash
# ============================================================================
# Fortune 50 Monitoring Scheduler
# Purpose: Schedule automated metric collection for real-time monitoring
# Method: pg_cron (preferred) or system cron (fallback)
# ============================================================================

set -euo pipefail

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-hrms_master}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"

echo -e "${BLUE}============================================================================${NC}"
echo -e "${BLUE}Fortune 50 Monitoring Scheduler${NC}"
echo -e "${BLUE}============================================================================${NC}"
echo ""

# ============================================================================
# Check if pg_cron is available
# ============================================================================

echo -e "${BLUE}Checking for pg_cron extension...${NC}"

PG_CRON_AVAILABLE=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -t -c "SELECT COUNT(*) FROM pg_available_extensions WHERE name = 'pg_cron'" 2>/dev/null | xargs)

if [ "$PG_CRON_AVAILABLE" -eq 1 ]; then
    echo -e "${GREEN}✓ pg_cron extension available${NC}"

    # Try to enable pg_cron
    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
        -c "CREATE EXTENSION IF NOT EXISTS pg_cron;" 2>/dev/null || true

    # Check if pg_cron is now enabled
    PG_CRON_ENABLED=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
        -t -c "SELECT COUNT(*) FROM pg_extension WHERE extname = 'pg_cron'" 2>/dev/null | xargs)

    if [ "$PG_CRON_ENABLED" -eq 1 ]; then
        echo -e "${GREEN}✓ pg_cron extension enabled${NC}"
        USE_PG_CRON=true
    else
        echo -e "${YELLOW}⚠ pg_cron available but not enabled (requires superuser)${NC}"
        USE_PG_CRON=false
    fi
else
    echo -e "${YELLOW}⚠ pg_cron not available, will use system cron${NC}"
    USE_PG_CRON=false
fi

echo ""

# ============================================================================
# Schedule with pg_cron (if available)
# ============================================================================

if [ "$USE_PG_CRON" = true ]; then
    echo -e "${BLUE}Scheduling monitoring jobs with pg_cron...${NC}"

    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" << 'EOF'
-- Remove existing monitoring jobs (if any)
DELETE FROM cron.job WHERE jobname LIKE 'monitoring_%';

-- Schedule performance snapshot capture (every minute)
SELECT cron.schedule(
    'monitoring_performance_snapshot',
    '* * * * *',  -- Every minute
    $$SELECT monitoring.capture_performance_snapshot();$$
);

-- Schedule dashboard summary refresh (every minute)
SELECT cron.schedule(
    'monitoring_dashboard_refresh',
    '* * * * *',  -- Every minute
    $$SELECT monitoring.refresh_dashboard_summary();$$
);

-- Schedule daily cleanup (2 AM daily)
SELECT cron.schedule(
    'monitoring_daily_cleanup',
    '0 2 * * *',  -- 2 AM every day
    $$SELECT * FROM monitoring.cleanup_old_data();$$
);

-- Show scheduled jobs
SELECT jobid, jobname, schedule, command
FROM cron.job
WHERE jobname LIKE 'monitoring_%'
ORDER BY jobname;
EOF

    echo -e "${GREEN}✓ Monitoring jobs scheduled with pg_cron${NC}"
    echo ""

# ============================================================================
# Schedule with system cron (fallback)
# ============================================================================
else
    echo -e "${BLUE}Configuring system cron...${NC}"

    # Create cron script
    cat > /tmp/monitoring-cron.sh << 'CRONEOF'
#!/bin/bash
# Monitoring metric collection script

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-hrms_master}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"

# Capture performance snapshot
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -c "SELECT monitoring.capture_performance_snapshot();" > /dev/null 2>&1

# Refresh dashboard summary
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -c "SELECT monitoring.refresh_dashboard_summary();" > /dev/null 2>&1
CRONEOF

    chmod +x /tmp/monitoring-cron.sh

    # Create cleanup script
    cat > /tmp/monitoring-cleanup.sh << 'CLEANUPEOF'
#!/bin/bash
# Daily monitoring data cleanup script

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"
POSTGRES_DB="${POSTGRES_DB:-hrms_master}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"

PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -c "SELECT * FROM monitoring.cleanup_old_data();" > /dev/null 2>&1
CLEANUPEOF

    chmod +x /tmp/monitoring-cleanup.sh

    echo ""
    echo -e "${YELLOW}Manual cron configuration required:${NC}"
    echo ""
    echo "Run: crontab -e"
    echo ""
    echo "Add these lines:"
    echo ""
    echo "# HRMS Monitoring - Metric Collection (every minute)"
    echo "* * * * * /tmp/monitoring-cron.sh"
    echo ""
    echo "# HRMS Monitoring - Daily Cleanup (2 AM)"
    echo "0 2 * * * /tmp/monitoring-cleanup.sh"
    echo ""
    echo -e "${GREEN}✓ Cron scripts created in /tmp/${NC}"
    echo ""
fi

# ============================================================================
# Verify monitoring is working
# ============================================================================

echo -e "${BLUE}Verifying monitoring status...${NC}"

# Capture a test snapshot
SNAPSHOT_ID=$(PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -t -c "SELECT monitoring.capture_performance_snapshot();" 2>/dev/null | xargs)

if [ -n "$SNAPSHOT_ID" ] && [ "$SNAPSHOT_ID" != "0" ]; then
    echo -e "${GREEN}✓ Test snapshot captured (ID: $SNAPSHOT_ID)${NC}"
else
    echo -e "${YELLOW}⚠ Test snapshot failed${NC}"
fi

# Show recent metrics
echo ""
echo -e "${BLUE}Recent performance metrics:${NC}"
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
    -c "SELECT captured_at, cache_hit_rate, active_connections, p95_query_time_ms, active_tenant_count FROM monitoring.performance_metrics ORDER BY captured_at DESC LIMIT 3;"

echo ""
echo -e "${GREEN}============================================================================${NC}"
echo -e "${GREEN}Monitoring Scheduler Configuration Complete${NC}"
echo -e "${GREEN}============================================================================${NC}"
echo ""

if [ "$USE_PG_CRON" = true ]; then
    echo "✅ Automated metric collection scheduled with pg_cron"
    echo "   - Performance snapshots: Every minute"
    echo "   - Dashboard refresh: Every minute"
    echo "   - Data cleanup: Daily at 2 AM"
else
    echo "⚠️  Manual cron configuration required"
    echo "   - Scripts created in /tmp/"
    echo "   - Run 'crontab -e' and add the lines above"
fi

echo ""
echo "Next Steps:"
echo "  1. Import Grafana dashboards from monitoring/grafana/dashboards/"
echo "  2. Configure Prometheus and Alertmanager"
echo "  3. Update notification channels (Slack, PagerDuty, Email)"
echo ""
