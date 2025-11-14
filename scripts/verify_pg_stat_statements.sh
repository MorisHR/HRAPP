#!/bin/bash
#
# Verify pg_stat_statements Installation and Configuration
#
# This script performs comprehensive verification of pg_stat_statements setup
#
# Usage: bash verify_pg_stat_statements.sh
#

# Configuration
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
PGPASSWORD="${PGPASSWORD:-postgres}"

export PGPASSWORD

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Counters
PASS_COUNT=0
FAIL_COUNT=0
WARN_COUNT=0

# Function to print colored output
print_pass() {
    echo -e "${GREEN}[PASS]${NC} $1"
    PASS_COUNT=$((PASS_COUNT + 1))
}

print_fail() {
    echo -e "${RED}[FAIL]${NC} $1"
    FAIL_COUNT=$((FAIL_COUNT + 1))
}

print_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
    WARN_COUNT=$((WARN_COUNT + 1))
}

print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_header() {
    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}========================================${NC}"
}

print_section() {
    echo -e "${BLUE}>>> $1${NC}"
}

# Main execution
clear
print_header "pg_stat_statements Verification Script"
echo ""

# Test 1: PostgreSQL Connection
print_section "Test 1: PostgreSQL Connection"
if pg_isready -h "$PGHOST" -p "$PGPORT" >/dev/null 2>&1; then
    print_pass "PostgreSQL is running and accepting connections"
    VERSION=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT version();" 2>/dev/null)
    echo "  Version: $VERSION"
else
    print_fail "Cannot connect to PostgreSQL at $PGHOST:$PGPORT"
    exit 1
fi
echo ""

# Test 2: shared_preload_libraries Configuration
print_section "Test 2: shared_preload_libraries Configuration"
PRELOAD_LIBS=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SHOW shared_preload_libraries;" 2>/dev/null || echo "ERROR")

if [ "$PRELOAD_LIBS" = "ERROR" ]; then
    print_fail "Failed to query shared_preload_libraries"
elif echo "$PRELOAD_LIBS" | grep -q "pg_stat_statements"; then
    print_pass "pg_stat_statements is in shared_preload_libraries"
    echo "  Current value: $PRELOAD_LIBS"
else
    print_fail "pg_stat_statements is NOT in shared_preload_libraries"
    echo "  Current value: '$PRELOAD_LIBS'"
    echo ""
    print_info "Action required: Run configuration script and restart PostgreSQL"
    echo "  bash /workspaces/HRAPP/scripts/update_postgresql_config.sh"
    echo "  sudo service postgresql restart"
fi
echo ""

# Test 3: Extension Availability
print_section "Test 3: Extension Availability"
AVAILABLE=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT COUNT(*) FROM pg_available_extensions WHERE name = 'pg_stat_statements';" 2>/dev/null || echo "0")

if [ "$AVAILABLE" = "1" ]; then
    print_pass "pg_stat_statements extension is available"

    EXT_VERSION=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT default_version FROM pg_available_extensions WHERE name = 'pg_stat_statements';" 2>/dev/null)
    echo "  Available version: $EXT_VERSION"
else
    print_fail "pg_stat_statements extension is not available"
    echo "  Check PostgreSQL installation"
fi
echo ""

# Test 4: Configuration Parameters
print_section "Test 4: Configuration Parameters"

# Check pg_stat_statements.max
MAX_STATEMENTS=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SHOW pg_stat_statements.max;" 2>/dev/null || echo "ERROR")
if [ "$MAX_STATEMENTS" != "ERROR" ]; then
    print_pass "pg_stat_statements.max is configured"
    echo "  Value: $MAX_STATEMENTS"

    if [ "$MAX_STATEMENTS" -lt 5000 ]; then
        print_warn "pg_stat_statements.max is low (< 5000)"
        echo "  Consider increasing for better tracking"
    fi
else
    print_warn "pg_stat_statements.max not found (using default)"
fi

# Check pg_stat_statements.track
TRACK=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SHOW pg_stat_statements.track;" 2>/dev/null || echo "ERROR")
if [ "$TRACK" != "ERROR" ]; then
    print_pass "pg_stat_statements.track is configured"
    echo "  Value: $TRACK"
else
    print_warn "pg_stat_statements.track not found (using default)"
fi

# Check pg_stat_statements.track_planning
TRACK_PLANNING=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SHOW pg_stat_statements.track_planning;" 2>/dev/null || echo "ERROR")
if [ "$TRACK_PLANNING" != "ERROR" ]; then
    print_pass "pg_stat_statements.track_planning is configured"
    echo "  Value: $TRACK_PLANNING"
else
    print_warn "pg_stat_statements.track_planning not found (using default)"
fi

echo ""

# Test 5: Extension Installation in Databases
print_section "Test 5: Extension Installation in Databases"

# Get list of databases
DATABASES=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;" 2>/dev/null)

if [ -z "$DATABASES" ]; then
    print_warn "No databases found"
else
    echo "Checking extension installation in databases:"
    echo ""

    while IFS= read -r db; do
        if [ -n "$db" ]; then
            INSTALLED=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null || echo "0")

            if [ "$INSTALLED" = "1" ]; then
                print_pass "Extension installed in database: $db"

                EXT_VER=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT extversion FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null)
                STMT_COUNT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_stat_statements;" 2>/dev/null || echo "0")

                echo "  Version: $EXT_VER"
                echo "  Tracked statements: $STMT_COUNT"
            else
                print_warn "Extension NOT installed in database: $db"
                echo "  To install: PGPASSWORD=postgres psql -h localhost -U postgres -d $db -c \"CREATE EXTENSION pg_stat_statements;\""
            fi
        fi
    done <<< "$DATABASES"
fi
echo ""

# Test 6: Functional Test
print_section "Test 6: Functional Test"

# Try to query pg_stat_statements in postgres database
TEST_RESULT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT COUNT(*) FROM pg_stat_statements;" 2>/dev/null || echo "ERROR")

if [ "$TEST_RESULT" = "ERROR" ]; then
    print_fail "Cannot query pg_stat_statements in postgres database"
    print_info "Extension may not be installed. Run:"
    echo "  PGPASSWORD=postgres psql -h localhost -U postgres -d postgres -c \"CREATE EXTENSION pg_stat_statements;\""
elif [ "$TEST_RESULT" -ge 0 ]; then
    print_pass "pg_stat_statements is functioning correctly"
    echo "  Currently tracking $TEST_RESULT statements"

    # Show sample query
    if [ "$TEST_RESULT" -gt 0 ]; then
        echo ""
        print_info "Sample of top 3 queries by execution time:"
        psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -c "SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, LEFT(query, 80) as query_preview FROM pg_stat_statements WHERE query NOT LIKE '%pg_stat_statements%' ORDER BY mean_exec_time DESC LIMIT 3;" 2>/dev/null || echo "  (Could not retrieve sample)"
    fi
fi
echo ""

# Test 7: Memory Usage
print_section "Test 7: Memory Usage"

# Calculate expected memory usage
if [ "$MAX_STATEMENTS" != "ERROR" ] && [ "$MAX_STATEMENTS" -gt 0 ]; then
    MEMORY_KB=$((MAX_STATEMENTS * 600 / 1024))
    MEMORY_MB=$((MEMORY_KB / 1024))

    print_info "Estimated memory usage: ${MEMORY_KB} KB (~${MEMORY_MB} MB)"

    if [ $MEMORY_MB -gt 50 ]; then
        print_warn "Memory usage is high (> 50 MB)"
        echo "  Consider reducing pg_stat_statements.max if this is a concern"
    else
        print_pass "Memory usage is within acceptable range"
    fi
fi
echo ""

# Test 8: Configuration File
print_section "Test 8: Configuration File"

CONFIG_FILE="/etc/postgresql/16/main/postgresql.conf"

if [ -f "$CONFIG_FILE" ]; then
    print_pass "Configuration file exists: $CONFIG_FILE"

    # Check if shared_preload_libraries is in config
    if grep -q "^shared_preload_libraries.*pg_stat_statements" "$CONFIG_FILE"; then
        print_pass "shared_preload_libraries configured in postgresql.conf"
    else
        print_warn "shared_preload_libraries with pg_stat_statements not found in config file"
        echo "  May be set via command line or other method"
    fi

    # Check for custom settings
    if grep -q "^pg_stat_statements\." "$CONFIG_FILE"; then
        print_pass "Custom pg_stat_statements settings found in config"
    else
        print_warn "No custom pg_stat_statements settings in config (using defaults)"
    fi
else
    print_warn "Cannot access configuration file: $CONFIG_FILE"
fi
echo ""

# Summary
print_header "Verification Summary"
echo ""
echo "Tests Passed:  $PASS_COUNT"
echo "Tests Failed:  $FAIL_COUNT"
echo "Warnings:      $WARN_COUNT"
echo ""

if [ $FAIL_COUNT -eq 0 ] && [ $WARN_COUNT -eq 0 ]; then
    print_pass "All tests passed! pg_stat_statements is fully configured and operational"
elif [ $FAIL_COUNT -eq 0 ]; then
    print_warn "All tests passed with warnings. Review warnings above."
else
    print_fail "Some tests failed. Review failures above and take corrective action."
    echo ""
    print_info "Common solutions:"
    echo ""
    echo "1. If shared_preload_libraries is not configured:"
    echo "   bash /workspaces/HRAPP/scripts/update_postgresql_config.sh"
    echo "   sudo service postgresql restart"
    echo ""
    echo "2. If extension is not installed in databases:"
    echo "   bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh"
    echo ""
    echo "3. For complete setup:"
    echo "   sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh"
fi

echo ""
print_info "For detailed documentation, see:"
echo "  /workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md"
echo ""

exit 0
