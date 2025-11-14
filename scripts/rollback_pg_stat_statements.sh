#!/bin/bash
#
# Rollback pg_stat_statements Configuration
#
# This script safely rolls back pg_stat_statements configuration changes
# by restoring a previous backup of postgresql.conf
#
# Usage: sudo bash rollback_pg_stat_statements.sh
#

set -e  # Exit on error

# Configuration
POSTGRES_VERSION="16"
CONFIG_FILE="/etc/postgresql/${POSTGRES_VERSION}/main/postgresql.conf"
BACKUP_DIR="/etc/postgresql/${POSTGRES_VERSION}/main/backups"

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

# Check if running as root or with sudo
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root or with sudo"
   exit 1
fi

print_header "=========================================="
print_header "pg_stat_statements Rollback Script"
print_header "=========================================="
echo ""

# Check if backup directory exists
if [ ! -d "$BACKUP_DIR" ]; then
    print_error "Backup directory not found: $BACKUP_DIR"
    echo ""
    print_info "No backups available. Manual configuration may be required."
    exit 1
fi

# List available backups
print_info "Available backups:"
echo ""

BACKUPS=$(ls -1t "$BACKUP_DIR"/postgresql.conf.backup.* 2>/dev/null || echo "")

if [ -z "$BACKUPS" ]; then
    print_error "No backup files found in $BACKUP_DIR"
    exit 1
fi

# Display backups with numbers
BACKUP_ARRAY=()
INDEX=1

while IFS= read -r backup; do
    BACKUP_ARRAY+=("$backup")
    BACKUP_DATE=$(echo "$backup" | sed 's/.*backup\.//')
    BACKUP_SIZE=$(stat -f%z "$backup" 2>/dev/null || stat -c%s "$backup" 2>/dev/null || echo "unknown")

    echo "  [$INDEX] $(basename $backup)"
    echo "      Date: $BACKUP_DATE"
    echo "      Size: $BACKUP_SIZE bytes"
    echo "      Path: $backup"
    echo ""

    INDEX=$((INDEX + 1))
done <<< "$BACKUPS"

# Ask user to select a backup
echo ""
print_warning "Select a backup to restore (or 0 to cancel):"
read -p "Enter backup number [1-$((INDEX-1))]: " BACKUP_CHOICE

if [ "$BACKUP_CHOICE" = "0" ]; then
    print_warning "Rollback cancelled by user"
    exit 0
fi

# Validate choice
if [ "$BACKUP_CHOICE" -lt 1 ] || [ "$BACKUP_CHOICE" -ge "$INDEX" ]; then
    print_error "Invalid backup number"
    exit 1
fi

# Get selected backup
SELECTED_BACKUP="${BACKUP_ARRAY[$((BACKUP_CHOICE-1))]}"

print_info "Selected backup: $(basename $SELECTED_BACKUP)"
echo ""

# Show preview of changes
print_info "Current shared_preload_libraries setting:"
CURRENT=$(grep "^shared_preload_libraries" "$CONFIG_FILE" || echo "Not found")
echo "  $CURRENT"
echo ""

print_info "Backup shared_preload_libraries setting:"
BACKUP=$(grep "^shared_preload_libraries" "$SELECTED_BACKUP" || echo "Not found")
echo "  $BACKUP"
echo ""

# Confirm rollback
print_warning "This will replace the current configuration with the selected backup"
print_warning "PostgreSQL restart will be required after rollback"
echo ""
read -p "Do you want to proceed with rollback? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    print_warning "Rollback cancelled by user"
    exit 0
fi

echo ""
print_info "Creating safety backup of current configuration..."
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
SAFETY_BACKUP="${BACKUP_DIR}/postgresql.conf.pre-rollback.${TIMESTAMP}"

cp "$CONFIG_FILE" "$SAFETY_BACKUP"

if [ $? -eq 0 ]; then
    print_success "Safety backup created: $SAFETY_BACKUP"
else
    print_error "Failed to create safety backup"
    exit 1
fi

# Perform rollback
print_info "Restoring configuration from backup..."
cp "$SELECTED_BACKUP" "$CONFIG_FILE"

if [ $? -eq 0 ]; then
    print_success "Configuration restored successfully"
else
    print_error "Failed to restore configuration"
    print_info "Restoring safety backup..."
    cp "$SAFETY_BACKUP" "$CONFIG_FILE"
    exit 1
fi

# Set proper permissions
chown postgres:postgres "$CONFIG_FILE"
chmod 644 "$CONFIG_FILE"

# Verify
print_info "Verifying restored configuration..."
NEW_VALUE=$(grep "^shared_preload_libraries" "$CONFIG_FILE" || echo "Not found")
echo "  Restored setting: $NEW_VALUE"
echo ""

print_success "Rollback completed successfully!"
echo ""
print_warning "IMPORTANT: PostgreSQL must be restarted for changes to take effect"
echo ""
echo "To restart PostgreSQL, run ONE of these commands:"
echo "  sudo systemctl restart postgresql@16-main      # For systemd systems"
echo "  sudo service postgresql restart                # For container environments"
echo "  sudo pg_ctlcluster 16 main restart            # Using pg_ctlcluster"
echo ""

read -p "Do you want to restart PostgreSQL now? (yes/no): " RESTART_NOW

if [ "$RESTART_NOW" = "yes" ]; then
    echo ""
    echo "Choose restart method:"
    echo "  1) service postgresql restart (recommended for containers)"
    echo "  2) systemctl restart postgresql@16-main (for systemd)"
    echo "  3) pg_ctlcluster 16 main restart"
    echo ""

    read -p "Enter choice (1-3): " RESTART_METHOD

    case $RESTART_METHOD in
        1)
            print_info "Restarting PostgreSQL using service command..."
            service postgresql restart
            ;;
        2)
            print_info "Restarting PostgreSQL using systemctl..."
            systemctl restart postgresql@16-main
            ;;
        3)
            print_info "Restarting PostgreSQL using pg_ctlcluster..."
            pg_ctlcluster 16 main restart
            ;;
        *)
            print_error "Invalid choice. Please restart manually."
            exit 1
            ;;
    esac

    if [ $? -eq 0 ]; then
        print_success "PostgreSQL restarted successfully"

        # Wait for PostgreSQL to be ready
        print_info "Waiting for PostgreSQL to be ready..."
        sleep 3

        if pg_isready -h localhost -p 5432 >/dev/null 2>&1; then
            print_success "PostgreSQL is ready"

            # Verify rollback
            CURRENT_LIBS=$(PGPASSWORD=postgres psql -h localhost -U postgres -d postgres -t -A -c "SHOW shared_preload_libraries;" 2>/dev/null || echo "ERROR")

            if [ "$CURRENT_LIBS" != "ERROR" ]; then
                echo ""
                print_info "Current shared_preload_libraries:"
                echo "  $CURRENT_LIBS"
                echo ""
                print_success "Rollback verified successfully"
            fi
        else
            print_warning "PostgreSQL is not responding. Please check status manually."
        fi
    else
        print_error "PostgreSQL restart failed. Please restart manually."
    fi
else
    print_info "Remember to restart PostgreSQL manually"
fi

echo ""
print_info "Safety backup saved at: $SAFETY_BACKUP"
echo ""

exit 0
