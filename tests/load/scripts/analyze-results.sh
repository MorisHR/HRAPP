#!/bin/bash

# ════════════════════════════════════════════════════════════════════════════
# Analyze Load Test Results
# Parse K6 JSON output and generate summary report
# ════════════════════════════════════════════════════════════════════════════

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}ERROR: jq is not installed${NC}"
    echo "Install jq: https://stedolan.github.io/jq/download/"
    exit 1
fi

# Get input file
if [ -z "$1" ]; then
    echo "Usage: $0 <results-file.json>"
    echo "Example: $0 results/stress-test-20250122-143000.json"
    exit 1
fi

RESULTS_FILE="$1"

if [ ! -f "$RESULTS_FILE" ]; then
    echo -e "${RED}ERROR: File not found: $RESULTS_FILE${NC}"
    exit 1
fi

echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Load Test Results Analysis${NC}"
echo -e "${BLUE}  File: $(basename $RESULTS_FILE)${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

# ────────────────────────────────────────────────────────────────────────────
# Extract Metrics
# ────────────────────────────────────────────────────────────────────────────

# HTTP Request Duration
HTTP_REQ_AVG=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values.avg // "N/A"')
HTTP_REQ_MIN=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values.min // "N/A"')
HTTP_REQ_MED=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values.med // "N/A"')
HTTP_REQ_MAX=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values.max // "N/A"')
HTTP_REQ_P90=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values["p(90)"] // "N/A"')
HTTP_REQ_P95=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values["p(95)"] // "N/A"')
HTTP_REQ_P99=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_duration.values["p(99)"] // "N/A"')

# HTTP Requests
TOTAL_REQUESTS=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_reqs.values.count // 0')
REQUEST_RATE=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_reqs.values.rate // 0')

# Failed Requests
FAILED_REQUESTS=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_failed.values.passes // 0')
FAILED_RATE=$(cat "$RESULTS_FILE" | jq -r '.metrics.http_req_failed.values.rate // 0')

# Checks
CHECKS_PASSED=$(cat "$RESULTS_FILE" | jq -r '.metrics.checks.values.passes // 0')
CHECKS_FAILED=$(cat "$RESULTS_FILE" | jq -r '.metrics.checks.values.fails // 0')
CHECKS_RATE=$(cat "$RESULTS_FILE" | jq -r '.metrics.checks.values.rate // 0')

# VUs
VUS_MIN=$(cat "$RESULTS_FILE" | jq -r '.metrics.vus.values.min // 0')
VUS_MAX=$(cat "$RESULTS_FILE" | jq -r '.metrics.vus.values.max // 0')

# Data Transfer
DATA_RECEIVED=$(cat "$RESULTS_FILE" | jq -r '.metrics.data_received.values.count // 0')
DATA_SENT=$(cat "$RESULTS_FILE" | jq -r '.metrics.data_sent.values.count // 0')

# ────────────────────────────────────────────────────────────────────────────
# Display Summary
# ────────────────────────────────────────────────────────────────────────────

echo -e "${BLUE}┌─ Request Performance${NC}"
echo -e "│"
printf "│  %-20s %s\n" "Total Requests:" "$TOTAL_REQUESTS"
printf "│  %-20s %.2f req/s\n" "Request Rate:" "$REQUEST_RATE"
echo -e "│"
printf "│  %-20s %.2f ms\n" "Response Time (avg):" "$HTTP_REQ_AVG"
printf "│  %-20s %.2f ms\n" "Response Time (min):" "$HTTP_REQ_MIN"
printf "│  %-20s %.2f ms\n" "Response Time (med):" "$HTTP_REQ_MED"
printf "│  %-20s %.2f ms\n" "Response Time (max):" "$HTTP_REQ_MAX"
printf "│  %-20s %.2f ms\n" "Response Time (p90):" "$HTTP_REQ_P90"
printf "│  %-20s %.2f ms\n" "Response Time (p95):" "$HTTP_REQ_P95"
printf "│  %-20s %.2f ms\n" "Response Time (p99):" "$HTTP_REQ_P99"
echo -e "│"
echo -e "${BLUE}└────────────────────────────────────────────────────────────${NC}"
echo ""

echo -e "${BLUE}┌─ Error Analysis${NC}"
echo -e "│"
printf "│  %-20s %d (%.2f%%)\n" "Failed Requests:" "$FAILED_REQUESTS" "$(echo "$FAILED_RATE * 100" | bc)"
printf "│  %-20s %d\n" "Checks Passed:" "$CHECKS_PASSED"
printf "│  %-20s %d\n" "Checks Failed:" "$CHECKS_FAILED"
printf "│  %-20s %.2f%%\n" "Check Success Rate:" "$(echo "$CHECKS_RATE * 100" | bc)"
echo -e "│"
echo -e "${BLUE}└────────────────────────────────────────────────────────────${NC}"
echo ""

echo -e "${BLUE}┌─ Load Characteristics${NC}"
echo -e "│"
printf "│  %-20s %d\n" "Min VUs:" "$VUS_MIN"
printf "│  %-20s %d\n" "Max VUs:" "$VUS_MAX"
printf "│  %-20s %.2f MB\n" "Data Received:" "$(echo "scale=2; $DATA_RECEIVED / 1024 / 1024" | bc)"
printf "│  %-20s %.2f MB\n" "Data Sent:" "$(echo "scale=2; $DATA_SENT / 1024 / 1024" | bc)"
echo -e "│"
echo -e "${BLUE}└────────────────────────────────────────────────────────────${NC}"
echo ""

# ────────────────────────────────────────────────────────────────────────────
# Performance Assessment
# ────────────────────────────────────────────────────────────────────────────

echo -e "${BLUE}┌─ Performance Assessment${NC}"
echo -e "│"

# P95 Latency Check
if (( $(echo "$HTTP_REQ_P95 < 500" | bc -l) )); then
    echo -e "│  ${GREEN}✓ P95 Latency: EXCELLENT (<500ms)${NC}"
elif (( $(echo "$HTTP_REQ_P95 < 1000" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ P95 Latency: ACCEPTABLE (<1s)${NC}"
elif (( $(echo "$HTTP_REQ_P95 < 2000" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ P95 Latency: DEGRADED (<2s)${NC}"
else
    echo -e "│  ${RED}✗ P95 Latency: POOR (>2s)${NC}"
fi

# Error Rate Check
FAILED_PERCENT=$(echo "$FAILED_RATE * 100" | bc)
if (( $(echo "$FAILED_PERCENT < 1" | bc -l) )); then
    echo -e "│  ${GREEN}✓ Error Rate: EXCELLENT (<1%)${NC}"
elif (( $(echo "$FAILED_PERCENT < 5" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ Error Rate: ACCEPTABLE (<5%)${NC}"
elif (( $(echo "$FAILED_PERCENT < 10" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ Error Rate: DEGRADED (<10%)${NC}"
else
    echo -e "│  ${RED}✗ Error Rate: POOR (>10%)${NC}"
fi

# Check Success Rate
CHECK_PERCENT=$(echo "$CHECKS_RATE * 100" | bc)
if (( $(echo "$CHECK_PERCENT > 99" | bc -l) )); then
    echo -e "│  ${GREEN}✓ Check Success: EXCELLENT (>99%)${NC}"
elif (( $(echo "$CHECK_PERCENT > 95" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ Check Success: ACCEPTABLE (>95%)${NC}"
else
    echo -e "│  ${RED}✗ Check Success: POOR (<95%)${NC}"
fi

echo -e "│"
echo -e "${BLUE}└────────────────────────────────────────────────────────────${NC}"
echo ""

# ────────────────────────────────────────────────────────────────────────────
# Custom Metrics (if available)
# ────────────────────────────────────────────────────────────────────────────

CACHE_HIT_RATE=$(cat "$RESULTS_FILE" | jq -r '.metrics.cache_hit_rate.values.rate // null')
if [ "$CACHE_HIT_RATE" != "null" ]; then
    echo -e "${BLUE}┌─ Cache Performance${NC}"
    echo -e "│"
    CACHE_HIT_PERCENT=$(echo "$CACHE_HIT_RATE * 100" | bc)
    printf "│  %-20s %.2f%%\n" "Cache Hit Rate:" "$CACHE_HIT_PERCENT"

    if (( $(echo "$CACHE_HIT_PERCENT > 90" | bc -l) )); then
        echo -e "│  ${GREEN}✓ Cache Performance: EXCELLENT (>90%)${NC}"
    elif (( $(echo "$CACHE_HIT_PERCENT > 80" | bc -l) )); then
        echo -e "│  ${YELLOW}⚠ Cache Performance: ACCEPTABLE (>80%)${NC}"
    else
        echo -e "│  ${RED}✗ Cache Performance: POOR (<80%)${NC}"
    fi

    echo -e "│"
    echo -e "${BLUE}└────────────────────────────────────────────────────────────${NC}"
    echo ""
fi

# ────────────────────────────────────────────────────────────────────────────
# Recommendations
# ────────────────────────────────────────────────────────────────────────────

echo -e "${YELLOW}┌─ Recommendations${NC}"
echo -e "│"

if (( $(echo "$HTTP_REQ_P95 > 1000" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ High latency detected. Consider:${NC}"
    echo -e "│    - Adding database indexes"
    echo -e "│    - Optimizing slow queries"
    echo -e "│    - Increasing cache TTL"
    echo -e "│    - Scaling application instances"
fi

if (( $(echo "$FAILED_PERCENT > 5" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ High error rate detected. Consider:${NC}"
    echo -e "│    - Increasing database connection pool"
    echo -e "│    - Adding rate limiting"
    echo -e "│    - Scaling infrastructure"
    echo -e "│    - Reviewing error logs"
fi

if [ "$CACHE_HIT_RATE" != "null" ] && (( $(echo "$CACHE_HIT_RATE < 0.8" | bc -l) )); then
    echo -e "│  ${YELLOW}⚠ Low cache hit rate. Consider:${NC}"
    echo -e "│    - Increasing cache memory"
    echo -e "│    - Optimizing cache keys"
    echo -e "│    - Reviewing cache invalidation logic"
fi

if (( $(echo "$HTTP_REQ_P95 < 500 && $FAILED_PERCENT < 1 && $CHECK_PERCENT > 99" | bc -l) )); then
    echo -e "│  ${GREEN}✓ System performing well! No action needed.${NC}"
fi

echo -e "│"
echo -e "${YELLOW}└────────────────────────────────────────────────────────────${NC}"
echo ""

echo -e "${GREEN}Analysis complete!${NC}"
