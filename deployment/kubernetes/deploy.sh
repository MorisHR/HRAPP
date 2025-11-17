#!/bin/bash

# HRMS Kubernetes Cost Optimization Deployment Script
# Version: 1.0
# Usage: ./deploy.sh [--dry-run] [--skip-validation]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="hrms-production"
PROJECT_ID="${GCP_PROJECT_ID:-}"
REGION="${GCP_REGION:-us-central1}"
CLUSTER_NAME="${GKE_CLUSTER_NAME:-hrms-cluster}"
DRY_RUN=false
SKIP_VALIDATION=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --skip-validation)
            SKIP_VALIDATION=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites..."

    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl not found. Please install kubectl."
        exit 1
    fi

    # Check gcloud
    if ! command -v gcloud &> /dev/null; then
        log_error "gcloud not found. Please install Google Cloud SDK."
        exit 1
    fi

    # Check PROJECT_ID
    if [ -z "$PROJECT_ID" ]; then
        log_error "GCP_PROJECT_ID environment variable not set."
        exit 1
    fi

    # Check cluster connection
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster."
        exit 1
    fi

    # Check namespace
    if ! kubectl get namespace $NAMESPACE &> /dev/null; then
        log_warning "Namespace $NAMESPACE does not exist. Creating..."
        kubectl create namespace $NAMESPACE
        kubectl label namespace $NAMESPACE environment=production cost-center=hrms team=platform
    fi

    log_success "Prerequisites check passed"
}

replace_variables() {
    log_info "Replacing configuration variables..."

    # Create temporary directory for processed files
    TEMP_DIR=$(mktemp -d)

    # Copy all YAML files to temp directory
    cp -r autoscaling node-pools workloads infrastructure resource-management config "$TEMP_DIR/"

    # Replace variables
    find "$TEMP_DIR" -type f -name "*.yaml" -exec sed -i \
        -e "s/PROJECT_ID/$PROJECT_ID/g" \
        -e "s/REGION/$REGION/g" \
        {} \;

    log_success "Variables replaced. Using temporary directory: $TEMP_DIR"
    echo "$TEMP_DIR"
}

deploy_resource_management() {
    log_info "Deploying resource management..."

    if [ "$DRY_RUN" = true ]; then
        kubectl apply -f "$1/resource-management/resource-quotas.yaml" --dry-run=client -n $NAMESPACE
    else
        kubectl apply -f "$1/resource-management/resource-quotas.yaml" -n $NAMESPACE
    fi

    log_success "Resource management deployed"
}

deploy_cloudsql_proxy() {
    log_info "Deploying Cloud SQL Proxy..."

    if [ "$DRY_RUN" = true ]; then
        kubectl apply -f "$1/infrastructure/cloudsql-proxy-deployment.yaml" --dry-run=client -n $NAMESPACE
    else
        kubectl apply -f "$1/infrastructure/cloudsql-proxy-deployment.yaml" -n $NAMESPACE

        log_info "Waiting for Cloud SQL Proxy to be ready..."
        kubectl rollout status deployment/cloudsql-proxy -n $NAMESPACE --timeout=300s
    fi

    log_success "Cloud SQL Proxy deployed"
}

deploy_config_maps() {
    log_info "Deploying ConfigMaps..."

    if [ "$DRY_RUN" = true ]; then
        kubectl apply -f "$1/config/cost-optimization-config.yaml" --dry-run=client -n $NAMESPACE
    else
        kubectl apply -f "$1/config/cost-optimization-config.yaml" -n $NAMESPACE
    fi

    log_success "ConfigMaps deployed"
}

create_preemptible_node_pool() {
    log_info "Creating preemptible node pool..."

    if [ "$DRY_RUN" = true ]; then
        log_info "DRY RUN: Would create preemptible-jobs-pool"
        return
    fi

    # Check if node pool already exists
    if gcloud container node-pools describe preemptible-jobs-pool \
        --cluster=$CLUSTER_NAME \
        --region=$REGION \
        --project=$PROJECT_ID &> /dev/null; then
        log_warning "Preemptible node pool already exists. Skipping..."
        return
    fi

    gcloud container node-pools create preemptible-jobs-pool \
        --cluster=$CLUSTER_NAME \
        --region=$REGION \
        --project=$PROJECT_ID \
        --machine-type=n1-standard-2 \
        --preemptible \
        --num-nodes=2 \
        --min-nodes=1 \
        --max-nodes=5 \
        --enable-autoscaling \
        --enable-autorepair \
        --enable-autoupgrade \
        --disk-type=pd-standard \
        --disk-size=50GB \
        --node-taints=workload-type=background-jobs:NoSchedule \
        --node-labels=workload-type=background-jobs,cost-optimized=true

    log_success "Preemptible node pool created"
}

deploy_workloads() {
    log_info "Deploying workloads..."

    if [ "$DRY_RUN" = true ]; then
        kubectl apply -f "$1/workloads/hangfire-deployment.yaml" --dry-run=client -n $NAMESPACE
    else
        kubectl apply -f "$1/workloads/hangfire-deployment.yaml" -n $NAMESPACE

        log_info "Waiting for Hangfire deployment to be ready..."
        kubectl rollout status deployment/hangfire-jobs -n $NAMESPACE --timeout=300s
    fi

    log_success "Workloads deployed"
}

deploy_autoscaling() {
    log_info "Deploying autoscaling configurations..."

    if [ "$DRY_RUN" = true ]; then
        kubectl apply -f "$1/autoscaling/" --dry-run=client -n $NAMESPACE
    else
        kubectl apply -f "$1/autoscaling/" -n $NAMESPACE
    fi

    log_success "Autoscaling configurations deployed"
}

validate_deployment() {
    if [ "$SKIP_VALIDATION" = true ]; then
        log_warning "Skipping validation"
        return
    fi

    log_info "Validating deployment..."

    # Check Cloud SQL Proxy
    log_info "Checking Cloud SQL Proxy..."
    PROXY_READY=$(kubectl get deployment cloudsql-proxy -n $NAMESPACE -o jsonpath='{.status.readyReplicas}')
    if [ "$PROXY_READY" -ge 1 ]; then
        log_success "Cloud SQL Proxy is ready ($PROXY_READY replicas)"
    else
        log_error "Cloud SQL Proxy is not ready"
        exit 1
    fi

    # Check HPAs
    log_info "Checking HPAs..."
    HPA_COUNT=$(kubectl get hpa -n $NAMESPACE --no-headers | wc -l)
    if [ "$HPA_COUNT" -ge 3 ]; then
        log_success "HPAs configured ($HPA_COUNT total)"
    else
        log_error "Expected at least 3 HPAs, found $HPA_COUNT"
        exit 1
    fi

    # Check preemptible nodes
    log_info "Checking preemptible nodes..."
    PREEMPTIBLE_NODES=$(kubectl get nodes -l workload-type=background-jobs --no-headers | wc -l)
    if [ "$PREEMPTIBLE_NODES" -ge 1 ]; then
        log_success "Preemptible nodes available ($PREEMPTIBLE_NODES nodes)"
    else
        log_warning "No preemptible nodes found yet"
    fi

    # Check Hangfire pods
    log_info "Checking Hangfire pods..."
    HANGFIRE_READY=$(kubectl get deployment hangfire-jobs -n $NAMESPACE -o jsonpath='{.status.readyReplicas}')
    if [ "$HANGFIRE_READY" -ge 1 ]; then
        log_success "Hangfire jobs are ready ($HANGFIRE_READY replicas)"
    else
        log_error "Hangfire jobs are not ready"
        exit 1
    fi

    # Check resource quotas
    log_info "Checking resource quotas..."
    QUOTA_COUNT=$(kubectl get resourcequota -n $NAMESPACE --no-headers | wc -l)
    if [ "$QUOTA_COUNT" -ge 1 ]; then
        log_success "Resource quotas configured ($QUOTA_COUNT quotas)"
    else
        log_warning "No resource quotas found"
    fi

    log_success "Validation completed successfully"
}

print_summary() {
    echo ""
    echo "========================================="
    echo "DEPLOYMENT SUMMARY"
    echo "========================================="
    echo "Namespace: $NAMESPACE"
    echo "Project ID: $PROJECT_ID"
    echo "Region: $REGION"
    echo "Cluster: $CLUSTER_NAME"
    echo ""
    echo "Deployed Components:"
    echo "  ✓ Resource Management (quotas, limits, priority classes)"
    echo "  ✓ Cloud SQL Proxy (2 replicas, HA)"
    echo "  ✓ ConfigMaps (cost optimization settings)"
    echo "  ✓ Preemptible Node Pool (1-5 nodes)"
    echo "  ✓ Hangfire Deployment (on preemptible nodes)"
    echo "  ✓ Horizontal Pod Autoscalers (3 HPAs)"
    echo ""
    echo "Expected Monthly Savings: \$720"
    echo "  - HPA: \$400/month"
    echo "  - Preemptible VMs: \$240/month"
    echo "  - Cloud SQL Proxy: \$80/month"
    echo ""
    echo "Next Steps:"
    echo "  1. Monitor HPA scaling: kubectl get hpa -n $NAMESPACE -w"
    echo "  2. Check node pool status: gcloud container node-pools list --cluster=$CLUSTER_NAME"
    echo "  3. Validate Cloud SQL connectivity: kubectl exec -it deployment/cloudsql-proxy -n $NAMESPACE -- nc -zv localhost 5432"
    echo "  4. Review cost allocation: kubectl get pods -n $NAMESPACE --show-labels | grep cost-optimization"
    echo ""
    echo "Documentation: deployment/kubernetes/README.md"
    echo "========================================="
}

cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
    fi
}

trap cleanup EXIT

# Main execution
main() {
    echo "========================================="
    echo "HRMS Kubernetes Cost Optimization Deploy"
    echo "========================================="
    echo ""

    check_prerequisites
    TEMP_DIR=$(replace_variables)
    deploy_resource_management "$TEMP_DIR"
    deploy_cloudsql_proxy "$TEMP_DIR"
    deploy_config_maps "$TEMP_DIR"
    create_preemptible_node_pool
    deploy_workloads "$TEMP_DIR"
    deploy_autoscaling "$TEMP_DIR"

    if [ "$DRY_RUN" = false ]; then
        validate_deployment
    fi

    print_summary
}

main
