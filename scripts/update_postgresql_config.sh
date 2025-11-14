#!/bin/bash
#
# PostgreSQL Configuration Updater for pg_stat_statements
#
# This script safely updates postgresql.conf to enable pg_stat_statements
# by modifying the shared_preload_libraries parameter.
#
# Usage: sudo bash update_postgresql_config.sh
#

set -e  # Exit on error

# Configuration
POSTGRES_VERSION="16"
CONFIG_FILE="/etc/postgresql/${POSTGRES_VERSION}/main/postgresql.conf"
BACKUP_DIR="/etc/postgresql/${POSTGRES_VERSION}/main/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/postgresql.conf.backup.${TIMESTAMP}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Check if running as root or with sudo
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root or with sudo"
   exit 1
fi

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    print_error "PostgreSQL configuration file not found: $CONFIG_FILE"
    exit 1
fi

print_info "PostgreSQL pg_stat_statements Configuration Updater"
echo "=================================================="
echo ""
echo "Configuration file: $CONFIG_FILE"
echo "Backup location: $BACKUP_FILE"
echo ""

# Create backup directory if it doesn't exist
mkdir -p "$BACKUP_DIR"

# Backup current configuration
print_info "Creating backup of current configuration..."
cp "$CONFIG_FILE" "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    print_info "Backup created successfully: $BACKUP_FILE"
else
    print_error "Failed to create backup"
    exit 1
fi

# Check current shared_preload_libraries setting
print_info "Checking current shared_preload_libraries setting..."
CURRENT_VALUE=$(grep "^shared_preload_libraries\s*=" "$CONFIG_FILE" || echo "not found")

if [ "$CURRENT_VALUE" == "not found" ]; then
    # Check if it's commented out
    COMMENTED_VALUE=$(grep "^#shared_preload_libraries\s*=" "$CONFIG_FILE" || echo "")
    if [ -n "$COMMENTED_VALUE" ]; then
        print_info "Found commented setting: $COMMENTED_VALUE"
    else
        print_warning "shared_preload_libraries not found in config file"
    fi
else
    print_info "Current setting: $CURRENT_VALUE"

    # Check if pg_stat_statements is already configured
    if echo "$CURRENT_VALUE" | grep -q "pg_stat_statements"; then
        print_warning "pg_stat_statements is already configured in shared_preload_libraries"
        print_info "No changes needed. Exiting."
        exit 0
    fi
fi

# Update the configuration
print_info "Updating configuration..."

# Create a temporary file with the changes
TEMP_FILE=$(mktemp)

# Process the configuration file
# Find the shared_preload_libraries line and update it
awk '
BEGIN { found = 0; section_found = 0 }
/^# - Shared Library Preloading -/ { section_found = 1 }
section_found && /^#shared_preload_libraries = / {
    if (!found) {
        print "shared_preload_libraries = '\''pg_stat_statements'\''	# (change requires restart)"
        print ""
        print "# pg_stat_statements configuration"
        print "pg_stat_statements.max = 10000                    # Maximum number of statements tracked"
        print "pg_stat_statements.track = all                     # Track all statements (top-level and nested)"
        print "pg_stat_statements.track_utility = on              # Track utility commands (CREATE, DROP, etc.)"
        print "pg_stat_statements.track_planning = on             # Track query planning statistics"
        found = 1
        next
    }
}
section_found && /^shared_preload_libraries = / {
    if (!found) {
        # If already uncommented, append pg_stat_statements
        gsub(/shared_preload_libraries = '\''/, "shared_preload_libraries = '\''pg_stat_statements,")
        gsub(/shared_preload_libraries = '\'''\''/, "shared_preload_libraries = '\''pg_stat_statements'\''")
        print
        print ""
        print "# pg_stat_statements configuration"
        print "pg_stat_statements.max = 10000                    # Maximum number of statements tracked"
        print "pg_stat_statements.track = all                     # Track all statements (top-level and nested)"
        print "pg_stat_statements.track_utility = on              # Track utility commands (CREATE, DROP, etc.)"
        print "pg_stat_statements.track_planning = on             # Track query planning statistics"
        found = 1
        next
    }
}
{ print }
' "$CONFIG_FILE" > "$TEMP_FILE"

# Verify the temp file was created successfully
if [ $? -ne 0 ]; then
    print_error "Failed to process configuration file"
    rm -f "$TEMP_FILE"
    exit 1
fi

# Replace the original file with the updated one
mv "$TEMP_FILE" "$CONFIG_FILE"

if [ $? -eq 0 ]; then
    print_info "Configuration file updated successfully"
else
    print_error "Failed to update configuration file"
    print_info "Restoring backup..."
    cp "$BACKUP_FILE" "$CONFIG_FILE"
    exit 1
fi

# Set proper permissions
chown postgres:postgres "$CONFIG_FILE"
chmod 644 "$CONFIG_FILE"

# Verify the changes
print_info "Verifying changes..."
NEW_VALUE=$(grep "^shared_preload_libraries" "$CONFIG_FILE")
echo "New setting: $NEW_VALUE"
echo ""

print_info "Configuration updated successfully!"
echo ""
print_warning "IMPORTANT: PostgreSQL must be restarted for changes to take effect"
echo ""
echo "To restart PostgreSQL, run ONE of these commands:"
echo "  sudo systemctl restart postgresql@16-main      # For systemd systems"
echo "  sudo service postgresql restart                # For container environments"
echo "  sudo pg_ctlcluster 16 main restart            # Using pg_ctlcluster"
echo ""
echo "After restart, verify with:"
echo "  PGPASSWORD=postgres psql -h localhost -U postgres -c \"SHOW shared_preload_libraries;\""
echo ""
print_info "Backup saved at: $BACKUP_FILE"
echo ""
print_info "To rollback changes, run:"
echo "  sudo cp $BACKUP_FILE $CONFIG_FILE"
echo "  sudo service postgresql restart"
echo ""
print_info "Next step: Create the extension in your databases using:"
echo "  bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh"
echo ""

exit 0
