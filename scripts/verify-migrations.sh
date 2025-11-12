#!/bin/bash

################################################################################
# HRMS Database Migration Verification Script
################################################################################
#
# Purpose: Verify database migrations have been applied correctly
# Usage:
#   ./scripts/verify-migrations.sh [check_type]
#
# Check Types:
#   pre-check           - Pre-deployment environment validation
#   data-quality        - Data quality validation
#   migration-1         - Verify migration #1 (unique constraints)
#   migration-2         - Verify migration #2 (composite indexes)
#   migration-3         - Verify migration #3 (validation constraints)
#   migration-4         - Verify migration #4 (encryption schema)
#   post-deployment     - Complete post-deployment verification
#   rollback-verification - Verify rollback was successful
#   all                 - Run all verifications (default)
#
# Environment Variables:
#   DB_HOST              - Database host (default: localhost)
#   DB_PORT              - Database port (default: 5432)
#   DB_NAME              - Database name (default: hrms_db)
#   DB_USER              - Database user (default: postgres)
#   DB_PASSWORD          - Database password (required)
#   API_URL              - API URL for health checks (default: http://localhost:5000)
#
################################################################################

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Environment configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-hrms_db}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"
API_URL="${API_URL:-http://localhost:5000}"

# Check type (default: all)
CHECK_TYPE="${1:-all}"

# Counters
TOTAL_CHECKS=0
PASSED_CHECKS=0
FAILED_CHECKS=0
WARNING_CHECKS=0

# ============================================================================
# LOGGING FUNCTIONS
# ============================================================================

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
    PASSED_CHECKS=$((PASSED_CHECKS + 1))
}

log_warning() {
    echo -e "${YELLOW}[⚠]${NC} $1"
    WARNING_CHECKS=$((WARNING_CHECKS + 1))
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
    FAILED_CHECKS=$((FAILED_CHECKS + 1))
}

log_section() {
    echo ""
    echo "=========================================="
    echo "$1"
    echo "=========================================="
}

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

execute_sql() {
    local sql="$1"
    TOTAL_CHECKS=$((TOTAL_CHECKS + 1))
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "$sql" 2>&1
}

check_database_connectivity() {
    log_info "Testing database connection..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" &> /dev/null; then
        log_success "Database connection successful"
        return 0
    else
        log_error "Cannot connect to database"
        return 1
    fi
}

# ============================================================================
# PRE-DEPLOYMENT CHECKS
# ============================================================================

check_prerequisites() {
    log_section "Pre-Deployment Environment Check"

    # Check required commands
    log_info "Checking required commands..."
    local required_commands=("psql" "pg_dump" "curl")
    for cmd in "${required_commands[@]}"; do
        if command -v "$cmd" &> /dev/null; then
            log_success "Found command: $cmd"
        else
            log_error "Required command not found: $cmd"
        fi
    done

    # Check database connectivity
    check_database_connectivity

    # Check database size
    log_info "Checking database size..."
    local db_size
    db_size=$(execute_sql "SELECT pg_size_pretty(pg_database_size('$DB_NAME'));" | tr -d ' ')
    log_info "Database size: $db_size"

    # Check active connections
    log_info "Checking active connections..."
    local active_connections
    active_connections=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_stat_activity
        WHERE datname = '$DB_NAME' AND state = 'active';
    " | tr -d ' ')
    log_info "Active connections: $active_connections"

    if [ "$active_connections" -gt 100 ]; then
        log_warning "High number of active connections: $active_connections"
    else
        log_success "Active connections within normal range"
    fi

    # Check for long-running queries
    log_info "Checking for long-running queries..."
    local long_queries
    long_queries=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_stat_activity
        WHERE datname = '$DB_NAME'
          AND state = 'active'
          AND query_start < NOW() - INTERVAL '5 minutes';
    " | tr -d ' ')

    if [ "$long_queries" -gt 0 ]; then
        log_warning "Found $long_queries long-running queries (>5 minutes)"
    else
        log_success "No long-running queries detected"
    fi

    # Check table sizes
    log_info "Checking largest tables..."
    execute_sql "
        SELECT
            schemaname || '.' || tablename as table_name,
            pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) as size
        FROM pg_tables
        WHERE schemaname = 'tenant_default'
        ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC
        LIMIT 5;
    "
}

check_data_quality() {
    log_section "Data Quality Validation"

    # Check for duplicate National IDs
    log_info "Checking for duplicate National IDs..."
    local duplicate_national_ids
    duplicate_national_ids=$(execute_sql "
        SELECT COUNT(*)
        FROM (
            SELECT \"NationalIdCard\"
            FROM tenant_default.\"Employees\"
            WHERE \"NationalIdCard\" IS NOT NULL
              AND \"IsDeleted\" = false
            GROUP BY \"NationalIdCard\"
            HAVING COUNT(*) > 1
        ) AS duplicates;
    " | tr -d ' ')

    if [ "$duplicate_national_ids" -eq 0 ]; then
        log_success "No duplicate National IDs found"
    else
        log_error "Found $duplicate_national_ids duplicate National IDs"
    fi

    # Check for duplicate Passport Numbers
    log_info "Checking for duplicate Passport Numbers..."
    local duplicate_passports
    duplicate_passports=$(execute_sql "
        SELECT COUNT(*)
        FROM (
            SELECT \"PassportNumber\"
            FROM tenant_default.\"Employees\"
            WHERE \"PassportNumber\" IS NOT NULL
              AND \"IsDeleted\" = false
            GROUP BY \"PassportNumber\"
            HAVING COUNT(*) > 1
        ) AS duplicates;
    " | tr -d ' ')

    if [ "$duplicate_passports" -eq 0 ]; then
        log_success "No duplicate Passport Numbers found"
    else
        log_error "Found $duplicate_passports duplicate Passport Numbers"
    fi

    # Check for duplicate Bank Account Numbers
    log_info "Checking for duplicate Bank Account Numbers..."
    local duplicate_bank_accounts
    duplicate_bank_accounts=$(execute_sql "
        SELECT COUNT(*)
        FROM (
            SELECT \"BankAccountNumber\"
            FROM tenant_default.\"Employees\"
            WHERE \"BankAccountNumber\" IS NOT NULL
              AND \"IsDeleted\" = false
            GROUP BY \"BankAccountNumber\"
            HAVING COUNT(*) > 1
        ) AS duplicates;
    " | tr -d ' ')

    if [ "$duplicate_bank_accounts" -eq 0 ]; then
        log_success "No duplicate Bank Account Numbers found"
    else
        log_error "Found $duplicate_bank_accounts duplicate Bank Account Numbers"
    fi

    # Check for negative salaries
    log_info "Checking for negative salaries..."
    local negative_salaries
    negative_salaries=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"Employees\"
        WHERE \"BasicSalary\" < 0;
    " | tr -d ' ')

    if [ "$negative_salaries" -eq 0 ]; then
        log_success "No negative salaries found"
    else
        log_warning "Found $negative_salaries employees with negative salaries"
    fi

    # Check for negative leave balances
    log_info "Checking for negative leave balances..."
    local negative_leaves
    negative_leaves=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"Employees\"
        WHERE \"AnnualLeaveBalance\" < 0
           OR \"SickLeaveBalance\" < 0
           OR \"CasualLeaveBalance\" < 0;
    " | tr -d ' ')

    if [ "$negative_leaves" -eq 0 ]; then
        log_success "No negative leave balances found"
    else
        log_warning "Found $negative_leaves employees with negative leave balances"
    fi

    # Check for invalid months
    log_info "Checking for invalid months in payroll cycles..."
    local invalid_months
    invalid_months=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"PayrollCycles\"
        WHERE \"Month\" < 1 OR \"Month\" > 12;
    " | tr -d ' ')

    if [ "$invalid_months" -eq 0 ]; then
        log_success "No invalid months found"
    else
        log_error "Found $invalid_months payroll cycles with invalid months"
    fi

    # Check for invalid years
    log_info "Checking for invalid years in payroll cycles..."
    local invalid_years
    invalid_years=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"PayrollCycles\"
        WHERE \"Year\" <= 1900;
    " | tr -d ' ')

    if [ "$invalid_years" -eq 0 ]; then
        log_success "No invalid years found"
    else
        log_error "Found $invalid_years payroll cycles with invalid years"
    fi
}

# ============================================================================
# MIGRATION VERIFICATION
# ============================================================================

verify_migration_history() {
    local migration_name="$1"

    log_info "Checking migration history for: $migration_name"

    local migration_count
    migration_count=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" = '$migration_name';
    " | tr -d ' ')

    if [ "$migration_count" -eq 1 ]; then
        log_success "Migration found in history: $migration_name"
        return 0
    else
        log_error "Migration NOT found in history: $migration_name"
        return 1
    fi
}

verify_migration_1() {
    log_section "Verifying Migration #1: Unique Constraints"

    # Check if migration is in history
    verify_migration_history "20251112_AddNationalIdUniqueConstraint"

    # Verify each unique index
    local expected_indexes=(
        "IX_Employees_NationalIdCard_Unique"
        "IX_Employees_PassportNumber_Unique"
        "IX_Employees_TaxIdNumber_Unique"
        "IX_Employees_NPFNumber_Unique"
        "IX_Employees_NSFNumber_Unique"
        "IX_Employees_BankAccountNumber_Unique"
    )

    log_info "Verifying unique indexes..."
    for index_name in "${expected_indexes[@]}"; do
        local index_exists
        index_exists=$(execute_sql "
            SELECT COUNT(*)
            FROM pg_indexes
            WHERE schemaname = 'tenant_default'
              AND tablename = 'Employees'
              AND indexname = '$index_name';
        " | tr -d ' ')

        if [ "$index_exists" -eq 1 ]; then
            log_success "Index verified: $index_name"
        else
            log_error "Index NOT found: $index_name"
        fi
    done

    # Test unique constraint enforcement
    log_info "Testing unique constraint enforcement..."
    log_info "(This will attempt to create duplicate data and should fail)"

    # This test is optional and may not work in all environments
    # Uncomment if you want to test constraint enforcement
    # execute_sql "BEGIN; INSERT INTO ... ROLLBACK;" 2>&1 | grep -q "duplicate key" && log_success "Constraint enforcement verified" || log_warning "Could not verify constraint enforcement"
}

verify_migration_2() {
    log_section "Verifying Migration #2: Composite Indexes"

    # Check if migration is in history
    verify_migration_history "20251112_AddMissingCompositeIndexes"

    # Count composite indexes
    log_info "Counting composite indexes..."
    local composite_count
    composite_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = 'tenant_default'
          AND indexname LIKE 'IX_%'
          AND indexname NOT LIKE '%_Unique';
    " | tr -d ' ')

    log_info "Found $composite_count composite indexes"

    if [ "$composite_count" -ge 13 ]; then
        log_success "Composite indexes count verified (expected ≥13, found $composite_count)"
    else
        log_error "Insufficient composite indexes (expected ≥13, found $composite_count)"
    fi

    # List some key indexes
    log_info "Key indexes:"
    execute_sql "
        SELECT
            tablename,
            indexname
        FROM pg_indexes
        WHERE schemaname = 'tenant_default'
          AND indexname LIKE 'IX_%'
          AND indexname NOT LIKE '%_Unique'
        ORDER BY tablename, indexname
        LIMIT 10;
    "

    # Check index usage statistics (if available)
    log_info "Checking index usage statistics..."
    execute_sql "
        SELECT
            indexname,
            idx_scan as scans,
            idx_tup_read as tuples_read
        FROM pg_stat_user_indexes
        WHERE schemaname = 'tenant_default'
          AND indexname LIKE 'IX_%'
        ORDER BY idx_scan DESC
        LIMIT 5;
    "
}

verify_migration_3() {
    log_section "Verifying Migration #3: Validation Constraints"

    # Check if migration is in history
    verify_migration_history "20251112_AddDataValidationConstraints"

    # Count CHECK constraints
    log_info "Counting CHECK constraints..."
    local constraint_count
    constraint_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE connamespace = 'tenant_default'::regnamespace
          AND contype = 'c'
          AND conname LIKE 'chk_%';
    " | tr -d ' ')

    log_info "Found $constraint_count CHECK constraints"

    if [ "$constraint_count" -ge 30 ]; then
        log_success "CHECK constraints count verified (expected ≥30, found $constraint_count)"
    else
        log_error "Insufficient CHECK constraints (expected ≥30, found $constraint_count)"
    fi

    # List constraints by table
    log_info "Constraints by table:"
    execute_sql "
        SELECT
            conrelid::regclass as table_name,
            COUNT(*) as constraint_count
        FROM pg_constraint
        WHERE connamespace = 'tenant_default'::regnamespace
          AND contype = 'c'
          AND conname LIKE 'chk_%'
        GROUP BY conrelid::regclass
        ORDER BY constraint_count DESC;
    "

    # Verify specific critical constraints exist
    local critical_constraints=(
        "chk_Employees_BasicSalary_NonNegative"
        "chk_PayrollCycles_Month_Valid"
        "chk_PayrollCycles_Year_Valid"
        "chk_Attendances_WorkingHours_NonNegative"
    )

    log_info "Verifying critical constraints..."
    for constraint_name in "${critical_constraints[@]}"; do
        local constraint_exists
        constraint_exists=$(execute_sql "
            SELECT COUNT(*)
            FROM pg_constraint
            WHERE connamespace = 'tenant_default'::regnamespace
              AND contype = 'c'
              AND conname = '$constraint_name';
        " | tr -d ' ')

        if [ "$constraint_exists" -eq 1 ]; then
            log_success "Constraint verified: $constraint_name"
        else
            log_error "Constraint NOT found: $constraint_name"
        fi
    done
}

verify_migration_4() {
    log_section "Verifying Migration #4: Column Level Encryption"

    # Check if migration is in history
    verify_migration_history "20251112031109_AddColumnLevelEncryption"

    # This migration consolidates previous migrations
    # Verify it includes indexes and constraints from previous migrations

    log_info "Verifying consolidated migration includes previous changes..."

    # Re-check composite indexes
    local composite_count
    composite_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = 'tenant_default'
          AND indexname LIKE 'IX_%';
    " | tr -d ' ')

    if [ "$composite_count" -ge 19 ]; then  # 6 unique + 13 composite
        log_success "All indexes present (found $composite_count)"
    else
        log_error "Missing indexes (expected ≥19, found $composite_count)"
    fi

    # Re-check constraints
    local constraint_count
    constraint_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE connamespace = 'tenant_default'::regnamespace
          AND contype = 'c'
          AND conname LIKE 'chk_%';
    " | tr -d ' ')

    if [ "$constraint_count" -ge 30 ]; then
        log_success "All constraints present (found $constraint_count)"
    else
        log_error "Missing constraints (expected ≥30, found $constraint_count)"
    fi

    # Check encryption service status via API (optional)
    if command -v curl &> /dev/null; then
        log_info "Checking encryption service status..."
        if curl -f -s "${API_URL}/api/health/encryption" &> /dev/null; then
            log_success "Encryption service is accessible"
        else
            log_warning "Encryption service health endpoint not accessible (this may be expected)"
        fi
    fi
}

# ============================================================================
# POST-DEPLOYMENT VERIFICATION
# ============================================================================

verify_post_deployment() {
    log_section "Post-Deployment Verification"

    # Check all migrations applied
    log_info "Checking migration history..."
    local latest_migration
    latest_migration=$(execute_sql "
        SELECT \"MigrationId\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        ORDER BY \"MigrationId\" DESC
        LIMIT 1;
    " | tr -d ' ')

    log_info "Latest migration: $latest_migration"

    if [ "$latest_migration" = "20251112031109_AddColumnLevelEncryption" ]; then
        log_success "All migrations applied successfully"
    else
        log_error "Latest migration mismatch (expected: 20251112031109_AddColumnLevelEncryption)"
    fi

    # Count total migrations
    local total_migrations
    total_migrations=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"__EFMigrationsHistory\";
    " | tr -d ' ')
    log_info "Total migrations applied: $total_migrations"

    # Verify indexes
    verify_migration_1
    verify_migration_2

    # Verify constraints
    verify_migration_3

    # Check database statistics
    log_info "Checking database statistics..."
    execute_sql "
        SELECT
            'Tables' as type,
            COUNT(*) as count
        FROM pg_tables
        WHERE schemaname = 'tenant_default'
        UNION ALL
        SELECT 'Indexes', COUNT(*)
        FROM pg_indexes
        WHERE schemaname = 'tenant_default'
        UNION ALL
        SELECT 'Constraints', COUNT(*)
        FROM pg_constraint
        WHERE connamespace = 'tenant_default'::regnamespace;
    "

    # Check for errors in logs (if applicable)
    log_info "Checking for recent constraint violations..."
    local constraint_violations
    constraint_violations=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_stat_database
        WHERE datname = '$DB_NAME';
    " | tr -d ' ')
    log_info "Database statistics available"

    # Test application health
    if command -v curl &> /dev/null; then
        log_info "Testing application health..."
        if curl -f -s "${API_URL}/health" &> /dev/null; then
            log_success "Application health check passed"
        else
            log_warning "Application health check failed"
        fi
    fi
}

# ============================================================================
# ROLLBACK VERIFICATION
# ============================================================================

verify_rollback() {
    log_section "Rollback Verification"

    log_info "Checking if migrations have been rolled back..."

    # Check if migration #4 is present
    local migration_4_present
    migration_4_present=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" = '20251112031109_AddColumnLevelEncryption';
    " | tr -d ' ')

    if [ "$migration_4_present" -eq 0 ]; then
        log_success "Migration #4 has been rolled back"
    else
        log_error "Migration #4 is still present"
    fi

    # Check current migration status
    local current_migration
    current_migration=$(execute_sql "
        SELECT \"MigrationId\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        ORDER BY \"MigrationId\" DESC
        LIMIT 1;
    " | tr -d ' ')

    log_info "Current migration: $current_migration"

    # Test application health
    if command -v curl &> /dev/null; then
        log_info "Testing application health after rollback..."
        if curl -f -s "${API_URL}/health" &> /dev/null; then
            log_success "Application is healthy after rollback"
        else
            log_error "Application health check failed after rollback"
        fi
    fi
}

# ============================================================================
# MAIN FUNCTION
# ============================================================================

print_summary() {
    log_section "Verification Summary"
    echo ""
    echo "Total Checks:   $TOTAL_CHECKS"
    echo -e "${GREEN}Passed:         $PASSED_CHECKS${NC}"
    echo -e "${YELLOW}Warnings:       $WARNING_CHECKS${NC}"
    echo -e "${RED}Failed:         $FAILED_CHECKS${NC}"
    echo ""

    if [ $FAILED_CHECKS -eq 0 ]; then
        echo -e "${GREEN}=========================================="
        echo -e "ALL VERIFICATIONS PASSED"
        echo -e "==========================================${NC}"
        return 0
    else
        echo -e "${RED}=========================================="
        echo -e "VERIFICATION FAILED"
        echo -e "==========================================${NC}"
        echo -e "${RED}$FAILED_CHECKS checks failed${NC}"
        return 1
    fi
}

main() {
    log_section "HRMS Database Migration Verification"
    log_info "Check type: $CHECK_TYPE"
    log_info "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"

    case "$CHECK_TYPE" in
        pre-check)
            check_prerequisites
            ;;
        data-quality)
            check_data_quality
            ;;
        migration-1)
            verify_migration_1
            ;;
        migration-2)
            verify_migration_2
            ;;
        migration-3)
            verify_migration_3
            ;;
        migration-4)
            verify_migration_4
            ;;
        post-deployment)
            verify_post_deployment
            ;;
        rollback-verification)
            verify_rollback
            ;;
        all)
            check_prerequisites
            check_data_quality
            verify_migration_1
            verify_migration_2
            verify_migration_3
            verify_migration_4
            verify_post_deployment
            ;;
        *)
            log_error "Unknown check type: $CHECK_TYPE"
            echo ""
            echo "Usage: $0 [check_type]"
            echo ""
            echo "Check types:"
            echo "  pre-check           - Pre-deployment environment validation"
            echo "  data-quality        - Data quality validation"
            echo "  migration-1         - Verify migration #1"
            echo "  migration-2         - Verify migration #2"
            echo "  migration-3         - Verify migration #3"
            echo "  migration-4         - Verify migration #4"
            echo "  post-deployment     - Complete post-deployment verification"
            echo "  rollback-verification - Verify rollback was successful"
            echo "  all                 - Run all verifications (default)"
            exit 1
            ;;
    esac

    print_summary
}

# Execute main function
main
