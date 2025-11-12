#!/bin/bash

################################################################################
# Post-Migration Health Check Script
# Purpose: Comprehensive validation after database migrations
# Usage: ./post-migration-health-check.sh [database_name] [schema_name]
# Example: ./post-migration-health-check.sh hrms_db tenant_default
################################################################################

set -euo pipefail

# Configuration
DB_NAME="${1:-hrms_db}"
SCHEMA_NAME="${2:-tenant_default}"
DB_USER="${3:-postgres}"
DB_HOST="${4:-localhost}"
DB_PORT="${5:-5432}"

# Color codes
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
CHECKS_PASSED=0
CHECKS_FAILED=0
CHECKS_WARNING=0

# Report file
REPORT_FILE="/var/log/hrms/post-migration-report-$(date +%Y%m%d-%H%M%S).log"
mkdir -p "$(dirname "$REPORT_FILE")"

################################################################################
# Helper Functions
################################################################################

log() {
    echo "$*" | tee -a "$REPORT_FILE"
}

run_query() {
    local query="$1"
    PGPASSWORD="${DB_PASSWORD:-}" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "$query" 2>&1
}

check_result() {
    local test_name="$1"
    local expected="$2"
    local actual="$3"
    local severity="${4:-CRITICAL}"  # CRITICAL or WARNING

    if [ "$expected" = "$actual" ]; then
        echo -e "${GREEN}[PASS]${NC} $test_name"
        log "[PASS] $test_name (Expected: $expected, Got: $actual)"
        ((CHECKS_PASSED++))
        return 0
    else
        if [ "$severity" = "WARNING" ]; then
            echo -e "${YELLOW}[WARN]${NC} $test_name"
            log "[WARN] $test_name (Expected: $expected, Got: $actual)"
            ((CHECKS_WARNING++))
        else
            echo -e "${RED}[FAIL]${NC} $test_name"
            log "[FAIL] $test_name (Expected: $expected, Got: $actual)"
            ((CHECKS_FAILED++))
        fi
        return 1
    fi
}

section() {
    local title="$1"
    echo ""
    echo "================================================================================"
    echo "$title"
    echo "================================================================================"
    log ""
    log "================================================================================"
    log "$title"
    log "================================================================================"
}

################################################################################
# Migration 1: Unique Constraints Validation
################################################################################

validate_unique_constraints() {
    section "MIGRATION 1: UNIQUE CONSTRAINTS VALIDATION"

    echo "Checking unique indexes on Employees table..."

    # Check NationalIdCard unique index
    local idx1=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_NationalIdCard_Unique';
    ")
    check_result "IX_Employees_NationalIdCard_Unique exists" "1" "$idx1"

    # Check PassportNumber unique index
    local idx2=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_PassportNumber_Unique';
    ")
    check_result "IX_Employees_PassportNumber_Unique exists" "1" "$idx2"

    # Check TaxIdNumber unique index
    local idx3=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_TaxIdNumber_Unique';
    ")
    check_result "IX_Employees_TaxIdNumber_Unique exists" "1" "$idx3"

    # Check NPFNumber unique index
    local idx4=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_NPFNumber_Unique';
    ")
    check_result "IX_Employees_NPFNumber_Unique exists" "1" "$idx4"

    # Check NSFNumber unique index
    local idx5=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_NSFNumber_Unique';
    ")
    check_result "IX_Employees_NSFNumber_Unique exists" "1" "$idx5"

    # Check BankAccountNumber unique index
    local idx6=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_BankAccountNumber_Unique';
    ")
    check_result "IX_Employees_BankAccountNumber_Unique exists" "1" "$idx6"

    # Verify no duplicate National IDs (excluding NULL and deleted)
    local duplicates=$(run_query "
        SELECT COUNT(*)
        FROM (
            SELECT \"NationalIdCard\"
            FROM \"$SCHEMA_NAME\".\"Employees\"
            WHERE \"NationalIdCard\" IS NOT NULL
            AND \"IsDeleted\" = false
            GROUP BY \"NationalIdCard\"
            HAVING COUNT(*) > 1
        ) AS dupes;
    ")
    check_result "No duplicate National IDs" "0" "$duplicates"

    # Verify no duplicate Passport Numbers
    local dup_passport=$(run_query "
        SELECT COUNT(*)
        FROM (
            SELECT \"PassportNumber\"
            FROM \"$SCHEMA_NAME\".\"Employees\"
            WHERE \"PassportNumber\" IS NOT NULL
            AND \"IsDeleted\" = false
            GROUP BY \"PassportNumber\"
            HAVING COUNT(*) > 1
        ) AS dupes;
    ")
    check_result "No duplicate Passport Numbers" "0" "$dup_passport"
}

################################################################################
# Migration 2: Composite Indexes Validation
################################################################################

validate_composite_indexes() {
    section "MIGRATION 2: COMPOSITE INDEXES VALIDATION"

    echo "Checking composite indexes..."

    # Payroll Cycle indexes
    local idx1=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'PayrollCycles'
        AND indexname = 'IX_PayrollCycles_Year_Month';
    ")
    check_result "IX_PayrollCycles_Year_Month exists" "1" "$idx1"

    local idx2=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'PayrollCycles'
        AND indexname = 'IX_PayrollCycles_Status_PaymentDate';
    ")
    check_result "IX_PayrollCycles_Status_PaymentDate exists" "1" "$idx2"

    # Leave Balance indexes
    local idx3=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'LeaveBalances'
        AND indexname = 'IX_LeaveBalances_EmployeeId_Year_LeaveTypeId';
    ")
    check_result "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId exists" "1" "$idx3"

    # Attendance indexes
    local idx4=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Attendances'
        AND indexname = 'IX_Attendances_EmployeeId_Date_Status';
    ")
    check_result "IX_Attendances_EmployeeId_Date_Status exists" "1" "$idx4"

    local idx5=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Attendances'
        AND indexname = 'IX_Attendances_DeviceId_Date';
    ")
    check_result "IX_Attendances_DeviceId_Date exists" "1" "$idx5"

    # Timesheet indexes
    local idx6=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Timesheets'
        AND indexname = 'IX_Timesheets_Status_PeriodStart';
    ")
    check_result "IX_Timesheets_Status_PeriodStart exists" "1" "$idx6"

    local idx7=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Timesheets'
        AND indexname = 'IX_Timesheets_EmployeeId_Status_PeriodStart';
    ")
    check_result "IX_Timesheets_EmployeeId_Status_PeriodStart exists" "1" "$idx7"

    # Employee search index
    local idx8=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'Employees'
        AND indexname = 'IX_Employees_FirstName_LastName_IsActive';
    ")
    check_result "IX_Employees_FirstName_LastName_IsActive exists" "1" "$idx8"

    # Biometric punch indexes
    local idx9=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'BiometricPunchRecords'
        AND indexname = 'IX_BiometricPunchRecords_ProcessingStatus_PunchTime';
    ")
    check_result "IX_BiometricPunchRecords_ProcessingStatus_PunchTime exists" "1" "$idx9"

    local idx10=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'BiometricPunchRecords'
        AND indexname = 'IX_BiometricPunchRecords_EmployeeId_PunchTime';
    ")
    check_result "IX_BiometricPunchRecords_EmployeeId_PunchTime exists" "1" "$idx10"

    local idx11=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND tablename = 'BiometricPunchRecords'
        AND indexname = 'IX_BiometricPunchRecords_DeviceId_PunchTime';
    ")
    check_result "IX_BiometricPunchRecords_DeviceId_PunchTime exists" "1" "$idx11"

    # Verify all indexes are valid
    local invalid_indexes=$(run_query "
        SELECT COUNT(*)
        FROM pg_indexes i
        LEFT JOIN pg_class c ON c.relname = i.indexname
        LEFT JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = '$SCHEMA_NAME'
        AND NOT pg_index.indisvalid
        FROM pg_index
        WHERE pg_index.indexrelid = c.oid;
    ")
    check_result "All indexes are valid" "0" "$invalid_indexes" "WARNING"
}

################################################################################
# Migration 3: CHECK Constraints Validation
################################################################################

validate_check_constraints() {
    section "MIGRATION 3: CHECK CONSTRAINTS VALIDATION"

    echo "Checking CHECK constraints..."

    # Employee constraints
    local chk1=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_Employees_PasswordHash_Length'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_Employees_PasswordHash_Length exists" "1" "$chk1"

    local chk2=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_Employees_BasicSalary_NonNegative'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_Employees_BasicSalary_NonNegative exists" "1" "$chk2"

    # Payroll constraints
    local chk3=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_PayrollCycles_Month_Valid'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_PayrollCycles_Month_Valid exists" "1" "$chk3"

    local chk4=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_PayrollCycles_Year_Valid'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_PayrollCycles_Year_Valid exists" "1" "$chk4"

    # Attendance constraints
    local chk5=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_Attendances_WorkingHours_NonNegative'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_Attendances_WorkingHours_NonNegative exists" "1" "$chk5"

    # Device API Key constraints
    local chk6=$(run_query "
        SELECT COUNT(*)
        FROM pg_constraint
        WHERE conname = 'chk_DeviceApiKeys_Hash_Length'
        AND connamespace = '$SCHEMA_NAME'::regnamespace;
    ")
    check_result "chk_DeviceApiKeys_Hash_Length exists" "1" "$chk6"

    # Verify no constraint violations
    echo ""
    echo "Checking for data that would violate constraints..."

    # Check for negative salaries
    local neg_salary=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Employees\"
        WHERE \"BasicSalary\" < 0;
    ")
    check_result "No negative salaries" "0" "$neg_salary"

    # Check for invalid months in payroll
    local invalid_months=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"PayrollCycles\"
        WHERE \"Month\" < 1 OR \"Month\" > 12;
    ")
    check_result "No invalid months in PayrollCycles" "0" "$invalid_months"

    # Check for negative working hours
    local neg_hours=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Attendances\"
        WHERE \"WorkingHours\" < 0;
    ")
    check_result "No negative working hours" "0" "$neg_hours"
}

################################################################################
# Migration 4: Column-Level Encryption Validation
################################################################################

validate_encryption() {
    section "MIGRATION 4: COLUMN-LEVEL ENCRYPTION VALIDATION"

    echo "Checking encryption implementation..."

    # Note: Column-level encryption is implemented at the application layer
    # via EF Core value converters, not in the database schema itself

    echo "Encryption is handled by application-layer value converters"
    echo "Checking for encrypted data patterns..."

    # Check if NationalIdCard column contains data
    local encrypted_records=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Employees\"
        WHERE \"NationalIdCard\" IS NOT NULL;
    ")

    if [ "$encrypted_records" -gt 0 ]; then
        echo -e "${GREEN}[INFO]${NC} Found $encrypted_records employees with National ID data"
        log "[INFO] Found $encrypted_records employees with National ID data"

        # Sample check: Verify data doesn't look like plaintext
        # Encrypted data should be base64 encoded and look random
        local plaintext_pattern=$(run_query "
            SELECT COUNT(*)
            FROM \"$SCHEMA_NAME\".\"Employees\"
            WHERE \"NationalIdCard\" IS NOT NULL
            AND \"NationalIdCard\" ~ '^[0-9]{14}$';
        " || echo "0")

        if [ "$plaintext_pattern" = "0" ]; then
            echo -e "${GREEN}[PASS]${NC} National ID data appears to be encrypted (not plaintext)"
            log "[PASS] National ID data appears to be encrypted"
            ((CHECKS_PASSED++))
        else
            echo -e "${YELLOW}[WARN]${NC} Found $plaintext_pattern records that may contain plaintext data"
            log "[WARN] Found $plaintext_pattern records that may contain plaintext data"
            ((CHECKS_WARNING++))
        fi
    else
        echo -e "${BLUE}[INFO]${NC} No encrypted data found (expected for new installations)"
        log "[INFO] No encrypted data found"
    fi

    # Check for encryption service configuration in appsettings
    echo ""
    echo "Verifying encryption service configuration..."
    if [ -f "/workspaces/HRAPP/src/HRMS.API/appsettings.json" ]; then
        if grep -q '"Encryption"' "/workspaces/HRAPP/src/HRMS.API/appsettings.json"; then
            echo -e "${GREEN}[PASS]${NC} Encryption configuration found in appsettings.json"
            log "[PASS] Encryption configuration found"
            ((CHECKS_PASSED++))
        else
            echo -e "${RED}[FAIL]${NC} Encryption configuration missing from appsettings.json"
            log "[FAIL] Encryption configuration missing"
            ((CHECKS_FAILED++))
        fi
    fi
}

################################################################################
# Performance Benchmarks
################################################################################

run_performance_benchmarks() {
    section "PERFORMANCE BENCHMARKS"

    echo "Running query performance tests..."

    # Test 1: Employee lookup by National ID (should use unique index)
    local start=$(date +%s%N)
    run_query "
        SELECT \"Id\", \"FirstName\", \"LastName\"
        FROM \"$SCHEMA_NAME\".\"Employees\"
        WHERE \"NationalIdCard\" = 'test-id'
        AND \"IsDeleted\" = false
        LIMIT 1;
    " > /dev/null
    local end=$(date +%s%N)
    local duration=$(( (end - start) / 1000000 ))
    echo "Employee lookup by National ID: ${duration}ms"
    log "PERF: Employee lookup by National ID: ${duration}ms"

    # Test 2: Payroll cycle lookup (should use composite index)
    start=$(date +%s%N)
    run_query "
        SELECT \"Id\", \"Status\"
        FROM \"$SCHEMA_NAME\".\"PayrollCycles\"
        WHERE \"Year\" = 2025 AND \"Month\" = 1
        AND \"IsDeleted\" = false
        LIMIT 10;
    " > /dev/null
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))
    echo "Payroll cycle lookup by Year/Month: ${duration}ms"
    log "PERF: Payroll cycle lookup: ${duration}ms"

    # Test 3: Attendance query (should use composite index)
    start=$(date +%s%N)
    run_query "
        SELECT \"Id\", \"Status\", \"WorkingHours\"
        FROM \"$SCHEMA_NAME\".\"Attendances\"
        WHERE \"EmployeeId\" = '00000000-0000-0000-0000-000000000000'
        AND \"Date\" >= CURRENT_DATE - INTERVAL '30 days'
        AND \"IsDeleted\" = false
        ORDER BY \"Date\" DESC
        LIMIT 30;
    " > /dev/null
    end=$(date +%s%N)
    duration=$(( (end - start) / 1000000 ))
    echo "Attendance 30-day lookup: ${duration}ms"
    log "PERF: Attendance lookup: ${duration}ms"

    # Cache hit ratio
    local cache_hit=$(run_query "
        SELECT ROUND(100.0 * sum(blks_hit) / NULLIF(sum(blks_hit + blks_read), 0), 2)
        FROM pg_stat_database
        WHERE datname = '$DB_NAME';
    ")
    echo "Cache Hit Ratio: ${cache_hit}%"
    log "PERF: Cache Hit Ratio: ${cache_hit}%"

    if (( $(echo "$cache_hit < 90" | bc -l) )); then
        echo -e "${YELLOW}[WARN]${NC} Cache hit ratio below 90% (may improve over time)"
        ((CHECKS_WARNING++))
    else
        echo -e "${GREEN}[PASS]${NC} Cache hit ratio is healthy"
        ((CHECKS_PASSED++))
    fi
}

################################################################################
# Index Health Check
################################################################################

check_index_health() {
    section "INDEX HEALTH CHECK"

    echo "Checking for bloated or invalid indexes..."

    # Check for invalid indexes
    local invalid=$(run_query "
        SELECT COUNT(*)
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_index i ON i.indexrelid = c.oid
        WHERE n.nspname = '$SCHEMA_NAME'
        AND c.relkind = 'i'
        AND NOT i.indisvalid;
    ")
    check_result "No invalid indexes" "0" "$invalid"

    # Show index usage statistics for new indexes
    echo ""
    echo "Index usage statistics for migration indexes:"
    run_query "
        SELECT
            indexrelname AS index_name,
            idx_scan AS scans,
            idx_tup_read AS tuples_read,
            idx_tup_fetch AS tuples_fetched
        FROM pg_stat_user_indexes
        WHERE schemaname = '$SCHEMA_NAME'
        AND (
            indexrelname LIKE 'IX_Employees_%_Unique'
            OR indexrelname LIKE 'IX_PayrollCycles_%'
            OR indexrelname LIKE 'IX_LeaveBalances_%'
            OR indexrelname LIKE 'IX_Attendances_%'
            OR indexrelname LIKE 'IX_Timesheets_%'
            OR indexrelname LIKE 'IX_BiometricPunchRecords_%'
        )
        ORDER BY idx_scan DESC;
    " | column -t -s '|'
}

################################################################################
# Data Integrity Check
################################################################################

check_data_integrity() {
    section "DATA INTEGRITY CHECK"

    echo "Running data integrity checks..."

    # Check for orphaned records
    local orphaned_attendances=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Attendances\" a
        LEFT JOIN \"$SCHEMA_NAME\".\"Employees\" e ON e.\"Id\" = a.\"EmployeeId\"
        WHERE e.\"Id\" IS NULL;
    ")
    check_result "No orphaned attendance records" "0" "$orphaned_attendances" "WARNING"

    # Check for orphaned payslips
    local orphaned_payslips=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Payslips\" p
        LEFT JOIN \"$SCHEMA_NAME\".\"Employees\" e ON e.\"Id\" = p.\"EmployeeId\"
        WHERE e.\"Id\" IS NULL;
    ")
    check_result "No orphaned payslip records" "0" "$orphaned_payslips" "WARNING"

    # Check for future dates in attendance (data quality check)
    local future_attendance=$(run_query "
        SELECT COUNT(*)
        FROM \"$SCHEMA_NAME\".\"Attendances\"
        WHERE \"Date\" > CURRENT_DATE + INTERVAL '1 day';
    ")
    check_result "No future attendance records" "0" "$future_attendance" "WARNING"
}

################################################################################
# Summary Report
################################################################################

generate_summary() {
    section "HEALTH CHECK SUMMARY"

    local total_checks=$((CHECKS_PASSED + CHECKS_FAILED + CHECKS_WARNING))

    echo ""
    echo "Total Checks: $total_checks"
    echo -e "${GREEN}Passed: $CHECKS_PASSED${NC}"
    echo -e "${YELLOW}Warnings: $CHECKS_WARNING${NC}"
    echo -e "${RED}Failed: $CHECKS_FAILED${NC}"
    echo ""

    log ""
    log "Total Checks: $total_checks"
    log "Passed: $CHECKS_PASSED"
    log "Warnings: $CHECKS_WARNING"
    log "Failed: $CHECKS_FAILED"

    if [ $CHECKS_FAILED -eq 0 ]; then
        echo -e "${GREEN}ALL CRITICAL CHECKS PASSED${NC}"
        log "ALL CRITICAL CHECKS PASSED"

        if [ $CHECKS_WARNING -gt 0 ]; then
            echo -e "${YELLOW}Some warnings detected - review recommended${NC}"
            log "Some warnings detected - review recommended"
            return 1
        fi
        return 0
    else
        echo -e "${RED}CRITICAL ISSUES DETECTED - IMMEDIATE ACTION REQUIRED${NC}"
        log "CRITICAL ISSUES DETECTED - IMMEDIATE ACTION REQUIRED"
        return 2
    fi
}

################################################################################
# Main Execution
################################################################################

main() {
    echo "================================================================================"
    echo "POST-MIGRATION HEALTH CHECK"
    echo "Database: $DB_NAME"
    echo "Schema: $SCHEMA_NAME"
    echo "Time: $(date)"
    echo "================================================================================"
    echo ""

    log "================================================================================"
    log "POST-MIGRATION HEALTH CHECK"
    log "Database: $DB_NAME"
    log "Schema: $SCHEMA_NAME"
    log "Time: $(date)"
    log "================================================================================"

    # Run all validation checks
    validate_unique_constraints
    validate_composite_indexes
    validate_check_constraints
    validate_encryption
    check_index_health
    check_data_integrity
    run_performance_benchmarks

    # Generate summary
    generate_summary
    local exit_code=$?

    echo ""
    echo "Full report saved to: $REPORT_FILE"

    exit $exit_code
}

# Run main function
main
