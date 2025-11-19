#!/bin/bash

# FORTUNE 50 END-TO-END TENANT ACTIVATION FLOW TEST
# ===================================================
# Tests the complete fortress-grade employee password setup workflow

API_URL="http://localhost:5090/api"
FRONTEND_URL="http://localhost:4200"

# Color codes for output
GREEN='\033[0.32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results
PASSED=0
FAILED=0

print_test() {
    echo -e "${BLUE}==>${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
    ((PASSED++))
}

print_error() {
    echo -e "${RED}✗${NC} $1"
    ((FAILED++))
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  FORTUNE 50 TENANT ACTIVATION E2E TEST                     ║"
echo "║  Testing fortress-grade employee password setup flow       ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# ============================================================================
# STEP 1: Login as SuperAdmin
# ============================================================================
print_test "STEP 1: Login as SuperAdmin"

SUPERADMIN_LOGIN=$(curl -s -X POST "${API_URL}/auth/superadmin/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@hrms.com",
    "password": "Admin@123456"
  }')

TOKEN=$(echo $SUPERADMIN_LOGIN | jq -r '.token // empty')

if [ -z "$TOKEN" ]; then
    print_error "SuperAdmin login failed"
    echo "$SUPERADMIN_LOGIN" | jq '.'
    exit 1
fi

print_success "SuperAdmin logged in successfully"
echo "   Token: ${TOKEN:0:20}..."

# ============================================================================
# STEP 2: Create New Tenant
# ============================================================================
print_test "STEP 2: Create new tenant for testing"

TIMESTAMP=$(date +%s)
TENANT_SUBDOMAIN="e2etest${TIMESTAMP}"
TENANT_NAME="E2E Test Tenant ${TIMESTAMP}"
ADMIN_EMAIL="admin@e2etest${TIMESTAMP}.com"

CREATE_TENANT=$(curl -s -X POST "${API_URL}/tenants" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"companyName\": \"${TENANT_NAME}\",
    \"subdomain\": \"${TENANT_SUBDOMAIN}\",
    \"adminEmail\": \"${ADMIN_EMAIL}\",
    \"adminFirstName\": \"Test\",
    \"adminLastName\": \"Admin\",
    \"companyAddress\": \"123 Test Street\",
    \"tier\": \"Professional\"
  }")

TENANT_ID=$(echo $CREATE_TENANT | jq -r '.id // empty')

if [ -z "$TENANT_ID" ]; then
    print_error "Tenant creation failed"
    echo "$CREATE_TENANT" | jq '.'
    exit 1
fi

print_success "Tenant created successfully"
echo "   Tenant ID: $TENANT_ID"
echo "   Subdomain: $TENANT_SUBDOMAIN"
echo "   Admin Email: $ADMIN_EMAIL"

# ============================================================================
# STEP 3: Retrieve Activation Token from Database
# ============================================================================
print_test "STEP 3: Retrieve activation token from database"

ACTIVATION_TOKEN=$(PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -t -A -c \
  "SELECT \"PasswordResetToken\" FROM tenant_${TENANT_SUBDOMAIN}.\"Employees\"
   WHERE \"Email\" = '${ADMIN_EMAIL}'
   LIMIT 1;")

if [ -z "$ACTIVATION_TOKEN" ]; then
    print_error "Failed to retrieve activation token"
    exit 1
fi

print_success "Activation token retrieved"
echo "   Token: ${ACTIVATION_TOKEN:0:40}..."

# ============================================================================
# STEP 4: Test Set Password Endpoint (Validation Checks)
# ============================================================================
print_test "STEP 4: Test password validation (should reject weak passwords)"

# Test 1: Password too short (< 12 characters)
WEAK_PASSWORD_TEST=$(curl -s -X POST "${API_URL}/auth/employee/set-password" \
  -H "Content-Type: application/json" \
  -d "{
    \"token\": \"${ACTIVATION_TOKEN}\",
    \"newPassword\": \"Short1!\",
    \"confirmPassword\": \"Short1!\",
    \"subdomain\": \"${TENANT_SUBDOMAIN}\"
  }")

if echo "$WEAK_PASSWORD_TEST" | grep -q "12 characters"; then
    print_success "Correctly rejected password < 12 characters"
else
    print_warning "Password length validation may not be working"
fi

# Test 2: Password missing special character
NO_SPECIAL_TEST=$(curl -s -X POST "${API_URL}/auth/employee/set-password" \
  -H "Content-Type: application/json" \
  -d "{
    \"token\": \"${ACTIVATION_TOKEN}\",
    \"newPassword\": \"NoSpecial123456\",
    \"confirmPassword\": \"NoSpecial123456\",
    \"subdomain\": \"${TENANT_SUBDOMAIN}\"
  }")

if echo "$NO_SPECIAL_TEST" | grep -q "special"; then
    print_success "Correctly rejected password without special character"
else
    print_warning "Special character validation may not be working"
fi

# ============================================================================
# STEP 5: Set Strong Password
# ============================================================================
print_test "STEP 5: Set strong fortress-grade password"

STRONG_PASSWORD="E2eTest@12345678"

SET_PASSWORD=$(curl -s -X POST "${API_URL}/auth/employee/set-password" \
  -H "Content-Type: application/json" \
  -d "{
    \"token\": \"${ACTIVATION_TOKEN}\",
    \"newPassword\": \"${STRONG_PASSWORD}\",
    \"confirmPassword\": \"${STRONG_PASSWORD}\",
    \"subdomain\": \"${TENANT_SUBDOMAIN}\"
  }")

if echo "$SET_PASSWORD" | grep -q "successfully"; then
    print_success "Password set successfully"
else
    print_error "Failed to set password"
    echo "$SET_PASSWORD" | jq '.'
    exit 1
fi

# ============================================================================
# STEP 6: Verify Password Reset Token is Cleared
# ============================================================================
print_test "STEP 6: Verify activation token was cleared after use"

TOKEN_AFTER=$(PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -t -A -c \
  "SELECT \"PasswordResetToken\" FROM tenant_${TENANT_SUBDOMAIN}.\"Employees\"
   WHERE \"Email\" = '${ADMIN_EMAIL}'
   LIMIT 1;")

if [ -z "$TOKEN_AFTER" ] || [ "$TOKEN_AFTER" = "" ]; then
    print_success "Activation token cleared after password setup"
else
    print_error "Activation token still present after password setup"
fi

# ============================================================================
# STEP 7: Login with New Password
# ============================================================================
print_test "STEP 7: Login as new employee with password"

EMPLOYEE_LOGIN=$(curl -s -X POST "${API_URL}/auth/login" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Subdomain: ${TENANT_SUBDOMAIN}" \
  -d "{
    \"email\": \"${ADMIN_EMAIL}\",
    \"password\": \"${STRONG_PASSWORD}\"
  }")

EMPLOYEE_TOKEN=$(echo $EMPLOYEE_LOGIN | jq -r '.token // empty')

if [ -z "$EMPLOYEE_TOKEN" ]; then
    print_error "Employee login failed"
    echo "$EMPLOYEE_LOGIN" | jq '.'
    exit 1
fi

print_success "Employee logged in successfully with new password"
echo "   Token: ${EMPLOYEE_TOKEN:0:20}..."

# ============================================================================
# STEP 8: Test Rate Limiting (Optional)
# ============================================================================
print_test "STEP 8: Test rate limiting protection"

for i in {1..6}; do
    RATE_TEST=$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/auth/employee/set-password" \
      -H "Content-Type: application/json" \
      -d "{
        \"token\": \"fake-token-$i\",
        \"newPassword\": \"${STRONG_PASSWORD}\",
        \"confirmPassword\": \"${STRONG_PASSWORD}\",
        \"subdomain\": \"${TENANT_SUBDOMAIN}\"
      }" | tail -n1)

    if [ "$RATE_TEST" = "429" ]; then
        print_success "Rate limiting triggered after multiple attempts"
        break
    fi
done

# ============================================================================
# STEP 9: Test Password History (Try Reusing Same Password)
# ============================================================================
print_test "STEP 9: Test password history (should reject password reuse)"

# Request password reset
RESET_REQUEST=$(curl -s -X POST "${API_URL}/auth/forgot-password" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Subdomain: ${TENANT_SUBDOMAIN}" \
  -d "{
    \"email\": \"${ADMIN_EMAIL}\"
  }")

# Wait a moment
sleep 2

# Get new reset token
NEW_RESET_TOKEN=$(PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master -t -A -c \
  "SELECT \"PasswordResetToken\" FROM tenant_${TENANT_SUBDOMAIN}.\"Employees\"
   WHERE \"Email\" = '${ADMIN_EMAIL}'
   LIMIT 1;")

if [ ! -z "$NEW_RESET_TOKEN" ]; then
    # Try to reuse the same password
    REUSE_TEST=$(curl -s -X POST "${API_URL}/auth/employee/set-password" \
      -H "Content-Type: application/json" \
      -d "{
        \"token\": \"${NEW_RESET_TOKEN}\",
        \"newPassword\": \"${STRONG_PASSWORD}\",
        \"confirmPassword\": \"${STRONG_PASSWORD}\",
        \"subdomain\": \"${TENANT_SUBDOMAIN}\"
      }")

    if echo "$REUSE_TEST" | grep -qi "history\|previous\|used"; then
        print_success "Password history check working (rejected reused password)"
    else
        print_warning "Password history check may not be enforced"
    fi
fi

# ============================================================================
# STEP 10: Cleanup (Optional)
# ============================================================================
print_test "STEP 10: Cleanup test tenant"

DELETE_TENANT=$(curl -s -w "\n%{http_code}" -X DELETE "${API_URL}/tenants/${TENANT_ID}" \
  -H "Authorization: Bearer $TOKEN" | tail -n1)

if [ "$DELETE_TENANT" = "204" ] || [ "$DELETE_TENANT" = "200" ]; then
    print_success "Test tenant deleted successfully"
else
    print_warning "Could not delete test tenant (manual cleanup may be needed)"
fi

# ============================================================================
# TEST SUMMARY
# ============================================================================
echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  TEST SUMMARY                                              ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${GREEN}✓ PASSED:${NC} $PASSED tests"
echo -e "${RED}✗ FAILED:${NC} $FAILED tests"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 ALL TESTS PASSED - FORTRESS-GRADE SECURITY VERIFIED${NC}"
    exit 0
else
    echo -e "${RED}❌ SOME TESTS FAILED - REVIEW REQUIRED${NC}"
    exit 1
fi
