#!/bin/bash

# ════════════════════════════════════════════════════════════════════════════
# Run All Load Tests
# Executes complete test suite in sequence
# ════════════════════════════════════════════════════════════════════════════

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
LOAD_TEST_DIR="$PROJECT_ROOT/tests/load"
RESULTS_DIR="$LOAD_TEST_DIR/results"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

# Create results directory
mkdir -p "$RESULTS_DIR"

echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  HRMS Load Test Suite${NC}"
echo -e "${BLUE}  Timestamp: $TIMESTAMP${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

# Check if k6 is installed
if ! command -v k6 &> /dev/null; then
    echo -e "${RED}ERROR: k6 is not installed${NC}"
    echo "Install k6: https://k6.io/docs/getting-started/installation"
    exit 1
fi

echo -e "${GREEN}✓ k6 installed: $(k6 version)${NC}"
echo ""

# Load environment variables if .env exists
if [ -f "$LOAD_TEST_DIR/.env" ]; then
    echo -e "${GREEN}✓ Loading environment variables from .env${NC}"
    export $(grep -v '^#' "$LOAD_TEST_DIR/.env" | xargs)
else
    echo -e "${YELLOW}⚠ No .env file found, using default values${NC}"
fi

# Verify API is accessible
if [ -n "$API_URL" ]; then
    echo -e "${BLUE}Testing API accessibility: $API_URL${NC}"
    if curl -s -f -o /dev/null "$API_URL/health"; then
        echo -e "${GREEN}✓ API is accessible${NC}"
    else
        echo -e "${RED}ERROR: Cannot reach API at $API_URL${NC}"
        exit 1
    fi
else
    echo -e "${YELLOW}⚠ API_URL not set${NC}"
fi

echo ""
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Test Execution Plan${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""
echo "  1. Authentication Test (~10 min)"
echo "  2. End-to-End Test (~15 min)"
echo "  3. Database & Cache Test (~10 min)"
echo "  4. Stress Test (~45 min) [OPTIONAL]"
echo ""
echo -e "${YELLOW}Total estimated time: 35-80 minutes${NC}"
echo ""

# Ask for confirmation
read -p "Continue with test execution? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
fi

# ────────────────────────────────────────────────────────────────────────────
# Test 1: Authentication Test
# ────────────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Test 1: Authentication Test${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

TEST_FILE="$LOAD_TEST_DIR/k6/auth-test.js"
OUTPUT_FILE="$RESULTS_DIR/auth-test-$TIMESTAMP.json"

if k6 run --out json="$OUTPUT_FILE" "$TEST_FILE"; then
    echo -e "${GREEN}✓ Authentication test completed successfully${NC}"
else
    echo -e "${RED}✗ Authentication test failed${NC}"
    exit 1
fi

# ────────────────────────────────────────────────────────────────────────────
# Test 2: End-to-End Test
# ────────────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Test 2: End-to-End Test${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

TEST_FILE="$LOAD_TEST_DIR/k6/end-to-end-test.js"
OUTPUT_FILE="$RESULTS_DIR/end-to-end-test-$TIMESTAMP.json"

if k6 run --out json="$OUTPUT_FILE" "$TEST_FILE"; then
    echo -e "${GREEN}✓ End-to-end test completed successfully${NC}"
else
    echo -e "${RED}✗ End-to-end test failed${NC}"
    exit 1
fi

# ────────────────────────────────────────────────────────────────────────────
# Test 3: Database & Cache Test
# ────────────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Test 3: Database & Cache Performance Test${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}"
echo ""

TEST_FILE="$LOAD_TEST_DIR/k6/database-cache-test.js"
OUTPUT_FILE="$RESULTS_DIR/database-cache-test-$TIMESTAMP.json"

if k6 run --out json="$OUTPUT_FILE" "$TEST_FILE"; then
    echo -e "${GREEN}✓ Database & cache test completed successfully${NC}"
else
    echo -e "${RED}✗ Database & cache test failed${NC}"
    exit 1
fi

# ────────────────────────────────────────────────────────────────────────────
# Test 4: Stress Test (Optional)
# ────────────────────────────────────────────────────────────────────────────

echo ""
echo -e "${YELLOW}════════════════════════════════════════════════════════════${NC}"
echo -e "${YELLOW}  Test 4: Stress Test (10,000+ Users)${NC}"
echo -e "${YELLOW}  WARNING: This test takes 40-50 minutes${NC}"
echo -e "${YELLOW}  WARNING: This test will push the system to its limits${NC}"
echo -e "${YELLOW}════════════════════════════════════════════════════════════${NC}"
echo ""

read -p "Run stress test? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    TEST_FILE="$LOAD_TEST_DIR/k6/stress-test.js"
    OUTPUT_FILE="$RESULTS_DIR/stress-test-$TIMESTAMP.json"

    if k6 run --out json="$OUTPUT_FILE" "$TEST_FILE"; then
        echo -e "${GREEN}✓ Stress test completed${NC}"
    else
        echo -e "${YELLOW}⚠ Stress test encountered issues (expected under extreme load)${NC}"
    fi
else
    echo -e "${YELLOW}⊘ Stress test skipped${NC}"
fi

# ────────────────────────────────────────────────────────────────────────────
# Summary
# ────────────────────────────────────────────────────────────────────────────

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Test Suite Complete!${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Results saved to: ${BLUE}$RESULTS_DIR${NC}"
echo ""
echo "Test files:"
ls -lh "$RESULTS_DIR"/*$TIMESTAMP* 2>/dev/null | awk '{print "  - " $9 " (" $5 ")"}'
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "  1. Review results: $RESULTS_DIR"
echo "  2. Analyze metrics: ./scripts/analyze-results.sh $RESULTS_DIR"
echo "  3. Compare with baseline performance targets"
echo ""
echo -e "${GREEN}✓ All tests completed successfully!${NC}"
