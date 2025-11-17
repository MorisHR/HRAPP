#!/bin/bash

# Cost Optimization Rollback Script
# Safely removes cost optimization configurations

set -e

NAMESPACE="hrms-production"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${RED}=========================================${NC}"
echo -e "${RED}COST OPTIMIZATION ROLLBACK${NC}"
echo -e "${RED}=========================================${NC}"
echo ""
echo -e "${YELLOW}WARNING: This will remove all cost optimization configurations${NC}"
echo -e "${YELLOW}and may increase monthly costs by \$720${NC}"
echo ""
read -p "Are you sure you want to proceed? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Rollback cancelled"
    exit 0
fi

echo ""
echo "Starting rollback..."
echo ""

# Step 1: Remove HPAs
echo "Step 1: Removing Horizontal Pod Autoscalers..."
kubectl delete hpa --all -n $NAMESPACE 2>/dev/null || echo "No HPAs to delete"

# Set fixed replica counts
echo "Setting fixed replica counts..."
kubectl scale deployment hrms-api --replicas=5 -n $NAMESPACE 2>/dev/null || echo "hrms-api deployment not found"
kubectl scale deployment hrms-frontend --replicas=3 -n $NAMESPACE 2>/dev/null || echo "hrms-frontend deployment not found"
kubectl scale deployment hangfire-jobs --replicas=2 -n $NAMESPACE 2>/dev/null || echo "hangfire-jobs deployment not found"

echo -e "${GREEN}✓${NC} HPAs removed, fixed replicas set"
echo ""

# Step 2: Migrate workloads off preemptible nodes
echo "Step 2: Migrating workloads off preemptible nodes..."

# Remove node selector and tolerations from Hangfire
kubectl patch deployment hangfire-jobs -n $NAMESPACE --type=json \
    -p='[{"op": "remove", "path": "/spec/template/spec/nodeSelector"}]' 2>/dev/null || echo "No nodeSelector to remove"

kubectl patch deployment hangfire-jobs -n $NAMESPACE --type=json \
    -p='[{"op": "remove", "path": "/spec/template/spec/tolerations"}]' 2>/dev/null || echo "No tolerations to remove"

# Wait for pods to reschedule
echo "Waiting for pods to reschedule..."
kubectl rollout status deployment/hangfire-jobs -n $NAMESPACE --timeout=300s

echo -e "${GREEN}✓${NC} Workloads migrated to standard nodes"
echo ""

# Step 3: Drain and delete preemptible node pool
echo "Step 3: Removing preemptible node pool..."
echo -e "${YELLOW}⚠${NC} Draining preemptible nodes first..."

for node in $(kubectl get nodes -l workload-type=background-jobs -o name); do
    kubectl drain $node --ignore-daemonsets --delete-emptydir-data --force --grace-period=60 || echo "Failed to drain $node"
done

echo ""
read -p "Delete preemptible node pool? (yes/no): " delete_pool

if [ "$delete_pool" = "yes" ]; then
    echo "Deleting node pool..."
    gcloud container node-pools delete preemptible-jobs-pool \
        --cluster="${GKE_CLUSTER_NAME:-hrms-cluster}" \
        --region="${GCP_REGION:-us-central1}" \
        --quiet || echo "Failed to delete node pool"

    echo -e "${GREEN}✓${NC} Preemptible node pool deleted"
else
    echo -e "${YELLOW}⚠${NC} Preemptible node pool retained (manual deletion required)"
fi
echo ""

# Step 4: Remove Cloud SQL Proxy
echo "Step 4: Removing Cloud SQL Proxy..."
echo -e "${YELLOW}⚠${NC} Warning: This requires updating application connection strings"
echo ""
read -p "Remove Cloud SQL Proxy? (yes/no): " remove_proxy

if [ "$remove_proxy" = "yes" ]; then
    # Scale down first
    kubectl scale deployment cloudsql-proxy --replicas=0 -n $NAMESPACE
    sleep 10

    # Delete deployment
    kubectl delete deployment cloudsql-proxy -n $NAMESPACE 2>/dev/null || echo "Deployment already deleted"
    kubectl delete service cloudsql-proxy -n $NAMESPACE 2>/dev/null || echo "Service already deleted"
    kubectl delete serviceaccount cloudsql-proxy-sa -n $NAMESPACE 2>/dev/null || echo "ServiceAccount already deleted"

    echo -e "${GREEN}✓${NC} Cloud SQL Proxy removed"
    echo -e "${RED}⚠ IMPORTANT: Update application connection strings to use direct Cloud SQL IP${NC}"
else
    echo -e "${YELLOW}⚠${NC} Cloud SQL Proxy retained"
fi
echo ""

# Step 5: Remove ConfigMaps
echo "Step 5: Removing cost optimization ConfigMaps..."
read -p "Remove cost optimization ConfigMaps? (yes/no): " remove_config

if [ "$remove_config" = "yes" ]; then
    kubectl delete configmap cost-optimization-config -n $NAMESPACE 2>/dev/null || echo "ConfigMap already deleted"
    kubectl delete configmap database-connection-config -n $NAMESPACE 2>/dev/null || echo "ConfigMap already deleted"
    echo -e "${GREEN}✓${NC} ConfigMaps removed"
else
    echo -e "${YELLOW}⚠${NC} ConfigMaps retained"
fi
echo ""

# Step 6: Remove resource quotas (optional)
echo "Step 6: Resource quotas and limits..."
read -p "Remove resource quotas? (not recommended) (yes/no): " remove_quotas

if [ "$remove_quotas" = "yes" ]; then
    kubectl delete resourcequota --all -n $NAMESPACE
    kubectl delete limitrange --all -n $NAMESPACE
    echo -e "${GREEN}✓${NC} Resource quotas and limits removed"
else
    echo -e "${GREEN}✓${NC} Resource quotas and limits retained (recommended)"
fi
echo ""

# Summary
echo "========================================="
echo "ROLLBACK SUMMARY"
echo "========================================="
echo ""
echo "Removed:"
echo "  ✓ Horizontal Pod Autoscalers"
echo "  ✓ Preemptible node configurations"
if [ "$delete_pool" = "yes" ]; then
    echo "  ✓ Preemptible node pool"
fi
if [ "$remove_proxy" = "yes" ]; then
    echo "  ✓ Cloud SQL Proxy"
fi
if [ "$remove_config" = "yes" ]; then
    echo "  ✓ Cost optimization ConfigMaps"
fi
if [ "$remove_quotas" = "yes" ]; then
    echo "  ✓ Resource quotas"
fi
echo ""

echo "Current State:"
kubectl get deployments -n $NAMESPACE -o wide
echo ""

echo "Estimated Monthly Cost Impact: +\$720"
echo ""
echo "Next Steps:"
if [ "$remove_proxy" = "yes" ]; then
    echo "  1. Update application connection strings to direct Cloud SQL"
    echo "  2. Redeploy applications with updated configurations"
fi
echo "  3. Monitor resource utilization"
echo "  4. Update capacity planning"
echo ""
echo "========================================="
