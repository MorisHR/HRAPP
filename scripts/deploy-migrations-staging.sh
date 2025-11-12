#!/bin/bash

################################################################################
# HRMS Database Migration Deployment Script - STAGING
################################################################################
#
# Purpose: Deploy 4 database migrations to STAGING environment
# Migrations:
#   1. 20251112_AddNationalIdUniqueConstraint
#   2. 20251112_AddMissingCompositeIndexes
#   3. 20251112_AddDataValidationConstraints
#   4. 20251112031109_AddColumnLevelEncryption
#
# Safety Features:
#   - Pre-deployment validation
#   - Data quality checks
#   - Step-by-step deployment with verification
#   - Automatic rollback on failure
#   - Comprehensive logging
#
# Usage:
#   ./scripts/deploy-migrations-staging.sh
#
# Environment Variables (required):
#   DB_HOST              - Database host (default: localhost)
#   DB_PORT              - Database port (default: 5432)
#   DB_NAME              - Database name (default: hrms_db_staging)
#   DB_USER              - Database user (default: postgres)
#   DB_PASSWORD          - Database password (required)
#   BACKUP_DIR           - Backup directory (default: /tmp/backups)
#
################################################################################

set -euo pipefail  # Exit on error, undefined variables, pipe failures

# ============================================================================
# CONFIGURATION
# ============================================================================

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Environment configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-hrms_db_staging}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_DIR="${BACKUP_DIR:-/tmp/backups}"

# Project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
INFRASTRUCTURE_PROJECT="$PROJECT_ROOT/src/HRMS.Infrastructure"
API_PROJECT="$PROJECT_ROOT/src/HRMS.API"

# Deployment configuration
DEPLOYMENT_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="${BACKUP_DIR}/migration_staging_${DEPLOYMENT_TIMESTAMP}.log"
CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};CommandTimeout=300"

# Migration list (in order)
declare -a MIGRATIONS=(
    "20251112_AddNationalIdUniqueConstraint"
    "20251112_AddMissingCompositeIndexes"
    "20251112_AddDataValidationConstraints"
    "20251112031109_AddColumnLevelEncryption"
)

# ============================================================================
# LOGGING FUNCTIONS
# ============================================================================

log() {
    local message="$1"
    echo -e "[$(date '+%Y-%m-%d %H:%M:%S')] $message" | tee -a "$LOG_FILE"
}

log_info() {
    log "${BLUE}[INFO]${NC} $1"
}

log_success() {
    log "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

log_section() {
    local title="$1"
    log ""
    log "=========================================="
    log "$title"
    log "=========================================="
}

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

check_prerequisites() {
    log_section "Checking Prerequisites"

    # Check required commands
    local required_commands=("dotnet" "psql" "pg_dump")
    for cmd in "${required_commands[@]}"; do
        if ! command -v "$cmd" &> /dev/null; then
            log_error "Required command not found: $cmd"
            exit 1
        fi
        log_success "Found command: $cmd"
    done

    # Check database password
    if [ -z "$DB_PASSWORD" ]; then
        log_error "DB_PASSWORD environment variable is required"
        exit 1
    fi

    # Create backup directory
    mkdir -p "$BACKUP_DIR"
    log_success "Backup directory: $BACKUP_DIR"

    # Check database connectivity
    log_info "Testing database connection..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" &> /dev/null; then
        log_success "Database connection successful"
    else
        log_error "Cannot connect to database"
        exit 1
    fi

    # Check .NET project paths
    if [ ! -d "$INFRASTRUCTURE_PROJECT" ]; then
        log_error "Infrastructure project not found: $INFRASTRUCTURE_PROJECT"
        exit 1
    fi
    log_success "Found Infrastructure project"

    if [ ! -d "$API_PROJECT" ]; then
        log_error "API project not found: $API_PROJECT"
        exit 1
    fi
    log_success "Found API project"

    log_success "All prerequisites met"
}

execute_sql() {
    local sql="$1"
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "$sql" 2>&1
}

execute_sql_file() {
    local file="$1"
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$file" 2>&1
}

# ============================================================================
# PRE-DEPLOYMENT FUNCTIONS
# ============================================================================

check_data_quality() {
    log_section "Data Quality Validation"

    local issues_found=0

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

    if [ "$duplicate_national_ids" -gt 0 ]; then
        log_error "Found $duplicate_national_ids duplicate National IDs"
        issues_found=$((issues_found + 1))
    else
        log_success "No duplicate National IDs found"
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

    if [ "$duplicate_passports" -gt 0 ]; then
        log_error "Found $duplicate_passports duplicate Passport Numbers"
        issues_found=$((issues_found + 1))
    else
        log_success "No duplicate Passport Numbers found"
    fi

    # Check for negative salaries
    log_info "Checking for negative salaries..."
    local negative_salaries
    negative_salaries=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"Employees\"
        WHERE \"BasicSalary\" < 0;
    " | tr -d ' ')

    if [ "$negative_salaries" -gt 0 ]; then
        log_warning "Found $negative_salaries employees with negative salaries"
        log_info "Auto-fixing negative salaries..."
        execute_sql "UPDATE tenant_default.\"Employees\" SET \"BasicSalary\" = 0 WHERE \"BasicSalary\" < 0;" > /dev/null
        log_success "Fixed negative salaries"
    else
        log_success "No negative salaries found"
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

    if [ "$negative_leaves" -gt 0 ]; then
        log_warning "Found $negative_leaves employees with negative leave balances"
        log_info "Auto-fixing negative leave balances..."
        execute_sql "
            UPDATE tenant_default.\"Employees\"
            SET \"AnnualLeaveBalance\" = GREATEST(\"AnnualLeaveBalance\", 0),
                \"SickLeaveBalance\" = GREATEST(\"SickLeaveBalance\", 0),
                \"CasualLeaveBalance\" = GREATEST(\"CasualLeaveBalance\", 0)
            WHERE \"AnnualLeaveBalance\" < 0
               OR \"SickLeaveBalance\" < 0
               OR \"CasualLeaveBalance\" < 0;
        " > /dev/null
        log_success "Fixed negative leave balances"
    else
        log_success "No negative leave balances found"
    fi

    # Check for invalid months
    log_info "Checking for invalid months in payroll cycles..."
    local invalid_months
    invalid_months=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"PayrollCycles\"
        WHERE \"Month\" < 1 OR \"Month\" > 12;
    " | tr -d ' ')

    if [ "$invalid_months" -gt 0 ]; then
        log_error "Found $invalid_months payroll cycles with invalid months"
        issues_found=$((issues_found + 1))
    else
        log_success "No invalid months found"
    fi

    if [ $issues_found -gt 0 ]; then
        log_error "Data quality validation failed with $issues_found critical issues"
        log_error "Please review and fix the issues before proceeding"
        return 1
    fi

    log_success "All data quality checks passed"
    return 0
}

create_backup() {
    log_section "Creating Database Backup"

    local backup_file="${BACKUP_DIR}/hrms_staging_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

    log_info "Creating backup: $backup_file"
    log_info "This may take several minutes..."

    if PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        -F c \
        -b \
        -v \
        -f "$backup_file" 2>&1 | tee -a "$LOG_FILE"; then

        # Verify backup exists and has size > 0
        if [ -f "$backup_file" ] && [ -s "$backup_file" ]; then
            local backup_size
            backup_size=$(du -h "$backup_file" | cut -f1)
            log_success "Backup created successfully: $backup_size"

            # Create checksum
            local checksum_file="${backup_file}.sha256"
            sha256sum "$backup_file" > "$checksum_file"
            log_info "Backup checksum: $(cat "$checksum_file")"

            # Export backup path for rollback
            echo "$backup_file" > "${BACKUP_DIR}/latest_backup.txt"

            return 0
        else
            log_error "Backup file is empty or does not exist"
            return 1
        fi
    else
        log_error "Backup creation failed"
        return 1
    fi
}

# ============================================================================
# MIGRATION FUNCTIONS
# ============================================================================

get_current_migration() {
    local current_migration
    current_migration=$(execute_sql "
        SELECT \"MigrationId\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        ORDER BY \"MigrationId\" DESC
        LIMIT 1;
    " | tr -d ' ')

    echo "$current_migration"
}

apply_migration() {
    local migration_name="$1"

    log_info "Applying migration: $migration_name"

    # Use dotnet ef to update to specific migration
    if dotnet ef database update "$migration_name" \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --context TenantDbContext \
        --connection "$CONNECTION_STRING" \
        --verbose 2>&1 | tee -a "$LOG_FILE"; then

        log_success "Migration applied: $migration_name"
        return 0
    else
        log_error "Migration failed: $migration_name"
        return 1
    fi
}

verify_migration() {
    local migration_name="$1"

    log_info "Verifying migration: $migration_name"

    # Check if migration is recorded in history
    local migration_count
    migration_count=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" = '$migration_name';
    " | tr -d ' ')

    if [ "$migration_count" -eq 1 ]; then
        log_success "Migration verified in history: $migration_name"
        return 0
    else
        log_error "Migration not found in history: $migration_name"
        return 1
    fi
}

verify_unique_indexes() {
    log_info "Verifying unique indexes..."

    local expected_indexes=(
        "IX_Employees_NationalIdCard_Unique"
        "IX_Employees_PassportNumber_Unique"
        "IX_Employees_TaxIdNumber_Unique"
        "IX_Employees_NPFNumber_Unique"
        "IX_Employees_NSFNumber_Unique"
        "IX_Employees_BankAccountNumber_Unique"
    )

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
            log_error "Index not found: $index_name"
            return 1
        fi
    done

    log_success "All unique indexes verified"
    return 0
}

verify_composite_indexes() {
    log_info "Verifying composite indexes..."

    # Count composite indexes (should be 13+)
    local composite_count
    composite_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = 'tenant_default'
          AND indexname LIKE 'IX_%'
          AND indexname NOT LIKE '%_Unique';
    " | tr -d ' ')

    if [ "$composite_count" -ge 13 ]; then
        log_success "Found $composite_count composite indexes (expected ≥13)"
        return 0
    else
        log_error "Only found $composite_count composite indexes (expected ≥13)"
        return 1
    fi
}

verify_check_constraints() {
    log_info "Verifying CHECK constraints..."

    # Count CHECK constraints (should be 30+)
    local constraint_count
    constraint_count=$(execute_sql "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE connamespace = 'tenant_default'::regnamespace
          AND contype = 'c'
          AND conname LIKE 'chk_%';
    " | tr -d ' ')

    if [ "$constraint_count" -ge 30 ]; then
        log_success "Found $constraint_count CHECK constraints (expected ≥30)"
        return 0
    else
        log_error "Only found $constraint_count CHECK constraints (expected ≥30)"
        return 1
    fi
}

# ============================================================================
# ROLLBACK FUNCTIONS
# ============================================================================

rollback_migrations() {
    log_section "INITIATING ROLLBACK"

    log_warning "Rolling back all migrations..."

    # Get previous migration (before our changes)
    # This assumes migrations were applied sequentially
    # We need to get the migration before the first one we applied

    log_info "Reverting to previous migration state..."

    # Option 1: Use dotnet ef to revert to previous migration
    # We would need to know the previous migration name

    # Option 2: Restore from backup (safer)
    local backup_file
    if [ -f "${BACKUP_DIR}/latest_backup.txt" ]; then
        backup_file=$(cat "${BACKUP_DIR}/latest_backup.txt")
        log_info "Restoring from backup: $backup_file"

        if [ -f "$backup_file" ]; then
            # Drop and recreate database
            log_warning "Dropping database..."
            PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
                -c "DROP DATABASE IF EXISTS $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"

            log_info "Recreating database..."
            PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
                -c "CREATE DATABASE $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"

            log_info "Restoring backup..."
            if PGPASSWORD="$DB_PASSWORD" pg_restore \
                -h "$DB_HOST" \
                -p "$DB_PORT" \
                -U "$DB_USER" \
                -d "$DB_NAME" \
                -v \
                "$backup_file" 2>&1 | tee -a "$LOG_FILE"; then

                log_success "Rollback completed - database restored from backup"
                return 0
            else
                log_error "Backup restoration failed"
                return 1
            fi
        else
            log_error "Backup file not found: $backup_file"
            return 1
        fi
    else
        log_error "No backup reference found"
        return 1
    fi
}

# ============================================================================
# MAIN DEPLOYMENT FLOW
# ============================================================================

main() {
    log_section "HRMS Database Migration Deployment - STAGING"
    log_info "Deployment started at: $(date)"
    log_info "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    log_info "Log file: $LOG_FILE"

    # Phase 1: Pre-Deployment
    log_section "Phase 1: Pre-Deployment Checks"

    if ! check_prerequisites; then
        log_error "Prerequisites check failed"
        exit 1
    fi

    if ! check_data_quality; then
        log_error "Data quality validation failed"
        log_error "Please fix data quality issues and try again"
        exit 1
    fi

    if ! create_backup; then
        log_error "Backup creation failed"
        log_error "Cannot proceed without backup"
        exit 1
    fi

    log_success "Pre-deployment phase completed"

    # Phase 2: Migration Deployment
    log_section "Phase 2: Migration Deployment"

    local migration_success=true

    # Apply Migration #1
    log_section "Migration 1/4: AddNationalIdUniqueConstraint"
    if apply_migration "${MIGRATIONS[0]}"; then
        if verify_migration "${MIGRATIONS[0]}" && verify_unique_indexes; then
            log_success "Migration #1 completed successfully"
        else
            log_error "Migration #1 verification failed"
            migration_success=false
        fi
    else
        log_error "Migration #1 application failed"
        migration_success=false
    fi

    if [ "$migration_success" = false ]; then
        rollback_migrations
        exit 1
    fi

    # Apply Migration #2
    log_section "Migration 2/4: AddMissingCompositeIndexes"
    if apply_migration "${MIGRATIONS[1]}"; then
        if verify_migration "${MIGRATIONS[1]}" && verify_composite_indexes; then
            log_success "Migration #2 completed successfully"
        else
            log_error "Migration #2 verification failed"
            migration_success=false
        fi
    else
        log_error "Migration #2 application failed"
        migration_success=false
    fi

    if [ "$migration_success" = false ]; then
        rollback_migrations
        exit 1
    fi

    # Apply Migration #3
    log_section "Migration 3/4: AddDataValidationConstraints"
    if apply_migration "${MIGRATIONS[2]}"; then
        if verify_migration "${MIGRATIONS[2]}" && verify_check_constraints; then
            log_success "Migration #3 completed successfully"
        else
            log_error "Migration #3 verification failed"
            migration_success=false
        fi
    else
        log_error "Migration #3 application failed"
        migration_success=false
    fi

    if [ "$migration_success" = false ]; then
        rollback_migrations
        exit 1
    fi

    # Apply Migration #4
    log_section "Migration 4/4: AddColumnLevelEncryption"
    if apply_migration "${MIGRATIONS[3]}"; then
        if verify_migration "${MIGRATIONS[3]}"; then
            log_success "Migration #4 completed successfully"
        else
            log_error "Migration #4 verification failed"
            migration_success=false
        fi
    else
        log_error "Migration #4 application failed"
        migration_success=false
    fi

    if [ "$migration_success" = false ]; then
        rollback_migrations
        exit 1
    fi

    # Phase 3: Post-Deployment Verification
    log_section "Phase 3: Post-Deployment Verification"

    # Verify all migrations applied
    local current_migration
    current_migration=$(get_current_migration)
    log_info "Current migration: $current_migration"

    if [ "$current_migration" = "${MIGRATIONS[3]}" ]; then
        log_success "All migrations applied successfully"
    else
        log_error "Migration state mismatch - expected ${MIGRATIONS[3]}, got $current_migration"
        migration_success=false
    fi

    # Final Summary
    log_section "Deployment Summary"

    if [ "$migration_success" = true ]; then
        log_success "=========================================="
        log_success "DEPLOYMENT COMPLETED SUCCESSFULLY"
        log_success "=========================================="
        log_info "Migrations applied:"
        for migration in "${MIGRATIONS[@]}"; do
            log_info "  ✓ $migration"
        done
        log_info "Deployment completed at: $(date)"
        log_info "Log file: $LOG_FILE"
        log_success "=========================================="
        exit 0
    else
        log_error "=========================================="
        log_error "DEPLOYMENT FAILED"
        log_error "=========================================="
        log_error "Deployment failed at: $(date)"
        log_error "Log file: $LOG_FILE"
        log_error "Please review the log for details"
        log_error "=========================================="
        exit 1
    fi
}

# Execute main function
main "$@"
