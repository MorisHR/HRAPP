#!/bin/bash

################################################################################
# Comprehensive Biometric Device API Test Script
# Tests the complete device registration, API key generation, and webhook flow
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="${API_BASE_URL:-https://repulsive-toad-7vjj6xv99745hrvj-5090.app.github.dev}"
TENANT_SUBDOMAIN="${TENANT_SUBDOMAIN:-testorg}"
ADMIN_EMAIL="${ADMIN_EMAIL:-testadmin@example.com}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-TestAdmin@123}"

# Test variables
JWT_TOKEN=""
DEVICE_ID=""
API_KEY=""
DEVICE_CODE="TEST-DEVICE-$(date +%s)"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Biometric Device API Test Suite${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Testing against: $BASE_URL"
echo "Tenant: $TENANT_SUBDOMAIN"
echo ""

################################################################################
# Helper Functions
################################################################################

function print_step() {
    echo -e "\n${YELLOW}➜ $1${NC}"
}

function print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

function print_error() {
    echo -e "${RED}✗ $1${NC}"
}

function check_response() {
    local response=$1
    local expected_status=$2
    local step_name=$3

    local status=$(echo "$response" | jq -r '.status // 0')
    local success=$(echo "$response" | jq -r '.success // false')

    if [[ "$success" == "true" ]] || [[ "$status" == "$expected_status" ]]; then
        print_success "$step_name passed"
        return 0
    else
        print_error "$step_name failed"
        echo "Response: $response" | jq '.'
        return 1
    fi
}

################################################################################
# Test Step 1: Authenticate as Admin
################################################################################

print_step "Step 1: Authenticating as admin..."

LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
  -d "{
    \"email\": \"$ADMIN_EMAIL\",
    \"password\": \"$ADMIN_PASSWORD\"
  }")

JWT_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token // .data.token // empty')

if [[ -z "$JWT_TOKEN" ]] || [[ "$JWT_TOKEN" == "null" ]]; then
    print_error "Failed to authenticate"
    echo "Response: $LOGIN_RESPONSE" | jq '.'
    exit 1
fi

print_success "Authentication successful"
echo "JWT Token (first 50 chars): ${JWT_TOKEN:0:50}..."

################################################################################
# Test Step 2: Create a Biometric Device
################################################################################

print_step "Step 2: Creating biometric device..."

# First, get a location ID
LOCATIONS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/locations" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN")

LOCATION_ID=$(echo "$LOCATIONS_RESPONSE" | jq -r '.data[0].id // empty')

if [[ -z "$LOCATION_ID" ]] || [[ "$LOCATION_ID" == "null" ]]; then
    print_error "No locations found. Please create a location first."
    exit 1
fi

print_success "Found location ID: $LOCATION_ID"

# Create device
CREATE_DEVICE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/biometric-devices" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
  -d "{
    \"deviceCode\": \"$DEVICE_CODE\",
    \"machineName\": \"Test Biometric Device - Automated Test\",
    \"machineId\": \"MACHINE-$DEVICE_CODE\",
    \"deviceType\": \"ZKTeco\",
    \"model\": \"ZK-F18\",
    \"locationId\": \"$LOCATION_ID\",
    \"ipAddress\": \"192.168.1.100\",
    \"port\": 4370,
    \"serialNumber\": \"SN-$DEVICE_CODE\",
    \"firmwareVersion\": \"Ver 6.60\",
    \"syncEnabled\": true,
    \"syncIntervalMinutes\": 15,
    \"connectionMethod\": \"TCP/IP\",
    \"connectionTimeoutSeconds\": 30,
    \"deviceStatus\": \"Active\",
    \"isActive\": true,
    \"offlineAlertEnabled\": true,
    \"offlineThresholdMinutes\": 60
  }")

DEVICE_ID=$(echo "$CREATE_DEVICE_RESPONSE" | jq -r '.id // .data.id // empty')

if [[ -z "$DEVICE_ID" ]] || [[ "$DEVICE_ID" == "null" ]]; then
    print_error "Failed to create device"
    echo "Response: $CREATE_DEVICE_RESPONSE" | jq '.'
    exit 1
fi

print_success "Device created successfully"
echo "Device ID: $DEVICE_ID"
echo "Device Code: $DEVICE_CODE"

################################################################################
# Test Step 3: Generate API Key for Device
################################################################################

print_step "Step 3: Generating API key for device..."

GENERATE_KEY_RESPONSE=$(curl -s -X POST "$BASE_URL/api/biometric-devices/$DEVICE_ID/generate-api-key" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
  -d "{
    \"description\": \"Automated Test API Key\",
    \"expiresAt\": \"2026-12-31T23:59:59Z\",
    \"rateLimitPerMinute\": 60
  }")

API_KEY=$(echo "$GENERATE_KEY_RESPONSE" | jq -r '.data.plaintextKey // empty')

if [[ -z "$API_KEY" ]] || [[ "$API_KEY" == "null" ]]; then
    print_error "Failed to generate API key"
    echo "Response: $GENERATE_KEY_RESPONSE" | jq '.'
    exit 1
fi

print_success "API key generated successfully"
echo "API Key (first 30 chars): ${API_KEY:0:30}..."
echo ""
echo -e "${YELLOW}⚠️  IMPORTANT: Save this API key - it won't be shown again!${NC}"
echo -e "${YELLOW}Full API Key: ${API_KEY}${NC}"
echo ""

################################################################################
# Test Step 4: Test Device Webhook - Ping Endpoint
################################################################################

print_step "Step 4: Testing webhook ping endpoint..."

PING_RESPONSE=$(curl -s -X GET "$BASE_URL/api/device-webhook/ping")

PING_SUCCESS=$(echo "$PING_RESPONSE" | jq -r '.success // false')

if [[ "$PING_SUCCESS" == "true" ]]; then
    print_success "Webhook ping successful"
    echo "$PING_RESPONSE" | jq '.'
else
    print_error "Webhook ping failed"
    echo "$PING_RESPONSE" | jq '.'
fi

################################################################################
# Test Step 5: Push Attendance Data to Webhook
################################################################################

print_step "Step 5: Pushing attendance data to webhook..."

# Create sample attendance records
CURRENT_TIME=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
PUNCH_TIME=$(date -u -d "1 hour ago" +"%Y-%m-%dT%H:%M:%SZ")

PUSH_ATTENDANCE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/device-webhook/attendance" \
  -H "Content-Type: application/json" \
  -d "{
    \"deviceId\": \"$DEVICE_CODE\",
    \"apiKey\": \"$API_KEY\",
    \"timestamp\": \"$CURRENT_TIME\",
    \"records\": [
      {
        \"employeeId\": \"EMP001\",
        \"punchTime\": \"$PUNCH_TIME\",
        \"punchType\": 0,
        \"verifyMode\": 1,
        \"deviceRecordId\": \"REC-$(date +%s)-001\"
      },
      {
        \"employeeId\": \"EMP002\",
        \"punchTime\": \"$PUNCH_TIME\",
        \"punchType\": 0,
        \"verifyMode\": 1,
        \"deviceRecordId\": \"REC-$(date +%s)-002\"
      }
    ]
  }")

PUSH_SUCCESS=$(echo "$PUSH_ATTENDANCE_RESPONSE" | jq -r '.success // false')

if [[ "$PUSH_SUCCESS" == "true" ]]; then
    print_success "Attendance push successful"
    echo "$PUSH_ATTENDANCE_RESPONSE" | jq '.'
else
    print_error "Attendance push failed"
    echo "$PUSH_ATTENDANCE_RESPONSE" | jq '.'
fi

################################################################################
# Test Step 6: Verify Device Sync Status
################################################################################

print_step "Step 6: Verifying device sync status..."

SYNC_STATUS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/biometric-devices/$DEVICE_ID/sync-status" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN")

LAST_SYNC=$(echo "$SYNC_STATUS_RESPONSE" | jq -r '.data.lastSyncTime // empty')

if [[ -n "$LAST_SYNC" ]] && [[ "$LAST_SYNC" != "null" ]]; then
    print_success "Device sync status retrieved"
    echo "Last Sync Time: $LAST_SYNC"
    echo "$SYNC_STATUS_RESPONSE" | jq '.data'
else
    print_error "Failed to retrieve sync status"
    echo "$SYNC_STATUS_RESPONSE" | jq '.'
fi

################################################################################
# Test Step 7: Test Invalid API Key (Security Test)
################################################################################

print_step "Step 7: Testing security - invalid API key..."

INVALID_KEY_RESPONSE=$(curl -s -X POST "$BASE_URL/api/device-webhook/attendance" \
  -H "Content-Type: application/json" \
  -d "{
    \"deviceId\": \"$DEVICE_CODE\",
    \"apiKey\": \"invalid-key-123\",
    \"timestamp\": \"$CURRENT_TIME\",
    \"records\": [
      {
        \"employeeId\": \"EMP001\",
        \"punchTime\": \"$PUNCH_TIME\",
        \"punchType\": 0,
        \"verifyMode\": 1,
        \"deviceRecordId\": \"REC-INVALID\"
      }
    ]
  }")

INVALID_KEY_SUCCESS=$(echo "$INVALID_KEY_RESPONSE" | jq -r '.success // false')

if [[ "$INVALID_KEY_SUCCESS" == "false" ]]; then
    print_success "Security test passed - invalid key rejected"
else
    print_error "Security test failed - invalid key was accepted!"
    echo "$INVALID_KEY_RESPONSE" | jq '.'
fi

################################################################################
# Test Step 8: Get All API Keys for Device
################################################################################

print_step "Step 8: Retrieving all API keys for device..."

GET_KEYS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/biometric-devices/$DEVICE_ID/api-keys" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN")

KEY_COUNT=$(echo "$GET_KEYS_RESPONSE" | jq -r '.data | length')

if [[ "$KEY_COUNT" -gt 0 ]]; then
    print_success "Retrieved $KEY_COUNT API key(s)"
    echo "$GET_KEYS_RESPONSE" | jq '.data[] | {id, description, isActive, createdAt, usageCount}'
else
    print_error "No API keys found"
    echo "$GET_KEYS_RESPONSE" | jq '.'
fi

################################################################################
# Test Summary
################################################################################

echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Device Code: $DEVICE_CODE"
echo "Device ID: $DEVICE_ID"
echo "API Key (first 30 chars): ${API_KEY:0:30}..."
echo ""
print_success "All tests completed successfully!"
echo ""
echo -e "${YELLOW}Note: Remember to clean up test data if needed.${NC}"
echo ""
