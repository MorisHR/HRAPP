#!/bin/bash

# ════════════════════════════════════════════════════════════════════════════
# OWASP ZAP Baseline Security Scan
# Quick passive security scan for basic vulnerabilities
# ════════════════════════════════════════════════════════════════════════════

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SECURITY_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
RESULTS_DIR="$SECURITY_DIR/results"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

# Default values
TARGET_URL="${API_URL:-https://api.yourdomain.com}"
REPORT_FILE="$RESULTS_DIR/zap-baseline-$TIMESTAMP.html"
JSON_FILE="$RESULTS_DIR/zap-baseline-$TIMESTAMP.json"

# Create results directory
mkdir -p "$RESULTS_DIR"

echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  OWASP ZAP Baseline Security Scan${NC}"
echo -e "${BLUE}  Target: $TARGET_URL${NC}"
echo -e "${BLUE}  Timestamp: $TIMESTAMP${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo -e "${RED}ERROR: Docker is not installed${NC}"
    echo "Install Docker: https://docs.docker.com/get-docker/"
    exit 1
fi

echo -e "${GREEN}✓ Docker is available${NC}"
echo ""

# Pull OWASP ZAP Docker image
echo -e "${BLUE}Pulling OWASP ZAP Docker image...${NC}"
docker pull owasp/zap2docker-stable

echo ""
echo -e "${BLUE}Starting baseline scan...${NC}"
echo -e "${YELLOW}This is a passive scan and will take 5-10 minutes${NC}"
echo ""

# Run ZAP baseline scan
docker run --rm \
  -v "$RESULTS_DIR:/zap/wrk/:rw" \
  -t owasp/zap2docker-stable \
  zap-baseline.py \
  -t "$TARGET_URL" \
  -r "zap-baseline-$TIMESTAMP.html" \
  -J "zap-baseline-$TIMESTAMP.json" \
  -I || true  # Don't fail on warnings

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Baseline Scan Complete!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Results saved to:"
echo -e "  HTML Report: ${BLUE}$REPORT_FILE${NC}"
echo -e "  JSON Report: ${BLUE}$JSON_FILE${NC}"
echo ""

# Check if results exist
if [ -f "$REPORT_FILE" ]; then
    echo -e "${GREEN}✓ HTML report generated successfully${NC}"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo "  1. Review the HTML report: open $REPORT_FILE"
    echo "  2. Address any HIGH or MEDIUM severity issues"
    echo "  3. Run full scan for comprehensive testing: ./full-scan.sh"
else
    echo -e "${RED}✗ Report generation failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}Scan summary:${NC}"
docker run --rm \
  -v "$RESULTS_DIR:/zap/wrk/:rw" \
  -t owasp/zap2docker-stable \
  cat "/zap/wrk/zap-baseline-$TIMESTAMP.html" | grep -i "risk\|alert" | head -20 || true

echo ""
echo -e "${GREEN}Baseline scan completed!${NC}"
