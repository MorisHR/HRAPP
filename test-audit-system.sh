#!/bin/bash

# ==============================================
# COMPREHENSIVE AUDIT LOGGING TEST SUITE
# Phase 2: Automatic Data Change Tracking + Testing
# ==============================================

set -e  # Exit on error

echo "=========================================="
echo "AUDIT LOGGING COMPREHENSIVE TEST SUITE"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test results tracking
TESTS_PASSED=0
TESTS_FAILED=0
TESTS_TOTAL=0

# Function to print section header
print_header() {
    echo ""
    echo -e "${BLUE}=========================================="
    echo "$1"
    echo -e "==========================================${NC}"
    echo ""
}

# Function to print test result
print_test_result() {
    local test_name="$1"
    local passed="$2"

    TESTS_TOTAL=$((TESTS_TOTAL + 1))

    if [ "$passed" == "true" ]; then
        echo -e "${GREEN}✓ PASSED:${NC} $test_name"
        TESTS_PASSED=$((TESTS_PASSED + 1))
    else
        echo -e "${RED}✗ FAILED:${NC} $test_name"
        TESTS_FAILED=$((TESTS_FAILED + 1))
    fi
}

# Function to execute SQL and check result
execute_sql() {
    local query="$1"
    PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_db -t -c "$query" 2>&1
}

# ==============================================
# TEST ENVIRONMENT SETUP
# ==============================================
print_header "TEST ENVIRONMENT SETUP"

# Check if PostgreSQL is running
echo "Checking PostgreSQL connection..."
if PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_db -c "SELECT 1" > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} PostgreSQL connection successful"
else
    echo -e "${RED}✗${NC} PostgreSQL connection failed"
    exit 1
fi

# Get initial audit log count
INITIAL_AUDIT_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs;" | xargs)
echo "Initial audit log count: $INITIAL_AUDIT_COUNT"

# ==============================================
# TEST SUITE 1: AUTHENTICATION AUDIT LOGGING
# ==============================================
print_header "TEST SUITE 1: Authentication Audit Logging"

echo "Test 1.1: Login Success Audit"
# Make a login request
LOGIN_RESPONSE=$(curl -s -X POST "http://localhost:5090/api/auth/tenant/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@acme.com",
    "password": "Admin123!",
    "subdomain": "acme"
  }')

# Wait for async audit logging
sleep 2

# Check if LOGIN_SUCCESS was logged
LOGIN_SUCCESS_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type = 'LOGIN_SUCCESS' AND user_email = 'admin@acme.com' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

if [ "$LOGIN_SUCCESS_COUNT" -gt "0" ]; then
    print_test_result "Login success should be audited" "true"
else
    print_test_result "Login success should be audited" "false"
fi

echo ""
echo "Test 1.2: Failed Login Audit"
# Make a failed login request
curl -s -X POST "http://localhost:5090/api/auth/tenant/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@acme.com",
    "password": "WrongPassword123!",
    "subdomain": "acme"
  }' > /dev/null

# Wait for async audit logging
sleep 2

# Check if LOGIN_FAILED was logged
LOGIN_FAILED_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type = 'LOGIN_FAILED' AND user_email = 'admin@acme.com' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

if [ "$LOGIN_FAILED_COUNT" -gt "0" ]; then
    print_test_result "Failed login should be audited" "true"
else
    print_test_result "Failed login should be audited" "false"
fi

# ==============================================
# TEST SUITE 2: DATA CHANGE AUDIT LOGGING
# ==============================================
print_header "TEST SUITE 2: Data Change Audit Logging"

# Extract JWT token from login response
TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo -e "${YELLOW}⚠${NC} Cannot run data change tests - login failed"
else
    echo "Test 2.1: Employee Creation Audit"

    # Get count before
    CREATE_COUNT_BEFORE=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%CREATE%EMPLOYEE%';" | xargs)

    # Create an employee
    CREATE_RESPONSE=$(curl -s -X POST "http://localhost:5090/api/employees" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d '{
        "firstName": "Test",
        "lastName": "Employee",
        "email": "test.employee@acme.com",
        "jobTitle": "Software Engineer",
        "hireDate": "2025-01-01",
        "salary": 50000
      }')

    # Wait for async audit logging
    sleep 2

    # Get count after
    CREATE_COUNT_AFTER=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%CREATE%EMPLOYEE%';" | xargs)

    if [ "$CREATE_COUNT_AFTER" -gt "$CREATE_COUNT_BEFORE" ]; then
        print_test_result "Employee creation should be audited" "true"
    else
        print_test_result "Employee creation should be audited" "false"
    fi

    echo ""
    echo "Test 2.2: Employee Update (Non-Sensitive) Audit"

    # Get the created employee ID
    EMPLOYEE_ID=$(echo $CREATE_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)

    if [ -n "$EMPLOYEE_ID" ]; then
        # Update employee (non-sensitive field)
        curl -s -X PUT "http://localhost:5090/api/employees/$EMPLOYEE_ID" \
          -H "Authorization: Bearer $TOKEN" \
          -H "Content-Type: application/json" \
          -d '{
            "jobTitle": "Senior Software Engineer"
          }' > /dev/null

        # Wait for async audit logging
        sleep 2

        # Check if UPDATE was logged with severity INFO
        UPDATE_INFO_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%UPDATE%EMPLOYEE%' AND entity_id::text = '$EMPLOYEE_ID' AND severity = 'INFO' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

        if [ "$UPDATE_INFO_COUNT" -gt "0" ]; then
            print_test_result "Non-sensitive employee update should be audited with INFO severity" "true"
        else
            print_test_result "Non-sensitive employee update should be audited with INFO severity" "false"
        fi
    else
        print_test_result "Non-sensitive employee update should be audited with INFO severity" "false"
    fi

    echo ""
    echo "Test 2.3: Employee Update (Sensitive) Audit"

    if [ -n "$EMPLOYEE_ID" ]; then
        # Update employee (sensitive field: salary)
        curl -s -X PUT "http://localhost:5090/api/employees/$EMPLOYEE_ID" \
          -H "Authorization: Bearer $TOKEN" \
          -H "Content-Type: application/json" \
          -d '{
            "salary": 75000
          }' > /dev/null

        # Wait for async audit logging
        sleep 2

        # Check if UPDATE was logged with severity WARNING
        UPDATE_WARNING_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%UPDATE%EMPLOYEE%' AND entity_id::text = '$EMPLOYEE_ID' AND severity = 'WARNING' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

        if [ "$UPDATE_WARNING_COUNT" -gt "0" ]; then
            print_test_result "Sensitive employee update should be audited with WARNING severity" "true"
        else
            print_test_result "Sensitive employee update should be audited with WARNING severity" "false"
        fi
    else
        print_test_result "Sensitive employee update should be audited with WARNING severity" "false"
    fi

    echo ""
    echo "Test 2.4: Employee Deletion Audit"

    if [ -n "$EMPLOYEE_ID" ]; then
        # Delete employee
        curl -s -X DELETE "http://localhost:5090/api/employees/$EMPLOYEE_ID" \
          -H "Authorization: Bearer $TOKEN" > /dev/null

        # Wait for async audit logging
        sleep 2

        # Check if DELETE was logged with severity WARNING
        DELETE_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%DELETE%EMPLOYEE%' AND entity_id::text = '$EMPLOYEE_ID' AND severity = 'WARNING' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

        if [ "$DELETE_COUNT" -gt "0" ]; then
            print_test_result "Employee deletion should be audited with WARNING severity" "true"
        else
            print_test_result "Employee deletion should be audited with WARNING severity" "false"
        fi
    else
        print_test_result "Employee deletion should be audited with WARNING severity" "false"
    fi
fi

# ==============================================
# TEST SUITE 3: HTTP REQUEST AUDIT LOGGING
# ==============================================
print_header "TEST SUITE 3: HTTP Request Audit Logging"

echo "Test 3.1: GET Request Audit"

# Make a GET request
curl -s -X GET "http://localhost:5090/api/employees" \
  -H "Authorization: Bearer $TOKEN" > /dev/null

# Wait for async audit logging
sleep 2

# Check if HTTP GET was logged
HTTP_GET_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type = 'HTTP_REQUEST' AND request_method = 'GET' AND request_path LIKE '/api/employees%' AND performed_at > NOW() - INTERVAL '1 minute';" | xargs)

if [ "$HTTP_GET_COUNT" -gt "0" ]; then
    print_test_result "GET request should be audited" "true"
else
    print_test_result "GET request should be audited" "false"
fi

# ==============================================
# TEST SUITE 4: TENANT OPERATIONS AUDIT
# ==============================================
print_header "TEST SUITE 4: Tenant Operations Audit"

echo "Test 4.1: Tenant Login Audit"

# Tenant-specific audit logs should exist
TENANT_AUDIT_COUNT=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE tenant_id IS NOT NULL AND performed_at > NOW() - INTERVAL '5 minutes';" | xargs)

if [ "$TENANT_AUDIT_COUNT" -gt "0" ]; then
    print_test_result "Tenant-specific audit logs should exist" "true"
else
    print_test_result "Tenant-specific audit logs should exist" "false"
fi

# ==============================================
# TEST SUITE 5: PERFORMANCE TESTING
# ==============================================
print_header "TEST SUITE 5: Performance Testing"

echo "Test 5.1: Query Performance (< 100ms)"

# Measure query performance
START_TIME=$(date +%s%N)
execute_sql "SELECT * FROM audit_logs WHERE performed_at > NOW() - INTERVAL '1 day' LIMIT 100;" > /dev/null
END_TIME=$(date +%s%N)

ELAPSED_MS=$(( ($END_TIME - $START_TIME) / 1000000 ))

echo "Query time: ${ELAPSED_MS}ms"

if [ "$ELAPSED_MS" -lt "100" ]; then
    print_test_result "Audit log query should complete in < 100ms" "true"
else
    print_test_result "Audit log query should complete in < 100ms" "false"
fi

# ==============================================
# TEST SUITE 6: DATA COMPLETENESS
# ==============================================
print_header "TEST SUITE 6: Data Completeness Verification"

echo "Test 6.1: Required Fields Populated"

# Check that recent audit logs have required fields
COMPLETE_LOGS=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE performed_at > NOW() - INTERVAL '5 minutes' AND action_type IS NOT NULL AND category IS NOT NULL AND severity IS NOT NULL;" | xargs)

TOTAL_RECENT_LOGS=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE performed_at > NOW() - INTERVAL '5 minutes';" | xargs)

if [ "$COMPLETE_LOGS" == "$TOTAL_RECENT_LOGS" ] && [ "$TOTAL_RECENT_LOGS" -gt "0" ]; then
    print_test_result "All audit logs should have required fields populated" "true"
else
    print_test_result "All audit logs should have required fields populated" "false"
fi

echo ""
echo "Test 6.2: Before/After Values for Updates"

# Check that UPDATE operations have old_values and new_values
UPDATE_LOGS_WITH_VALUES=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%UPDATE%' AND performed_at > NOW() - INTERVAL '5 minutes' AND old_values IS NOT NULL AND new_values IS NOT NULL;" | xargs)

TOTAL_UPDATE_LOGS=$(execute_sql "SELECT COUNT(*) FROM audit_logs WHERE action_type LIKE '%UPDATE%' AND performed_at > NOW() - INTERVAL '5 minutes';" | xargs)

if [ "$TOTAL_UPDATE_LOGS" -gt "0" ]; then
    if [ "$UPDATE_LOGS_WITH_VALUES" == "$TOTAL_UPDATE_LOGS" ]; then
        print_test_result "UPDATE operations should have before/after values" "true"
    else
        print_test_result "UPDATE operations should have before/after values" "false"
    fi
else
    echo -e "${YELLOW}⚠${NC} No UPDATE operations found in last 5 minutes"
fi

# ==============================================
# FINAL SUMMARY
# ==============================================
print_header "TEST RESULTS SUMMARY"

echo "Total Tests: $TESTS_TOTAL"
echo -e "${GREEN}Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Failed: $TESTS_FAILED${NC}"
echo ""

if [ "$TESTS_FAILED" -eq "0" ]; then
    echo -e "${GREEN}=========================================="
    echo "ALL TESTS PASSED!"
    echo -e "==========================================${NC}"
    exit 0
else
    echo -e "${RED}=========================================="
    echo "SOME TESTS FAILED - Review output above"
    echo -e "==========================================${NC}"
    exit 1
fi
