#!/bin/bash

# HRMS System Setup Test Script
# This script tests the setup endpoints

API_URL="http://localhost:5000"
SETUP_URL="$API_URL/api/admin/setup"
AUTH_URL="$API_URL/api/admin/auth"

echo "=================================="
echo "HRMS System Setup Test Script"
echo "=================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test 1: Check Setup Status
echo -e "${YELLOW}[TEST 1]${NC} Checking setup status..."
echo "GET $SETUP_URL/status"
echo ""
curl -s -X GET "$SETUP_URL/status" | jq '.'
echo ""
echo ""

# Test 2: Create First Admin
echo -e "${YELLOW}[TEST 2]${NC} Creating first admin user..."
echo "POST $SETUP_URL/create-first-admin"
echo ""
ADMIN_RESPONSE=$(curl -s -X POST "$SETUP_URL/create-first-admin")
echo "$ADMIN_RESPONSE" | jq '.'

# Check if admin was created successfully
if echo "$ADMIN_RESPONSE" | jq -e '.success == true' > /dev/null; then
    echo -e "${GREEN}✅ Admin user created successfully!${NC}"

    # Extract credentials
    EMAIL=$(echo "$ADMIN_RESPONSE" | jq -r '.data.email')
    PASSWORD=$(echo "$ADMIN_RESPONSE" | jq -r '.data.password')

    echo ""
    echo -e "${GREEN}Default Credentials:${NC}"
    echo "  Email: $EMAIL"
    echo "  Password: $PASSWORD"
else
    echo -e "${RED}❌ Failed to create admin user${NC}"
fi

echo ""
echo ""

# Test 3: Check Status Again
echo -e "${YELLOW}[TEST 3]${NC} Checking setup status again..."
echo "GET $SETUP_URL/status"
echo ""
curl -s -X GET "$SETUP_URL/status" | jq '.'
echo ""
echo ""

# Test 4: Try to Create Admin Again (Should Fail)
echo -e "${YELLOW}[TEST 4]${NC} Attempting to create admin again (should fail)..."
echo "POST $SETUP_URL/create-first-admin"
echo ""
curl -s -X POST "$SETUP_URL/create-first-admin" | jq '.'
echo ""
echo ""

# Test 5: Login with Admin Credentials
echo -e "${YELLOW}[TEST 5]${NC} Testing login with admin credentials..."
echo "POST $AUTH_URL/login"
echo ""
LOGIN_RESPONSE=$(curl -s -X POST "$AUTH_URL/login" \
    -H "Content-Type: application/json" \
    -d "{
        \"email\": \"admin@hrms.com\",
        \"password\": \"Admin@123\"
    }")

echo "$LOGIN_RESPONSE" | jq '.'

# Check if login was successful
if echo "$LOGIN_RESPONSE" | jq -e '.success == true' > /dev/null; then
    echo -e "${GREEN}✅ Login successful!${NC}"

    # Extract token
    TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.token')

    echo ""
    echo -e "${GREEN}JWT Token (first 50 chars):${NC}"
    echo "${TOKEN:0:50}..."

    echo ""
    echo -e "${GREEN}You can now use this token for authenticated requests:${NC}"
    echo "curl -H \"Authorization: Bearer \$TOKEN\" $API_URL/api/..."
else
    echo -e "${RED}❌ Login failed${NC}"
fi

echo ""
echo ""
echo "=================================="
echo "Setup Test Complete!"
echo "=================================="
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Start Angular frontend: cd hrms-frontend && npm start"
echo "2. Open browser: http://localhost:4200"
echo "3. Login with: admin@hrms.com / Admin@123"
echo ""
