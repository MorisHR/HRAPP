#!/bin/bash

################################################################################
# HRMS Frontend - Staging Deployment Script
################################################################################
#
# Purpose: Deploy Angular application to staging environment
# Features:
#   - Pre-deployment validation
#   - Build optimization
#   - Health checks
#   - Smoke tests
#   - Rollback capability
#   - Monitoring integration
#
# Usage:
#   ./scripts/deploy-staging.sh [--skip-tests] [--force]
#
# Environment Variables (required):
#   STAGING_URL         - Staging environment URL
#   STAGING_DEPLOY_PATH - Deployment path (e.g., /var/www/hrms-staging)
#   STAGING_SSH_HOST    - SSH host for deployment
#   STAGING_SSH_USER    - SSH user
#
# Optional:
#   SLACK_WEBHOOK_URL   - Slack notifications
#   DATADOG_API_KEY     - DataDog deployment tracking
#
################################################################################

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
NC='\033[0m'

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Environment configuration
ENVIRONMENT="staging"
STAGING_URL="${STAGING_URL:-https://staging.hrms.example.com}"
STAGING_DEPLOY_PATH="${STAGING_DEPLOY_PATH:-/var/www/hrms-staging}"
STAGING_SSH_HOST="${STAGING_SSH_HOST:-}"
STAGING_SSH_USER="${STAGING_SSH_USER:-deploy}"
SLACK_WEBHOOK_URL="${SLACK_WEBHOOK_URL:-}"
DATADOG_API_KEY="${DATADOG_API_KEY:-}"

# Deployment configuration
DEPLOYMENT_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BUILD_DIR="$PROJECT_ROOT/dist/hrms-frontend/browser"
BACKUP_DIR="$STAGING_DEPLOY_PATH/backups"
LOG_FILE="$PROJECT_ROOT/logs/deploy-staging-${DEPLOYMENT_TIMESTAMP}.log"

# Script options
SKIP_TESTS=false
FORCE_DEPLOY=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --force)
            FORCE_DEPLOY=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-tests] [--force]"
            exit 1
            ;;
    esac
done

# ============================================================================
# LOGGING FUNCTIONS
# ============================================================================

mkdir -p "$(dirname "$LOG_FILE")"

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

log_step() {
    echo -e "${MAGENTA}[STEP]${NC} $1" | tee -a "$LOG_FILE"
}

# ============================================================================
# NOTIFICATION FUNCTIONS
# ============================================================================

send_slack_notification() {
    local message="$1"
    local color="${2:-good}"

    if [ -z "$SLACK_WEBHOOK_URL" ]; then
        return
    fi

    local payload=$(cat <<EOF
{
    "attachments": [
        {
            "color": "$color",
            "title": "HRMS Frontend Deployment - Staging",
            "text": "$message",
            "fields": [
                {
                    "title": "Environment",
                    "value": "Staging",
                    "short": true
                },
                {
                    "title": "Timestamp",
                    "value": "$(date)",
                    "short": true
                }
            ]
        }
    ]
}
EOF
)

    curl -X POST -H 'Content-type: application/json' \
        --data "$payload" \
        "$SLACK_WEBHOOK_URL" &>/dev/null || true
}

track_datadog_deployment() {
    if [ -z "$DATADOG_API_KEY" ]; then
        return
    fi

    local git_sha=$(git rev-parse HEAD 2>/dev/null || echo "unknown")

    curl -X POST "https://api.datadoghq.com/api/v1/events" \
        -H "Content-Type: application/json" \
        -H "DD-API-KEY: $DATADOG_API_KEY" \
        -d @- <<EOF &>/dev/null || true
{
    "title": "HRMS Frontend Deployment - Staging",
    "text": "Deployed commit $git_sha to staging",
    "tags": ["env:staging", "service:hrms-frontend", "deployment:true"]
}
EOF
}

# ============================================================================
# VALIDATION FUNCTIONS
# ============================================================================

check_prerequisites() {
    log_step "Checking prerequisites..."

    # Check required commands
    local required_commands=("node" "npm" "rsync" "curl")
    for cmd in "${required_commands[@]}"; do
        if ! command -v "$cmd" &> /dev/null; then
            log_error "Required command not found: $cmd"
            exit 1
        fi
    done

    # Check if SSH host is configured
    if [ -z "$STAGING_SSH_HOST" ]; then
        log_error "STAGING_SSH_HOST environment variable not set"
        exit 1
    fi

    # Check SSH connectivity
    if ! ssh -o ConnectTimeout=5 "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "echo 'Connection successful'" &>/dev/null; then
        log_error "Cannot connect to staging server: ${STAGING_SSH_HOST}"
        exit 1
    fi

    log_success "Prerequisites check passed"
}

validate_environment() {
    log_step "Validating environment configuration..."

    cd "$PROJECT_ROOT"

    # Check if environment file exists
    if [ ! -f "src/environments/environment.ts" ]; then
        log_error "Environment file not found"
        exit 1
    fi

    # Validate Node version
    local node_version=$(node -v | cut -d'v' -f2)
    local required_version="18.0.0"
    if [ "$(printf '%s\n' "$required_version" "$node_version" | sort -V | head -n1)" != "$required_version" ]; then
        log_error "Node version $node_version is below required $required_version"
        exit 1
    fi

    log_success "Environment validation passed"
}

# ============================================================================
# BUILD FUNCTIONS
# ============================================================================

install_dependencies() {
    log_step "Installing dependencies..."

    cd "$PROJECT_ROOT"

    if [ -f "package-lock.json" ]; then
        npm ci --silent
    else
        npm install --silent
    fi

    log_success "Dependencies installed"
}

run_tests() {
    if [ "$SKIP_TESTS" = true ]; then
        log_warning "Skipping tests (--skip-tests flag set)"
        return
    fi

    log_step "Running tests..."

    cd "$PROJECT_ROOT"

    # Run unit tests with coverage
    npm run test -- --watch=false --code-coverage --browsers=ChromeHeadless || {
        log_error "Tests failed"
        send_slack_notification "Deployment failed: Tests did not pass" "danger"
        exit 1
    }

    log_success "Tests passed"
}

build_application() {
    log_step "Building application for staging..."

    cd "$PROJECT_ROOT"

    # Clean previous build
    rm -rf dist/

    # Build with production configuration
    npm run build -- --configuration=production --stats-json || {
        log_error "Build failed"
        send_slack_notification "Deployment failed: Build error" "danger"
        exit 1
    }

    # Verify build output
    if [ ! -d "$BUILD_DIR" ] || [ -z "$(ls -A "$BUILD_DIR")" ]; then
        log_error "Build directory is empty or missing"
        exit 1
    fi

    # Check bundle sizes
    local main_bundle_size=$(find "$BUILD_DIR" -name 'main.*.js' -exec stat -f%z {} \; 2>/dev/null || find "$BUILD_DIR" -name 'main.*.js' -exec stat -c%s {} \;)
    local max_bundle_size=524288  # 512KB

    if [ "$main_bundle_size" -gt "$max_bundle_size" ]; then
        log_warning "Main bundle size ($main_bundle_size bytes) exceeds recommended size ($max_bundle_size bytes)"

        if [ "$FORCE_DEPLOY" = false ]; then
            log_error "Bundle size check failed. Use --force to override."
            exit 1
        fi
    fi

    log_success "Application built successfully"
    log_info "Build directory: $BUILD_DIR"
    log_info "Build size: $(du -sh "$BUILD_DIR" | cut -f1)"
}

# ============================================================================
# DEPLOYMENT FUNCTIONS
# ============================================================================

create_backup() {
    log_step "Creating backup of current deployment..."

    # Create backup directory on remote server
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "mkdir -p $BACKUP_DIR" || {
        log_error "Failed to create backup directory"
        exit 1
    }

    # Backup current deployment
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        if [ -d $STAGING_DEPLOY_PATH/current ]; then
            tar -czf $BACKUP_DIR/backup-${DEPLOYMENT_TIMESTAMP}.tar.gz -C $STAGING_DEPLOY_PATH/current . || exit 1
            echo 'Backup created successfully'
        else
            echo 'No existing deployment to backup'
        fi
    " || {
        log_error "Backup creation failed"
        exit 1
    }

    log_success "Backup created: backup-${DEPLOYMENT_TIMESTAMP}.tar.gz"
}

deploy_files() {
    log_step "Deploying files to staging..."

    # Create deployment directories
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        mkdir -p $STAGING_DEPLOY_PATH/releases/${DEPLOYMENT_TIMESTAMP}
        mkdir -p $STAGING_DEPLOY_PATH/current
    " || {
        log_error "Failed to create deployment directories"
        exit 1
    }

    # Sync files to staging server
    rsync -avz --delete \
        --exclude='*.map' \
        --exclude='node_modules' \
        --exclude='.git' \
        "$BUILD_DIR/" \
        "${STAGING_SSH_USER}@${STAGING_SSH_HOST}:$STAGING_DEPLOY_PATH/releases/${DEPLOYMENT_TIMESTAMP}/" || {
        log_error "File sync failed"
        exit 1
    }

    # Create/update symlink to current release
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        ln -sfn $STAGING_DEPLOY_PATH/releases/${DEPLOYMENT_TIMESTAMP} $STAGING_DEPLOY_PATH/current
        echo 'Symlink updated'
    " || {
        log_error "Failed to update symlink"
        exit 1
    }

    log_success "Files deployed successfully"
}

# ============================================================================
# VERIFICATION FUNCTIONS
# ============================================================================

health_check() {
    log_step "Running health checks..."

    local max_attempts=10
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        log_info "Health check attempt $attempt/$max_attempts..."

        local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$STAGING_URL" || echo "000")

        if [ "$http_code" = "200" ]; then
            log_success "Health check passed (HTTP $http_code)"
            return 0
        fi

        log_warning "Health check failed (HTTP $http_code). Retrying in 5 seconds..."
        sleep 5
        attempt=$((attempt + 1))
    done

    log_error "Health check failed after $max_attempts attempts"
    return 1
}

smoke_tests() {
    log_step "Running smoke tests..."

    # Test critical endpoints
    local endpoints=(
        "$STAGING_URL"
        "$STAGING_URL/auth/subdomain"
        "$STAGING_URL/admin/login"
    )

    for endpoint in "${endpoints[@]}"; do
        log_info "Testing endpoint: $endpoint"

        local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$endpoint" || echo "000")

        if [ "$http_code" != "200" ]; then
            log_error "Smoke test failed for $endpoint (HTTP $http_code)"
            return 1
        fi
    done

    log_success "Smoke tests passed"
}

verify_deployment() {
    log_step "Verifying deployment..."

    # Run health checks
    if ! health_check; then
        log_error "Health checks failed"
        return 1
    fi

    # Run smoke tests
    if ! smoke_tests; then
        log_error "Smoke tests failed"
        return 1
    fi

    log_success "Deployment verification completed"
}

# ============================================================================
# ROLLBACK FUNCTIONS
# ============================================================================

rollback_deployment() {
    log_error "Deployment failed. Initiating rollback..."

    send_slack_notification "Deployment failed. Rolling back to previous version..." "warning"

    # Find latest backup
    local latest_backup=$(ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "ls -t $BACKUP_DIR/backup-*.tar.gz | head -1" || echo "")

    if [ -z "$latest_backup" ]; then
        log_error "No backup found for rollback"
        exit 1
    fi

    # Restore backup
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        rm -rf $STAGING_DEPLOY_PATH/current/*
        tar -xzf $latest_backup -C $STAGING_DEPLOY_PATH/current
    " || {
        log_error "Rollback failed"
        exit 1
    }

    log_success "Rollback completed successfully"
    send_slack_notification "Rollback completed successfully" "good"
}

# ============================================================================
# CLEANUP FUNCTIONS
# ============================================================================

cleanup_old_releases() {
    log_step "Cleaning up old releases..."

    # Keep last 5 releases
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        cd $STAGING_DEPLOY_PATH/releases
        ls -t | tail -n +6 | xargs rm -rf
    " || log_warning "Cleanup failed (non-critical)"

    # Clean old backups (keep last 10)
    ssh "${STAGING_SSH_USER}@${STAGING_SSH_HOST}" "
        cd $BACKUP_DIR
        ls -t backup-*.tar.gz | tail -n +11 | xargs rm -f
    " || log_warning "Backup cleanup failed (non-critical)"

    log_success "Cleanup completed"
}

# ============================================================================
# MAIN DEPLOYMENT FLOW
# ============================================================================

print_header() {
    echo ""
    echo "================================================================"
    echo "  HRMS Frontend - Staging Deployment"
    echo "================================================================"
    echo "  Environment: $ENVIRONMENT"
    echo "  Target URL: $STAGING_URL"
    echo "  Timestamp: $(date)"
    echo "================================================================"
    echo ""
}

print_summary() {
    local deployment_duration=$(($(date +%s) - DEPLOYMENT_START))

    echo ""
    echo "================================================================"
    echo "  DEPLOYMENT SUMMARY"
    echo "================================================================"
    echo "  Status: SUCCESS"
    echo "  Duration: ${deployment_duration}s"
    echo "  Deployed to: $STAGING_URL"
    echo "  Release: ${DEPLOYMENT_TIMESTAMP}"
    echo "  Log file: $LOG_FILE"
    echo "================================================================"
    echo ""
    echo "Next steps:"
    echo "  1. Verify application: $STAGING_URL"
    echo "  2. Run E2E tests"
    echo "  3. Update feature flags (if needed)"
    echo "  4. Monitor error rates in DataDog/Grafana"
    echo ""
}

main() {
    local DEPLOYMENT_START=$(date +%s)

    print_header

    send_slack_notification "Starting staging deployment..." "warning"

    # Pre-deployment
    check_prerequisites
    validate_environment
    install_dependencies
    run_tests

    # Build
    build_application

    # Deploy
    create_backup
    deploy_files

    # Verify
    if ! verify_deployment; then
        rollback_deployment
        exit 1
    fi

    # Post-deployment
    cleanup_old_releases

    # Track deployment
    track_datadog_deployment
    send_slack_notification "Staging deployment completed successfully" "good"

    print_summary
}

# Run main function
main
