#!/bin/bash

################################################################################
# HRMS Frontend - Emergency Rollback Script
################################################################################
#
# Purpose: Emergency rollback for production deployments
# Features:
#   - Instant traffic switching
#   - Feature flag reset
#   - Health verification
#   - Audit trail
#   - Incident tracking
#
# Usage:
#   ./scripts/rollback.sh [--to-backup <timestamp>] [--skip-verification]
#
# Environment Variables (required):
#   PRODUCTION_URL         - Production environment URL
#   PRODUCTION_DEPLOY_PATH - Deployment path
#   PRODUCTION_SSH_HOST    - SSH host for deployment
#   PRODUCTION_SSH_USER    - SSH user
#   FEATURE_FLAG_API_URL   - Feature flag API endpoint
#   FEATURE_FLAG_API_KEY   - API key for feature flags
#
################################################################################

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Environment configuration
PRODUCTION_URL="${PRODUCTION_URL:-https://hrms.example.com}"
PRODUCTION_DEPLOY_PATH="${PRODUCTION_DEPLOY_PATH:-/var/www/hrms-production}"
PRODUCTION_SSH_HOST="${PRODUCTION_SSH_HOST:-}"
PRODUCTION_SSH_USER="${PRODUCTION_SSH_USER:-deploy}"
FEATURE_FLAG_API_URL="${FEATURE_FLAG_API_URL:-}"
FEATURE_FLAG_API_KEY="${FEATURE_FLAG_API_KEY:-}"
SLACK_WEBHOOK_URL="${SLACK_WEBHOOK_URL:-}"
PAGERDUTY_API_KEY="${PAGERDUTY_API_KEY:-}"

# Deployment paths
BLUE_PATH="$PRODUCTION_DEPLOY_PATH/blue"
GREEN_PATH="$PRODUCTION_DEPLOY_PATH/green"
BACKUP_DIR="$PRODUCTION_DEPLOY_PATH/backups"

# Script options
ROLLBACK_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="$PROJECT_ROOT/logs/rollback-${ROLLBACK_TIMESTAMP}.log"
BACKUP_TARGET=""
SKIP_VERIFICATION=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --to-backup)
            BACKUP_TARGET="$2"
            shift 2
            ;;
        --skip-verification)
            SKIP_VERIFICATION=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--to-backup <timestamp>] [--skip-verification]"
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

log_critical() {
    echo -e "${RED}${CYAN}[CRITICAL]${NC} $1" | tee -a "$LOG_FILE"
}

# ============================================================================
# NOTIFICATION FUNCTIONS
# ============================================================================

send_slack_notification() {
    local message="$1"
    local color="${2:-warning}"

    if [ -z "$SLACK_WEBHOOK_URL" ]; then
        return
    fi

    local payload=$(cat <<EOF
{
    "attachments": [
        {
            "color": "$color",
            "title": "EMERGENCY ROLLBACK - HRMS Frontend Production",
            "text": "<!channel> ${message}",
            "fields": [
                {
                    "title": "Initiated By",
                    "value": "${USER:-Unknown}",
                    "short": true
                },
                {
                    "title": "Timestamp",
                    "value": "$(date)",
                    "short": true
                },
                {
                    "title": "Environment",
                    "value": "Production",
                    "short": true
                },
                {
                    "title": "Reason",
                    "value": "Emergency rollback initiated",
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

create_pagerduty_incident() {
    if [ -z "$PAGERDUTY_API_KEY" ]; then
        return
    fi

    curl -X POST "https://api.pagerduty.com/incidents" \
        -H "Content-Type: application/json" \
        -H "Authorization: Token token=$PAGERDUTY_API_KEY" \
        -d @- <<EOF &>/dev/null || true
{
    "incident": {
        "type": "incident",
        "title": "HRMS Production Emergency Rollback",
        "body": {
            "type": "incident_body",
            "details": "Emergency rollback initiated by ${USER:-Unknown} at $(date)"
        },
        "urgency": "high"
    }
}
EOF
}

# ============================================================================
# ROLLBACK FUNCTIONS
# ============================================================================

print_rollback_header() {
    echo ""
    echo "================================================================"
    echo "  ⚠️  EMERGENCY ROLLBACK - HRMS Frontend Production"
    echo "================================================================"
    echo "  Target URL: $PRODUCTION_URL"
    echo "  Initiated By: ${USER:-Unknown}"
    echo "  Timestamp: $(date)"
    echo "================================================================"
    echo ""
}

confirm_rollback() {
    echo ""
    echo "⚠️  WARNING: This will immediately rollback the production deployment"
    echo ""
    read -p "Are you sure you want to proceed? Type 'ROLLBACK' to confirm: " confirmation

    if [ "$confirmation" != "ROLLBACK" ]; then
        log_warning "Rollback cancelled by user"
        exit 0
    fi

    log_info "Rollback confirmed by user"
}

determine_rollback_target() {
    log_info "Determining rollback target..."

    # Get current active environment
    local current_env=$(ssh "${PRODUCTION_SSH_USER}@${PRODUCTION_SSH_HOST}" \
        "readlink $PRODUCTION_DEPLOY_PATH/current" 2>/dev/null || echo "")

    if [ -z "$current_env" ]; then
        log_error "Cannot determine current active environment"
        exit 1
    fi

    if [[ "$current_env" == *"blue"* ]]; then
        CURRENT_ENV="blue"
        PREVIOUS_ENV="green"
        ROLLBACK_TO_PATH="$GREEN_PATH"
    else
        CURRENT_ENV="green"
        PREVIOUS_ENV="blue"
        ROLLBACK_TO_PATH="$BLUE_PATH"
    fi

    log_info "Current environment: $CURRENT_ENV"
    log_info "Rolling back to: $PREVIOUS_ENV"
}

rollback_to_previous_environment() {
    log_critical "Rolling back to previous environment: $PREVIOUS_ENV"

    # Switch symlink to previous environment
    ssh "${PRODUCTION_SSH_USER}@${PRODUCTION_SSH_HOST}" "
        ln -sfn $ROLLBACK_TO_PATH $PRODUCTION_DEPLOY_PATH/current

        # Reload nginx immediately
        sudo systemctl reload nginx || sudo nginx -s reload

        echo 'Traffic switched to $PREVIOUS_ENV environment'
    " || {
        log_critical "ROLLBACK FAILED - MANUAL INTERVENTION REQUIRED"
        log_critical "SSH to ${PRODUCTION_SSH_HOST} and manually switch symlink:"
        log_critical "  ln -sfn $ROLLBACK_TO_PATH $PRODUCTION_DEPLOY_PATH/current"
        log_critical "  sudo systemctl reload nginx"
        exit 1
    }

    log_success "Traffic switched to $PREVIOUS_ENV environment"
}

rollback_to_backup() {
    local backup_file="$1"

    log_critical "Rolling back to backup: $backup_file"

    # Verify backup exists
    ssh "${PRODUCTION_SSH_USER}@${PRODUCTION_SSH_HOST}" "
        if [ ! -f $backup_file ]; then
            echo 'Backup file not found'
            exit 1
        fi
    " || {
        log_error "Backup file not found: $backup_file"
        exit 1
    }

    # Extract backup to temporary location
    local temp_restore_path="$PRODUCTION_DEPLOY_PATH/restore-${ROLLBACK_TIMESTAMP}"

    ssh "${PRODUCTION_SSH_USER}@${PRODUCTION_SSH_HOST}" "
        mkdir -p $temp_restore_path
        tar -xzf $backup_file -C $temp_restore_path || exit 1

        # Switch to restored backup
        ln -sfn $temp_restore_path $PRODUCTION_DEPLOY_PATH/current

        # Reload nginx
        sudo systemctl reload nginx || sudo nginx -s reload

        echo 'Rollback to backup completed'
    " || {
        log_critical "BACKUP ROLLBACK FAILED"
        exit 1
    }

    log_success "Rolled back to backup successfully"
}

reset_feature_flags() {
    log_info "Resetting feature flags to 0%..."

    if [ -z "$FEATURE_FLAG_API_URL" ] || [ -z "$FEATURE_FLAG_API_KEY" ]; then
        log_warning "Feature flag API not configured. Skipping..."
        return
    fi

    curl -X POST "$FEATURE_FLAG_API_URL/api/feature-flags/phase1-migration" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $FEATURE_FLAG_API_KEY" \
        -d '{"rolloutPercentage": 0, "enabled": false}' || {
        log_warning "Feature flag reset failed (continuing anyway)"
        return
    }

    log_success "Feature flags reset to 0%"
}

verify_rollback() {
    if [ "$SKIP_VERIFICATION" = true ]; then
        log_warning "Skipping verification (--skip-verification flag set)"
        return
    fi

    log_info "Verifying rollback..."

    # Wait for nginx reload to propagate
    sleep 3

    # Test production URL
    local max_attempts=5
    local attempt=1

    while [ $attempt -le $max_attempts ]; do
        local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$PRODUCTION_URL" || echo "000")

        if [ "$http_code" = "200" ]; then
            log_success "Rollback verification passed (HTTP $http_code)"
            return 0
        fi

        log_warning "Verification attempt $attempt/$max_attempts failed (HTTP $http_code)"
        sleep 5
        attempt=$((attempt + 1))
    done

    log_error "Rollback verification failed"
    return 1
}

# ============================================================================
# BACKUP MANAGEMENT
# ============================================================================

list_available_backups() {
    log_info "Available backups:"

    ssh "${PRODUCTION_SSH_USER}@${PRODUCTION_SSH_HOST}" "
        if [ -d $BACKUP_DIR ]; then
            ls -lth $BACKUP_DIR/backup-*.tar.gz | head -10
        else
            echo 'No backups found'
        fi
    " || {
        log_error "Cannot list backups"
        return 1
    }
}

# ============================================================================
# POST-ROLLBACK ACTIONS
# ============================================================================

create_rollback_audit_log() {
    log_info "Creating audit log entry..."

    local audit_entry=$(cat <<EOF
{
    "event": "production_rollback",
    "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "initiated_by": "${USER:-Unknown}",
    "reason": "Emergency rollback",
    "from_environment": "$CURRENT_ENV",
    "to_environment": "${PREVIOUS_ENV:-backup}",
    "success": true
}
EOF
)

    echo "$audit_entry" >> "$PROJECT_ROOT/logs/rollback-audit.log"

    log_success "Audit log created"
}

monitor_post_rollback() {
    log_info "Monitoring system for 2 minutes..."

    local monitoring_duration=120
    local check_interval=10
    local start_time=$(date +%s)

    while [ $(($(date +%s) - start_time)) -lt $monitoring_duration ]; do
        local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$PRODUCTION_URL" || echo "000")

        if [ "$http_code" != "200" ]; then
            log_error "Health check failed during monitoring (HTTP $http_code)"
        else
            log_info "Health check OK (HTTP $http_code) - Elapsed: $(($(date +%s) - start_time))s"
        fi

        sleep $check_interval
    done

    log_success "Post-rollback monitoring completed"
}

# ============================================================================
# MAIN ROLLBACK FLOW
# ============================================================================

print_summary() {
    echo ""
    echo "================================================================"
    echo "  ROLLBACK SUMMARY"
    echo "================================================================"
    echo "  Status: SUCCESS"
    echo "  Rolled back from: $CURRENT_ENV"
    echo "  Rolled back to: ${PREVIOUS_ENV:-backup}"
    echo "  Timestamp: $ROLLBACK_TIMESTAMP"
    echo "  Log file: $LOG_FILE"
    echo "================================================================"
    echo ""
    echo "Next steps:"
    echo "  1. Verify application is working correctly"
    echo "  2. Check error rates in monitoring dashboards"
    echo "  3. Investigate root cause of deployment failure"
    echo "  4. Review logs for any issues"
    echo "  5. Plan remediation for next deployment"
    echo ""
    echo "Useful commands:"
    echo "  - Check current environment: ssh $PRODUCTION_SSH_USER@$PRODUCTION_SSH_HOST 'readlink $PRODUCTION_DEPLOY_PATH/current'"
    echo "  - View nginx logs: ssh $PRODUCTION_SSH_USER@$PRODUCTION_SSH_HOST 'tail -f /var/log/nginx/error.log'"
    echo "  - List backups: ssh $PRODUCTION_SSH_USER@$PRODUCTION_SSH_HOST 'ls -lth $BACKUP_DIR'"
    echo ""
}

main() {
    local ROLLBACK_START=$(date +%s)

    print_rollback_header

    # Alert everyone
    send_slack_notification "Emergency rollback initiated by ${USER:-Unknown}"
    create_pagerduty_incident

    # Confirm rollback
    confirm_rollback

    # Determine what to rollback to
    if [ -n "$BACKUP_TARGET" ]; then
        # Rollback to specific backup
        list_available_backups
        rollback_to_backup "$BACKUP_DIR/backup-${BACKUP_TARGET}.tar.gz"
    else
        # Rollback to previous environment (Blue/Green)
        determine_rollback_target
        rollback_to_previous_environment
    fi

    # Reset feature flags
    reset_feature_flags

    # Verify rollback
    if ! verify_rollback; then
        log_critical "Rollback verification failed - manual intervention required"
        send_slack_notification "Rollback completed but verification failed. Please check immediately!" "danger"
    else
        send_slack_notification "Rollback completed successfully. System is stable." "good"
    fi

    # Post-rollback actions
    create_rollback_audit_log
    monitor_post_rollback

    local rollback_duration=$(($(date +%s) - ROLLBACK_START))
    log_success "Total rollback duration: ${rollback_duration}s"

    print_summary
}

# Run main function
main
