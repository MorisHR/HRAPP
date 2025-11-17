#!/bin/bash

################################################################################
# Script: add-onpush-detection.sh
# Description: Automatically add OnPush change detection to all Angular components
# Author: Performance Engineering Team
# Date: 2025-11-17
# Version: 1.0
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="/workspaces/HRAPP/hrms-frontend"
SRC_DIR="${PROJECT_ROOT}/src/app"
BACKUP_DIR="${PROJECT_ROOT}/.onpush-backup-$(date +%Y%m%d-%H%M%S)"
LOG_FILE="/workspaces/HRAPP/onpush-migration.log"

# Counters
TOTAL_FILES=0
MODIFIED_FILES=0
SKIPPED_FILES=0
ERROR_FILES=0

################################################################################
# Helper Functions
################################################################################

log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

log_warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING:${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR:${NC} $1" | tee -a "$LOG_FILE"
}

log_info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] INFO:${NC} $1" | tee -a "$LOG_FILE"
}

################################################################################
# Backup Function
################################################################################

create_backup() {
    log "Creating backup at: $BACKUP_DIR"
    mkdir -p "$BACKUP_DIR"
    cp -r "$SRC_DIR" "$BACKUP_DIR/"
    log "Backup created successfully"
}

################################################################################
# Main Migration Function
################################################################################

add_onpush_to_component() {
    local file="$1"
    local filename=$(basename "$file")

    TOTAL_FILES=$((TOTAL_FILES + 1))

    # Check if already has OnPush
    if grep -q "ChangeDetectionStrategy.OnPush" "$file"; then
        log_info "SKIP: $filename (already has OnPush)"
        SKIPPED_FILES=$((SKIPPED_FILES + 1))
        return 0
    fi

    # Check if file has @Component decorator
    if ! grep -q "@Component" "$file"; then
        log_warn "SKIP: $filename (no @Component decorator found)"
        SKIPPED_FILES=$((SKIPPED_FILES + 1))
        return 0
    fi

    log "Processing: $filename"

    # Create temporary file
    local temp_file="${file}.tmp"

    # Step 1: Add import if not present
    if ! grep -q "ChangeDetectionStrategy" "$file"; then
        # Check if there's an existing import from @angular/core
        if grep -q "import.*{.*}.*from '@angular/core'" "$file"; then
            # Add ChangeDetectionStrategy to existing import
            sed -i "s/import[[:space:]]*{[[:space:]]*\([^}]*\)[[:space:]]*}[[:space:]]*from[[:space:]]*'@angular\/core'/import { \1, ChangeDetectionStrategy } from '@angular\/core'/" "$file"
        else
            # Add new import at the top
            sed -i "1i import { ChangeDetectionStrategy } from '@angular/core';" "$file"
        fi
    fi

    # Step 2: Add changeDetection property to @Component decorator
    # This works by finding the @Component({ and adding changeDetection property
    if grep -q "@Component({" "$file"; then
        # Use awk to add changeDetection property
        awk '
        /@Component\(\{/ {
            print
            in_decorator=1
            next
        }
        in_decorator && /^[[:space:]]*selector:/ {
            print
            print "  changeDetection: ChangeDetectionStrategy.OnPush,"
            in_decorator=0
            next
        }
        { print }
        ' "$file" > "$temp_file"

        mv "$temp_file" "$file"
    else
        log_error "Could not find @Component({ in $filename"
        ERROR_FILES=$((ERROR_FILES + 1))
        return 1
    fi

    # Verify the change
    if grep -q "ChangeDetectionStrategy.OnPush" "$file"; then
        log "✅ SUCCESS: $filename"
        MODIFIED_FILES=$((MODIFIED_FILES + 1))
        echo "  - $file" >> "${LOG_FILE}.modified"
    else
        log_error "FAILED: $filename (verification failed)"
        ERROR_FILES=$((ERROR_FILES + 1))
        return 1
    fi
}

################################################################################
# Main Execution
################################################################################

main() {
    echo ""
    echo "╔═══════════════════════════════════════════════════════════════════════╗"
    echo "║        Angular OnPush Change Detection Migration Tool                 ║"
    echo "║        Performance Optimization - Fortune 500 Implementation          ║"
    echo "╚═══════════════════════════════════════════════════════════════════════╝"
    echo ""

    # Initialize log files
    > "$LOG_FILE"
    > "${LOG_FILE}.modified"

    log "Starting OnPush migration process"
    log "Project root: $PROJECT_ROOT"
    log "Source directory: $SRC_DIR"

    # Create backup
    create_backup

    # Find all component files
    log "Finding all component TypeScript files..."
    component_files=$(find "$SRC_DIR" -type f -name "*.component.ts")
    total_count=$(echo "$component_files" | wc -l)

    log "Found $total_count component files"
    echo ""

    # Process each component
    log "Processing components..."
    echo ""

    while IFS= read -r file; do
        add_onpush_to_component "$file" || true
    done <<< "$component_files"

    # Summary Report
    echo ""
    echo "╔═══════════════════════════════════════════════════════════════════════╗"
    echo "║                        Migration Summary                              ║"
    echo "╚═══════════════════════════════════════════════════════════════════════╝"
    echo ""
    printf "  Total Files Processed:    ${BLUE}%3d${NC}\n" $TOTAL_FILES
    printf "  Successfully Modified:    ${GREEN}%3d${NC}\n" $MODIFIED_FILES
    printf "  Skipped (Already OnPush): ${YELLOW}%3d${NC}\n" $SKIPPED_FILES
    printf "  Errors:                   ${RED}%3d${NC}\n" $ERROR_FILES
    echo ""

    if [ $MODIFIED_FILES -gt 0 ]; then
        echo -e "${GREEN}✅ Migration completed successfully!${NC}"
        echo ""
        echo "Modified files list saved to: ${LOG_FILE}.modified"
        echo "Full log saved to: $LOG_FILE"
        echo "Backup created at: $BACKUP_DIR"
        echo ""
        echo "Next Steps:"
        echo "  1. Run: cd $PROJECT_ROOT && npm run build"
        echo "  2. Test the application thoroughly"
        echo "  3. Check for any runtime errors in browser console"
        echo "  4. If issues occur, restore from backup: cp -r $BACKUP_DIR/app/* $SRC_DIR/"
        echo ""
        echo "⚠️  IMPORTANT: Test all components to ensure they update correctly!"
        echo "    Components using OnPush require immutable data patterns or manual change detection."
    else
        echo -e "${YELLOW}ℹ️  No files were modified (all already have OnPush or no components found)${NC}"
    fi

    echo ""
}

# Run main function
main

exit 0
