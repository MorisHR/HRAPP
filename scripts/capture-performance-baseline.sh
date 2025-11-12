#!/bin/bash

################################################################################
# Performance Baseline Capture Script
# Purpose: Capture database performance metrics before migrations
# Usage: ./capture-performance-baseline.sh [database_name] [output_file]
# Example: ./capture-performance-baseline.sh hrms_db baseline-2025-11-12.json
################################################################################

set -euo pipefail

# Configuration
DB_NAME="${1:-hrms_db}"
OUTPUT_FILE="${2:-baseline-$(date +%Y%m%d-%H%M%S).json}"
DB_USER="${3:-postgres}"
DB_HOST="${4:-localhost}"
DB_PORT="${5:-5432}"

# Color codes
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}================================================================================${NC}"
echo -e "${BLUE}DATABASE PERFORMANCE BASELINE CAPTURE${NC}"
echo -e "${BLUE}================================================================================${NC}"
echo ""
echo "Database: $DB_NAME"
echo "Output: $OUTPUT_FILE"
echo "Timestamp: $(date)"
echo ""

################################################################################
# Helper Functions
################################################################################

run_query() {
    local query="$1"
    PGPASSWORD="${DB_PASSWORD:-}" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "$query" 2>/dev/null
}

run_query_json() {
    local query="$1"
    local label="$2"
    echo "  \"$label\": $(run_query "$query"),"
}

################################################################################
# Start JSON Output
################################################################################

{
    echo "{"
    echo "  \"metadata\": {"
    echo "    \"database\": \"$DB_NAME\","
    echo "    \"timestamp\": \"$(date -Iseconds)\","
    echo "    \"capture_date\": \"$(date +%Y-%m-%d)\","
    echo "    \"capture_time\": \"$(date +%H:%M:%S)\","
    echo "    \"hostname\": \"$(hostname)\","
    echo "    \"postgresql_version\": \"$(run_query "SELECT version();")\""
    echo "  },"

    ################################################################################
    # Connection Metrics
    ################################################################################

    echo "  \"connections\": {"
    echo -n "    \"total\": "
    run_query "SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME';"

    echo -n "    \"active\": "
    run_query "SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND state = 'active';"

    echo -n "    \"idle\": "
    run_query "SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND state = 'idle';"

    echo -n "    \"idle_in_transaction\": "
    run_query "SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME' AND state = 'idle in transaction';"

    echo -n "    \"max_connections\": "
    run_query "SHOW max_connections;" | tr -d '\n'
    echo ""

    echo "  },"

    ################################################################################
    # Database Size Metrics
    ################################################################################

    echo "  \"database_size\": {"
    echo -n "    \"total_bytes\": "
    run_query "SELECT pg_database_size('$DB_NAME');"

    echo -n "    \"total_pretty\": \""
    run_query "SELECT pg_size_pretty(pg_database_size('$DB_NAME'));" | tr -d '\n'
    echo "\""

    echo "  },"

    ################################################################################
    # Cache Performance
    ################################################################################

    echo "  \"cache_performance\": {"
    echo -n "    \"hit_ratio_percent\": "
    run_query "SELECT ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2) FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"blocks_hit\": "
    run_query "SELECT sum(blks_hit) FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"blocks_read\": "
    run_query "SELECT sum(blks_read) FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"shared_buffers\": \""
    run_query "SHOW shared_buffers;" | tr -d '\n'
    echo "\""

    echo "  },"

    ################################################################################
    # Transaction Metrics
    ################################################################################

    echo "  \"transactions\": {"
    echo -n "    \"commits\": "
    run_query "SELECT xact_commit FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"rollbacks\": "
    run_query "SELECT xact_rollback FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"conflicts\": "
    run_query "SELECT conflicts FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo -n "    \"deadlocks\": "
    run_query "SELECT deadlocks FROM pg_stat_database WHERE datname = '$DB_NAME';"

    echo "  },"

    ################################################################################
    # Table Sizes (Top 20)
    ################################################################################

    echo "  \"table_sizes\": ["

    first=true
    while IFS='|' read -r schema table total_size table_size index_size row_count; do
        if [ "$first" = true ]; then
            first=false
        else
            echo ","
        fi
        echo "    {"
        echo "      \"schema\": \"$schema\","
        echo "      \"table\": \"$table\","
        echo "      \"total_size_bytes\": $total_size,"
        echo -n "      \"table_size_bytes\": $table_size,"
        echo -n "      \"index_size_bytes\": $index_size,"
        echo -n "      \"row_count\": $row_count"
        echo -n "    }"
    done < <(run_query "
        SELECT
            schemaname,
            tablename,
            pg_total_relation_size(schemaname||'.'||tablename),
            pg_relation_size(schemaname||'.'||tablename),
            pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename),
            COALESCE(n_live_tup, 0)
        FROM pg_tables t
        LEFT JOIN pg_stat_user_tables s ON s.schemaname = t.schemaname AND s.tablename = t.tablename
        WHERE t.schemaname IN ('tenant_default', 'public')
        ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
        LIMIT 20;
    ")

    echo ""
    echo "  ],"

    ################################################################################
    # Index Statistics (Top 30)
    ################################################################################

    echo "  \"index_statistics\": ["

    first=true
    while IFS='|' read -r schema table index scans tuples_read tuples_fetched size; do
        if [ "$first" = true ]; then
            first=false
        else
            echo ","
        fi
        echo "    {"
        echo "      \"schema\": \"$schema\","
        echo "      \"table\": \"$table\","
        echo "      \"index\": \"$index\","
        echo "      \"scans\": $scans,"
        echo "      \"tuples_read\": $tuples_read,"
        echo "      \"tuples_fetched\": $tuples_fetched,"
        echo -n "      \"size_bytes\": $size"
        echo -n "    }"
    done < <(run_query "
        SELECT
            schemaname,
            tablename,
            indexrelname,
            COALESCE(idx_scan, 0),
            COALESCE(idx_tup_read, 0),
            COALESCE(idx_tup_fetch, 0),
            pg_relation_size(indexrelid)
        FROM pg_stat_user_indexes
        WHERE schemaname IN ('tenant_default', 'public')
        ORDER BY idx_scan DESC NULLS LAST
        LIMIT 30;
    ")

    echo ""
    echo "  ],"

    ################################################################################
    # Query Performance Benchmarks
    ################################################################################

    echo "  \"query_benchmarks\": ["

    # Benchmark 1: Employee lookup by ID
    echo "    {"
    echo "      \"name\": \"employee_lookup_by_id\","
    echo "      \"description\": \"Lookup employee by primary key\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"FirstName\", \"LastName\" FROM tenant_default.\"Employees\" WHERE \"Id\" = '00000000-0000-0000-0000-000000000000' LIMIT 1;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 2: Employee lookup by National ID (will improve after migration)
    echo "    {"
    echo "      \"name\": \"employee_lookup_by_national_id\","
    echo "      \"description\": \"Lookup employee by National ID (pre-index)\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"FirstName\", \"LastName\" FROM tenant_default.\"Employees\" WHERE \"NationalIdCard\" = 'test-id' AND \"IsDeleted\" = false LIMIT 1;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 3: Payroll cycle lookup (will improve after migration)
    echo "    {"
    echo "      \"name\": \"payroll_cycle_lookup\","
    echo "      \"description\": \"Lookup payroll cycle by year/month\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"Status\" FROM tenant_default.\"PayrollCycles\" WHERE \"Year\" = 2025 AND \"Month\" = 1 AND \"IsDeleted\" = false LIMIT 10;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 4: Attendance query (will improve after migration)
    echo "    {"
    echo "      \"name\": \"attendance_30day_lookup\","
    echo "      \"description\": \"Lookup 30 days of attendance for an employee\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"Date\", \"Status\", \"WorkingHours\" FROM tenant_default.\"Attendances\" WHERE \"EmployeeId\" = '00000000-0000-0000-0000-000000000000' AND \"Date\" >= CURRENT_DATE - INTERVAL '30 days' AND \"IsDeleted\" = false ORDER BY \"Date\" DESC LIMIT 30;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 5: Leave balance lookup (will improve after migration)
    echo "    {"
    echo "      \"name\": \"leave_balance_lookup\","
    echo "      \"description\": \"Lookup leave balances for employee by year\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"LeaveTypeId\", \"TotalEntitlement\", \"UsedDays\" FROM tenant_default.\"LeaveBalances\" WHERE \"EmployeeId\" = '00000000-0000-0000-0000-000000000000' AND \"Year\" = 2025 AND \"IsDeleted\" = false;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 6: Employee search by name
    echo "    {"
    echo "      \"name\": \"employee_search_by_name\","
    echo "      \"description\": \"Search active employees by first/last name\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"FirstName\", \"LastName\", \"Email\" FROM tenant_default.\"Employees\" WHERE \"FirstName\" LIKE 'John%' AND \"LastName\" LIKE 'Smith%' AND \"IsActive\" = true AND \"IsDeleted\" = false LIMIT 20;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    },"

    # Benchmark 7: Biometric punch processing query
    echo "    {"
    echo "      \"name\": \"biometric_punch_processing\","
    echo "      \"description\": \"Query pending biometric punches for processing\","

    start=$(date +%s%N)
    run_query "SELECT \"Id\", \"EmployeeId\", \"PunchTime\" FROM tenant_default.\"BiometricPunchRecords\" WHERE \"ProcessingStatus\" = 'Pending' AND \"IsDeleted\" = false ORDER BY \"PunchTime\" DESC LIMIT 100;" > /dev/null 2>&1 || true
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))

    echo "      \"duration_ms\": $duration"
    echo "    }"

    echo "  ],"

    ################################################################################
    # Table Statistics
    ################################################################################

    echo "  \"table_statistics\": ["

    first=true
    while IFS='|' read -r schema table live_tuples dead_tuples last_vacuum last_autovacuum last_analyze; do
        if [ "$first" = true ]; then
            first=false
        else
            echo ","
        fi
        echo "    {"
        echo "      \"schema\": \"$schema\","
        echo "      \"table\": \"$table\","
        echo "      \"live_tuples\": $live_tuples,"
        echo "      \"dead_tuples\": $dead_tuples,"
        echo "      \"last_vacuum\": \"$last_vacuum\","
        echo "      \"last_autovacuum\": \"$last_autovacuum\","
        echo -n "      \"last_analyze\": \"$last_analyze\""
        echo -n "    }"
    done < <(run_query "
        SELECT
            schemaname,
            tablename,
            COALESCE(n_live_tup, 0),
            COALESCE(n_dead_tup, 0),
            COALESCE(last_vacuum::text, 'never'),
            COALESCE(last_autovacuum::text, 'never'),
            COALESCE(last_analyze::text, 'never')
        FROM pg_stat_user_tables
        WHERE schemaname IN ('tenant_default', 'public')
        ORDER BY schemaname, tablename;
    ")

    echo ""
    echo "  ],"

    ################################################################################
    # Lock Statistics
    ################################################################################

    echo "  \"locks\": {"
    echo -n "    \"total_locks\": "
    run_query "SELECT count(*) FROM pg_locks;"

    echo -n "    \"waiting_locks\": "
    run_query "SELECT count(*) FROM pg_locks WHERE NOT granted;"

    echo -n "    \"exclusive_locks\": "
    run_query "SELECT count(*) FROM pg_locks WHERE mode = 'AccessExclusiveLock';"

    echo "  },"

    ################################################################################
    # Replication Status
    ################################################################################

    echo "  \"replication\": {"
    echo -n "    \"is_replica\": "
    run_query "SELECT pg_is_in_recovery();"

    echo -n "    \"replica_count\": "
    run_query "SELECT count(*) FROM pg_stat_replication;" || echo "0"

    echo "  },"

    ################################################################################
    # System Configuration
    ################################################################################

    echo "  \"configuration\": {"
    echo -n "    \"max_connections\": "
    run_query "SHOW max_connections;"

    echo -n "    \"shared_buffers\": \""
    run_query "SHOW shared_buffers;" | tr -d '\n'
    echo "\","

    echo -n "    \"effective_cache_size\": \""
    run_query "SHOW effective_cache_size;" | tr -d '\n'
    echo "\","

    echo -n "    \"maintenance_work_mem\": \""
    run_query "SHOW maintenance_work_mem;" | tr -d '\n'
    echo "\","

    echo -n "    \"work_mem\": \""
    run_query "SHOW work_mem;" | tr -d '\n'
    echo "\","

    echo -n "    \"checkpoint_completion_target\": \""
    run_query "SHOW checkpoint_completion_target;" | tr -d '\n'
    echo "\""

    echo "  },"

    ################################################################################
    # Migration History
    ################################################################################

    echo "  \"migration_history\": ["

    first=true
    while IFS='|' read -r migration_id product_version; do
        if [ "$first" = true ]; then
            first=false
        else
            echo ","
        fi
        echo "    {"
        echo "      \"migration_id\": \"$migration_id\","
        echo -n "      \"product_version\": \"$product_version\""
        echo -n "    }"
    done < <(run_query "
        SELECT \"MigrationId\", \"ProductVersion\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        ORDER BY \"MigrationId\" DESC
        LIMIT 10;
    " 2>/dev/null || echo "")

    echo ""
    echo "  ]"

    echo "}"

} > "$OUTPUT_FILE"

################################################################################
# Summary
################################################################################

echo -e "${GREEN}================================================================================${NC}"
echo -e "${GREEN}BASELINE CAPTURE COMPLETE${NC}"
echo -e "${GREEN}================================================================================${NC}"
echo ""
echo "Baseline saved to: $OUTPUT_FILE"
echo ""
echo "Key Metrics Summary:"
echo "-------------------"

# Extract and display key metrics
echo -n "Database Size: "
grep "\"total_pretty\"" "$OUTPUT_FILE" | cut -d'"' -f4

echo -n "Cache Hit Ratio: "
grep "\"hit_ratio_percent\"" "$OUTPUT_FILE" | cut -d':' -f2 | tr -d ' ,'
echo "%"

echo -n "Total Connections: "
grep "\"total\":" "$OUTPUT_FILE" | head -1 | cut -d':' -f2 | tr -d ' ,'

echo -n "Active Connections: "
grep "\"active\":" "$OUTPUT_FILE" | head -1 | cut -d':' -f2 | tr -d ' ,'

echo ""
echo -e "${YELLOW}Query Benchmarks (Pre-Migration):${NC}"
echo "-------------------"
grep -A 1 "\"name\":" "$OUTPUT_FILE" | grep -A 1 "employee_lookup_by_national_id" | grep "duration_ms" | sed 's/.*: /Employee by National ID: /; s/,/ ms/'
grep -A 1 "\"name\":" "$OUTPUT_FILE" | grep -A 1 "payroll_cycle_lookup" | grep "duration_ms" | sed 's/.*: /Payroll Cycle Lookup: /; s/,/ ms/'
grep -A 1 "\"name\":" "$OUTPUT_FILE" | grep -A 1 "attendance_30day_lookup" | grep "duration_ms" | sed 's/.*: /30-Day Attendance: /; s/,/ ms/'
grep -A 1 "\"name\":" "$OUTPUT_FILE" | grep -A 1 "leave_balance_lookup" | grep "duration_ms" | sed 's/.*: /Leave Balance Lookup: /; s/,/ ms/'

echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Review baseline file: $OUTPUT_FILE"
echo "2. Store baseline in version control"
echo "3. After migrations, capture new baseline for comparison"
echo "4. Use 'diff' or JSON comparison tool to analyze changes"
echo ""
echo "Comparison command after migration:"
echo "  ./capture-performance-baseline.sh $DB_NAME baseline-after-migration.json"
echo ""
