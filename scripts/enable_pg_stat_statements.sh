#!/bin/bash
#
# Complete pg_stat_statements Setup Script
#
# This script performs the complete setup of pg_stat_statements:
# 1. Backs up current configuration
# 2. Updates postgresql.conf
# 3. Provides restart instructions
# 4. Offers to install extension after restart
#
# Usage: sudo bash enable_pg_stat_statements.sh
#

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
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
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_section() {
    echo -e "${CYAN}>>> $1${NC}"
}

# Main execution
clear
print_header "PostgreSQL pg_stat_statements Setup"
echo ""
print_info "This script will configure pg_stat_statements for query performance tracking"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   print_error "This script must be run as root or with sudo"
   echo ""
   echo "Usage: sudo bash $0"
   exit 1
fi

# Display what will be done
print_section "What this script will do:"
echo ""
echo "  1. Backup current PostgreSQL configuration"
echo "  2. Update postgresql.conf to enable pg_stat_statements"
echo "  3. Add recommended configuration parameters"
echo "  4. Provide instructions for restarting PostgreSQL"
echo "  5. Offer to install the extension in databases (after restart)"
echo ""
print_warning "NOTE: PostgreSQL restart will be required (brief service interruption)"
echo ""

# Confirm before proceeding
read -p "Do you want to proceed? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    print_warning "Setup cancelled by user"
    exit 0
fi

echo ""
print_section "Step 1: Updating PostgreSQL Configuration"
echo ""

# Run the configuration update script
if [ -f "/workspaces/HRAPP/scripts/update_postgresql_config.sh" ]; then
    bash /workspaces/HRAPP/scripts/update_postgresql_config.sh
    CONFIG_UPDATE_STATUS=$?
else
    print_error "Configuration update script not found"
    print_info "Please ensure /workspaces/HRAPP/scripts/update_postgresql_config.sh exists"
    exit 1
fi

if [ $CONFIG_UPDATE_STATUS -ne 0 ]; then
    print_error "Configuration update failed"
    exit 1
fi

echo ""
print_section "Step 2: PostgreSQL Restart Required"
echo ""
print_warning "PostgreSQL must be restarted for changes to take effect"
echo ""
echo "Choose restart method:"
echo ""
echo "  1) Restart now using 'service postgresql restart' (recommended for containers)"
echo "  2) Restart now using 'systemctl restart postgresql@16-main' (for systemd)"
echo "  3) Restart now using 'pg_ctlcluster 16 main restart'"
echo "  4) I will restart manually later"
echo ""

read -p "Enter your choice (1-4): " RESTART_CHOICE

case $RESTART_CHOICE in
    1)
        print_info "Restarting PostgreSQL using service command..."
        service postgresql restart
        RESTART_STATUS=$?
        ;;
    2)
        print_info "Restarting PostgreSQL using systemctl..."
        systemctl restart postgresql@16-main
        RESTART_STATUS=$?
        ;;
    3)
        print_info "Restarting PostgreSQL using pg_ctlcluster..."
        pg_ctlcluster 16 main restart
        RESTART_STATUS=$?
        ;;
    4)
        print_warning "Skipping automatic restart"
        echo ""
        print_info "Remember to restart PostgreSQL manually before the extension will work"
        echo ""
        echo "Use one of these commands:"
        echo "  sudo service postgresql restart"
        echo "  sudo systemctl restart postgresql@16-main"
        echo "  sudo pg_ctlcluster 16 main restart"
        echo ""
        RESTART_STATUS=255  # Special code to skip verification
        ;;
    *)
        print_error "Invalid choice"
        exit 1
        ;;
esac

if [ $RESTART_STATUS -eq 255 ]; then
    # Manual restart chosen
    echo ""
    print_section "Next Steps (after manual restart):"
    echo ""
    echo "1. Restart PostgreSQL"
    echo "2. Verify configuration:"
    echo "   PGPASSWORD=postgres psql -h localhost -U postgres -c \"SHOW shared_preload_libraries;\""
    echo ""
    echo "3. Install extension in databases:"
    echo "   bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh"
    echo ""
    exit 0
elif [ $RESTART_STATUS -ne 0 ]; then
    print_error "PostgreSQL restart failed"
    echo ""
    print_info "You may need to restart manually"
    exit 1
fi

# Wait for PostgreSQL to be ready
print_info "Waiting for PostgreSQL to be ready..."
sleep 3

MAX_RETRIES=10
RETRY_COUNT=0

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    if pg_isready -h localhost -p 5432 >/dev/null 2>&1; then
        print_success "PostgreSQL is ready"
        break
    fi

    RETRY_COUNT=$((RETRY_COUNT + 1))
    echo "Waiting... ($RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
    print_error "PostgreSQL did not become ready in time"
    echo ""
    print_info "Please check PostgreSQL status manually:"
    echo "  sudo service postgresql status"
    echo "  sudo tail -50 /var/log/postgresql/postgresql-16-main.log"
    exit 1
fi

echo ""
print_section "Step 3: Verifying Configuration"
echo ""

# Verify shared_preload_libraries
PRELOAD_LIBS=$(PGPASSWORD=postgres psql -h localhost -U postgres -d postgres -t -A -c "SHOW shared_preload_libraries;" 2>/dev/null || echo "ERROR")

if [ "$PRELOAD_LIBS" = "ERROR" ]; then
    print_error "Failed to verify configuration (cannot connect to PostgreSQL)"
    exit 1
fi

if echo "$PRELOAD_LIBS" | grep -q "pg_stat_statements"; then
    print_success "pg_stat_statements is properly configured"
    echo "  shared_preload_libraries = $PRELOAD_LIBS"
else
    print_error "pg_stat_statements was not found in shared_preload_libraries"
    echo "  Current value: $PRELOAD_LIBS"
    echo ""
    print_info "This may indicate the configuration was not updated correctly"
    exit 1
fi

echo ""
print_section "Step 4: Install Extension in Databases"
echo ""
print_info "The extension needs to be created in each database where you want to use it"
echo ""

read -p "Do you want to install the extension in databases now? (yes/no): " INSTALL_EXT

if [ "$INSTALL_EXT" = "yes" ]; then
    echo ""
    if [ -f "/workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh" ]; then
        bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh
    else
        print_error "Extension installation script not found"
        print_info "Please run manually:"
        echo "  bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh"
    fi
else
    print_info "Skipping extension installation"
    echo ""
    print_info "To install later, run:"
    echo "  bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh"
fi

echo ""
print_section "Setup Complete!"
echo ""
print_success "pg_stat_statements has been configured successfully"
echo ""
print_info "Documentation available at:"
echo "  /workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md"
echo ""
print_info "Example queries to get started:"
echo ""
echo "Top 10 slowest queries:"
echo "  PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c \\"
echo "    \"SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query"
echo "     FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 10;\""
echo ""
echo "Most frequently executed queries:"
echo "  PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c \\"
echo "    \"SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query"
echo "     FROM pg_stat_statements ORDER BY calls DESC LIMIT 10;\""
echo ""

exit 0
