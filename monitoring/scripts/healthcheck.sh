#!/bin/bash
# ============================================================================
# Monitoring Stack Health Check
# Runs every 60 seconds to verify all services are operational
# ============================================================================

set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

check_service() {
    local service_name=$1
    local url=$2
    local timeout=${3:-5}

    if timeout $timeout wget --quiet --tries=1 --spider "$url" 2>/dev/null; then
        echo -e "${GREEN}✓${NC} $service_name is healthy"
        return 0
    else
        echo -e "${RED}✗${NC} $service_name is DOWN or unhealthy"
        return 1
    fi
}

echo "========================================"
echo "HRMS Monitoring Stack Health Check"
echo "Time: $(date '+%Y-%m-%d %H:%M:%S UTC')"
echo "========================================"

failed=0

# Check Prometheus
check_service "Prometheus" "http://prometheus:9090/-/healthy" || ((failed++))

# Check Alertmanager
check_service "Alertmanager" "http://alertmanager:9093/-/healthy" || ((failed++))

# Check Grafana
check_service "Grafana" "http://grafana:3000/api/health" || ((failed++))

# Check PostgreSQL Exporter
check_service "PostgreSQL Exporter" "http://postgres-exporter:9187/metrics" || ((failed++))

# Check Node Exporter
check_service "Node Exporter" "http://node-exporter:9100/metrics" || ((failed++))

# Check Redis Exporter
check_service "Redis Exporter" "http://redis-exporter:9121/metrics" || ((failed++))

# Check HRMS Metrics Exporter
check_service "HRMS Metrics Exporter" "http://hrms-metrics-exporter:9188/metrics" || ((failed++))

# Check Loki
check_service "Loki" "http://loki:3100/ready" || ((failed++))

echo "========================================"
if [ $failed -eq 0 ]; then
    echo -e "${GREEN}✓ All monitoring services are healthy${NC}"
    exit 0
else
    echo -e "${RED}✗ $failed service(s) are unhealthy${NC}"
    exit 1
fi
