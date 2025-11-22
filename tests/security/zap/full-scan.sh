#!/bin/bash

# ════════════════════════════════════════════════════════════════════════════
# OWASP ZAP Full Security Scan
# Comprehensive active and passive security testing
# WARNING: This is an active scan that will attempt to exploit vulnerabilities
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
REPORT_FILE="$RESULTS_DIR/zap-full-scan-$TIMESTAMP.html"
JSON_FILE="$RESULTS_DIR/zap-full-scan-$TIMESTAMP.json"
XML_FILE="$RESULTS_DIR/zap-full-scan-$TIMESTAMP.xml"

# Create results directory
mkdir -p "$RESULTS_DIR"

echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  OWASP ZAP Full Security Scan${NC}"
echo -e "${BLUE}  Target: $TARGET_URL${NC}"
echo -e "${BLUE}  Timestamp: $TIMESTAMP${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${RED}WARNING: This is an ACTIVE security scan!${NC}"
echo -e "${RED}It will attempt to exploit vulnerabilities.${NC}"
echo -e "${RED}Only run this against authorized test environments.${NC}"
echo ""

# Confirmation
read -p "Are you sure you want to proceed? (yes/no) " -r
echo
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Scan cancelled."
    exit 0
fi

# Check if Docker is available
if ! command -v docker &> /dev/null; then
    echo -e "${RED}ERROR: Docker is not installed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Docker is available${NC}"
echo ""

# Pull OWASP ZAP Docker image
echo -e "${BLUE}Pulling OWASP ZAP Docker image...${NC}"
docker pull owasp/zap2docker-stable

echo ""
echo -e "${BLUE}Starting full active scan...${NC}"
echo -e "${YELLOW}This scan will take 30-60 minutes depending on API complexity${NC}"
echo -e "${YELLOW}The scan will test for:${NC}"
echo "  - SQL Injection"
echo "  - Cross-Site Scripting (XSS)"
echo "  - Security Misconfigurations"
echo "  - Sensitive Data Exposure"
echo "  - Broken Authentication"
echo "  - XML External Entities (XXE)"
echo "  - Broken Access Control"
echo "  - Security Headers"
echo "  - And more OWASP Top 10 vulnerabilities"
echo ""

# Run ZAP full scan
docker run --rm \
  -v "$RESULTS_DIR:/zap/wrk/:rw" \
  -t owasp/zap2docker-stable \
  zap-full-scan.py \
  -t "$TARGET_URL" \
  -r "zap-full-scan-$TIMESTAMP.html" \
  -J "zap-full-scan-$TIMESTAMP.json" \
  -x "zap-full-scan-$TIMESTAMP.xml" \
  -I || true  # Don't fail on warnings

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Full Security Scan Complete!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Results saved to:"
echo -e "  HTML Report: ${BLUE}$REPORT_FILE${NC}"
echo -e "  JSON Report: ${BLUE}$JSON_FILE${NC}"
echo -e "  XML Report: ${BLUE}$XML_FILE${NC}"
echo ""

# Analyze results
if [ -f "$JSON_FILE" ]; then
    echo -e "${BLUE}Analyzing results...${NC}"
    echo ""

    # Count alerts by risk level
    HIGH_RISK=$(cat "$JSON_FILE" | jq '[.site[].alerts[] | select(.riskdesc | startswith("High"))] | length' 2>/dev/null || echo "0")
    MEDIUM_RISK=$(cat "$JSON_FILE" | jq '[.site[].alerts[] | select(.riskdesc | startswith("Medium"))] | length' 2>/dev/null || echo "0")
    LOW_RISK=$(cat "$JSON_FILE" | jq '[.site[].alerts[] | select(.riskdesc | startswith("Low"))] | length' 2>/dev/null || echo "0")
    INFO=$(cat "$JSON_FILE" | jq '[.site[].alerts[] | select(.riskdesc | startswith("Informational"))] | length' 2>/dev/null || echo "0")

    echo -e "${BLUE}Security Issues Found:${NC}"
    echo -e "  ${RED}HIGH Risk:${NC} $HIGH_RISK"
    echo -e "  ${YELLOW}MEDIUM Risk:${NC} $MEDIUM_RISK"
    echo -e "  ${GREEN}LOW Risk:${NC} $LOW_RISK"
    echo -e "  ${BLUE}Informational:${NC} $INFO"
    echo ""

    # Fail if HIGH risk issues found
    if [ "$HIGH_RISK" -gt 0 ]; then
        echo -e "${RED}✗ CRITICAL: $HIGH_RISK HIGH risk vulnerabilities found!${NC}"
        echo -e "${RED}  These must be fixed before production deployment.${NC}"
        exit 1
    elif [ "$MEDIUM_RISK" -gt 0 ]; then
        echo -e "${YELLOW}⚠ WARNING: $MEDIUM_RISK MEDIUM risk vulnerabilities found.${NC}"
        echo -e "${YELLOW}  Review and address these issues.${NC}"
    else
        echo -e "${GREEN}✓ No HIGH or MEDIUM risk vulnerabilities found!${NC}"
    fi
fi

echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "  1. Review the detailed HTML report: open $REPORT_FILE"
echo "  2. Address all HIGH and MEDIUM severity issues"
echo "  3. Re-run scan after fixes"
echo "  4. Run manual security tests: ../manual/run-manual-tests.sh"
echo ""
echo -e "${GREEN}Full security scan completed!${NC}"
