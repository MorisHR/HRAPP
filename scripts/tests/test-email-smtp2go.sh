#!/bin/bash
# SMTP2GO Email Testing Script
# Purpose: Quick testing of email delivery after SMTP2GO configuration
# Usage: ./test-email-smtp2go.sh

set -e

echo "=========================================="
echo "SMTP2GO Email Testing Script"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
API_URL="http://localhost:5090"
TEST_EMAIL="${1:-your-email@example.com}"

echo -e "${YELLOW}Step 1: Checking if backend is running...${NC}"
if curl -s -f "${API_URL}/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Backend is running at ${API_URL}${NC}"
else
    echo -e "${RED}✗ Backend is NOT running!${NC}"
    echo "Please start the backend first:"
    echo "  cd src/HRMS.API && dotnet run"
    exit 1
fi

echo ""
echo -e "${YELLOW}Step 2: Getting SuperAdmin JWT token...${NC}"
echo "Please provide your SuperAdmin credentials:"
read -p "Email: " ADMIN_EMAIL
read -sp "Password: " ADMIN_PASSWORD
echo ""

# Login and get JWT token
LOGIN_RESPONSE=$(curl -s -X POST "${API_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${ADMIN_EMAIL}\",\"password\":\"${ADMIN_PASSWORD}\"}")

JWT_TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | sed 's/"token":"//')

if [ -z "$JWT_TOKEN" ]; then
    echo -e "${RED}✗ Failed to login. Please check your credentials.${NC}"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✓ Successfully authenticated${NC}"

echo ""
echo -e "${YELLOW}Step 3: Checking email configuration status...${NC}"
CONFIG_STATUS=$(curl -s -X GET "${API_URL}/api/admin/emailtest/config-status" \
  -H "Authorization: Bearer ${JWT_TOKEN}")

echo "$CONFIG_STATUS" | python3 -m json.tool 2>/dev/null || echo "$CONFIG_STATUS"

echo ""
echo -e "${YELLOW}Step 4: Sending test email to ${TEST_EMAIL}...${NC}"
TEST_RESPONSE=$(curl -s -X POST "${API_URL}/api/admin/emailtest/send-test" \
  -H "Authorization: Bearer ${JWT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"toEmail\":\"${TEST_EMAIL}\"}")

echo "$TEST_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$TEST_RESPONSE"

if echo "$TEST_RESPONSE" | grep -q '"success":true'; then
    echo ""
    echo -e "${GREEN}=========================================="
    echo "✓ Test email sent successfully!"
    echo "==========================================${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Check inbox for ${TEST_EMAIL}"
    echo "2. If not in inbox, check spam/junk folder"
    echo "3. Log in to SMTP2GO dashboard to verify delivery"
    echo ""
else
    echo ""
    echo -e "${RED}=========================================="
    echo "✗ Failed to send test email"
    echo "==========================================${NC}"
    echo ""
    echo "Troubleshooting:"
    echo "1. Verify SMTP credentials in appsettings.json"
    echo "2. Check FromEmail is verified in SMTP2GO dashboard"
    echo "3. Review application logs for detailed error"
    echo ""
fi

echo ""
echo -e "${YELLOW}Step 5: Would you like to test subscription email templates? (y/n)${NC}"
read -p "> " TEST_TEMPLATES

if [ "$TEST_TEMPLATES" = "y" ] || [ "$TEST_TEMPLATES" = "Y" ]; then
    echo ""
    echo -e "${YELLOW}Sending all subscription email templates to ${TEST_EMAIL}...${NC}"
    TEMPLATES_RESPONSE=$(curl -s -X POST "${API_URL}/api/admin/emailtest/send-subscription-templates" \
      -H "Authorization: Bearer ${JWT_TOKEN}" \
      -H "Content-Type: application/json" \
      -d "{\"toEmail\":\"${TEST_EMAIL}\"}")

    echo "$TEMPLATES_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$TEMPLATES_RESPONSE"

    if echo "$TEMPLATES_RESPONSE" | grep -q '"success":true'; then
        echo ""
        echo -e "${GREEN}✓ All subscription templates sent successfully!${NC}"
        echo ""
        echo "You should receive 4 emails:"
        echo "  1. 30-Day Renewal Reminder"
        echo "  2. 7-Day Expiring Warning"
        echo "  3. Subscription Expired Notice"
        echo "  4. Account Suspended Alert"
        echo ""
    else
        echo ""
        echo -e "${RED}✗ Failed to send subscription templates${NC}"
    fi
fi

echo ""
echo "=========================================="
echo "Testing complete!"
echo "=========================================="
echo ""
echo "For more information:"
echo "  - SMTP2GO Setup: docs/SMTP2GO_SETUP.md"
echo "  - Email Infrastructure: docs/EMAIL_INFRASTRUCTURE_SUMMARY.md"
echo "  - Production Deployment: PRODUCTION_DEPLOYMENT.md"
echo ""
