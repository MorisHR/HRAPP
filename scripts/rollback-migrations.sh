#!/bin/bash

################################################################################
# HRMS Database Migration Rollback Script
################################################################################
#
# Purpose: Rollback database migrations safely
# Usage:
#   ./scripts/rollback-migrations.sh [rollback_type]
#
# Rollback Types:
#   single              - Rollback last migration only
#   all                 - Rollback all 4 migrations
#   backup              - Restore from backup (complete rollback)
#   to-migration <name> - Rollback to specific migration
#
# Environment Variables:
#   DB_HOST              - Database host (default: localhost)
#   DB_PORT              - Database port (default: 5432)
#   DB_NAME              - Database name (default: hrms_db)
#   DB_USER              - Database user (default: postgres)
#   DB_PASSWORD          - Database password (required)
#   BACKUP_DIR           - Backup directory (default: /backup/database)
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
MAGENTA='\033[0;35m'
NC='\033[0m'

# Environment configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-hrms_db}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_DIR="${BACKUP_DIR:-/backup/database}"

# Rollback type (default: all)
ROLLBACK_TYPE="${1:-all}"
TARGET_MIGRATION="${2:-}"

# Project paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
INFRASTRUCTURE_PROJECT="$PROJECT_ROOT/src/HRMS.Infrastructure"
API_PROJECT="$PROJECT_ROOT/src/HRMS.API"

# Connection string
CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD};CommandTimeout=300"

# Timestamp
ROLLBACK_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="${BACKUP_DIR}/rollback_${ROLLBACK_TIMESTAMP}.log"

# Migration list (in reverse order for rollback)
declare -a MIGRATIONS=(
    "20251112031109_AddColumnLevelEncryption"
    "20251112_AddDataValidationConstraints"
    "20251112_AddMissingCompositeIndexes"
    "20251112_AddNationalIdUniqueConstraint"
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

log_critical() {
    log "${MAGENTA}[CRITICAL]${NC} $1"
}

log_section() {
    log ""
    log "=========================================="
    log "$1"
    log "=========================================="
}

# ============================================================================
# UTILITY FUNCTIONS
# ============================================================================

check_prerequisites() {
    log_section "Checking Prerequisites"

    # Check database password
    if [ -z "$DB_PASSWORD" ]; then
        log_error "DB_PASSWORD environment variable is required"
        exit 1
    fi

    # Check required commands
    local required_commands=("dotnet" "psql" "pg_restore")
    for cmd in "${required_commands[@]}"; do
        if ! command -v "$cmd" &> /dev/null; then
            log_error "Required command not found: $cmd"
            exit 1
        fi
    done

    # Check database connectivity
    log_info "Testing database connection..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" &> /dev/null; then
        log_success "Database connection successful"
    else
        log_error "Cannot connect to database"
        exit 1
    fi

    # Check backup directory exists
    if [ ! -d "$BACKUP_DIR" ]; then
        log_error "Backup directory not found: $BACKUP_DIR"
        exit 1
    fi

    log_success "All prerequisites met"
}

execute_sql() {
    local sql="$1"
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -c "$sql" 2>&1
}

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

# ============================================================================
# BACKUP FUNCTIONS
# ============================================================================

create_pre_rollback_backup() {
    log_section "Creating Pre-Rollback Backup"

    local backup_file="${BACKUP_DIR}/hrms_pre_rollback_${ROLLBACK_TIMESTAMP}.dump"

    log_warning "Creating backup before rollback (safety precaution)..."
    log_info "Backup file: $backup_file"

    if PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        -F c \
        -b \
        -v \
        -f "$backup_file" 2>&1 | tee -a "$LOG_FILE"; then

        if [ -f "$backup_file" ] && [ -s "$backup_file" ]; then
            local backup_size
            backup_size=$(du -h "$backup_file" | cut -f1)
            log_success "Pre-rollback backup created: $backup_size"

            # Create checksum
            sha256sum "$backup_file" > "${backup_file}.sha256"

            return 0
        else
            log_error "Pre-rollback backup failed"
            return 1
        fi
    else
        log_error "Pre-rollback backup command failed"
        return 1
    fi
}

find_latest_backup() {
    log_info "Searching for latest backup..."

    # Check for latest_backup.txt
    if [ -f "${BACKUP_DIR}/latest_backup.txt" ]; then
        local backup_file
        backup_file=$(cat "${BACKUP_DIR}/latest_backup.txt")

        if [ -f "$backup_file" ]; then
            log_success "Found backup reference: $backup_file"
            echo "$backup_file"
            return 0
        fi
    fi

    # Look for most recent backup file
    local latest_backup
    latest_backup=$(ls -t "${BACKUP_DIR}"/*.dump 2>/dev/null | head -n 1)

    if [ -n "$latest_backup" ] && [ -f "$latest_backup" ]; then
        log_success "Found latest backup: $latest_backup"
        echo "$latest_backup"
        return 0
    fi

    log_error "No backup found in: $BACKUP_DIR"
    return 1
}

restore_from_backup() {
    local backup_file="$1"

    log_section "Restoring from Backup"

    if [ ! -f "$backup_file" ]; then
        log_error "Backup file not found: $backup_file"
        return 1
    fi

    # Verify backup integrity
    log_info "Verifying backup integrity..."
    if ! pg_restore --list "$backup_file" > /dev/null 2>&1; then
        log_error "Backup file is corrupted"
        return 1
    fi
    log_success "Backup integrity verified"

    # Get backup size
    local backup_size
    backup_size=$(du -h "$backup_file" | cut -f1)
    log_info "Backup size: $backup_size"

    log_warning "=========================================="
    log_warning "ABOUT TO RESTORE DATABASE FROM BACKUP"
    log_warning "=========================================="
    log_warning "This will:"
    log_warning "  1. Terminate all active connections"
    log_warning "  2. Drop the current database"
    log_warning "  3. Recreate the database"
    log_warning "  4. Restore from backup"
    log_warning ""
    log_warning "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    log_warning "Backup: $backup_file"
    log_warning ""
    read -p "Type 'RESTORE' to proceed with backup restoration: " confirmation

    if [ "$confirmation" != "RESTORE" ]; then
        log_warning "Backup restoration cancelled by user"
        return 1
    fi

    # Terminate active connections
    log_info "Terminating active connections..."
    execute_sql "
        SELECT pg_terminate_backend(pid)
        FROM pg_stat_activity
        WHERE datname = '$DB_NAME'
          AND pid <> pg_backend_pid();
    " > /dev/null 2>&1 || true
    sleep 2

    # Drop database
    log_info "Dropping database..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
        -c "DROP DATABASE IF EXISTS $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"; then
        log_success "Database dropped"
    else
        log_error "Failed to drop database"
        return 1
    fi

    # Recreate database
    log_info "Recreating database..."
    if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres \
        -c "CREATE DATABASE $DB_NAME;" 2>&1 | tee -a "$LOG_FILE"; then
        log_success "Database created"
    else
        log_error "Failed to create database"
        return 1
    fi

    # Restore backup
    log_info "Restoring backup (this may take 10-30 minutes)..."
    log_info "Started at: $(date)"

    if PGPASSWORD="$DB_PASSWORD" pg_restore \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        -v \
        "$backup_file" 2>&1 | tee -a "$LOG_FILE"; then

        log_success "Backup restored successfully"
        log_info "Completed at: $(date)"
        return 0
    else
        log_error "Backup restoration failed"
        return 1
    fi
}

# ============================================================================
# ROLLBACK FUNCTIONS
# ============================================================================

rollback_single_migration() {
    log_section "Rolling Back Single Migration"

    local current_migration
    current_migration=$(get_current_migration)

    log_info "Current migration: $current_migration"

    # Find the previous migration
    local previous_migration
    previous_migration=$(execute_sql "
        SELECT \"MigrationId\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" < '$current_migration'
        ORDER BY \"MigrationId\" DESC
        LIMIT 1;
    " | tr -d ' ')

    if [ -z "$previous_migration" ]; then
        log_error "No previous migration found - cannot rollback"
        return 1
    fi

    log_info "Will rollback to: $previous_migration"
    log_warning "This will revert: $current_migration"

    read -p "Press ENTER to proceed with rollback, or Ctrl+C to abort: "

    # Execute rollback
    log_info "Executing rollback..."
    if dotnet ef database update "$previous_migration" \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --context TenantDbContext \
        --connection "$CONNECTION_STRING" \
        --verbose 2>&1 | tee -a "$LOG_FILE"; then

        log_success "Migration rolled back successfully"

        # Verify rollback
        local new_current
        new_current=$(get_current_migration)
        if [ "$new_current" = "$previous_migration" ]; then
            log_success "Rollback verified: $new_current"
            return 0
        else
            log_error "Rollback verification failed"
            return 1
        fi
    else
        log_error "Rollback failed"
        return 1
    fi
}

rollback_all_migrations() {
    log_section "Rolling Back All Migrations"

    local current_migration
    current_migration=$(get_current_migration)

    log_info "Current migration: $current_migration"

    # Check if any of our migrations are applied
    local our_migrations_applied=false
    for migration in "${MIGRATIONS[@]}"; do
        local count
        count=$(execute_sql "
            SELECT COUNT(*)
            FROM tenant_default.\"__EFMigrationsHistory\"
            WHERE \"MigrationId\" = '$migration';
        " | tr -d ' ')

        if [ "$count" -eq 1 ]; then
            our_migrations_applied=true
            log_info "Found migration to rollback: $migration"
        fi
    done

    if [ "$our_migrations_applied" = false ]; then
        log_warning "None of the target migrations are applied"
        log_info "Nothing to rollback"
        return 0
    fi

    # Find the migration before our first migration
    local target_migration
    target_migration=$(execute_sql "
        SELECT \"MigrationId\"
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" < '20251112_AddNationalIdUniqueConstraint'
        ORDER BY \"MigrationId\" DESC
        LIMIT 1;
    " | tr -d ' ')

    if [ -z "$target_migration" ]; then
        log_warning "No migration found before our changes"
        log_info "Will rollback all migrations"
    else
        log_info "Will rollback to: $target_migration"
    fi

    log_warning "=========================================="
    log_warning "ABOUT TO ROLLBACK ALL 4 MIGRATIONS"
    log_warning "=========================================="
    log_warning "This will revert:"
    for migration in "${MIGRATIONS[@]}"; do
        log_warning "  - $migration"
    done
    log_warning ""
    read -p "Type 'ROLLBACK' to proceed: " confirmation

    if [ "$confirmation" != "ROLLBACK" ]; then
        log_warning "Rollback cancelled by user"
        return 1
    fi

    # Execute rollback
    log_info "Executing rollback..."

    if [ -n "$target_migration" ]; then
        # Rollback to specific migration
        if dotnet ef database update "$target_migration" \
            --project "$INFRASTRUCTURE_PROJECT" \
            --startup-project "$API_PROJECT" \
            --context TenantDbContext \
            --connection "$CONNECTION_STRING" \
            --verbose 2>&1 | tee -a "$LOG_FILE"; then

            log_success "All migrations rolled back successfully"
            return 0
        else
            log_error "Rollback failed"
            return 1
        fi
    else
        # Rollback all migrations (risky)
        log_critical "Cannot rollback to a specific migration - this would require dropping all migrations"
        log_critical "Consider using 'backup' rollback type instead"
        return 1
    fi
}

rollback_to_migration() {
    local target="$1"

    if [ -z "$target" ]; then
        log_error "Target migration not specified"
        log_error "Usage: $0 to-migration <migration_name>"
        return 1
    fi

    log_section "Rolling Back to Specific Migration"

    log_info "Target migration: $target"

    # Verify target migration exists
    local target_exists
    target_exists=$(execute_sql "
        SELECT COUNT(*)
        FROM tenant_default.\"__EFMigrationsHistory\"
        WHERE \"MigrationId\" = '$target';
    " | tr -d ' ')

    if [ "$target_exists" -eq 0 ]; then
        log_error "Target migration not found in history: $target"
        return 1
    fi

    local current_migration
    current_migration=$(get_current_migration)

    log_info "Current migration: $current_migration"

    if [ "$current_migration" = "$target" ]; then
        log_warning "Already at target migration - nothing to do"
        return 0
    fi

    log_warning "This will rollback from $current_migration to $target"
    read -p "Press ENTER to proceed, or Ctrl+C to abort: "

    # Execute rollback
    log_info "Executing rollback..."
    if dotnet ef database update "$target" \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --context TenantDbContext \
        --connection "$CONNECTION_STRING" \
        --verbose 2>&1 | tee -a "$LOG_FILE"; then

        log_success "Rollback completed successfully"

        # Verify rollback
        local new_current
        new_current=$(get_current_migration)
        if [ "$new_current" = "$target" ]; then
            log_success "Rollback verified: $new_current"
            return 0
        else
            log_error "Rollback verification failed"
            return 1
        fi
    else
        log_error "Rollback failed"
        return 1
    fi
}

# ============================================================================
# MAIN FUNCTION
# ============================================================================

main() {
    log_section "HRMS Database Migration Rollback"
    log_info "Rollback type: $ROLLBACK_TYPE"
    log_info "Database: ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    log_info "Log file: $LOG_FILE"

    # Check prerequisites
    check_prerequisites

    # Create pre-rollback backup
    log_warning "Creating safety backup before rollback..."
    if ! create_pre_rollback_backup; then
        log_error "Failed to create pre-rollback backup"
        log_error "Aborting rollback for safety"
        exit 1
    fi

    # Execute rollback based on type
    case "$ROLLBACK_TYPE" in
        single)
            if rollback_single_migration; then
                log_success "=========================================="
                log_success "SINGLE MIGRATION ROLLBACK COMPLETED"
                log_success "=========================================="
                exit 0
            else
                log_error "Single migration rollback failed"
                exit 1
            fi
            ;;

        all)
            if rollback_all_migrations; then
                log_success "=========================================="
                log_success "ALL MIGRATIONS ROLLBACK COMPLETED"
                log_success "=========================================="
                exit 0
            else
                log_error "All migrations rollback failed"
                log_warning "Consider using 'backup' rollback type for complete restoration"
                exit 1
            fi
            ;;

        backup)
            log_info "Searching for backup to restore..."
            local backup_file
            backup_file=$(find_latest_backup)

            if [ -n "$backup_file" ]; then
                if restore_from_backup "$backup_file"; then
                    log_success "=========================================="
                    log_success "BACKUP RESTORATION COMPLETED"
                    log_success "=========================================="
                    log_info "Database restored from: $backup_file"
                    exit 0
                else
                    log_critical "Backup restoration failed"
                    exit 1
                fi
            else
                log_error "No backup found for restoration"
                exit 1
            fi
            ;;

        to-migration)
            if rollback_to_migration "$TARGET_MIGRATION"; then
                log_success "=========================================="
                log_success "ROLLBACK TO MIGRATION COMPLETED"
                log_success "=========================================="
                exit 0
            else
                log_error "Rollback to migration failed"
                exit 1
            fi
            ;;

        *)
            log_error "Unknown rollback type: $ROLLBACK_TYPE"
            echo ""
            echo "Usage: $0 [rollback_type] [options]"
            echo ""
            echo "Rollback types:"
            echo "  single              - Rollback last migration only"
            echo "  all                 - Rollback all 4 migrations"
            echo "  backup              - Restore from backup (complete rollback)"
            echo "  to-migration <name> - Rollback to specific migration"
            echo ""
            echo "Examples:"
            echo "  $0 single"
            echo "  $0 all"
            echo "  $0 backup"
            echo "  $0 to-migration 20251111125329_PreviousMigration"
            exit 1
            ;;
    esac
}

# Execute main function
main "$@"
