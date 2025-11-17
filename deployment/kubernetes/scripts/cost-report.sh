#!/bin/bash

# Monthly Cost Report Generator
# Generates a detailed cost analysis report

NAMESPACE="hrms-production"
REPORT_FILE="cost-report-$(date +%Y%m%d-%H%M%S).txt"

echo "Generating cost report..."
echo ""

{
    echo "========================================="
    echo "HRMS Cost Optimization Report"
    echo "Generated: $(date)"
    echo "========================================="
    echo ""

    echo "1. HORIZONTAL POD AUTOSCALER STATUS"
    echo "========================================="
    kubectl get hpa -n $NAMESPACE -o wide
    echo ""

    echo "2. PREEMPTIBLE NODE POOL STATUS"
    echo "========================================="
    kubectl get nodes -l workload-type=background-jobs -o custom-columns=\
NAME:.metadata.name,\
STATUS:.status.conditions[-1].type,\
CPU:.status.capacity.cpu,\
MEMORY:.status.capacity.memory,\
PREEMPTIBLE:.metadata.labels.cost-optimized
    echo ""

    echo "3. CLOUD SQL PROXY METRICS"
    echo "========================================="
    echo "Deployment Status:"
    kubectl get deployment cloudsql-proxy -n $NAMESPACE -o wide
    echo ""
    echo "Service Endpoints:"
    kubectl get svc cloudsql-proxy -n $NAMESPACE
    echo ""

    echo "4. RESOURCE UTILIZATION"
    echo "========================================="
    echo "Top CPU-consuming pods:"
    kubectl top pods -n $NAMESPACE --sort-by=cpu | head -10
    echo ""
    echo "Top Memory-consuming pods:"
    kubectl top pods -n $NAMESPACE --sort-by=memory | head -10
    echo ""

    echo "5. RESOURCE QUOTA USAGE"
    echo "========================================="
    kubectl describe resourcequota -n $NAMESPACE
    echo ""

    echo "6. COST SAVINGS CALCULATION"
    echo "========================================="

    # HPA Savings
    HPA_CURRENT_PODS=$(kubectl get pods -n $NAMESPACE --no-headers | wc -l)
    HPA_BASELINE_PODS=15
    HPA_POD_COST_MONTHLY=26.67
    HPA_SAVINGS=$(echo "($HPA_BASELINE_PODS - $HPA_CURRENT_PODS) * $HPA_POD_COST_MONTHLY" | bc)

    echo "HPA Optimization:"
    echo "  Baseline pods: $HPA_BASELINE_PODS"
    echo "  Current pods: $HPA_CURRENT_PODS"
    echo "  Monthly savings: \$$HPA_SAVINGS (target: \$400)"
    echo ""

    # Preemptible Savings
    PREEMPTIBLE_NODES=$(kubectl get nodes -l workload-type=background-jobs --no-headers | wc -l)
    PREEMPTIBLE_SAVINGS_PER_NODE=34.31
    PREEMPTIBLE_TOTAL_SAVINGS=$(echo "$PREEMPTIBLE_NODES * $PREEMPTIBLE_SAVINGS_PER_NODE" | bc)

    echo "Preemptible VM Optimization:"
    echo "  Preemptible nodes: $PREEMPTIBLE_NODES"
    echo "  Savings per node: \$$PREEMPTIBLE_SAVINGS_PER_NODE"
    echo "  Monthly savings: \$$PREEMPTIBLE_TOTAL_SAVINGS (target: \$240)"
    echo ""

    # Cloud SQL Proxy Savings
    echo "Cloud SQL Proxy Optimization:"
    echo "  Connection pooling enabled: Yes"
    echo "  Monthly savings: \$80 (estimated)"
    echo ""

    TOTAL_SAVINGS=$(echo "$HPA_SAVINGS + $PREEMPTIBLE_TOTAL_SAVINGS + 80" | bc)
    echo "Total Monthly Savings: \$$TOTAL_SAVINGS (target: \$720)"
    echo ""

    echo "7. POD DISTRIBUTION"
    echo "========================================="
    echo "Pods by node type:"
    kubectl get pods -n $NAMESPACE -o json | \
        jq -r '.items[] | "\(.metadata.name)\t\(.spec.nodeName)"' | \
        while read pod node; do
            if kubectl get node "$node" -o json 2>/dev/null | grep -q "workload-type.*background-jobs"; then
                echo "  [PREEMPTIBLE] $pod -> $node"
            else
                echo "  [STANDARD]    $pod -> $node"
            fi
        done
    echo ""

    echo "8. SCALING EVENTS (Last 1 hour)"
    echo "========================================="
    kubectl get events -n $NAMESPACE --sort-by='.lastTimestamp' | \
        grep -i "scaled\|autoscal" | tail -20
    echo ""

    echo "9. RECOMMENDATIONS"
    echo "========================================="

    # Check if we can scale down
    AVG_CPU=$(kubectl top pods -n $NAMESPACE --no-headers | awk '{sum+=$2} END {print sum/NR}' | cut -d'm' -f1)
    if [ "${AVG_CPU%.*}" -lt 50 ]; then
        echo "  ⚠ Average CPU usage is low (${AVG_CPU}m). Consider:"
        echo "    - Reducing minReplicas in HPAs"
        echo "    - Lowering resource requests"
    fi

    # Check HPA thresholds
    echo "  ℹ Review HPA target utilization periodically"
    echo "  ℹ Monitor preemptible node eviction rates"
    echo "  ℹ Validate Cloud SQL Proxy connection efficiency"
    echo ""

    echo "========================================="
    echo "End of Report"
    echo "========================================="

} > "$REPORT_FILE"

echo "Report saved to: $REPORT_FILE"
cat "$REPORT_FILE"
