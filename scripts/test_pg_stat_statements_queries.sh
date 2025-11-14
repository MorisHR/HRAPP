#!/bin/bash
#
# Test and Demonstrate pg_stat_statements Queries
#
# This script demonstrates various useful queries for pg_stat_statements
# and helps you understand the data available.
#
# Usage: bash test_pg_stat_statements_queries.sh [database_name]
#

# Configuration
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
PGPASSWORD="${PGPASSWORD:-postgres}"
PGDATABASE="${1:-postgres}"

export PGPASSWORD

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

print_header() {
    echo ""
    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo ""
}

print_query_title() {
    echo ""
    echo -e "${MAGENTA}>>> $1${NC}"
    echo ""
}

print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to run query and display results
run_query() {
    local title="$1"
    local query="$2"

    print_query_title "$title"
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -c "$query" 2>&1
}

# Function to check if extension is installed
check_extension() {
    INSTALLED=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -t -A -c "SELECT COUNT(*) FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null || echo "0")

    if [ "$INSTALLED" = "1" ]; then
        return 0
    else
        return 1
    fi
}

# Main execution
clear
print_header "pg_stat_statements Query Demonstration"

print_info "Database: $PGDATABASE"
print_info "Host: $PGHOST:$PGPORT"
print_info "User: $PGUSER"
echo ""

# Check connection
if ! pg_isready -h "$PGHOST" -p "$PGPORT" >/dev/null 2>&1; then
    print_error "Cannot connect to PostgreSQL at $PGHOST:$PGPORT"
    exit 1
fi

print_success "Connected to PostgreSQL"

# Check if extension is installed
if ! check_extension; then
    print_error "pg_stat_statements extension is not installed in database: $PGDATABASE"
    echo ""
    print_info "To install:"
    echo "  PGPASSWORD=postgres psql -h localhost -U postgres -d $PGDATABASE -c \"CREATE EXTENSION pg_stat_statements;\""
    exit 1
fi

print_success "pg_stat_statements extension is installed"

# Display menu
while true; do
    print_header "Available Queries"

    echo "Performance Analysis:"
    echo "  1)  Top 10 slowest queries (by average execution time)"
    echo "  2)  Top 10 most frequently executed queries"
    echo "  3)  Top 10 queries by total execution time"
    echo "  4)  Top 10 queries with highest execution time variance"
    echo "  5)  Queries running longer than 1 second on average"
    echo ""
    echo "I/O Analysis:"
    echo "  6)  Top 10 queries with most I/O operations"
    echo "  7)  Top 10 queries with poorest cache hit ratio"
    echo "  8)  Top 10 queries with most shared buffer reads"
    echo "  9)  Top 10 queries with most temp file usage"
    echo ""
    echo "Resource Usage:"
    echo "  10) Memory-intensive queries (temp buffers)"
    echo "  11) Queries with longest planning time"
    echo "  12) Queries with highest planning/execution time ratio"
    echo ""
    echo "Statistics:"
    echo "  13) Overall statistics summary"
    echo "  14) Query count by execution time buckets"
    echo "  15) Database activity overview"
    echo "  16) All available columns and data types"
    echo ""
    echo "Maintenance:"
    echo "  17) View current pg_stat_statements settings"
    echo "  18) Check extension version and info"
    echo "  19) Generate performance report (all queries)"
    echo "  20) Reset statistics (WARNING: clears all data)"
    echo ""
    echo "  0)  Exit"
    echo ""

    read -p "Enter your choice (0-20): " CHOICE

    case $CHOICE in
        1)
            run_query "Top 10 Slowest Queries (Average Execution Time)" "
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    max_exec_time::numeric(10,2) as max_ms,
    min_exec_time::numeric(10,2) as min_ms,
    stddev_exec_time::numeric(10,2) as stddev_ms,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 10;"
            ;;

        2)
            run_query "Top 10 Most Frequently Executed Queries" "
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    total_exec_time::numeric(10,2) as total_ms,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY calls DESC
LIMIT 10;"
            ;;

        3)
            run_query "Top 10 Queries by Total Execution Time" "
SELECT
    calls,
    total_exec_time::numeric(10,2) as total_ms,
    mean_exec_time::numeric(10,2) as avg_ms,
    (total_exec_time / sum(total_exec_time) OVER ()) * 100 as pct_total,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY total_exec_time DESC
LIMIT 10;"
            ;;

        4)
            run_query "Top 10 Queries with Highest Execution Time Variance" "
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    stddev_exec_time::numeric(10,2) as stddev_ms,
    max_exec_time::numeric(10,2) as max_ms,
    min_exec_time::numeric(10,2) as min_ms,
    (stddev_exec_time / NULLIF(mean_exec_time, 0) * 100)::numeric(10,2) as variance_pct,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND calls > 10
ORDER BY stddev_exec_time DESC
LIMIT 10;"
            ;;

        5)
            run_query "Queries Running Longer Than 1 Second (Average)" "
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    max_exec_time::numeric(10,2) as max_ms,
    total_exec_time::numeric(10,2) as total_ms,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE mean_exec_time > 1000
  AND query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC;"
            ;;

        6)
            run_query "Top 10 Queries with Most I/O Operations" "
SELECT
    calls,
    (shared_blks_read + shared_blks_written + local_blks_read + local_blks_written) as total_blocks,
    shared_blks_read,
    shared_blks_written,
    local_blks_read,
    local_blks_written,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY (shared_blks_read + shared_blks_written + local_blks_read + local_blks_written) DESC
LIMIT 10;"
            ;;

        7)
            run_query "Top 10 Queries with Poorest Cache Hit Ratio" "
SELECT
    calls,
    shared_blks_hit,
    shared_blks_read,
    CASE
        WHEN (shared_blks_hit + shared_blks_read) > 0
        THEN (shared_blks_hit::float / (shared_blks_hit + shared_blks_read) * 100)::numeric(5,2)
        ELSE 100
    END as cache_hit_ratio,
    mean_exec_time::numeric(10,2) as avg_ms,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND (shared_blks_hit + shared_blks_read) > 100
ORDER BY cache_hit_ratio ASC
LIMIT 10;"
            ;;

        8)
            run_query "Top 10 Queries with Most Shared Buffer Reads" "
SELECT
    calls,
    shared_blks_read,
    shared_blks_hit,
    (shared_blks_read::float / NULLIF(calls, 0))::numeric(10,2) as avg_reads_per_call,
    mean_exec_time::numeric(10,2) as avg_ms,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY shared_blks_read DESC
LIMIT 10;"
            ;;

        9)
            run_query "Top 10 Queries with Most Temp File Usage" "
SELECT
    calls,
    temp_blks_written,
    temp_blks_read,
    (temp_blks_written * 8192 / 1024 / 1024)::numeric(10,2) as temp_mb_written,
    mean_exec_time::numeric(10,2) as avg_ms,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND temp_blks_written > 0
ORDER BY temp_blks_written DESC
LIMIT 10;"
            ;;

        10)
            run_query "Memory-Intensive Queries (Temp Buffers)" "
SELECT
    calls,
    (local_blks_read + local_blks_written) as local_blocks,
    (temp_blks_read + temp_blks_written) as temp_blocks,
    ((local_blks_read + local_blks_written + temp_blks_read + temp_blks_written) * 8192 / 1024 / 1024)::numeric(10,2) as total_temp_mb,
    mean_exec_time::numeric(10,2) as avg_ms,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND (local_blks_read + local_blks_written + temp_blks_read + temp_blks_written) > 0
ORDER BY (local_blks_read + local_blks_written + temp_blks_read + temp_blks_written) DESC
LIMIT 10;"
            ;;

        11)
            run_query "Queries with Longest Planning Time" "
SELECT
    calls,
    mean_plan_time::numeric(10,2) as avg_plan_ms,
    mean_exec_time::numeric(10,2) as avg_exec_ms,
    (mean_plan_time + mean_exec_time)::numeric(10,2) as total_avg_ms,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND mean_plan_time > 0
ORDER BY mean_plan_time DESC
LIMIT 10;"
            ;;

        12)
            run_query "Queries with Highest Planning/Execution Time Ratio" "
SELECT
    calls,
    mean_plan_time::numeric(10,2) as avg_plan_ms,
    mean_exec_time::numeric(10,2) as avg_exec_ms,
    (mean_plan_time / NULLIF(mean_exec_time, 0) * 100)::numeric(10,2) as plan_exec_ratio_pct,
    LEFT(query, 80) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND mean_plan_time > 0
  AND mean_exec_time > 0
ORDER BY (mean_plan_time / NULLIF(mean_exec_time, 0)) DESC
LIMIT 10;"
            ;;

        13)
            run_query "Overall Statistics Summary" "
SELECT
    COUNT(*) as total_queries,
    SUM(calls) as total_executions,
    SUM(total_exec_time)::numeric(10,2) as total_exec_time_ms,
    AVG(mean_exec_time)::numeric(10,2) as avg_exec_time_ms,
    MAX(mean_exec_time)::numeric(10,2) as max_avg_exec_time_ms,
    MIN(mean_exec_time)::numeric(10,2) as min_avg_exec_time_ms,
    SUM(shared_blks_read + shared_blks_written) as total_io_blocks,
    COUNT(*) FILTER (WHERE mean_exec_time > 1000) as queries_over_1s,
    COUNT(*) FILTER (WHERE mean_exec_time > 100) as queries_over_100ms
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%';"
            ;;

        14)
            run_query "Query Count by Execution Time Buckets" "
SELECT
    CASE
        WHEN mean_exec_time < 1 THEN '< 1ms'
        WHEN mean_exec_time < 10 THEN '1-10ms'
        WHEN mean_exec_time < 100 THEN '10-100ms'
        WHEN mean_exec_time < 1000 THEN '100ms-1s'
        WHEN mean_exec_time < 10000 THEN '1-10s'
        ELSE '> 10s'
    END as execution_time_bucket,
    COUNT(*) as query_count,
    SUM(calls) as total_calls,
    (COUNT(*) * 100.0 / SUM(COUNT(*)) OVER ())::numeric(5,2) as pct_of_total
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
GROUP BY execution_time_bucket
ORDER BY
    CASE execution_time_bucket
        WHEN '< 1ms' THEN 1
        WHEN '1-10ms' THEN 2
        WHEN '10-100ms' THEN 3
        WHEN '100ms-1s' THEN 4
        WHEN '1-10s' THEN 5
        ELSE 6
    END;"
            ;;

        15)
            run_query "Database Activity Overview" "
SELECT
    'Total Unique Queries' as metric,
    COUNT(*)::text as value
FROM pg_stat_statements
UNION ALL
SELECT
    'Total Query Executions',
    SUM(calls)::text
FROM pg_stat_statements
UNION ALL
SELECT
    'Total Execution Time (hours)',
    (SUM(total_exec_time) / 1000 / 60 / 60)::numeric(10,2)::text
FROM pg_stat_statements
UNION ALL
SELECT
    'Average Queries per Second',
    (SUM(calls) / GREATEST(EXTRACT(EPOCH FROM (now() - stats_reset)), 1))::numeric(10,2)::text
FROM pg_stat_statements, pg_stat_database
WHERE datname = current_database()
LIMIT 1;"
            ;;

        16)
            run_query "All Available Columns and Data Types" "
SELECT
    column_name,
    data_type,
    column_default
FROM information_schema.columns
WHERE table_name = 'pg_stat_statements'
ORDER BY ordinal_position;"
            ;;

        17)
            run_query "Current pg_stat_statements Settings" "
SELECT
    name,
    setting,
    unit,
    context,
    short_desc
FROM pg_settings
WHERE name LIKE 'pg_stat_statements%'
ORDER BY name;"
            ;;

        18)
            run_query "Extension Version and Info" "
SELECT
    e.extname,
    e.extversion,
    n.nspname as schema,
    e.extrelocatable,
    e.extconfig
FROM pg_extension e
JOIN pg_namespace n ON e.extnamespace = n.oid
WHERE e.extname = 'pg_stat_statements';"
            ;;

        19)
            print_query_title "Generating Comprehensive Performance Report"
            REPORT_FILE="/tmp/pg_stat_statements_report_$(date +%Y%m%d_%H%M%S).txt"

            {
                echo "=================================="
                echo "pg_stat_statements Performance Report"
                echo "Database: $PGDATABASE"
                echo "Generated: $(date)"
                echo "=================================="
                echo ""

                echo "=== Overall Statistics ==="
                psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -c "
SELECT
    COUNT(*) as total_queries,
    SUM(calls) as total_executions,
    SUM(total_exec_time)::numeric(10,2) as total_exec_time_ms,
    AVG(mean_exec_time)::numeric(10,2) as avg_exec_time_ms
FROM pg_stat_statements;"

                echo ""
                echo "=== Top 10 Slowest Queries ==="
                psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -c "
SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC LIMIT 10;"

                echo ""
                echo "=== Top 10 Most Frequent Queries ==="
                psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -c "
SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY calls DESC LIMIT 10;"

            } > "$REPORT_FILE"

            print_success "Report generated: $REPORT_FILE"
            echo ""
            print_info "Viewing report..."
            cat "$REPORT_FILE"
            ;;

        20)
            echo ""
            print_error "WARNING: This will reset ALL pg_stat_statements data!"
            print_error "All statistics will be lost and cannot be recovered."
            echo ""
            read -p "Are you absolutely sure? Type 'YES' to confirm: " CONFIRM

            if [ "$CONFIRM" = "YES" ]; then
                run_query "Resetting pg_stat_statements" "SELECT pg_stat_statements_reset();"
                print_success "Statistics reset successfully"
            else
                print_info "Reset cancelled"
            fi
            ;;

        0)
            print_info "Exiting..."
            exit 0
            ;;

        *)
            print_error "Invalid choice. Please enter a number between 0 and 20."
            ;;
    esac

    echo ""
    read -p "Press Enter to continue..."
done
