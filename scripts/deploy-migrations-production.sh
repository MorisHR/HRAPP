#!/bin/bash

################################################################################
# HRMS Database Migration Deployment Script - PRODUCTION
################################################################################
#
# Purpose: Deploy 4 database migrations to PRODUCTION environment
# Migrations:
#   1. 20251112_AddNationalIdUniqueConstraint
#   2. 20251112_AddMissingCompositeIndexes
#   3. 20251112_AddDataValidationConstraints
#   4. 20251112031109_AddColumnLevelEncryption
#
# Safety Features:
#   - Pre-deployment validation
#   - Data quality checks
#   - Automated duplicate resolution prompts
#   - Step-by-step deployment with verification
#   - Automatic rollback on failure
#   - Maintenance mode support
#   - Comprehensive logging
#   - Stakeholder notifications
#
# Usage:
#   ./scripts/deploy-migrations-production.sh [--skip-maintenance-mode]
#
# Environment Variables (required):
#   DB_HOST              - Database host
#   DB_PORT              - Database port (default: 5432)
#   DB_NAME              - Database name (default: hrms_db)
#   DB_USER              - Database user (default: postgres)
#   DB_PASSWORD          - Database password (required)
#   BACKUP_DIR           - Backup directory (default: /backup/database)
#   API_URL              - API URL for health checks (default: http://localhost:5000)
#
# Optional:
#   NOTIFICATION_EMAIL   - Email for deployment notifications
#   SLACK_WEBHOOK_URL    - Slack webhook for notifications
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
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Environment configuration
DB_HOST="${DB_HOST:-}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-hrms_db}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_DIR="${BACKUP_DIR:-/backup/database}"
API_URL="${API_URL:-http://localhost:5000}"

# Notification configuration
NOTIFICATION_EMAIL="${NOTIFICATION_EMAIL:-}"
SLACK_WEBHOOK_URL="${SLACK_WEBHOOK_URL:-}"

# Script options
SKIP_MAINTENANCE_MODE=false
if [ "${1:-}" = "--skip-maintenance-mode" ]; then
    SKIP_MAINTENANCE_MODE=true
fi

# Project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
INFRASTRUCTURE_PROJECT="$PROJECT_ROOT/src/HRMS.Infrastructure"
API_PROJECT="$PROJECT_ROOT/src/HRMS.API"

# Deployment configuration
DEPLOYMENT_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="${BACKUP_DIR}/migration_production_${DEPLOYMENT_TIMESTAMP}.log"
CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};CommandTimeout=300;SSL Mode=Require;Trust Server Certificate=false"

# Migration list (in order)
declare -a MIGRATIONS=(
    "20251112_AddNationalIdUniqueConstraint"
    "20251112_AddMissingCompositeIndexes"
    "20251112_AddDataValidationConstraints"
    "20251112031109_AddColumnLevelEncryption"
)

# Deployment start time
DEPLOYMENT_START=$(date +%s)

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

log_critical() {
    log "${MAGENTA}[CRITICAL]${NC} $1"
}

log_section() {
    local title="$1"
    log ""
    log "=========================================="
    log "$title"
    log "=========================================="
}

# ============================================================================
# NOTIFICATION FUNCTIONS
# ============================================================================

send_notification() {
    local title="$1"
    local message="$2"
    local severity="${3:-INFO}"  # INFO, WARNING, ERROR, CRITICAL

    log_info "Sending notification: $title"

    # Email notification
    if [ -n "$NOTIFICATION_EMAIL" ]; then
        echo "$message" | mail -s "[HRMS Production] $title" "$NOTIFICATION_EMAIL" || true
    fi

    # Slack notification
    if [ -n "$SLACK_WEBHOOK_URL" ]; then
        local color="good"
        case $severity in
            WARNING) color="warning" ;;
            ERROR|CRITICAL) color="danger" ;;
        esac

        curl -X POST "$SLACK_WEBHOOK_URL" \
            -H 'Content-Type: application/json' \
            -d "{
                \"attachments\": [{
                    \"color\": \"$color\",
                    \"title\": \"$title\",
                    \"text\": \"$message\",
                    \"footer\": \"HRMS Production Deployment\",
                    \"ts\": $(date +%s)
                }]
            }" &> /dev/null || true
    fi
}

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

check_prerequisites() {
    log_section "Checking Prerequisites"

    # Check if running as root or with sufficient privileges
    if [ "$EUID" -eq 0 ]; then
        log_warning "Running as root - this is not recommended"
    fi

    # Check required environment variables
    if [ -z "$DB_HOST" ]; then
        log_error "DB_HOST environment variable is required"
        exit 1
    fi

    if [ -z "$DB_PASSWORD" ]; then
        log_error "DB_PASSWORD environment variable is required"
        exit 1
    fi

    # Check required commands
    local required_commands=("dotnet" "psql" "pg_dump" "curl")
    for cmd in "${required_commands[@]}"; do
        if ! command -v "$cmd" &> /dev/null; then
            log_error "Required command not found: $cmd"
            exit 1
        fi
        log_success "Found command: $cmd"
    done

    # Create backup directory
    if [ ! -d "$BACKUP_DIR" ]; then
        log_warning "Backup directory does not exist: $BACKUP_DIR"
        log_info "Creating backup directory..."
        mkdir -p "$BACKUP_DIR"
    fi
    log_success "Backup directory: $BACKUP_DIR"

    # Check disk space (need at least 10GB)
    local available_space
    available_space=$(df -BG "$BACKUP_DIR" | tail -1 | awk '{print $4}' | sed 's/G//')
    if [ "$available_space" -lt 10 ]; then
        log_error "Insufficient disk space: ${available_space}GB (need at least 10GB)"
        exit 1
    fi
    log_success "Available disk space: ${available_space}GB"

    # Check database connectivity
    log_info "Testing database connection..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT version();" &> /dev/null; then
        log_success "Database connection successful"
    else
        log_error "Cannot connect to database"
        exit 1
    fi

    # Check API connectivity
    log_info "Testing API connectivity..."
    if curl -f -s "${API_URL}/health" &> /dev/null; then
        log_success "API is accessible"
    else
        log_warning "API health check failed - this is expected if API is down for maintenance"
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

# ============================================================================
# MAINTENANCE MODE FUNCTIONS
# ============================================================================

enable_maintenance_mode() {
    if [ "$SKIP_MAINTENANCE_MODE" = true ]; then
        log_warning "Skipping maintenance mode (--skip-maintenance-mode flag set)"
        return 0
    fi

    log_section "Enabling Maintenance Mode"

    # Try to enable via API
    if curl -f -s -X POST "${API_URL}/admin/maintenance/enable" &> /dev/null; then
        log_success "Maintenance mode enabled via API"
    else
        log_warning "Could not enable maintenance mode via API"
        log_warning "Ensure API is in maintenance mode before proceeding"

        # Prompt user to confirm
        read -p "Press ENTER to continue once maintenance mode is enabled manually, or Ctrl+C to abort: "
    fi

    # Verify maintenance mode
    sleep 2
    if curl -s "${API_URL}/health" | grep -q "503\|maintenance"; then
        log_success "Maintenance mode verified"
    else
        log_warning "Could not verify maintenance mode status"
    fi
}

disable_maintenance_mode() {
    if [ "$SKIP_MAINTENANCE_MODE" = true ]; then
        log_warning "Skipping maintenance mode disable (--skip-maintenance-mode flag set)"
        return 0
    fi

    log_section "Disabling Maintenance Mode"

    # Try to disable via API
    if curl -f -s -X POST "${API_URL}/admin/maintenance/disable" &> /dev/null; then
        log_success "Maintenance mode disabled via API"
    else
        log_warning "Could not disable maintenance mode via API"
        log_warning "Please disable maintenance mode manually"
    fi

    # Verify application is accessible
    sleep 2
    if curl -f -s "${API_URL}/health" &> /dev/null; then
        log_success "Application is accessible"
    else
        log_warning "Application health check failed"
    fi
}

# ============================================================================
# PRE-DEPLOYMENT FUNCTIONS
# ============================================================================

check_data_quality() {
    log_section "Data Quality Validation"

    local issues_found=0
    local auto_fixable=0

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
        log_error "This MUST be resolved before proceeding"

        # Show duplicate details
        log_info "Duplicate National IDs:"
        execute_sql "
            SELECT
                \"NationalIdCard\",
                STRING_AGG(\"Id\"::text || ' (' || \"FirstName\" || ' ' || \"LastName\" || ')', ', ') as employees,
                COUNT(*) as count
            FROM tenant_default.\"Employees\"
            WHERE \"NationalIdCard\" IS NOT NULL
              AND \"IsDeleted\" = false
            GROUP BY \"NationalIdCard\"
            HAVING COUNT(*) > 1
            ORDER BY count DESC
            LIMIT 10;
        " | tee -a "$LOG_FILE"

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
        log_error "This MUST be resolved before proceeding"
        issues_found=$((issues_found + 1))
    else
        log_success "No duplicate Passport Numbers found"
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

    if [ "$duplicate_bank_accounts" -gt 0 ]; then
        log_error "Found $duplicate_bank_accounts duplicate Bank Account Numbers"
        log_error "This MUST be resolved before proceeding"
        issues_found=$((issues_found + 1))
    else
        log_success "No duplicate Bank Account Numbers found"
    fi

    # Check for negative salaries (auto-fixable)
    log_info "Checking for negative salaries..."
    local negative_salaries
    negative_salaries=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"Employees\"
        WHERE \"BasicSalary\" < 0;
    " | tr -d ' ')

    if [ "$negative_salaries" -gt 0 ]; then
        log_warning "Found $negative_salaries employees with negative salaries (AUTO-FIXABLE)"
        auto_fixable=$((auto_fixable + 1))
    else
        log_success "No negative salaries found"
    fi

    # Check for negative leave balances (auto-fixable)
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
        log_warning "Found $negative_leaves employees with negative leave balances (AUTO-FIXABLE)"
        auto_fixable=$((auto_fixable + 1))
    else
        log_success "No negative leave balances found"
    fi

    # Check for invalid months (critical issue)
    log_info "Checking for invalid months in payroll cycles..."
    local invalid_months
    invalid_months=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"PayrollCycles\"
        WHERE \"Month\" < 1 OR \"Month\" > 12;
    " | tr -d ' ')

    if [ "$invalid_months" -gt 0 ]; then
        log_error "Found $invalid_months payroll cycles with invalid months"
        log_error "This MUST be resolved before proceeding"
        issues_found=$((issues_found + 1))
    else
        log_success "No invalid months found"
    fi

    # Summary
    if [ $issues_found -gt 0 ]; then
        log_error "=========================================="
        log_error "DATA QUALITY VALIDATION FAILED"
        log_error "=========================================="
        log_error "Found $issues_found critical issues that MUST be resolved"
        log_error "Deployment CANNOT proceed until these are fixed"
        log_error "=========================================="
        return 1
    fi

    if [ $auto_fixable -gt 0 ]; then
        log_warning "Found $auto_fixable auto-fixable issues"
        log_warning "These will be automatically fixed during deployment"
    fi

    log_success "Data quality validation passed"
    return 0
}

auto_fix_data_issues() {
    log_section "Auto-Fixing Data Issues"

    # Fix negative salaries
    log_info "Fixing negative salaries..."
    local fixed_salaries
    fixed_salaries=$(execute_sql "
        UPDATE tenant_default.\"Employees\"
        SET \"BasicSalary\" = 0
        WHERE \"BasicSalary\" < 0
        RETURNING \"Id\";
    " | wc -l)

    if [ "$fixed_salaries" -gt 0 ]; then
        log_success "Fixed $fixed_salaries negative salaries"
    fi

    # Fix negative leave balances
    log_info "Fixing negative leave balances..."
    local fixed_leaves
    fixed_leaves=$(execute_sql "
        UPDATE tenant_default.\"Employees\"
        SET \"AnnualLeaveBalance\" = GREATEST(\"AnnualLeaveBalance\", 0),
            \"SickLeaveBalance\" = GREATEST(\"SickLeaveBalance\", 0),
            \"CasualLeaveBalance\" = GREATEST(\"CasualLeaveBalance\", 0)
        WHERE \"AnnualLeaveBalance\" < 0
           OR \"SickLeaveBalance\" < 0
           OR \"CasualLeaveBalance\" < 0
        RETURNING \"Id\";
    " | wc -l)

    if [ "$fixed_leaves" -gt 0 ]; then
        log_success "Fixed $fixed_leaves negative leave balances"
    fi

    if [ "$fixed_salaries" -eq 0 ] && [ "$fixed_leaves" -eq 0 ]; then
        log_info "No auto-fixable issues found"
    fi
}

create_backup() {
    log_section "Creating Database Backup"

    local backup_file="${BACKUP_DIR}/hrms_production_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

    log_info "Creating backup: $backup_file"
    log_info "This may take 10-30 minutes depending on database size..."
    log_info "Started at: $(date)"

    # Send notification
    send_notification \
        "Backup Started" \
        "Database backup initiated for production migration deployment" \
        "INFO"

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
            log_info "Completed at: $(date)"

            # Create checksum
            local checksum_file="${backup_file}.sha256"
            sha256sum "$backup_file" > "$checksum_file"
            log_info "Backup checksum: $(cat "$checksum_file")"

            # Export backup path for rollback
            echo "$backup_file" > "${BACKUP_DIR}/latest_backup.txt"

            # Test backup integrity
            log_info "Testing backup integrity..."
            if pg_restore --list "$backup_file" > /dev/null 2>&1; then
                log_success "Backup integrity verified"
            else
                log_error "Backup integrity check failed"
                return 1
            fi

            # Send notification
            send_notification \
                "Backup Completed" \
                "Database backup completed successfully (Size: $backup_size)" \
                "INFO"

            return 0
        else
            log_error "Backup file is empty or does not exist"
            send_notification \
                "Backup Failed" \
                "Database backup failed - file is empty or does not exist" \
                "CRITICAL"
            return 1
        fi
    else
        log_error "Backup creation failed"
        send_notification \
            "Backup Failed" \
            "Database backup command failed" \
            "CRITICAL"
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
    log_info "Started at: $(date)"

    # Send notification
    send_notification \
        "Migration Started: $migration_name" \
        "Applying migration $migration_name to production database" \
        "INFO"

    # Use dotnet ef to update to specific migration
    if dotnet ef database update "$migration_name" \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --context TenantDbContext \
        --connection "$CONNECTION_STRING" \
        --verbose 2>&1 | tee -a "$LOG_FILE"; then

        log_success "Migration applied: $migration_name"
        log_info "Completed at: $(date)"

        send_notification \
            "Migration Completed: $migration_name" \
            "Successfully applied migration $migration_name" \
            "INFO"

        return 0
    else
        log_error "Migration failed: $migration_name"

        send_notification \
            "Migration Failed: $migration_name" \
            "CRITICAL: Migration $migration_name failed. Check logs immediately." \
            "CRITICAL"

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

test_application_health() {
    log_info "Testing application health..."

    # Wait for application to restart (if needed)
    sleep 5

    # Test health endpoint
    if curl -f -s "${API_URL}/health" &> /dev/null; then
        log_success "Application health check passed"
        return 0
    else
        log_error "Application health check failed"
        return 1
    fi
}

# ============================================================================
# ROLLBACK FUNCTIONS
# ============================================================================

rollback_migrations() {
    log_section "INITIATING EMERGENCY ROLLBACK"

    send_notification \
        "ROLLBACK INITIATED" \
        "CRITICAL: Production migration rollback initiated due to deployment failure" \
        "CRITICAL"

    log_critical "Rolling back all migrations..."
    log_critical "This will restore the database to pre-migration state"

    local backup_file
    if [ -f "${BACKUP_DIR}/latest_backup.txt" ]; then
        backup_file=$(cat "${BACKUP_DIR}/latest_backup.txt")
        log_info "Restoring from backup: $backup_file"

        if [ -f "$backup_file" ]; then
            # Verify backup before restoring
            log_info "Verifying backup integrity..."
            if ! pg_restore --list "$backup_file" > /dev/null 2>&1; then
                log_critical "Backup is corrupted - cannot rollback!"
                send_notification \
                    "ROLLBACK FAILED - BACKUP CORRUPTED" \
                    "EMERGENCY: Backup file is corrupted. Manual intervention required immediately." \
                    "CRITICAL"
                return 1
            fi

            log_info "Terminating active connections..."
            execute_sql "
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '$DB_NAME'
                  AND pid <> pg_backend_pid();
            " > /dev/null 2>&1 || true

            log_info "Dropping database..."
            PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
                -c "DROP DATABASE IF EXISTS $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"

            log_info "Recreating database..."
            PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
                -c "CREATE DATABASE $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"

            log_info "Restoring backup (this may take 10-30 minutes)..."
            if PGPASSWORD="$DB_PASSWORD" pg_restore \
                -h "$DB_HOST" \
                -p "$DB_PORT" \
                -U "$DB_USER" \
                -d "$DB_NAME" \
                -v \
                "$backup_file" 2>&1 | tee -a "$LOG_FILE"; then

                log_success "Rollback completed - database restored from backup"

                send_notification \
                    "Rollback Completed" \
                    "Database successfully restored from backup. Production system recovered." \
                    "WARNING"

                return 0
            else
                log_critical "Backup restoration failed!"

                send_notification \
                    "ROLLBACK FAILED" \
                    "EMERGENCY: Database restoration failed. Manual intervention required IMMEDIATELY." \
                    "CRITICAL"

                return 1
            fi
        else
            log_critical "Backup file not found: $backup_file"

            send_notification \
                "ROLLBACK FAILED - BACKUP NOT FOUND" \
                "EMERGENCY: Backup file not found. Manual intervention required IMMEDIATELY." \
                "CRITICAL"

            return 1
        fi
    else
        log_critical "No backup reference found"

        send_notification \
            "ROLLBACK FAILED - NO BACKUP" \
            "EMERGENCY: No backup reference found. Manual intervention required IMMEDIATELY." \
            "CRITICAL"

        return 1
    fi
}

# ============================================================================
# MAIN DEPLOYMENT FLOW
# ============================================================================

main() {
    log_section "HRMS Database Migration Deployment - PRODUCTION"
    log_info "=========================================="
    log_info "Deployment started at: $(date)"
    log_info "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    log_info "Log file: $LOG_FILE"
    log_info "=========================================="

    # Send deployment start notification
    send_notification \
        "Production Deployment Started" \
        "Database migration deployment to production has begun. Expected duration: 2-4 hours." \
        "INFO"

    # Phase 1: Pre-Deployment
    log_section "Phase 1: Pre-Deployment Checks"

    if ! check_prerequisites; then
        log_error "Prerequisites check failed"
        send_notification \
            "Deployment Aborted - Prerequisites Failed" \
            "Production deployment aborted due to failed prerequisite checks." \
            "ERROR"
        exit 1
    fi

    # Enable maintenance mode
    enable_maintenance_mode

    # Data quality validation
    if ! check_data_quality; then
        log_error "Data quality validation failed"
        log_error "Please fix data quality issues and try again"

        send_notification \
            "Deployment Aborted - Data Quality Issues" \
            "Production deployment aborted due to data quality issues. Manual intervention required." \
            "ERROR"

        disable_maintenance_mode
        exit 1
    fi

    # Auto-fix issues if any
    auto_fix_data_issues

    # Create backup
    if ! create_backup; then
        log_error "Backup creation failed"
        log_error "Cannot proceed without backup"

        send_notification \
            "Deployment Aborted - Backup Failed" \
            "Production deployment aborted due to backup failure. System is still in maintenance mode." \
            "CRITICAL"

        disable_maintenance_mode
        exit 1
    fi

    log_success "Pre-deployment phase completed"

    # Confirmation prompt
    log_warning "=========================================="
    log_warning "READY TO DEPLOY TO PRODUCTION"
    log_warning "=========================================="
    log_warning "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    log_warning "Backup: ${BACKUP_DIR}/hrms_production_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"
    log_warning ""
    read -p "Type 'DEPLOY' to proceed with production deployment: " confirmation

    if [ "$confirmation" != "DEPLOY" ]; then
        log_warning "Deployment aborted by user"
        disable_maintenance_mode
        exit 0
    fi

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
        disable_maintenance_mode
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
        disable_maintenance_mode
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
        disable_maintenance_mode
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
        disable_maintenance_mode
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
        rollback_migrations
        disable_maintenance_mode
        exit 1
    fi

    # Disable maintenance mode
    disable_maintenance_mode

    # Test application health
    if ! test_application_health; then
        log_error "Application health check failed after migrations"
        log_warning "Database migrations succeeded but application is not responding"
        log_warning "Manual intervention may be required"
    fi

    # Calculate deployment duration
    DEPLOYMENT_END=$(date +%s)
    DEPLOYMENT_DURATION=$((DEPLOYMENT_END - DEPLOYMENT_START))
    DEPLOYMENT_DURATION_MIN=$((DEPLOYMENT_DURATION / 60))

    # Final Summary
    log_section "Deployment Summary"

    if [ "$migration_success" = true ]; then
        log_success "=========================================="
        log_success "PRODUCTION DEPLOYMENT COMPLETED SUCCESSFULLY"
        log_success "=========================================="
        log_info "Migrations applied:"
        for migration in "${MIGRATIONS[@]}"; do
            log_info "  ✓ $migration"
        done
        log_info ""
        log_info "Deployment started: $(date -d @$DEPLOYMENT_START)"
        log_info "Deployment completed: $(date)"
        log_info "Total duration: ${DEPLOYMENT_DURATION_MIN} minutes"
        log_info "Log file: $LOG_FILE"
        log_info "Backup: ${BACKUP_DIR}/hrms_production_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"
        log_success "=========================================="

        send_notification \
            "Production Deployment SUCCESS" \
            "All 4 database migrations deployed successfully to production. Total duration: ${DEPLOYMENT_DURATION_MIN} minutes. System is operational." \
            "INFO"

        exit 0
    else
        log_error "=========================================="
        log_error "PRODUCTION DEPLOYMENT FAILED"
        log_error "=========================================="
        log_error "Deployment failed at: $(date)"
        log_error "Duration before failure: ${DEPLOYMENT_DURATION_MIN} minutes"
        log_error "Log file: $LOG_FILE"
        log_error "Please review the log for details"
        log_error "Rollback may have been initiated"
        log_error "=========================================="

        send_notification \
            "Production Deployment FAILED" \
            "CRITICAL: Production deployment failed. Check logs immediately. Rollback may have been initiated." \
            "CRITICAL"

        exit 1
    fi
}

# Trap errors and send critical notification
trap 'log_critical "Deployment script crashed unexpectedly"; send_notification "Deployment Script Crashed" "EMERGENCY: Deployment script terminated unexpectedly. Manual intervention required." "CRITICAL"' ERR

# Execute main function
main "$@"
