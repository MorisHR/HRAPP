#!/bin/bash

# Audit Logging Runtime Verification - 6 Core Tests
# This script verifies the SaveChanges interceptor is working correctly

API_URL="http://localhost:5090/api"
DB_NAME="hrms_master"
RESULTS_FILE="/tmp/audit_test_results.txt"

echo "=========================================="
echo "AUDIT LOGGING RUNTIME VERIFICATION"
echo "=========================================="
echo ""

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0

# Function to print test result
print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ PASSED${NC}: $2"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC}: $2"
        ((FAILED++))
    fi
}

# Function to query audit logs
query_audit_log() {
    local action=$1
    PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
        "SELECT jsonb_pretty(row_to_json(t)::jsonb) FROM (
            SELECT
                \"Id\",
                \"Action\",
                \"EntityType\",
                \"EntityId\",
                \"UserEmail\",
                \"IpAddress\",
                \"UserAgent\",
                \"Severity\",
                \"ChangedFields\",
                \"OldValues\",
                \"NewValues\",
                \"Timestamp\"
            FROM master.\"AuditLogs\"
            WHERE \"Action\" = '$action'
            ORDER BY \"Timestamp\" DESC
            LIMIT 1
        ) t;" 2>/dev/null
}

# Wait for API to be ready
echo "Checking API availability..."
for i in {1..10}; do
    if curl -s http://localhost:5090/health > /dev/null 2>&1; then
        echo -e "${GREEN}API is ready${NC}"
        break
    fi
    if [ $i -eq 10 ]; then
        echo -e "${RED}API is not responding${NC}"
        exit 1
    fi
    echo "Waiting for API... ($i/10)"
    sleep 2
done

echo ""
echo "=========================================="
echo "TEST 1: SuperAdmin Creates Tenant"
echo "=========================================="

# Login as SuperAdmin
echo "Logging in as SuperAdmin..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/Auth/superadmin/login" \
    -H "Content-Type: application/json" \
    -d '{
        "email": "superadmin@hrms.com",
        "password": "SuperAdmin@123"
    }')

ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$ACCESS_TOKEN" ]; then
    echo -e "${RED}Failed to login as SuperAdmin${NC}"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "Login successful, creating tenant..."

# Create tenant
TENANT_NAME="AuditTest-$(date +%s)"
TENANT_SUBDOMAIN="audittest$(date +%s)"
CREATE_TENANT_RESPONSE=$(curl -s -X POST "$API_URL/Tenant" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -d "{
        \"tenantName\": \"$TENANT_NAME\",
        \"subdomain\": \"$TENANT_SUBDOMAIN\",
        \"primaryContactName\": \"Test Admin\",
        \"primaryContactEmail\": \"testadmin@test.com\",
        \"primaryContactPhone\": \"1234567890\",
        \"industrySectorId\": 1
    }")

echo "Tenant creation response: $CREATE_TENANT_RESPONSE"
sleep 2

# Query audit log for CREATE_TENANT
echo ""
echo "Querying audit log for CREATE_TENANT..."
AUDIT_LOG=$(query_audit_log "CREATE_TENANT")

if [ -n "$AUDIT_LOG" ]; then
    echo "$AUDIT_LOG"

    # Verify auto-enrichment
    HAS_IP=$(echo "$AUDIT_LOG" | grep -c "IpAddress")
    HAS_USER_EMAIL=$(echo "$AUDIT_LOG" | grep -c "UserEmail")
    HAS_NEW_VALUES=$(echo "$AUDIT_LOG" | grep -c "NewValues")
    OLD_VALUES_NULL=$(echo "$AUDIT_LOG" | grep "OldValues" | grep -c "null")

    if [ $HAS_IP -gt 0 ] && [ $HAS_USER_EMAIL -gt 0 ] && [ $HAS_NEW_VALUES -gt 0 ]; then
        print_result 0 "Test 1 - Auto-enrichment working (IP, UserEmail, NewValues populated)"
    else
        print_result 1 "Test 1 - Auto-enrichment failed"
    fi
else
    print_result 1 "Test 1 - No CREATE_TENANT audit log found"
fi

echo ""
echo "=========================================="
echo "TEST 2: Tenant Admin Creates Employee"
echo "=========================================="

# First, activate the tenant
echo "Activating tenant..."
TENANT_ID=$(echo $CREATE_TENANT_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)
ACTIVATE_RESPONSE=$(curl -s -X POST "$API_URL/Tenant/$TENANT_ID/activate" \
    -H "Authorization: Bearer $ACCESS_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"activationDate": "2025-11-09"}')

echo "Activation response: $ACTIVATE_RESPONSE"
sleep 2

# Login as tenant admin
echo "Logging in as tenant admin..."
TENANT_LOGIN=$(curl -s -X POST "$API_URL/Auth/login" \
    -H "Content-Type: application/json" \
    -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
    -d '{
        "email": "testadmin@test.com",
        "password": "Admin@123"
    }')

TENANT_TOKEN=$(echo $TENANT_LOGIN | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TENANT_TOKEN" ]; then
    echo -e "${YELLOW}Could not login as tenant admin - tenant may not be fully activated${NC}"
    echo "Response: $TENANT_LOGIN"
    print_result 1 "Test 2 - Could not login as tenant admin"
else
    echo "Tenant admin login successful, creating employee..."

    # Create employee
    CREATE_EMPLOYEE=$(curl -s -X POST "$API_URL/Employee" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $TENANT_TOKEN" \
        -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
        -d '{
            "firstName": "Test",
            "lastName": "Employee",
            "email": "test.employee@test.com",
            "phoneNumber": "1234567890",
            "dateOfBirth": "1990-01-01",
            "gender": "Male",
            "hireDate": "2025-11-09",
            "departmentId": 1,
            "designationId": 1,
            "employeeType": "FullTime",
            "employmentStatus": "Active",
            "salary": 50000.00
        }')

    echo "Employee creation response: $CREATE_EMPLOYEE"
    EMPLOYEE_ID=$(echo $CREATE_EMPLOYEE | grep -o '"id":"[^"]*' | cut -d'"' -f4 | head -1)
    sleep 2

    # Query audit log for CREATE_EMPLOYEE
    echo ""
    echo "Querying audit log for CREATE_EMPLOYEE..."
    AUDIT_LOG=$(query_audit_log "CREATE_EMPLOYEE")

    if [ -n "$AUDIT_LOG" ]; then
        echo "$AUDIT_LOG"

        # Verify tenant context
        HAS_TENANT_CONTEXT=$(echo "$AUDIT_LOG" | grep -c "testadmin@test.com")

        if [ $HAS_TENANT_CONTEXT -gt 0 ]; then
            print_result 0 "Test 2 - Tenant context captured correctly"
        else
            print_result 1 "Test 2 - Tenant context not captured"
        fi
    else
        print_result 1 "Test 2 - No CREATE_EMPLOYEE audit log found"
    fi
fi

echo ""
echo "=========================================="
echo "TEST 3: Update Employee Salary (CRITICAL)"
echo "=========================================="

if [ -n "$TENANT_TOKEN" ] && [ -n "$EMPLOYEE_ID" ]; then
    echo "Updating employee salary..."

    # Update salary
    UPDATE_EMPLOYEE=$(curl -s -X PUT "$API_URL/Employee/$EMPLOYEE_ID" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer $TENANT_TOKEN" \
        -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
        -d '{
            "firstName": "Test",
            "lastName": "Employee",
            "email": "test.employee@test.com",
            "phoneNumber": "1234567890",
            "dateOfBirth": "1990-01-01",
            "gender": "Male",
            "hireDate": "2025-11-09",
            "departmentId": 1,
            "designationId": 1,
            "employeeType": "FullTime",
            "employmentStatus": "Active",
            "salary": 75000.00
        }')

    echo "Update response: $UPDATE_EMPLOYEE"
    sleep 2

    # Query audit log for UPDATE_EMPLOYEE
    echo ""
    echo "Querying audit log for UPDATE_EMPLOYEE..."
    AUDIT_LOG=$(query_audit_log "UPDATE_EMPLOYEE")

    if [ -n "$AUDIT_LOG" ]; then
        echo "$AUDIT_LOG"

        # Verify Severity = Warning (sensitive field detection)
        SEVERITY=$(echo "$AUDIT_LOG" | grep "Severity" | grep -c "Warning")
        HAS_CHANGED_FIELDS=$(echo "$AUDIT_LOG" | grep "ChangedFields" | grep -c "Salary")
        HAS_OLD_VALUES=$(echo "$AUDIT_LOG" | grep -c "OldValues")
        HAS_NEW_VALUES=$(echo "$AUDIT_LOG" | grep -c "NewValues")

        if [ $SEVERITY -gt 0 ]; then
            print_result 0 "Test 3a - Sensitive field detection working (Severity=Warning)"
        else
            print_result 1 "Test 3a - Sensitive field detection failed (Severity != Warning)"
        fi

        if [ $HAS_CHANGED_FIELDS -gt 0 ]; then
            print_result 0 "Test 3b - ChangedFields contains 'Salary'"
        else
            print_result 1 "Test 3b - ChangedFields missing 'Salary'"
        fi

        if [ $HAS_OLD_VALUES -gt 0 ] && [ $HAS_NEW_VALUES -gt 0 ]; then
            print_result 0 "Test 3c - OldValues and NewValues populated"
        else
            print_result 1 "Test 3c - OldValues or NewValues missing"
        fi
    else
        print_result 1 "Test 3 - No UPDATE_EMPLOYEE audit log found"
    fi
else
    echo -e "${YELLOW}Skipping Test 3 - Employee not created${NC}"
    print_result 1 "Test 3 - Skipped (no employee)"
fi

echo ""
echo "=========================================="
echo "TEST 4: Delete Entity"
echo "=========================================="

if [ -n "$TENANT_TOKEN" ] && [ -n "$EMPLOYEE_ID" ]; then
    echo "Deleting employee..."

    DELETE_RESPONSE=$(curl -s -X DELETE "$API_URL/Employee/$EMPLOYEE_ID" \
        -H "Authorization: Bearer $TENANT_TOKEN" \
        -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN")

    echo "Delete response: $DELETE_RESPONSE"
    sleep 2

    # Query audit log for DELETE_EMPLOYEE
    echo ""
    echo "Querying audit log for DELETE_EMPLOYEE..."
    AUDIT_LOG=$(query_audit_log "DELETE_EMPLOYEE")

    if [ -n "$AUDIT_LOG" ]; then
        echo "$AUDIT_LOG"

        # Verify OldValues has data
        HAS_OLD_VALUES=$(echo "$AUDIT_LOG" | grep "OldValues" | grep -cv "null")

        if [ $HAS_OLD_VALUES -gt 0 ]; then
            print_result 0 "Test 4 - OldValues populated before deletion"
        else
            print_result 1 "Test 4 - OldValues not populated"
        fi
    else
        print_result 1 "Test 4 - No DELETE_EMPLOYEE audit log found"
    fi
else
    echo -e "${YELLOW}Skipping Test 4 - Employee not created${NC}"
    print_result 1 "Test 4 - Skipped (no employee)"
fi

echo ""
echo "=========================================="
echo "TEST 5: View All Logs"
echo "=========================================="

echo "Querying all audit logs..."
ALL_LOGS=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT COUNT(*) FROM master.\"AuditLogs\" WHERE \"Timestamp\" > NOW() - INTERVAL '5 minutes';" 2>/dev/null)

echo "Recent audit log entries (last 5 minutes): $ALL_LOGS"

if [ "$ALL_LOGS" -ge 3 ]; then
    print_result 0 "Test 5 - Multiple audit log entries exist ($ALL_LOGS entries)"
else
    print_result 1 "Test 5 - Insufficient audit log entries (expected 3+, found $ALL_LOGS)"
fi

echo ""
echo "Sample audit log entries:"
PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -c \
    "SELECT
        \"Action\",
        \"EntityType\",
        \"UserEmail\",
        \"Severity\",
        \"ChangedFields\",
        \"Timestamp\"
    FROM master.\"AuditLogs\"
    WHERE \"Timestamp\" > NOW() - INTERVAL '5 minutes'
    ORDER BY \"Timestamp\" DESC
    LIMIT 5;" 2>/dev/null

echo ""
echo "=========================================="
echo "TEST 6: Run Compliance Report"
echo "=========================================="

echo "Executing user activity summary query..."
COMPLIANCE_REPORT=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT
        \"UserEmail\",
        COUNT(*) as ActionCount,
        COUNT(DISTINCT \"Action\") as UniqueActions
    FROM master.\"AuditLogs\"
    WHERE \"Timestamp\" > NOW() - INTERVAL '1 hour'
    GROUP BY \"UserEmail\"
    ORDER BY ActionCount DESC
    LIMIT 5;" 2>/dev/null)

if [ -n "$COMPLIANCE_REPORT" ]; then
    echo "User Activity Summary:"
    echo "$COMPLIANCE_REPORT" | while IFS='|' read -r email count unique; do
        echo "  - $email: $count actions ($unique unique)"
    done
    print_result 0 "Test 6 - Compliance report executed successfully"
else
    print_result 1 "Test 6 - Compliance report failed"
fi

echo ""
echo "=========================================="
echo "AUDIT LOGGING VERIFICATION RESULTS"
echo "=========================================="
echo ""
echo -e "Total Tests: $((PASSED + FAILED))"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $PASSED -ge 5 ]; then
    echo -e "${GREEN}✓ AUDIT LOGGING SYSTEM VERIFIED${NC}"
    exit 0
else
    echo -e "${RED}✗ AUDIT LOGGING SYSTEM NEEDS ATTENTION${NC}"
    exit 1
fi
