#!/bin/bash

################################################################################
# Database Health Monitoring Script
# Purpose: Real-time monitoring of PostgreSQL database health
# Usage: ./monitor-database-health.sh [interval_seconds] [database_name]
# Example: ./monitor-database-health.sh 5 hrms_db
################################################################################

set -euo pipefail

# Configuration
INTERVAL="${1:-10}"  # Default: Check every 10 seconds
DB_NAME="${2:-hrms_db}"
DB_USER="${3:-postgres}"
DB_HOST="${4:-localhost}"
DB_PORT="${5:-5432}"

# Color codes for output
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Thresholds (configurable)
CRITICAL_CONNECTION_THRESHOLD=400  # 80% of MaxPoolSize (500)
WARNING_CONNECTION_THRESHOLD=350   # 70% of MaxPoolSize
CRITICAL_LOCK_WAIT_SECONDS=30
WARNING_LOCK_WAIT_SECONDS=10
CRITICAL_SLOW_QUERY_SECONDS=30
WARNING_SLOW_QUERY_SECONDS=5
CRITICAL_DISK_USAGE_PERCENT=90
WARNING_DISK_USAGE_PERCENT=80

# Log file
LOG_FILE="/var/log/hrms/db-health-$(date +%Y%m%d).log"
mkdir -p "$(dirname "$LOG_FILE")"

################################################################################
# Helper Functions
################################################################################

log() {
    local level="$1"
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] [$level] $message" | tee -a "$LOG_FILE"
}

alert() {
    local severity="$1"
    local message="$2"

    case "$severity" in
        CRITICAL)
            echo -e "${RED}[CRITICAL]${NC} $message"
            log "CRITICAL" "$message"
            # Send alert (integrate with your alerting system)
            # send_alert "CRITICAL" "$message"
            ;;
        WARNING)
            echo -e "${YELLOW}[WARNING]${NC} $message"
            log "WARNING" "$message"
            ;;
        INFO)
            echo -e "${GREEN}[INFO]${NC} $message"
            log "INFO" "$message"
            ;;
        *)
            echo -e "${BLUE}[DEBUG]${NC} $message"
            log "DEBUG" "$message"
            ;;
    esac
}

run_query() {
    local query="$1"
    PGPASSWORD="${DB_PASSWORD:-}" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "$query" 2>/dev/null
}

################################################################################
# Monitoring Checks
################################################################################

check_active_connections() {
    echo -e "\n${BLUE}=== Active Database Connections ===${NC}"

    local total_connections=$(run_query "SELECT count(*) FROM pg_stat_activity;")
    local active_connections=$(run_query "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';")
    local idle_connections=$(run_query "SELECT count(*) FROM pg_stat_activity WHERE state = 'idle';")
    local idle_in_transaction=$(run_query "SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction';")

    echo "Total Connections: $total_connections"
    echo "Active Connections: $active_connections"
    echo "Idle Connections: $idle_connections"
    echo "Idle in Transaction: $idle_in_transaction"

    # Alert on high connection count
    if [ "$total_connections" -ge "$CRITICAL_CONNECTION_THRESHOLD" ]; then
        alert "CRITICAL" "Connection pool nearly exhausted: $total_connections/$CRITICAL_CONNECTION_THRESHOLD"
    elif [ "$total_connections" -ge "$WARNING_CONNECTION_THRESHOLD" ]; then
        alert "WARNING" "High connection count: $total_connections/$WARNING_CONNECTION_THRESHOLD"
    fi

    # Alert on idle in transaction (potential locks)
    if [ "$idle_in_transaction" -gt 5 ]; then
        alert "WARNING" "High number of idle-in-transaction connections: $idle_in_transaction"
    fi
}

check_locks() {
    echo -e "\n${BLUE}=== Database Locks ===${NC}"

    local blocked_queries=$(run_query "
        SELECT count(*)
        FROM pg_locks
        WHERE NOT granted;
    ")

    echo "Blocked Queries: $blocked_queries"

    if [ "$blocked_queries" -gt 0 ]; then
        alert "WARNING" "Found $blocked_queries blocked queries"

        # Show details of blocked queries
        echo -e "\n${YELLOW}Blocked Query Details:${NC}"
        run_query "
            SELECT
                blocked_locks.pid AS blocked_pid,
                blocked_activity.usename AS blocked_user,
                blocking_locks.pid AS blocking_pid,
                blocking_activity.usename AS blocking_user,
                blocked_activity.query AS blocked_query,
                blocking_activity.query AS blocking_query,
                EXTRACT(EPOCH FROM (now() - blocked_activity.query_start)) AS block_duration_seconds
            FROM pg_catalog.pg_locks blocked_locks
            JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
            JOIN pg_catalog.pg_locks blocking_locks
                ON blocking_locks.locktype = blocked_locks.locktype
                AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
                AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
                AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
                AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
                AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
                AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
                AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
                AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
                AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
                AND blocking_locks.pid != blocked_locks.pid
            JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
            WHERE NOT blocked_locks.granted;
        " | column -t -s '|'
    else
        echo -e "${GREEN}No blocked queries${NC}"
    fi
}

check_slow_queries() {
    echo -e "\n${BLUE}=== Slow Running Queries ===${NC}"

    local slow_query_count=$(run_query "
        SELECT count(*)
        FROM pg_stat_activity
        WHERE state = 'active'
        AND now() - query_start > interval '$WARNING_SLOW_QUERY_SECONDS seconds';
    ")

    echo "Slow Queries (>$WARNING_SLOW_QUERY_SECONDS seconds): $slow_query_count"

    if [ "$slow_query_count" -gt 0 ]; then
        alert "WARNING" "Found $slow_query_count slow queries"

        echo -e "\n${YELLOW}Slow Query Details:${NC}"
        run_query "
            SELECT
                pid,
                usename,
                EXTRACT(EPOCH FROM (now() - query_start)) AS duration_seconds,
                state,
                LEFT(query, 100) AS query_preview
            FROM pg_stat_activity
            WHERE state = 'active'
            AND now() - query_start > interval '$WARNING_SLOW_QUERY_SECONDS seconds'
            ORDER BY query_start ASC;
        " | column -t -s '|'
    else
        echo -e "${GREEN}No slow queries detected${NC}"
    fi
}

check_index_usage() {
    echo -e "\n${BLUE}=== Index Usage Statistics (Top 10) ===${NC}"

    run_query "
        SELECT
            schemaname,
            tablename,
            indexname,
            idx_scan AS scans,
            idx_tup_read AS tuples_read,
            idx_tup_fetch AS tuples_fetched
        FROM pg_stat_user_indexes
        WHERE schemaname IN ('tenant_default', 'public')
        ORDER BY idx_scan DESC
        LIMIT 10;
    " | column -t -s '|'

    # Check for unused indexes
    local unused_indexes=$(run_query "
        SELECT count(*)
        FROM pg_stat_user_indexes
        WHERE schemaname IN ('tenant_default', 'public')
        AND idx_scan = 0
        AND indexrelname NOT LIKE '%_pkey';
    ")

    if [ "$unused_indexes" -gt 0 ]; then
        alert "INFO" "Found $unused_indexes unused indexes (may be recently created)"
    fi
}

check_table_sizes() {
    echo -e "\n${BLUE}=== Table Sizes (Top 10) ===${NC}"

    run_query "
        SELECT
            schemaname,
            tablename,
            pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
            pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
            pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS indexes_size
        FROM pg_tables
        WHERE schemaname IN ('tenant_default', 'public')
        ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
        LIMIT 10;
    " | column -t -s '|'
}

check_database_size() {
    echo -e "\n${BLUE}=== Database Size ===${NC}"

    local db_size=$(run_query "SELECT pg_size_pretty(pg_database_size('$DB_NAME'));")
    echo "Database Size: $db_size"

    # Check disk usage
    local disk_usage=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
    echo "Disk Usage: $disk_usage%"

    if [ "$disk_usage" -ge "$CRITICAL_DISK_USAGE_PERCENT" ]; then
        alert "CRITICAL" "Disk usage critical: ${disk_usage}%"
    elif [ "$disk_usage" -ge "$WARNING_DISK_USAGE_PERCENT" ]; then
        alert "WARNING" "Disk usage high: ${disk_usage}%"
    fi
}

check_cache_hit_ratio() {
    echo -e "\n${BLUE}=== Cache Hit Ratio ===${NC}"

    local cache_hit_ratio=$(run_query "
        SELECT
            ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2) AS cache_hit_ratio
        FROM pg_stat_database
        WHERE datname = '$DB_NAME';
    ")

    echo "Cache Hit Ratio: ${cache_hit_ratio}%"

    # Cache hit ratio should be > 90%
    local cache_hit_int=${cache_hit_ratio%.*}
    if [ "$cache_hit_int" -lt 90 ]; then
        alert "WARNING" "Low cache hit ratio: ${cache_hit_ratio}% (expected > 90%)"
    fi
}

check_replication_lag() {
    echo -e "\n${BLUE}=== Replication Status ===${NC}"

    # Check if this is a primary or replica
    local is_in_recovery=$(run_query "SELECT pg_is_in_recovery();")

    if [ "$is_in_recovery" = "t" ]; then
        echo "Role: Replica"

        # Check replication lag
        local lag=$(run_query "
            SELECT EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp())) AS lag_seconds;
        ")

        echo "Replication Lag: ${lag} seconds"

        if (( $(echo "$lag > 60" | bc -l) )); then
            alert "WARNING" "Replication lag is high: ${lag} seconds"
        fi
    else
        echo "Role: Primary"

        # Check connected replicas
        local replica_count=$(run_query "SELECT count(*) FROM pg_stat_replication;")
        echo "Connected Replicas: $replica_count"
    fi
}

check_long_running_transactions() {
    echo -e "\n${BLUE}=== Long Running Transactions ===${NC}"

    local long_transactions=$(run_query "
        SELECT count(*)
        FROM pg_stat_activity
        WHERE state = 'idle in transaction'
        AND now() - xact_start > interval '5 minutes';
    ")

    echo "Long Running Transactions (>5 min): $long_transactions"

    if [ "$long_transactions" -gt 0 ]; then
        alert "WARNING" "Found $long_transactions long-running transactions"

        run_query "
            SELECT
                pid,
                usename,
                EXTRACT(EPOCH FROM (now() - xact_start)) AS transaction_duration_seconds,
                state,
                LEFT(query, 100) AS query_preview
            FROM pg_stat_activity
            WHERE state = 'idle in transaction'
            AND now() - xact_start > interval '5 minutes'
            ORDER BY xact_start ASC;
        " | column -t -s '|'
    else
        echo -e "${GREEN}No long-running transactions${NC}"
    fi
}

check_migration_progress() {
    echo -e "\n${BLUE}=== Migration Status ===${NC}"

    # Check if __EFMigrationsHistory exists
    local migrations_exist=$(run_query "
        SELECT EXISTS (
            SELECT FROM information_schema.tables
            WHERE table_schema = 'tenant_default'
            AND table_name = '__EFMigrationsHistory'
        );
    ")

    if [ "$migrations_exist" = "t" ]; then
        echo "Last 5 Migrations:"
        run_query "
            SELECT
                \"MigrationId\",
                \"ProductVersion\"
            FROM tenant_default.\"__EFMigrationsHistory\"
            ORDER BY \"MigrationId\" DESC
            LIMIT 5;
        " | column -t -s '|'
    else
        echo "No migrations table found"
    fi
}

check_constraint_violations() {
    echo -e "\n${BLUE}=== Constraint Violation Check ===${NC}"

    # This is a passive check - constraints prevent violations at write time
    # We'll check for any ERROR logs related to constraints
    echo "Checking for recent constraint-related errors..."

    # Check pg_stat_database for errors
    local db_conflicts=$(run_query "
        SELECT
            conflicts,
            deadlocks
        FROM pg_stat_database
        WHERE datname = '$DB_NAME';
    ")

    echo "Database Conflicts/Deadlocks: $db_conflicts"
}

################################################################################
# Main Monitoring Loop
################################################################################

monitor_continuous() {
    echo -e "${GREEN}Starting Database Health Monitoring${NC}"
    echo "Database: $DB_NAME"
    echo "Interval: ${INTERVAL}s"
    echo "Press Ctrl+C to stop"
    echo ""

    while true; do
        clear
        echo "================================================================================"
        echo "DATABASE HEALTH MONITOR - $(date)"
        echo "================================================================================"

        check_active_connections
        check_locks
        check_slow_queries
        check_long_running_transactions
        check_cache_hit_ratio
        check_database_size
        check_replication_lag
        check_migration_progress

        echo -e "\n${BLUE}Next check in ${INTERVAL}s...${NC}"
        sleep "$INTERVAL"
    done
}

monitor_once() {
    echo "================================================================================"
    echo "DATABASE HEALTH REPORT - $(date)"
    echo "================================================================================"

    check_active_connections
    check_locks
    check_slow_queries
    check_long_running_transactions
    check_index_usage
    check_table_sizes
    check_cache_hit_ratio
    check_database_size
    check_replication_lag
    check_migration_progress
    check_constraint_violations

    echo ""
    echo "================================================================================"
    echo "REPORT COMPLETE"
    echo "================================================================================"
}

################################################################################
# Script Entry Point
################################################################################

if [ "${CONTINUOUS:-false}" = "true" ]; then
    monitor_continuous
else
    monitor_once
fi
