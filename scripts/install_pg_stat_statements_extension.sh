#!/bin/bash
#
# Install pg_stat_statements Extension in PostgreSQL Databases
#
# This script installs the pg_stat_statements extension in all specified databases.
# It handles multi-tenant scenarios and provides comprehensive error handling.
#
# Usage: bash install_pg_stat_statements_extension.sh
#

set -e  # Exit on error

# Configuration
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-postgres}"
PGPASSWORD="${PGPASSWORD:-postgres}"

# Export for psql commands
export PGPASSWORD

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_header() {
    echo -e "${BLUE}$1${NC}"
}

# Function to check if PostgreSQL is running
check_postgres() {
    print_info "Checking PostgreSQL connection..."
    if pg_isready -h "$PGHOST" -p "$PGPORT" >/dev/null 2>&1; then
        print_success "PostgreSQL is running and accepting connections"
        return 0
    else
        print_error "Cannot connect to PostgreSQL at $PGHOST:$PGPORT"
        return 1
    fi
}

# Function to verify shared_preload_libraries is configured
verify_preload_libraries() {
    print_info "Verifying shared_preload_libraries configuration..."
    PRELOAD_LIBS=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SHOW shared_preload_libraries;" 2>/dev/null || echo "")

    if echo "$PRELOAD_LIBS" | grep -q "pg_stat_statements"; then
        print_success "pg_stat_statements is properly configured in shared_preload_libraries"
        return 0
    else
        print_error "pg_stat_statements is NOT in shared_preload_libraries"
        echo ""
        echo "Current value: '$PRELOAD_LIBS'"
        echo ""
        print_warning "You must configure shared_preload_libraries first and restart PostgreSQL"
        echo ""
        echo "Run: bash /workspaces/HRAPP/scripts/update_postgresql_config.sh"
        echo "Then restart: sudo service postgresql restart"
        return 1
    fi
}

# Function to check if extension is available
check_extension_available() {
    local db=$1
    AVAILABLE=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_available_extensions WHERE name = 'pg_stat_statements';" 2>/dev/null || echo "0")

    if [ "$AVAILABLE" -eq "1" ]; then
        return 0
    else
        print_error "pg_stat_statements extension is not available in database: $db"
        return 1
    fi
}

# Function to check if extension is already installed
check_extension_installed() {
    local db=$1
    INSTALLED=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null || echo "0")

    if [ "$INSTALLED" -eq "1" ]; then
        return 0
    else
        return 1
    fi
}

# Function to install extension in a database
install_extension() {
    local db=$1

    print_info "Processing database: $db"

    # Check if extension is available
    if ! check_extension_available "$db"; then
        return 1
    fi

    # Check if already installed
    if check_extension_installed "$db"; then
        print_warning "Extension already installed in database: $db"

        # Show current version
        VERSION=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT extversion FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null)
        echo "  Current version: $VERSION"
        return 0
    fi

    # Install the extension
    print_info "Installing pg_stat_statements in database: $db"
    if psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements;" >/dev/null 2>&1; then
        print_success "Extension installed successfully in database: $db"

        # Verify installation
        VERSION=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT extversion FROM pg_extension WHERE extname = 'pg_stat_statements';" 2>/dev/null)
        echo "  Version: $VERSION"

        # Show row count
        COUNT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_stat_statements;" 2>/dev/null || echo "0")
        echo "  Current tracked statements: $COUNT"

        return 0
    else
        print_error "Failed to install extension in database: $db"
        return 1
    fi
}

# Function to get list of all databases (excluding templates and system databases)
get_databases() {
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -t -A -c "SELECT datname FROM pg_database WHERE datistemplate = false AND datname NOT IN ('postgres') ORDER BY datname;" 2>/dev/null
}

# Function to test the extension
test_extension() {
    local db=$1

    print_info "Testing pg_stat_statements in database: $db"

    # Run a test query
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -c "SELECT 1 AS test;" >/dev/null 2>&1

    # Check if we can query pg_stat_statements
    RESULT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" -t -A -c "SELECT COUNT(*) FROM pg_stat_statements WHERE query LIKE '%SELECT 1 AS test%';" 2>/dev/null || echo "ERROR")

    if [ "$RESULT" != "ERROR" ] && [ "$RESULT" -ge "0" ]; then
        print_success "Extension is working correctly in database: $db"
        return 0
    else
        print_error "Extension test failed in database: $db"
        return 1
    fi
}

# Main execution
print_header "========================================"
print_header "pg_stat_statements Extension Installer"
print_header "========================================"
echo ""

# Check PostgreSQL connection
if ! check_postgres; then
    exit 1
fi

echo ""

# Verify shared_preload_libraries
if ! verify_preload_libraries; then
    exit 1
fi

echo ""
print_header "Available Databases:"
print_header "-------------------"

# Get list of databases
DATABASES=$(get_databases)

if [ -z "$DATABASES" ]; then
    print_warning "No user databases found (only system databases exist)"
    echo ""
    print_info "Available databases:"
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -c "\l"
    exit 0
fi

# Display databases
echo "$DATABASES" | while read -r db; do
    echo "  - $db"
done

echo ""
print_info "The extension will be installed in the following databases:"
echo "$DATABASES" | while read -r db; do
    echo "  - $db"
done

echo ""
read -p "Do you want to proceed? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    print_warning "Installation cancelled by user"
    exit 0
fi

echo ""
print_header "Installing Extension..."
print_header "----------------------"
echo ""

# Install extension in each database
SUCCESS_COUNT=0
FAIL_COUNT=0
SKIP_COUNT=0

echo "$DATABASES" | while read -r db; do
    if [ -n "$db" ]; then
        if install_extension "$db"; then
            ((SUCCESS_COUNT++)) || true

            # Test the extension
            test_extension "$db"
        else
            if check_extension_installed "$db"; then
                ((SKIP_COUNT++)) || true
            else
                ((FAIL_COUNT++)) || true
            fi
        fi
        echo ""
    fi
done

# Summary
print_header "Installation Summary"
print_header "-------------------"
echo "Successfully installed: $SUCCESS_COUNT database(s)"
echo "Already installed: $SKIP_COUNT database(s)"
echo "Failed: $FAIL_COUNT database(s)"
echo ""

if [ $FAIL_COUNT -eq 0 ]; then
    print_success "All databases processed successfully!"
else
    print_warning "Some databases failed to install the extension"
    echo "Check the error messages above for details"
fi

echo ""
print_header "Useful Queries:"
print_header "--------------"
echo ""
echo "1. View top 10 slowest queries (by average time):"
echo "   SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query"
echo "   FROM pg_stat_statements"
echo "   ORDER BY mean_exec_time DESC LIMIT 10;"
echo ""
echo "2. View most frequently executed queries:"
echo "   SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query"
echo "   FROM pg_stat_statements"
echo "   ORDER BY calls DESC LIMIT 10;"
echo ""
echo "3. View queries with most total time:"
echo "   SELECT calls, total_exec_time::numeric(10,2) as total_ms, query"
echo "   FROM pg_stat_statements"
echo "   ORDER BY total_exec_time DESC LIMIT 10;"
echo ""
echo "4. Reset statistics:"
echo "   SELECT pg_stat_statements_reset();"
echo ""

print_info "For more information, see: /workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md"
echo ""

exit 0
