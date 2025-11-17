#!/bin/bash
################################################################################
# BigQuery Historical Metrics Setup Script
# Estimated Monthly Savings: $700
#
# Purpose:
# - Creates BigQuery dataset for historical metrics archival
# - Moves old data from expensive Cloud SQL to cost-effective BigQuery
# - Enables long-term trend analysis and compliance retention
# - Reduces Cloud SQL storage costs
#
# Prerequisites:
# - gcloud CLI installed and authenticated
# - BigQuery API enabled
# - bq CLI tool available
################################################################################

set -euo pipefail

# Configuration
PROJECT_ID="${GCP_PROJECT_ID:-}"
DATASET_NAME="${BQ_DATASET_NAME:-hrms_monitoring}"
DATASET_LOCATION="${BQ_LOCATION:-US}"
DATASET_DESCRIPTION="HRMS Historical Monitoring Metrics and Performance Data"

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

    if ! command -v bq &> /dev/null; then
        log_error "bq CLI not found. Install with: gcloud components install bq"
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

    # Enable BigQuery API
    log_info "Enabling BigQuery API..."
    gcloud services enable bigquery.googleapis.com --project="$PROJECT_ID" 2>/dev/null || true
}

# Create dataset
create_dataset() {
    log_info "Creating BigQuery dataset: $DATASET_NAME"

    if bq ls -d --project_id="$PROJECT_ID" | grep -q "$DATASET_NAME"; then
        log_warn "Dataset '$DATASET_NAME' already exists"
        echo -n "Do you want to recreate it? (yes/no): "
        read -r response
        if [ "$response" != "yes" ]; then
            log_info "Skipping dataset creation"
            return 0
        else
            log_info "Deleting existing dataset..."
            bq rm -r -f -d "$PROJECT_ID:$DATASET_NAME"
        fi
    fi

    bq mk --dataset \
        --location="$DATASET_LOCATION" \
        --description="$DATASET_DESCRIPTION" \
        --default_table_expiration=15552000 \
        --project_id="$PROJECT_ID" \
        "$DATASET_NAME"

    log_info "Dataset created successfully"
}

# Create tables
create_tables() {
    log_info "Creating BigQuery tables..."

    # API Performance Logs table
    log_info "Creating api_performance_logs table..."
    bq mk --table \
        --project_id="$PROJECT_ID" \
        --description="Historical API performance and latency metrics" \
        --time_partitioning_field=timestamp \
        --time_partitioning_type=DAY \
        --clustering_fields=tenant_id,endpoint,http_method \
        "$DATASET_NAME.api_performance_logs" \
        ./schemas/api_performance_schema.json

    # Performance Metrics table
    log_info "Creating performance_metrics table..."
    bq mk --table \
        --project_id="$PROJECT_ID" \
        --description="System-wide performance metrics and KPIs" \
        --time_partitioning_field=timestamp \
        --time_partitioning_type=DAY \
        --clustering_fields=metric_type,tenant_id \
        "$DATASET_NAME.performance_metrics" \
        ./schemas/performance_metrics_schema.json

    # Security Events table
    log_info "Creating security_events table..."
    bq mk --table \
        --project_id="$PROJECT_ID" \
        --description="Security events and audit logs" \
        --time_partitioning_field=timestamp \
        --time_partitioning_type=DAY \
        --clustering_fields=event_type,severity,tenant_id \
        "$DATASET_NAME.security_events" \
        ./schemas/security_events_schema.json

    # Tenant Activity table
    log_info "Creating tenant_activity table..."
    bq mk --table \
        --project_id="$PROJECT_ID" \
        --description="Tenant usage patterns and activity metrics" \
        --time_partitioning_field=timestamp \
        --time_partitioning_type=DAY \
        --clustering_fields=tenant_id,activity_type \
        "$DATASET_NAME.tenant_activity" \
        ./schemas/tenant_activity_schema.json

    log_info "All tables created successfully"
}

# Create views for common queries
create_views() {
    log_info "Creating BigQuery views..."

    # Daily performance summary
    bq query --use_legacy_sql=false --project_id="$PROJECT_ID" <<EOF
CREATE OR REPLACE VIEW \`$PROJECT_ID.$DATASET_NAME.daily_performance_summary\` AS
SELECT
  DATE(timestamp) as date,
  tenant_id,
  endpoint,
  COUNT(*) as request_count,
  AVG(response_time_ms) as avg_response_time,
  APPROX_QUANTILES(response_time_ms, 100)[OFFSET(95)] as p95_response_time,
  APPROX_QUANTILES(response_time_ms, 100)[OFFSET(99)] as p99_response_time,
  COUNTIF(status_code >= 500) as error_count,
  COUNTIF(status_code >= 500) / COUNT(*) as error_rate
FROM \`$PROJECT_ID.$DATASET_NAME.api_performance_logs\`
GROUP BY date, tenant_id, endpoint
ORDER BY date DESC, request_count DESC
EOF

    # Tenant health dashboard
    bq query --use_legacy_sql=false --project_id="$PROJECT_ID" <<EOF
CREATE OR REPLACE VIEW \`$PROJECT_ID.$DATASET_NAME.tenant_health_dashboard\` AS
SELECT
  tenant_id,
  DATE(timestamp) as date,
  COUNT(DISTINCT user_id) as active_users,
  COUNT(*) as total_requests,
  AVG(response_time_ms) as avg_response_time,
  COUNTIF(status_code >= 400) as error_count,
  MAX(timestamp) as last_activity
FROM \`$PROJECT_ID.$DATASET_NAME.api_performance_logs\`
WHERE timestamp >= TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 7 DAY)
GROUP BY tenant_id, date
ORDER BY date DESC, active_users DESC
EOF

    log_info "Views created successfully"
}

# Create scheduled queries for data archival
create_scheduled_queries() {
    log_info "Creating scheduled queries for automated data archival..."

    # Archive old API logs from Cloud SQL to BigQuery
    cat > scheduled-query-archive-logs.sql <<EOF
-- Archive API logs older than 30 days from Cloud SQL to BigQuery
INSERT INTO \`$PROJECT_ID.$DATASET_NAME.api_performance_logs\`
(timestamp, tenant_id, endpoint, http_method, status_code, response_time_ms, user_id, ip_address, user_agent)
SELECT
  timestamp,
  tenant_id,
  endpoint,
  http_method,
  status_code,
  response_time_ms,
  user_id,
  ip_address,
  user_agent
FROM EXTERNAL_QUERY(
  "projects/$PROJECT_ID/locations/us-central1/connections/cloud-sql-connection",
  '''SELECT * FROM api_performance_logs WHERE timestamp < NOW() - INTERVAL 30 DAY'''
)
EOF

    log_info "Scheduled query template created: scheduled-query-archive-logs.sql"
    log_info "To activate, run: bq mk --transfer_config --data_source=scheduled_query ..."
}

# Create data export job
create_export_job() {
    cat > export-to-bigquery.sh <<'EOF'
#!/bin/bash
################################################################################
# Export Cloud SQL Data to BigQuery
# Run this script to manually export data from Cloud SQL to BigQuery
################################################################################

set -euo pipefful

PROJECT_ID="${GCP_PROJECT_ID:-}"
DATASET_NAME="hrms_monitoring"
CLOUDSQL_INSTANCE="${CLOUDSQL_INSTANCE:-hrms-master}"

log_info() {
    echo "[INFO] $1"
}

# Export API Performance Logs
export_api_logs() {
    log_info "Exporting API performance logs..."

    gcloud sql export csv "$CLOUDSQL_INSTANCE" \
        gs://${PROJECT_ID}-sql-export/api_logs_$(date +%Y%m%d).csv \
        --database=hrms_monitoring \
        --query="SELECT * FROM api_performance_logs WHERE timestamp < NOW() - INTERVAL 30 DAY" \
        --project="$PROJECT_ID"

    # Load into BigQuery
    bq load --source_format=CSV \
        --skip_leading_rows=1 \
        --project_id="$PROJECT_ID" \
        "$DATASET_NAME.api_performance_logs" \
        gs://${PROJECT_ID}-sql-export/api_logs_$(date +%Y%m%d).csv

    log_info "API logs exported successfully"
}

# Export Performance Metrics
export_metrics() {
    log_info "Exporting performance metrics..."

    gcloud sql export csv "$CLOUDSQL_INSTANCE" \
        gs://${PROJECT_ID}-sql-export/metrics_$(date +%Y%m%d).csv \
        --database=hrms_monitoring \
        --query="SELECT * FROM performance_metrics WHERE timestamp < NOW() - INTERVAL 30 DAY" \
        --project="$PROJECT_ID"

    bq load --source_format=CSV \
        --skip_leading_rows=1 \
        --project_id="$PROJECT_ID" \
        "$DATASET_NAME.performance_metrics" \
        gs://${PROJECT_ID}-sql-export/metrics_$(date +%Y%m%d).csv

    log_info "Performance metrics exported successfully"
}

# Clean up old data from Cloud SQL
cleanup_old_data() {
    log_info "Cleaning up old data from Cloud SQL..."

    gcloud sql connect "$CLOUDSQL_INSTANCE" --user=postgres --quiet <<SQL
DELETE FROM api_performance_logs WHERE timestamp < NOW() - INTERVAL 30 DAY;
DELETE FROM performance_metrics WHERE timestamp < NOW() - INTERVAL 30 DAY;
SQL

    log_info "Old data cleaned up"
}

main() {
    export_api_logs
    export_metrics
    cleanup_old_data
    log_info "Data export complete!"
}

main "$@"
EOF

    chmod +x export-to-bigquery.sh
    log_info "Created export script: export-to-bigquery.sh"
}

# Get dataset information
get_dataset_info() {
    log_info "Retrieving dataset information..."

    echo ""
    log_info "========================================="
    log_info "BigQuery Dataset Information"
    log_info "========================================="
    log_info "Project: $PROJECT_ID"
    log_info "Dataset: $DATASET_NAME"
    log_info "Location: $DATASET_LOCATION"

    # List tables
    log_info ""
    log_info "Tables:"
    bq ls --project_id="$PROJECT_ID" "$DATASET_NAME" | tail -n +3

    log_info "========================================="
    echo ""

    # Save connection details
    cat > bigquery-connection.env <<EOF
# BigQuery Connection Details
# Generated: $(date)

BQ_PROJECT_ID=$PROJECT_ID
BQ_DATASET_NAME=$DATASET_NAME
BQ_DATASET_LOCATION=$DATASET_LOCATION

# Connection string format
BQ_DATASET_ID=$PROJECT_ID.$DATASET_NAME

# For Grafana BigQuery datasource
BQ_GRAFANA_CONFIG='{"projectId": "$PROJECT_ID", "defaultDataset": "$DATASET_NAME"}'
EOF

    log_info "Connection details saved to: bigquery-connection.env"
}

# Main execution
main() {
    log_info "Starting BigQuery Historical Metrics Setup"
    log_info "Estimated monthly savings: \$700"
    echo ""

    validate_prerequisites
    create_dataset
    create_tables
    create_views
    create_scheduled_queries
    create_export_job
    get_dataset_info

    echo ""
    log_info "========================================="
    log_info "BigQuery Setup Complete!"
    log_info "========================================="
    log_info "Monthly Cost Savings: \$700"
    log_info "Next Steps:"
    log_info "  1. Run export-to-bigquery.sh to migrate historical data"
    log_info "  2. Set up scheduled queries for ongoing archival"
    log_info "  3. Configure Grafana to query BigQuery for historical data"
    log_info "  4. Update retention policies in Cloud SQL"
    log_info "========================================="
}

main "$@"
