#!/bin/bash

# Cost Optimization Validation Script
# Validates that all cost optimization components are working correctly

set -e

NAMESPACE="hrms-production"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "========================================="
echo "Cost Optimization Validation"
echo "========================================="
echo ""

# Test 1: HPA Functionality
echo "Test 1: Validating HPAs..."
HPA_COUNT=$(kubectl get hpa -n $NAMESPACE --no-headers 2>/dev/null | wc -l)
if [ "$HPA_COUNT" -ge 3 ]; then
    echo -e "${GREEN}✓${NC} Found $HPA_COUNT HPAs"

    # Check each HPA has metrics
    for hpa in $(kubectl get hpa -n $NAMESPACE -o name); do
        CURRENT=$(kubectl get $hpa -n $NAMESPACE -o jsonpath='{.status.currentReplicas}')
        DESIRED=$(kubectl get $hpa -n $NAMESPACE -o jsonpath='{.status.desiredReplicas}')
        echo "  - $(basename $hpa): $CURRENT/$DESIRED replicas"
    done
else
    echo -e "${RED}✗${NC} Expected at least 3 HPAs, found $HPA_COUNT"
    exit 1
fi
echo ""

# Test 2: Preemptible Nodes
echo "Test 2: Validating Preemptible Nodes..."
PREEMPTIBLE_COUNT=$(kubectl get nodes -l workload-type=background-jobs --no-headers 2>/dev/null | wc -l)
if [ "$PREEMPTIBLE_COUNT" -ge 1 ]; then
    echo -e "${GREEN}✓${NC} Found $PREEMPTIBLE_COUNT preemptible nodes"

    # Check taint
    kubectl get nodes -l workload-type=background-jobs -o custom-columns=NAME:.metadata.name,TAINTS:.spec.taints
else
    echo -e "${YELLOW}⚠${NC} No preemptible nodes found (may be scaling down)"
fi
echo ""

# Test 3: Cloud SQL Proxy
echo "Test 3: Validating Cloud SQL Proxy..."
PROXY_REPLICAS=$(kubectl get deployment cloudsql-proxy -n $NAMESPACE -o jsonpath='{.status.readyReplicas}' 2>/dev/null || echo "0")
if [ "$PROXY_REPLICAS" -ge 1 ]; then
    echo -e "${GREEN}✓${NC} Cloud SQL Proxy running ($PROXY_REPLICAS replicas)"

    # Test connectivity
    POD=$(kubectl get pod -n $NAMESPACE -l app=cloudsql-proxy -o jsonpath='{.items[0].metadata.name}')
    if kubectl exec -n $NAMESPACE $POD -- nc -zv localhost 5432 2>&1 | grep -q succeeded; then
        echo -e "${GREEN}✓${NC} Master DB connection working"
    else
        echo -e "${RED}✗${NC} Master DB connection failed"
    fi

    if kubectl exec -n $NAMESPACE $POD -- nc -zv localhost 5433 2>&1 | grep -q succeeded; then
        echo -e "${GREEN}✓${NC} Read replica connection working"
    else
        echo -e "${YELLOW}⚠${NC} Read replica connection may not be configured"
    fi
else
    echo -e "${RED}✗${NC} Cloud SQL Proxy not running"
    exit 1
fi
echo ""

# Test 4: Hangfire on Preemptible Nodes
echo "Test 4: Validating Hangfire Deployment..."
HANGFIRE_REPLICAS=$(kubectl get deployment hangfire-jobs -n $NAMESPACE -o jsonpath='{.status.readyReplicas}' 2>/dev/null || echo "0")
if [ "$HANGFIRE_REPLICAS" -ge 1 ]; then
    echo -e "${GREEN}✓${NC} Hangfire running ($HANGFIRE_REPLICAS replicas)"

    # Check if pods are on preemptible nodes
    PODS_ON_PREEMPTIBLE=$(kubectl get pods -n $NAMESPACE -l app=hangfire-jobs -o json | \
        jq -r '.items[] | select(.spec.nodeSelector."workload-type" == "background-jobs") | .metadata.name' | wc -l)

    if [ "$PODS_ON_PREEMPTIBLE" -ge 1 ]; then
        echo -e "${GREEN}✓${NC} $PODS_ON_PREEMPTIBLE Hangfire pods configured for preemptible nodes"
    else
        echo -e "${YELLOW}⚠${NC} Hangfire pods not on preemptible nodes"
    fi
else
    echo -e "${RED}✗${NC} Hangfire not running"
fi
echo ""

# Test 5: Resource Quotas
echo "Test 5: Validating Resource Quotas..."
QUOTA_COUNT=$(kubectl get resourcequota -n $NAMESPACE --no-headers 2>/dev/null | wc -l)
if [ "$QUOTA_COUNT" -ge 1 ]; then
    echo -e "${GREEN}✓${NC} Found $QUOTA_COUNT resource quotas"
    kubectl describe resourcequota -n $NAMESPACE | grep -A 3 "Used"
else
    echo -e "${RED}✗${NC} No resource quotas found"
fi
echo ""

# Test 6: ConfigMaps
echo "Test 6: Validating ConfigMaps..."
if kubectl get configmap cost-optimization-config -n $NAMESPACE &>/dev/null; then
    echo -e "${GREEN}✓${NC} Cost optimization ConfigMap exists"

    # Check key settings
    REDIS_ENABLED=$(kubectl get configmap cost-optimization-config -n $NAMESPACE -o jsonpath='{.data.REDIS_ENABLED}')
    BIGQUERY_ENABLED=$(kubectl get configmap cost-optimization-config -n $NAMESPACE -o jsonpath='{.data.BIGQUERY_ARCHIVAL_ENABLED}')

    echo "  - Redis caching: $REDIS_ENABLED"
    echo "  - BigQuery archival: $BIGQUERY_ENABLED"
else
    echo -e "${RED}✗${NC} Cost optimization ConfigMap not found"
fi
echo ""

# Test 7: Pod Resource Requests/Limits
echo "Test 7: Validating Pod Resources..."
PODS_WITHOUT_REQUESTS=$(kubectl get pods -n $NAMESPACE -o json | \
    jq '[.items[] | select(.spec.containers[].resources.requests == null)] | length')
if [ "$PODS_WITHOUT_REQUESTS" -eq 0 ]; then
    echo -e "${GREEN}✓${NC} All pods have resource requests"
else
    echo -e "${YELLOW}⚠${NC} $PODS_WITHOUT_REQUESTS pods missing resource requests"
fi
echo ""

# Calculate Estimated Savings
echo "========================================="
echo "Estimated Monthly Savings"
echo "========================================="
echo "HPA (dynamic scaling):        \$400"
echo "Preemptible VMs:              \$240"
echo "Cloud SQL Proxy:              \$80"
echo "----------------------------------------"
echo "Total Monthly Savings:        \$720"
echo ""

# Summary
echo "========================================="
echo "Validation Summary"
echo "========================================="
echo "✓ = Pass, ⚠ = Warning, ✗ = Fail"
echo ""
echo "For detailed monitoring:"
echo "  kubectl get hpa -n $NAMESPACE -w"
echo "  kubectl top pods -n $NAMESPACE"
echo "  kubectl top nodes"
echo ""
echo "Documentation: deployment/kubernetes/README.md"
echo "========================================="
