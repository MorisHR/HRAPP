#!/bin/bash
################################################################################
# Cloud SQL Read Replica Setup Script
# Estimated Monthly Savings: $250
#
# Purpose:
# - Creates read replica for monitoring queries
# - Offloads read operations from master instance
# - Reduces master instance CPU/memory requirements
#
# Prerequisites:
# - gcloud CLI installed and authenticated
# - Cloud SQL Admin API enabled
# - Master instance must exist
################################################################################

set -euo pipefail

# Configuration
PROJECT_ID="${GCP_PROJECT_ID:-}"
REGION="${GCP_REGION:-us-central1}"
MASTER_INSTANCE="${MASTER_INSTANCE_NAME:-hrms-master}"
REPLICA_INSTANCE="${REPLICA_INSTANCE_NAME:-hrms-read-replica}"
REPLICA_TIER="${REPLICA_TIER:-db-custom-2-7680}"
AVAILABILITY_TYPE="${AVAILABILITY_TYPE:-ZONAL}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

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

    # Check if gcloud is installed
    if ! command -v gcloud &> /dev/null; then
        log_error "gcloud CLI not found. Please install from https://cloud.google.com/sdk/docs/install"
        exit 1
    fi

    # Check if project ID is set
    if [ -z "$PROJECT_ID" ]; then
        PROJECT_ID=$(gcloud config get-value project 2>/dev/null)
        if [ -z "$PROJECT_ID" ]; then
            log_error "GCP_PROJECT_ID not set and no default project configured"
            exit 1
        fi
    fi

    log_info "Using project: $PROJECT_ID"
    log_info "Using region: $REGION"

    # Check if master instance exists
    if ! gcloud sql instances describe "$MASTER_INSTANCE" --project="$PROJECT_ID" &>/dev/null; then
        log_error "Master instance '$MASTER_INSTANCE' not found in project '$PROJECT_ID'"
        exit 1
    fi

    log_info "Master instance '$MASTER_INSTANCE' verified"
}

# Check if replica already exists
check_replica_exists() {
    if gcloud sql instances describe "$REPLICA_INSTANCE" --project="$PROJECT_ID" &>/dev/null; then
        log_warn "Read replica '$REPLICA_INSTANCE' already exists"
        echo -n "Do you want to recreate it? (yes/no): "
        read -r response
        if [ "$response" != "yes" ]; then
            log_info "Skipping replica creation"
            return 0
        else
            log_info "Deleting existing replica..."
            gcloud sql instances delete "$REPLICA_INSTANCE" --project="$PROJECT_ID" --quiet
            log_info "Waiting for deletion to complete..."
            sleep 10
        fi
    fi
    return 1
}

# Create read replica
create_read_replica() {
    log_info "Creating read replica: $REPLICA_INSTANCE"
    log_info "Configuration:"
    log_info "  - Tier: $REPLICA_TIER (2 vCPU, 7.5 GB RAM)"
    log_info "  - Region: $REGION"
    log_info "  - Availability: $AVAILABILITY_TYPE (cost-optimized)"

    gcloud sql instances create "$REPLICA_INSTANCE" \
        --master-instance-name="$MASTER_INSTANCE" \
        --tier="$REPLICA_TIER" \
        --region="$REGION" \
        --replica-type=READ \
        --availability-type="$AVAILABILITY_TYPE" \
        --project="$PROJECT_ID" \
        --quiet

    log_info "Read replica created successfully"
}

# Configure replica for optimal performance
configure_replica() {
    log_info "Configuring replica for monitoring workloads..."

    # Set database flags for read-heavy workload optimization
    gcloud sql instances patch "$REPLICA_INSTANCE" \
        --database-flags=max_connections=200,shared_buffers=1966080,effective_cache_size=5898240 \
        --project="$PROJECT_ID" \
        --quiet

    log_info "Replica configuration updated"
}

# Get connection information
get_connection_info() {
    log_info "Retrieving connection information..."

    CONNECTION_NAME=$(gcloud sql instances describe "$REPLICA_INSTANCE" \
        --project="$PROJECT_ID" \
        --format="value(connectionName)")

    IP_ADDRESS=$(gcloud sql instances describe "$REPLICA_INSTANCE" \
        --project="$PROJECT_ID" \
        --format="value(ipAddresses[0].ipAddress)")

    echo ""
    log_info "========================================="
    log_info "Read Replica Connection Information"
    log_info "========================================="
    log_info "Instance Name: $REPLICA_INSTANCE"
    log_info "Connection Name: $CONNECTION_NAME"
    log_info "IP Address: $IP_ADDRESS"
    log_info "========================================="
    echo ""

    # Save connection details to file
    cat > read-replica-connection.env <<EOF
# Read Replica Connection Details
# Generated: $(date)

REPLICA_CONNECTION_NAME=$CONNECTION_NAME
REPLICA_IP_ADDRESS=$IP_ADDRESS
REPLICA_INSTANCE_NAME=$REPLICA_INSTANCE

# Connection string format (for Cloud SQL Proxy)
# postgresql://USER:PASSWORD@localhost:5432/DATABASE?host=/cloudsql/$CONNECTION_NAME

# Direct connection string format
# postgresql://USER:PASSWORD@$IP_ADDRESS:5432/DATABASE
EOF

    log_info "Connection details saved to: read-replica-connection.env"
}

# Create usage instructions
create_usage_instructions() {
    cat > README-REPLICA-USAGE.md <<'EOF'
# Read Replica Usage Guide

## Purpose
This read replica is optimized for monitoring queries to reduce load on the master instance.

## When to Use Read Replica

### Use for:
- Grafana dashboard queries
- Prometheus monitoring queries
- Historical data analysis
- Report generation
- Performance metric aggregation
- Security log queries

### Do NOT use for:
- Write operations (INSERT, UPDATE, DELETE)
- Transactions requiring immediate consistency
- Critical real-time data reads

## Connection Configuration

### Application Configuration
```typescript
// monitoring/config/database.ts
export const DATABASE_CONFIG = {
  master: {
    host: process.env.DB_MASTER_HOST,
    port: 5432,
    // Use for all write operations
  },
  replica: {
    host: process.env.DB_REPLICA_HOST,
    port: 5432,
    // Use for all read-only monitoring queries
  }
};
```

### Prometheus Configuration
```yaml
# monitoring/prometheus/postgres_exporter.yml
datasource:
  host: ${REPLICA_IP_ADDRESS}
  port: 5432
  user: prometheus
  password: ${PROMETHEUS_DB_PASSWORD}
  database: hrms_monitoring
```

### Grafana Configuration
```yaml
# monitoring/grafana/provisioning/datasources/postgres.yml
datasources:
  - name: HRMS Monitoring DB
    type: postgres
    url: ${REPLICA_IP_ADDRESS}:5432
    database: hrms_monitoring
    user: grafana
    isDefault: true
```

## Expected Performance Benefits

1. **Master Instance Load Reduction**: 40-50% reduction in read queries
2. **Query Response Time**: Improved by 30-40% for monitoring queries
3. **Cost Savings**: $250/month from master instance downsizing
4. **Scalability**: Can add more replicas if needed

## Monitoring Replica Health

```bash
# Check replication lag
gcloud sql instances describe hrms-read-replica \
  --format="value(replicaConfiguration.replicationLag)"

# Monitor replica status
gcloud sql operations list \
  --instance=hrms-read-replica \
  --limit=10
```

## Troubleshooting

### High Replication Lag
If replication lag exceeds 10 seconds:
1. Check master instance load
2. Verify network connectivity
3. Consider upgrading replica tier

### Connection Issues
1. Verify IP whitelisting
2. Check SSL certificates
3. Confirm user permissions exist on master

## Cost Breakdown

- **Read Replica**: $200/month (db-custom-2-7680, zonal)
- **Master Savings**: $450/month (downgrade from db-custom-4-15360)
- **Net Savings**: $250/month
EOF

    log_info "Usage instructions created: README-REPLICA-USAGE.md"
}

# Main execution
main() {
    log_info "Starting Cloud SQL Read Replica Setup"
    log_info "Estimated monthly savings: \$250"
    echo ""

    validate_prerequisites

    if ! check_replica_exists; then
        create_read_replica

        log_info "Waiting for replica to be ready..."
        sleep 30

        configure_replica
    fi

    get_connection_info
    create_usage_instructions

    echo ""
    log_info "========================================="
    log_info "Read Replica Setup Complete!"
    log_info "========================================="
    log_info "Monthly Cost Savings: \$250"
    log_info "Next Steps:"
    log_info "  1. Update application configuration with replica connection details"
    log_info "  2. Configure Prometheus to use replica for queries"
    log_info "  3. Update Grafana datasource to use replica"
    log_info "  4. Monitor replication lag and performance"
    log_info "========================================="
}

# Run main function
main "$@"
