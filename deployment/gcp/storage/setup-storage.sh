#!/bin/bash
################################################################################
# Cloud Storage for Log Archival Setup Script
# Estimated Monthly Savings: $180
#
# Purpose:
# - Creates Cloud Storage buckets for log archival
# - Implements lifecycle policies for cost optimization
# - Reduces Cloud SQL and Cloud Logging storage costs
# - Enables long-term compliance retention
#
# Prerequisites:
# - gcloud CLI installed and authenticated
# - Cloud Storage API enabled
# - gsutil CLI tool available
################################################################################

set -euo pipefail

# Configuration
PROJECT_ID="${GCP_PROJECT_ID:-}"
REGION="${GCP_REGION:-us-central1}"
SECURITY_LOGS_BUCKET="${SECURITY_LOGS_BUCKET:-hrms-security-logs-archive}"
APPLICATION_LOGS_BUCKET="${APP_LOGS_BUCKET:-hrms-application-logs-archive}"
BACKUP_BUCKET="${BACKUP_BUCKET:-hrms-database-backups}"
STORAGE_CLASS="${STORAGE_CLASS:-ARCHIVE}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Validate prerequisites
validate_prerequisites() {
    log_info "Validating prerequisites..."

    if ! command -v gcloud &> /dev/null; then
        log_error "gcloud CLI not found"
        exit 1
    fi

    if ! command -v gsutil &> /dev/null; then
        log_error "gsutil CLI not found"
        exit 1
    fi

    if [ -z "$PROJECT_ID" ]; then
        PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
        if [ -z "$PROJECT_ID" ]; then
            log_error "GCP_PROJECT_ID not set"
            exit 1
        fi
    fi

    log_info "Using project: $PROJECT_ID"

    # Enable Cloud Storage API
    log_info "Enabling Cloud Storage API..."
    gcloud services enable storage-api.googleapis.com --project="$PROJECT_ID" 2>/dev/null || true
}

# Create storage buckets
create_buckets() {
    log_info "Creating Cloud Storage buckets..."

    # Security logs bucket
    if gsutil ls -p "$PROJECT_ID" | grep -q "gs://$SECURITY_LOGS_BUCKET/"; then
        log_warn "Bucket gs://$SECURITY_LOGS_BUCKET/ already exists"
    else
        log_info "Creating security logs bucket..."
        gsutil mb -c "$STORAGE_CLASS" -l "$REGION" -p "$PROJECT_ID" "gs://$SECURITY_LOGS_BUCKET/"
        log_info "Security logs bucket created"
    fi

    # Application logs bucket
    if gsutil ls -p "$PROJECT_ID" | grep -q "gs://$APPLICATION_LOGS_BUCKET/"; then
        log_warn "Bucket gs://$APPLICATION_LOGS_BUCKET/ already exists"
    else
        log_info "Creating application logs bucket..."
        gsutil mb -c STANDARD -l "$REGION" -p "$PROJECT_ID" "gs://$APPLICATION_LOGS_BUCKET/"
        log_info "Application logs bucket created"
    fi

    # Database backups bucket
    if gsutil ls -p "$PROJECT_ID" | grep -q "gs://$BACKUP_BUCKET/"; then
        log_warn "Bucket gs://$BACKUP_BUCKET/ already exists"
    else
        log_info "Creating database backups bucket..."
        gsutil mb -c NEARLINE -l "$REGION" -p "$PROJECT_ID" "gs://$BACKUP_BUCKET/"
        log_info "Database backups bucket created"
    fi
}

# Set lifecycle policies
set_lifecycle_policies() {
    log_info "Configuring lifecycle policies..."

    # Security logs lifecycle: Move to Coldline after 90 days, delete after 7 years
    log_info "Setting lifecycle policy for security logs..."
    gsutil lifecycle set lifecycle-policy-security.json "gs://$SECURITY_LOGS_BUCKET/"

    # Application logs lifecycle: Move to Nearline after 30 days, Coldline after 90, delete after 1 year
    log_info "Setting lifecycle policy for application logs..."
    gsutil lifecycle set lifecycle-policy-application.json "gs://$APPLICATION_LOGS_BUCKET/"

    # Backup lifecycle: Move to Coldline after 30 days, Archive after 90, delete after 180 days
    log_info "Setting lifecycle policy for backups..."
    gsutil lifecycle set lifecycle-policy-backup.json "gs://$BACKUP_BUCKET/"

    log_info "Lifecycle policies configured"
}

# Set bucket permissions and labels
configure_buckets() {
    log_info "Configuring bucket permissions and labels..."

    # Set uniform bucket-level access
    gsutil uniformbucketlevelaccess set on "gs://$SECURITY_LOGS_BUCKET/"
    gsutil uniformbucketlevelaccess set on "gs://$APPLICATION_LOGS_BUCKET/"
    gsutil uniformbucketlevelaccess set on "gs://$BACKUP_BUCKET/"

    # Add labels for cost tracking
    gsutil label ch -l environment:production "gs://$SECURITY_LOGS_BUCKET/"
    gsutil label ch -l purpose:security-logs "gs://$SECURITY_LOGS_BUCKET/"
    gsutil label ch -l cost-center:compliance "gs://$SECURITY_LOGS_BUCKET/"

    gsutil label ch -l environment:production "gs://$APPLICATION_LOGS_BUCKET/"
    gsutil label ch -l purpose:application-logs "gs://$APPLICATION_LOGS_BUCKET/"
    gsutil label ch -l cost-center:operations "gs://$APPLICATION_LOGS_BUCKET/"

    gsutil label ch -l environment:production "gs://$BACKUP_BUCKET/"
    gsutil label ch -l purpose:database-backups "gs://$BACKUP_BUCKET/"
    gsutil label ch -l cost-center:data-protection "gs://$BACKUP_BUCKET/"

    # Enable versioning for backup bucket
    gsutil versioning set on "gs://$BACKUP_BUCKET/"

    log_info "Bucket configuration complete"
}

# Create export scripts
create_export_scripts() {
    log_info "Creating log export scripts..."

    # Security logs export script
    cat > export-security-logs.sh <<'EOF'
#!/bin/bash
################################################################################
# Export Security Logs to Cloud Storage
# Run daily via cron to archive old logs
################################################################################

set -euo pipefail

PROJECT_ID="${GCP_PROJECT_ID:-}"
BUCKET="gs://hrms-security-logs-archive"
DATE=$(date +%Y/%m/%d)

log_info() {
    echo "[INFO] $1"
}

# Export Cloud SQL security logs
export_sql_logs() {
    log_info "Exporting Cloud SQL security logs..."

    gcloud sql export csv hrms-master \
        "$BUCKET/security-logs/$DATE/security_events.csv" \
        --database=hrms_monitoring \
        --query="SELECT * FROM security_events WHERE timestamp < NOW() - INTERVAL 30 DAY" \
        --project="$PROJECT_ID"
}

# Export Cloud Logging security events
export_cloud_logs() {
    log_info "Exporting Cloud Logging security events..."

    gcloud logging read \
        "resource.type=gce_instance AND severity>=WARNING AND timestamp<\"$(date -d '30 days ago' --iso-8601)\"" \
        --project="$PROJECT_ID" \
        --format=json \
        --limit=100000 > /tmp/security-logs.json

    gsutil cp /tmp/security-logs.json "$BUCKET/cloud-logs/$DATE/"
    rm /tmp/security-logs.json
}

main() {
    export_sql_logs
    export_cloud_logs
    log_info "Security logs export complete"
}

main "$@"
EOF

    chmod +x export-security-logs.sh

    # Application logs export script
    cat > export-application-logs.sh <<'EOF'
#!/bin/bash
################################################################################
# Export Application Logs to Cloud Storage
# Run daily via cron to archive old logs
################################################################################

set -euo pipefail

PROJECT_ID="${GCP_PROJECT_ID:-}"
BUCKET="gs://hrms-application-logs-archive"
DATE=$(date +%Y/%m/%d)

log_info() {
    echo "[INFO] $1"
}

# Export API performance logs
export_api_logs() {
    log_info "Exporting API performance logs..."

    gcloud sql export csv hrms-master \
        "$BUCKET/api-logs/$DATE/api_performance.csv" \
        --database=hrms_monitoring \
        --query="SELECT * FROM api_performance_logs WHERE timestamp < NOW() - INTERVAL 30 DAY" \
        --project="$PROJECT_ID"
}

# Export application logs from Cloud Logging
export_app_logs() {
    log_info "Exporting application logs..."

    gcloud logging read \
        "resource.type=gce_instance AND logName=projects/$PROJECT_ID/logs/application" \
        --project="$PROJECT_ID" \
        --format=json \
        --limit=100000 > /tmp/app-logs.json

    gsutil cp /tmp/app-logs.json "$BUCKET/application/$DATE/"
    rm /tmp/app-logs.json
}

main() {
    export_api_logs
    export_app_logs
    log_info "Application logs export complete"
}

main "$@"
EOF

    chmod +x export-application-logs.sh

    log_info "Export scripts created"
}

# Create monitoring and alerting
create_monitoring() {
    log_info "Creating storage monitoring configuration..."

    cat > storage-monitoring.yaml <<EOF
# Cloud Monitoring Alerts for Storage Buckets
# Deploy with: gcloud alpha monitoring policies create --policy-from-file=storage-monitoring.yaml

displayName: Storage Bucket Monitoring
conditions:
  - displayName: High Storage Costs
    conditionThreshold:
      filter: |
        metric.type="storage.googleapis.com/storage/total_bytes"
        resource.type="gcs_bucket"
      comparison: COMPARISON_GT
      thresholdValue: 1099511627776  # 1TB
      duration: 86400s  # 24 hours

  - displayName: Lifecycle Policy Failures
    conditionThreshold:
      filter: |
        metric.type="storage.googleapis.com/api/request_count"
        metric.label.response_code!="200"
      comparison: COMPARISON_GT
      thresholdValue: 100
      duration: 3600s  # 1 hour

notificationChannels:
  - projects/$PROJECT_ID/notificationChannels/[CHANNEL_ID]

alertStrategy:
  autoClose: 604800s  # 7 days
EOF

    log_info "Monitoring configuration created: storage-monitoring.yaml"
}

# Get bucket information
get_bucket_info() {
    log_info "Retrieving bucket information..."

    echo ""
    log_info "========================================="
    log_info "Cloud Storage Buckets Information"
    log_info "========================================="

    for bucket in "$SECURITY_LOGS_BUCKET" "$APPLICATION_LOGS_BUCKET" "$BACKUP_BUCKET"; do
        if gsutil ls -p "$PROJECT_ID" | grep -q "gs://$bucket/"; then
            log_info ""
            log_info "Bucket: gs://$bucket/"
            gsutil du -s "gs://$bucket/" 2>/dev/null || echo "  Size: Empty"
            gsutil lifecycle get "gs://$bucket/" 2>/dev/null || echo "  No lifecycle policy"
        fi
    done

    log_info "========================================="
    echo ""

    # Save bucket details
    cat > storage-buckets.env <<EOF
# Cloud Storage Bucket Details
# Generated: $(date)

SECURITY_LOGS_BUCKET=gs://$SECURITY_LOGS_BUCKET/
APPLICATION_LOGS_BUCKET=gs://$APPLICATION_LOGS_BUCKET/
BACKUP_BUCKET=gs://$BACKUP_BUCKET/

# Usage examples:
# Upload file: gsutil cp file.log gs://$SECURITY_LOGS_BUCKET/
# List contents: gsutil ls gs://$SECURITY_LOGS_BUCKET/
# Download file: gsutil cp gs://$SECURITY_LOGS_BUCKET/file.log .
EOF

    log_info "Bucket details saved to: storage-buckets.env"
}

# Main execution
main() {
    log_info "Starting Cloud Storage for Log Archival Setup"
    log_info "Estimated monthly savings: \$180"
    echo ""

    validate_prerequisites
    create_buckets
    set_lifecycle_policies
    configure_buckets
    create_export_scripts
    create_monitoring
    get_bucket_info

    echo ""
    log_info "========================================="
    log_info "Cloud Storage Setup Complete!"
    log_info "========================================="
    log_info "Monthly Cost Savings: \$180"
    log_info "Next Steps:"
    log_info "  1. Run export-security-logs.sh to start archiving security logs"
    log_info "  2. Run export-application-logs.sh to start archiving application logs"
    log_info "  3. Set up cron jobs for automated daily exports"
    log_info "  4. Configure Cloud SQL to export old data before deletion"
    log_info "  5. Deploy storage monitoring alerts"
    log_info "========================================="
}

main "$@"
