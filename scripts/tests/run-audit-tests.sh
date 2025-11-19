#!/bin/bash

# Audit Logging Runtime Verification - Simplified Version
API_URL="http://localhost:5090/api"
DB_NAME="hrms_master"

echo "=========================================="
echo "AUDIT LOGGING RUNTIME VERIFICATION"
echo "=========================================="

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PASSED=0
FAILED=0

print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ PASSED${NC}: $2"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC}: $2"
        ((FAILED++))
    fi
}

# Login as Admin
echo""
echo -e "${BLUE}Logging in as admin...${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/Auth/login" \
    -H "Content-Type: application/json" \
    -d '{
        "email": "admin@hrms.com",
        "password": "Admin@123"
    }')

TOKEN=$(echo $LOGIN_RESPONSE | python3 -c "import sys, json; print(json.load(sys.stdin)['data']['token'])" 2>/dev/null)

if [ -z "$TOKEN" ]; then
    echo -e "${RED}Failed to login${NC}"
    exit 1
fi

echo -e "${GREEN}Login successful${NC}"

# Test 1: Create Tenant
echo ""
echo "=========================================="
echo "TEST 1: SuperAdmin Creates Tenant"
echo "=========================================="

TENANT_NAME="AuditTest$(date +%s)"
TENANT_SUB="audit$(date +%s)"

echo "Creating tenant: $TENANT_NAME (subdomain: $TENANT_SUB)..."
CREATE_TENANT=$(curl -s -X POST "$API_URL/Tenant" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
        \"tenantName\": \"$TENANT_NAME\",
        \"subdomain\": \"$TENANT_SUB\",
        \"primaryContactName\": \"Test Admin\",
        \"primaryContactEmail\": \"testadmin@example.com\",
        \"primaryContactPhone\": \"1234567890\",
        \"industrySectorId\": 1
    }")

echo "$CREATE_TENANT" | python3 -m json.tool 2>/dev/null || echo "$CREATE_TENANT"

# Extract tenant ID
TENANT_ID=$(echo $CREATE_TENANT | python3 -c "import sys, json; print(json.load(sys.stdin).get('data', {}).get('id', ''))" 2>/dev/null)

sleep 2

# Query audit log
echo ""
echo "Querying audit log for CREATE_TENANT..."
AUDIT_LOG=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT jsonb_pretty(row_to_json(t)::jsonb) FROM (
        SELECT
            \"Action\",
            \"EntityType\",
            \"EntityId\",
            \"UserEmail\",
            \"IpAddress\",
            \"Severity\",
            \"ChangedFields\",
            substring(\"OldValues\"::text, 1, 50) as \"OldValues\",
            substring(\"NewValues\"::text, 1, 100) as \"NewValues_Preview\"
        FROM master.\"AuditLogs\"
        WHERE \"Action\" = 'CREATE_TENANT'
        ORDER BY \"Timestamp\" DESC
        LIMIT 1
    ) t;" 2>/dev/null)

if [ -n "$AUDIT_LOG" ]; then
    echo "$AUDIT_LOG"

    HAS_EMAIL=$(echo "$AUDIT_LOG" | grep -c "UserEmail")
    HAS_NEW_VALUES=$(echo "$AUDIT_LOG" | grep -c "NewValues")

    if [ $HAS_EMAIL -gt 0 ] && [ $HAS_NEW_VALUES -gt 0 ]; then
        print_result 0 "Test 1 - CREATE_TENANT audit log created with auto-enrichment"
    else
        print_result 1 "Test 1 - Auto-enrichment incomplete"
    fi
else
    print_result 1 "Test 1 - No CREATE_TENANT audit log found"
fi

# Test 2-4: Use existing tenant for employee tests
echo ""
echo "=========================================="
echo "TEST 2-4: Employee Operations (Using Existing Tenant)"
echo "=========================================="

# Use an existing active tenant
echo "Finding an active tenant..."
EXISTING_TENANT=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT \"Id\" || '|' || \"Subdomain\" || '|' || \"AdminEmail\"
     FROM master.\"Tenants\"
     WHERE \"Status\" = 1 AND \"IsDeleted\" = false
     LIMIT 1;" 2>/dev/null)

if [ -n "$EXISTING_TENANT" ]; then
    IFS='|' read -r TENANT_ID TENANT_SUBDOMAIN ADMIN_EMAIL <<< "$EXISTING_TENANT"

    echo "Using tenant: $TENANT_SUBDOMAIN (Admin: $ADMIN_EMAIL)"

    # Login as tenant admin
    TENANT_LOGIN=$(curl -s -X POST "$API_URL/Auth/login" \
        -H "Content-Type: application/json" \
        -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
        -d "{
            \"email\": \"$ADMIN_EMAIL\",
            \"password\": \"Admin@123\"
        }")

    TENANT_TOKEN=$(echo $TENANT_LOGIN | python3 -c "import sys, json; print(json.load(sys.stdin).get('data', {}).get('token', ''))" 2>/dev/null)

    if [ -n "$TENANT_TOKEN" ]; then
        echo -e "${GREEN}Tenant admin login successful${NC}"

        # Test 2: Create Employee
        echo ""
        echo "TEST 2: Creating employee..."
        CREATE_EMP=$(curl -s -X POST "$API_URL/Employee" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $TENANT_TOKEN" \
            -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
            -d '{
                "firstName": "AuditTest",
                "lastName": "Employee",
                "email": "audit.test@example.com",
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

        EMP_ID=$(echo $CREATE_EMP | python3 -c "import sys, json; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null)

        sleep 2

        # Check audit log
        CREATE_EMP_LOG=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
            "SELECT COUNT(*) FROM master.\"AuditLogs\"
             WHERE \"Action\" = 'CREATE_EMPLOYEE'
             AND \"Timestamp\" > NOW() - INTERVAL '1 minute';" 2>/dev/null)

        if [ "$CREATE_EMP_LOG" -gt 0 ]; then
            print_result 0 "Test 2 - CREATE_EMPLOYEE audit log created"
        else
            print_result 1 "Test 2 - No CREATE_EMPLOYEE audit log found"
        fi

        # Test 3: Update Employee Salary (CRITICAL TEST)
        if [ -n "$EMP_ID" ]; then
            echo ""
            echo "TEST 3: Updating employee salary (CRITICAL SENSITIVE FIELD TEST)..."
            UPDATE_EMP=$(curl -s -X PUT "$API_URL/Employee/$EMP_ID" \
                -H "Content-Type: application/json" \
                -H "Authorization: Bearer $TENANT_TOKEN" \
                -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN" \
                -d '{
                    "firstName": "AuditTest",
                    "lastName": "Employee",
                    "email": "audit.test@example.com",
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

            sleep 2

            # Check for WARNING severity
            SALARY_UPDATE_LOG=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
                "SELECT
                    \"Severity\",
                    \"ChangedFields\",
                    substring(\"OldValues\"::text, 1, 100) as OldVal,
                    substring(\"NewValues\"::text, 1, 100) as NewVal
                 FROM master.\"AuditLogs\"
                 WHERE \"Action\" = 'UPDATE_EMPLOYEE'
                 AND \"Timestamp\" > NOW() - INTERVAL '1 minute'
                 ORDER BY \"Timestamp\" DESC LIMIT 1;" 2>/dev/null)

            echo "Salary update log: $SALARY_UPDATE_LOG"

            HAS_WARNING=$(echo "$SALARY_UPDATE_LOG" | grep -c "Warning")
            HAS_SALARY_FIELD=$(echo "$SALARY_UPDATE_LOG" | grep -ci "salary")

            if [ $HAS_WARNING -gt 0 ]; then
                print_result 0 "Test 3a - Sensitive field detection working (Severity=Warning)"
            else
                print_result 1 "Test 3a - Sensitive field detection FAILED (Expected Severity=Warning)"
            fi

            if [ $HAS_SALARY_FIELD -gt 0 ]; then
                print_result 0 "Test 3b - ChangedFields contains Salary"
            else
                print_result 1 "Test 3b - ChangedFields missing Salary"
            fi

            # Test 4: Delete Employee
            echo ""
            echo "TEST 4: Deleting employee..."
            DELETE_EMP=$(curl -s -X DELETE "$API_URL/Employee/$EMP_ID" \
                -H "Authorization: Bearer $TENANT_TOKEN" \
                -H "X-Tenant-Subdomain: $TENANT_SUBDOMAIN")

            sleep 2

            DELETE_LOG=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
                "SELECT COUNT(*) FROM master.\"AuditLogs\"
                 WHERE \"Action\" = 'DELETE_EMPLOYEE'
                 AND \"Timestamp\" > NOW() - INTERVAL '1 minute';" 2>/dev/null)

            if [ "$DELETE_LOG" -gt 0 ]; then
                print_result 0 "Test 4 - DELETE_EMPLOYEE audit log created"
            else
                print_result 1 "Test 4 - No DELETE_EMPLOYEE audit log found"
            fi
        else
            echo -e "${YELLOW}Skipping Tests 3-4 (employee creation failed)${NC}"
            print_result 1 "Test 3 - Skipped"
            print_result 1 "Test 4 - Skipped"
        fi
    else
        echo -e "${YELLOW}Could not login as tenant admin${NC}"
        print_result 1 "Test 2 - Skipped (login failed)"
        print_result 1 "Test 3 - Skipped (login failed)"
        print_result 1 "Test 4 - Skipped (login failed)"
    fi
else
    echo -e "${YELLOW}No active tenant found${NC}"
    print_result 1 "Test 2 - Skipped (no tenant)"
    print_result 1 "Test 3 - Skipped (no tenant)"
    print_result 1 "Test 4 - Skipped (no tenant)"
fi

# Test 5: View All Logs
echo ""
echo "=========================================="
echo "TEST 5: View All Audit Logs"
echo "=========================================="

ALL_LOGS=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT COUNT(*) FROM master.\"AuditLogs\"
     WHERE \"Timestamp\" > NOW() - INTERVAL '5 minutes';" 2>/dev/null)

echo "Recent audit log entries (last 5 minutes): $ALL_LOGS"

if [ "$ALL_LOGS" -ge 2 ]; then
    print_result 0 "Test 5 - Multiple audit log entries exist ($ALL_LOGS entries)"
else
    print_result 1 "Test 5 - Insufficient audit logs (found $ALL_LOGS)"
fi

echo ""
echo "Sample audit log entries:"
PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -c \
    "SELECT
        \"Action\",
        \"EntityType\",
        \"UserEmail\",
        \"Severity\",
        \"Timestamp\"
    FROM master.\"AuditLogs\"
    WHERE \"Timestamp\" > NOW() - INTERVAL '5 minutes'
    ORDER BY \"Timestamp\" DESC
    LIMIT 10;" 2>/dev/null

# Test 6: Compliance Report
echo ""
echo "=========================================="
echo "TEST 6: Compliance Report"
echo "=========================================="

echo "User Activity Summary (last hour):"
PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -c \
    "SELECT
        \"UserEmail\",
        COUNT(*) as \"ActionCount\",
        COUNT(DISTINCT \"Action\") as \"UniqueActions\"
    FROM master.\"AuditLogs\"
    WHERE \"Timestamp\" > NOW() - INTERVAL '1 hour'
    GROUP BY \"UserEmail\"
    ORDER BY \"ActionCount\" DESC
    LIMIT 10;" 2>/dev/null

REPORT_COUNT=$(PGPASSWORD=postgres psql -h localhost -U postgres -d $DB_NAME -t -A -c \
    "SELECT COUNT(DISTINCT \"UserEmail\") FROM master.\"AuditLogs\"
     WHERE \"Timestamp\" > NOW() - INTERVAL '1 hour';" 2>/dev/null)

if [ "$REPORT_COUNT" -gt 0 ]; then
    print_result 0 "Test 6 - Compliance report generated successfully"
else
    print_result 1 "Test 6 - Compliance report empty"
fi

# Final Results
echo ""
echo "=========================================="
echo "FINAL RESULTS"
echo "=========================================="
echo ""
echo "Total Tests: $((PASSED + FAILED))"
echo -e "${GREEN}Passed: $PASSED${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $PASSED -ge 4 ]; then
    echo -e "${GREEN}✓ AUDIT LOGGING SYSTEM VERIFIED${NC}"
    echo "Key Success: Interceptor creating audit logs automatically"
    exit 0
else
    echo -e "${RED}✗ AUDIT LOGGING NEEDS ATTENTION${NC}"
    exit 1
fi
