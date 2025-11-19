# Database Migration Deployment Runbook

## Executive Summary

This runbook provides a comprehensive, step-by-step deployment plan for 4 critical database migrations to the HRMS production tenant database. The migrations enhance data integrity, query performance, and security through unique constraints, composite indexes, validation constraints, and column-level encryption.

**Deployment Date**: TBD
**Deployment Window**: 3-4 hours (off-peak recommended)
**Risk Level**: MEDIUM-HIGH
**Rollback Time**: 15-30 minutes

---

## Table of Contents

1. [Migration Overview](#migration-overview)
2. [Pre-Deployment Checklist](#pre-deployment-checklist)
3. [Risk Assessment](#risk-assessment)
4. [Deployment Strategy](#deployment-strategy)
5. [Step-by-Step Deployment Guide](#step-by-step-deployment-guide)
6. [Rollback Procedures](#rollback-procedures)
7. [Post-Deployment Verification](#post-deployment-verification)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Emergency Contacts](#emergency-contacts)

---

## Migration Overview

### 1. AddNationalIdUniqueConstraint (20251112)

**Purpose**: Enforce uniqueness on critical employee identification fields
**Risk Level**: HIGH - May fail if duplicate data exists
**Execution Time**: 5-10 seconds
**Locks**: ShareLock on Employees table during index creation

**Changes**:
- Creates 6 unique partial indexes on Employees table:
  - NationalIdCard (critical for data integrity)
  - PassportNumber (expatriate identification)
  - TaxIdNumber (tax compliance)
  - NPFNumber (Mauritius statutory)
  - NSFNumber (Mauritius statutory)
  - BankAccountNumber (payroll)

**Validation Requirements**:
- MUST scan for duplicate data BEFORE applying
- MUST clean duplicates if found
- Cannot proceed if active duplicates exist

---

### 2. AddMissingCompositeIndexes (20251112)

**Purpose**: Optimize query performance for high-traffic operations
**Risk Level**: LOW
**Execution Time**: 30-60 seconds
**Locks**: ShareLock during index creation (concurrent reads allowed)

**Changes**:
- Creates 13 composite indexes across multiple tables:
  - PayrollCycles: Year/Month, Status/PaymentDate
  - LeaveBalances: EmployeeId/Year/LeaveTypeId
  - Attendances: EmployeeId/Date/Status, DeviceId/Date
  - Timesheets: Status/PeriodStart, EmployeeId/Status/PeriodStart
  - Employees: FirstName/LastName/IsActive
  - TimesheetEntries: TimesheetId/Date
  - LeaveApplications: EmployeeId/StartDate/EndDate
  - BiometricPunchRecords: 3 indexes for processing optimization

**Expected Performance Improvements**:
- Payroll queries: 40-60% faster
- Attendance reports: 50-70% faster
- Leave lookups: 60-80% faster
- Biometric processing: 30-50% faster

---

### 3. AddDataValidationConstraints (20251112)

**Purpose**: Enforce data integrity rules at database level
**Risk Level**: MEDIUM - May fail if invalid data exists
**Execution Time**: 10-15 seconds
**Locks**: AccessExclusiveLock during constraint addition (writes blocked)

**Changes**:
- Adds 30+ CHECK constraints across 12 tables:
  - Non-negative numeric validations (salaries, hours, balances)
  - Valid date ranges (months 1-12, years > 1900)
  - String length validations (password hashes, API keys)
  - Non-empty text validations (descriptions)

**Validation Requirements**:
- MUST scan for invalid data BEFORE applying
- MUST clean invalid data if found
- Cannot proceed if constraint-violating data exists

**Tables Affected**:
- Employees, LeaveBalances, Attendances, PayrollCycles
- Payslips, Timesheets, TimesheetEntries, SalaryComponents
- LeaveEncashments, AttendanceAnomalies, DeviceApiKeys

---

### 4. AddColumnLevelEncryption (20251112031109)

**Purpose**: Enable AES-256-GCM encryption for sensitive PII data
**Risk Level**: MEDIUM
**Execution Time**: 5-10 seconds (schema only, no data migration)
**Locks**: ShareLock during index creation

**Special Considerations**:
- This migration is SCHEMA ONLY (no data encryption yet)
- It consolidates previous migrations (indexes + constraints)
- Actual data encryption happens via application layer
- Encryption service supports "passthrough mode" for zero-downtime deployment

**Encryption Implementation**:
- Algorithm: AES-256-GCM (FIPS 140-2 compliant)
- Passthrough Mode: Encryption disabled initially (existing plaintext data accessible)
- Progressive Encryption: New data encrypted, old data encrypted on update
- Zero-Downtime: No application restart required

**Encrypted Fields** (configured in application):
- NationalIdCard, PassportNumber, BankAccountNumber
- TaxIdNumber, NPFNumber, NSFNumber
- Salary components, contact information

---

## Pre-Deployment Checklist

### Environment Verification

```bash
# Run these commands to verify environment readiness
cd /workspaces/HRAPP

# 1. Verify database connectivity
psql -h <DB_HOST> -U postgres -d hrms_db -c "SELECT version();"

# 2. Check current migration status
dotnet ef migrations list --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context TenantDbContext

# 3. Verify disk space (need at least 10GB free)
df -h | grep -E 'Filesystem|/data'

# 4. Check database size
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    pg_size_pretty(pg_database_size('hrms_db')) as db_size,
    pg_size_pretty(pg_total_relation_size('tenant_default.\"Employees\"')) as employees_size;
"

# 5. Verify application is running
curl -f http://localhost:5000/health || echo "Application not responding"

# 6. Check active connections
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT count(*) as active_connections
FROM pg_stat_activity
WHERE datname = 'hrms_db' AND state = 'active';
"
```

### Data Quality Validation

```bash
# Run data quality checks BEFORE migration
cd /workspaces/HRAPP

# 1. Check for duplicate National IDs
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    \"NationalIdCard\",
    COUNT(*) as duplicate_count
FROM tenant_default.\"Employees\"
WHERE \"NationalIdCard\" IS NOT NULL
  AND \"IsDeleted\" = false
GROUP BY \"NationalIdCard\"
HAVING COUNT(*) > 1;
"

# 2. Check for duplicate Passport Numbers
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    \"PassportNumber\",
    COUNT(*) as duplicate_count
FROM tenant_default.\"Employees\"
WHERE \"PassportNumber\" IS NOT NULL
  AND \"IsDeleted\" = false
GROUP BY \"PassportNumber\"
HAVING COUNT(*) > 1;
"

# 3. Check for duplicate Bank Account Numbers
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    \"BankAccountNumber\",
    COUNT(*) as duplicate_count
FROM tenant_default.\"Employees\"
WHERE \"BankAccountNumber\" IS NOT NULL
  AND \"IsDeleted\" = false
GROUP BY \"BankAccountNumber\"
HAVING COUNT(*) > 1;
"

# 4. Check for negative salaries
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT COUNT(*) as negative_salary_count
FROM tenant_default.\"Employees\"
WHERE \"BasicSalary\" < 0;
"

# 5. Check for negative leave balances
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT COUNT(*) as negative_balance_count
FROM tenant_default.\"Employees\"
WHERE \"AnnualLeaveBalance\" < 0
   OR \"SickLeaveBalance\" < 0
   OR \"CasualLeaveBalance\" < 0;
"

# 6. Check for invalid months in payroll
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT COUNT(*) as invalid_month_count
FROM tenant_default.\"PayrollCycles\"
WHERE \"Month\" < 1 OR \"Month\" > 12;
"
```

### Backup Procedures

```bash
# 1. Create full database backup (MANDATORY)
pg_dump -h <DB_HOST> -U postgres -d hrms_db -F c -b -v \
  -f "backup_hrms_db_$(date +%Y%m%d_%H%M%S).dump"

# Expected time: 5-15 minutes depending on database size
# Expected size: 500MB - 5GB (varies by data volume)

# 2. Verify backup integrity
pg_restore --list "backup_hrms_db_$(date +%Y%m%d_%H%M%S).dump" | head -n 20

# 3. Create schema-only backup (for quick reference)
pg_dump -h <DB_HOST> -U postgres -d hrms_db --schema-only \
  -f "backup_hrms_db_schema_$(date +%Y%m%d_%H%M%S).sql"

# 4. Export migration status
dotnet ef migrations list --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API --context TenantDbContext \
  > "migration_status_$(date +%Y%m%d_%H%M%S).txt"

# 5. Copy backups to secure location
# IMPORTANT: Store backups on separate disk/server
cp backup_hrms_db_*.dump /backup/database/
```

### Staging Environment Testing

```bash
# MANDATORY: Test migrations in staging environment first

# 1. Restore production backup to staging
pg_restore -h <STAGING_HOST> -U postgres -d hrms_db_staging \
  -v -c backup_hrms_db_$(date +%Y%m%d_%H%M%S).dump

# 2. Apply migrations to staging
./scripts/deploy-migrations-staging.sh

# 3. Run comprehensive tests in staging
dotnet test tests/HRMS.Tests/HRMS.Tests.csproj

# 4. Verify application functionality in staging
curl -f http://staging.morishr.com/health
curl -f http://staging.morishr.com/api/employees

# 5. Performance testing in staging
# Run load tests to ensure indexes improve performance
# Measure query times before and after

# 6. Verify encryption service in staging
curl -f http://staging.morishr.com/api/health/encryption
```

### Required Access and Permissions

- [ ] PostgreSQL superuser access (for migrations)
- [ ] Database backup location write access (minimum 10GB free)
- [ ] Google Secret Manager access (for encryption key)
- [ ] Application deployment access (for restart if needed)
- [ ] Monitoring dashboard access (Datadog/Grafana/CloudWatch)
- [ ] Emergency rollback authority (approved change ticket)

### Communication Plan

- [ ] Notify stakeholders 48 hours before deployment
- [ ] Maintenance window scheduled (3-4 hours)
- [ ] Incident response team on standby
- [ ] Status page updated (maintenance mode)
- [ ] Rollback decision maker identified
- [ ] Post-deployment report template prepared

---

## Risk Assessment

### High Risk Scenarios

1. **Duplicate Data Blocking Migration #1**
   - **Probability**: MEDIUM
   - **Impact**: HIGH (blocks deployment)
   - **Mitigation**: Pre-deployment data quality scan
   - **Resolution**: Clean duplicates using provided SQL scripts

2. **Constraint Violations Blocking Migration #3**
   - **Probability**: MEDIUM
   - **Impact**: HIGH (blocks deployment)
   - **Mitigation**: Pre-deployment data validation
   - **Resolution**: Clean invalid data using provided SQL scripts

3. **Long-Running Index Creation Causing Timeouts**
   - **Probability**: LOW
   - **Impact**: MEDIUM (delays deployment)
   - **Mitigation**: Use CONCURRENTLY option, schedule during low-traffic
   - **Resolution**: Extend timeout, retry if transient

4. **Application Compatibility Issues Post-Migration**
   - **Probability**: LOW
   - **Impact**: HIGH (service disruption)
   - **Mitigation**: Staging environment testing, gradual rollout
   - **Resolution**: Rollback migration, investigate compatibility

### Medium Risk Scenarios

1. **Index Creation Performance Impact**
   - **Probability**: MEDIUM
   - **Impact**: LOW (temporary slowdown)
   - **Mitigation**: Schedule during off-peak hours
   - **Resolution**: Monitor and wait for completion

2. **Encryption Configuration Issues**
   - **Probability**: LOW
   - **Impact**: MEDIUM (encryption not working)
   - **Mitigation**: Passthrough mode allows graceful degradation
   - **Resolution**: Fix configuration, encryption activates on next update

### Low Risk Scenarios

1. **Rollback Complexity**
   - **Probability**: LOW
   - **Impact**: LOW (well-documented procedures)
   - **Mitigation**: Tested rollback scripts, full backup
   - **Resolution**: Execute rollback script

---

## Deployment Strategy

### Recommended: Sequential Deployment with Validation (Option B)

**Why This Strategy?**
- Provides checkpoints for validation at each step
- Easier to identify which migration caused issues
- Lower risk of cascading failures
- Better audit trail and logging
- Allows partial success (some migrations applied)

**Trade-offs**:
- Slightly longer deployment time (extra 10-15 minutes)
- More manual intervention points

### Alternative Strategies (NOT Recommended)

**Option A: Apply All Migrations Together**
- Faster (saves 10-15 minutes)
- Higher risk (harder to troubleshoot failures)
- All-or-nothing approach
- Use only if: Staging tests 100% successful, very tight time window

**Option C: Blue-Green Deployment**
- Zero downtime during migration
- Requires duplicate database (high cost)
- Complex coordination
- Use only if: Cannot tolerate any downtime, Fortune 500 SLA requirements

---

## Step-by-Step Deployment Guide

### Phase 1: Pre-Deployment (15-30 minutes)

#### Step 1.1: Final Environment Check

```bash
# Set deployment timestamp
export DEPLOYMENT_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
echo "Deployment started at: $DEPLOYMENT_TIMESTAMP"

# Change to project directory
cd /workspaces/HRAPP

# Verify all prerequisites
./scripts/verify-migrations.sh pre-check
```

**Expected Output**: All checks should pass (green)
**If Failed**: Review error messages, resolve issues before proceeding

#### Step 1.2: Enable Maintenance Mode (Optional)

```bash
# Put application in maintenance mode (optional but recommended)
# This prevents new data writes during migration

# Option 1: Using load balancer health check
curl -X POST http://localhost:5000/admin/maintenance/enable

# Option 2: Using feature flag
# Update appsettings.json or environment variable:
# "MaintenanceMode": { "Enabled": true }

# Verify maintenance mode active
curl http://localhost:5000/health
# Expected: Returns 503 Service Unavailable
```

#### Step 1.3: Create Final Backup

```bash
# Create timestamped backup
echo "Creating final backup..."
pg_dump -h <DB_HOST> -U postgres -d hrms_db -F c -b -v \
  -f "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

# Verify backup created successfully
ls -lh "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

# Record backup checksum
sha256sum "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump" \
  > "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump.sha256"

echo "Backup completed successfully!"
```

**Expected Time**: 5-15 minutes
**Expected Size**: 500MB - 5GB
**If Failed**: Cannot proceed without valid backup

#### Step 1.4: Data Quality Validation

```bash
# Run comprehensive data quality checks
./scripts/verify-migrations.sh data-quality

# Review output for any issues
# If duplicates or invalid data found, proceed to Step 1.5
```

**Expected Output**: "All data quality checks passed"
**If Failed**: Proceed to data cleanup (Step 1.5)

#### Step 1.5: Data Cleanup (If Needed)

```sql
-- Run these SQL scripts ONLY if data quality checks failed

-- ==========================================
-- DUPLICATE DATA CLEANUP
-- ==========================================

-- 1. Find and review duplicates
SELECT
    \"NationalIdCard\",
    STRING_AGG(\"Id\"::text, ', ') as employee_ids,
    COUNT(*) as count
FROM tenant_default.\"Employees\"
WHERE \"NationalIdCard\" IS NOT NULL
  AND \"IsDeleted\" = false
GROUP BY \"NationalIdCard\"
HAVING COUNT(*) > 1;

-- 2. Manual resolution required:
--    - Review each duplicate case
--    - Determine which record is correct
--    - Mark incorrect records as deleted OR update the duplicate field

-- Example: Mark oldest duplicate as deleted (REVIEW CAREFULLY)
-- DO NOT run this blindly - review each case individually
/*
UPDATE tenant_default.\"Employees\"
SET \"IsDeleted\" = true,
    \"DeletedAt\" = NOW(),
    \"DeletedBy\" = 'migration_cleanup'
WHERE \"Id\" IN (
    SELECT \"Id\"
    FROM (
        SELECT \"Id\",
               ROW_NUMBER() OVER (
                   PARTITION BY \"NationalIdCard\"
                   ORDER BY \"CreatedAt\" ASC
               ) as rn
        FROM tenant_default.\"Employees\"
        WHERE \"NationalIdCard\" IS NOT NULL
          AND \"IsDeleted\" = false
    ) sub
    WHERE rn > 1
);
*/

-- ==========================================
-- INVALID DATA CLEANUP
-- ==========================================

-- 3. Fix negative salaries (set to 0)
UPDATE tenant_default.\"Employees\"
SET \"BasicSalary\" = 0
WHERE \"BasicSalary\" < 0;

-- 4. Fix negative leave balances (set to 0)
UPDATE tenant_default.\"Employees\"
SET \"AnnualLeaveBalance\" = GREATEST(\"AnnualLeaveBalance\", 0),
    \"SickLeaveBalance\" = GREATEST(\"SickLeaveBalance\", 0),
    \"CasualLeaveBalance\" = GREATEST(\"CasualLeaveBalance\", 0)
WHERE \"AnnualLeaveBalance\" < 0
   OR \"SickLeaveBalance\" < 0
   OR \"CasualLeaveBalance\" < 0;

-- 5. Fix invalid months in payroll cycles
UPDATE tenant_default.\"PayrollCycles\"
SET \"Month\" = 1
WHERE \"Month\" < 1 OR \"Month\" > 12;

-- 6. Verify cleanup successful
SELECT 'Negative salaries' as issue, COUNT(*) as count
FROM tenant_default.\"Employees\"
WHERE \"BasicSalary\" < 0
UNION ALL
SELECT 'Negative leave balances', COUNT(*)
FROM tenant_default.\"Employees\"
WHERE \"AnnualLeaveBalance\" < 0 OR \"SickLeaveBalance\" < 0
UNION ALL
SELECT 'Invalid months', COUNT(*)
FROM tenant_default.\"PayrollCycles\"
WHERE \"Month\" < 1 OR \"Month\" > 12;

-- Expected: All counts should be 0
```

**After Cleanup**: Re-run data quality validation (Step 1.4)

---

### Phase 2: Migration Deployment (30-60 minutes)

#### Step 2.1: Apply Migration #1 - Unique Constraints

```bash
echo "=========================================="
echo "Applying Migration #1: AddNationalIdUniqueConstraint"
echo "=========================================="
echo "Start time: $(date)"

# Apply migration
cd /workspaces/HRAPP
dotnet ef database update 20251112_AddNationalIdUniqueConstraint \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --connection "<CONNECTION_STRING>"

# Check exit code
if [ $? -eq 0 ]; then
    echo "✓ Migration #1 applied successfully"
else
    echo "✗ Migration #1 FAILED"
    echo "Initiating rollback..."
    exit 1
fi

# Verify indexes created
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'tenant_default'
  AND tablename = 'Employees'
  AND indexname LIKE '%_Unique'
ORDER BY indexname;
"

# Expected: 6 unique indexes listed

echo "End time: $(date)"
echo ""
```

**Expected Duration**: 5-10 seconds
**Expected Locks**: ShareLock (reads allowed)
**Success Criteria**: 6 unique indexes created
**If Failed**: Likely duplicate data still exists - review Step 1.5

#### Step 2.2: Verify Migration #1

```bash
./scripts/verify-migrations.sh migration-1

# Test unique constraint enforcement
psql -h <DB_HOST> -U postgres -d hrms_db -c "
-- This should succeed (unique value)
INSERT INTO tenant_default.\"Employees\"
(\"Id\", \"FirstName\", \"LastName\", \"NationalIdCard\", \"BasicSalary\", \"IsActive\", \"IsDeleted\")
VALUES
(gen_random_uuid(), 'Test', 'User', 'TEST-UNIQUE-12345', 0, false, false);

-- This should fail (duplicate value)
INSERT INTO tenant_default.\"Employees\"
(\"Id\", \"FirstName\", \"LastName\", \"NationalIdCard\", \"BasicSalary\", \"IsActive\", \"IsDeleted\")
VALUES
(gen_random_uuid(), 'Test2', 'User2', 'TEST-UNIQUE-12345', 0, false, false);
"

# Expected: Second insert should fail with "duplicate key value violates unique constraint"

# Cleanup test data
psql -h <DB_HOST> -U postgres -d hrms_db -c "
DELETE FROM tenant_default.\"Employees\"
WHERE \"NationalIdCard\" = 'TEST-UNIQUE-12345';
"
```

**Expected Output**: Second insert fails with unique constraint error
**If Failed**: Rollback and investigate

#### Step 2.3: Apply Migration #2 - Composite Indexes

```bash
echo "=========================================="
echo "Applying Migration #2: AddMissingCompositeIndexes"
echo "=========================================="
echo "Start time: $(date)"

# Apply migration
dotnet ef database update 20251112_AddMissingCompositeIndexes \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --connection "<CONNECTION_STRING>"

# Check exit code
if [ $? -eq 0 ]; then
    echo "✓ Migration #2 applied successfully"
else
    echo "✗ Migration #2 FAILED"
    echo "Initiating rollback..."
    exit 1
fi

# Verify indexes created
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'tenant_default'
  AND indexname LIKE 'IX_%'
  AND indexname NOT LIKE '%_Unique'
ORDER BY tablename, indexname;
"

# Expected: 13 composite indexes listed

echo "End time: $(date)"
echo ""
```

**Expected Duration**: 30-60 seconds (depends on data volume)
**Expected Locks**: ShareLock (reads allowed)
**Success Criteria**: 13 composite indexes created
**If Failed**: Check disk space, database connectivity

#### Step 2.4: Verify Migration #2 - Performance Check

```bash
./scripts/verify-migrations.sh migration-2

# Test query performance improvements
psql -h <DB_HOST> -U postgres -d hrms_db -c "
-- Enable query timing
\timing on

-- Test 1: Payroll cycle lookup (should use IX_PayrollCycles_Year_Month)
EXPLAIN ANALYZE
SELECT * FROM tenant_default.\"PayrollCycles\"
WHERE \"Year\" = 2025 AND \"Month\" = 11
  AND \"IsDeleted\" = false;

-- Test 2: Attendance query (should use IX_Attendances_EmployeeId_Date_Status)
EXPLAIN ANALYZE
SELECT * FROM tenant_default.\"Attendances\"
WHERE \"EmployeeId\" = (SELECT \"Id\" FROM tenant_default.\"Employees\" LIMIT 1)
  AND \"Date\" >= '2025-11-01'
  AND \"Date\" <= '2025-11-30'
  AND \"IsDeleted\" = false;

-- Test 3: Leave balance lookup (should use IX_LeaveBalances_EmployeeId_Year_LeaveTypeId)
EXPLAIN ANALYZE
SELECT * FROM tenant_default.\"LeaveBalances\"
WHERE \"EmployeeId\" = (SELECT \"Id\" FROM tenant_default.\"Employees\" LIMIT 1)
  AND \"Year\" = 2025
  AND \"IsDeleted\" = false;
"

# Review EXPLAIN ANALYZE output
# Look for "Index Scan using IX_..." (indicates index is being used)
# Execution time should be < 10ms for small datasets
```

**Expected Output**: All queries should show "Index Scan using IX_..."
**Performance**: Query times should be significantly reduced
**If Failed**: Indexes may not be utilized - review query patterns

#### Step 2.5: Apply Migration #3 - Validation Constraints

```bash
echo "=========================================="
echo "Applying Migration #3: AddDataValidationConstraints"
echo "=========================================="
echo "Start time: $(date)"

# Apply migration
dotnet ef database update 20251112_AddDataValidationConstraints \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --connection "<CONNECTION_STRING>"

# Check exit code
if [ $? -eq 0 ]; then
    echo "✓ Migration #3 applied successfully"
else
    echo "✗ Migration #3 FAILED"
    echo "This likely means invalid data exists in the database"
    echo "Review error message and run data cleanup (Step 1.5)"
    echo "Initiating rollback..."
    exit 1
fi

# Verify constraints created
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    conname as constraint_name,
    conrelid::regclass as table_name,
    pg_get_constraintdef(oid) as constraint_definition
FROM pg_constraint
WHERE connamespace = 'tenant_default'::regnamespace
  AND contype = 'c'
  AND conname LIKE 'chk_%'
ORDER BY table_name, constraint_name;
"

# Expected: 30+ CHECK constraints listed

echo "End time: $(date)"
echo ""
```

**Expected Duration**: 10-15 seconds
**Expected Locks**: AccessExclusiveLock (writes blocked temporarily)
**Success Criteria**: 30+ CHECK constraints created
**If Failed**: Invalid data exists - must clean before proceeding

#### Step 2.6: Verify Migration #3 - Constraint Testing

```bash
./scripts/verify-migrations.sh migration-3

# Test constraint enforcement
psql -h <DB_HOST> -U postgres -d hrms_db -c "
-- Test 1: Try to insert negative salary (should fail)
DO $$
BEGIN
    INSERT INTO tenant_default.\"Employees\"
    (\"Id\", \"FirstName\", \"LastName\", \"BasicSalary\", \"IsActive\", \"IsDeleted\")
    VALUES
    (gen_random_uuid(), 'Test', 'User', -1000, false, false);

    RAISE EXCEPTION 'Constraint check failed: negative salary was allowed!';
EXCEPTION
    WHEN check_violation THEN
        RAISE NOTICE 'SUCCESS: Negative salary correctly rejected';
END $$;

-- Test 2: Try to insert invalid month (should fail)
DO $$
BEGIN
    INSERT INTO tenant_default.\"PayrollCycles\"
    (\"Id\", \"Year\", \"Month\", \"Status\", \"TotalGrossSalary\", \"TotalDeductions\",
     \"TotalNetSalary\", \"IsDeleted\")
    VALUES
    (gen_random_uuid(), 2025, 13, 'Draft', 0, 0, 0, false);

    RAISE EXCEPTION 'Constraint check failed: invalid month was allowed!';
EXCEPTION
    WHEN check_violation THEN
        RAISE NOTICE 'SUCCESS: Invalid month correctly rejected';
END $$;

-- Test 3: Try to insert negative leave balance (should fail)
DO $$
BEGIN
    UPDATE tenant_default.\"Employees\"
    SET \"AnnualLeaveBalance\" = -10
    WHERE \"Id\" = (SELECT \"Id\" FROM tenant_default.\"Employees\" LIMIT 1);

    RAISE EXCEPTION 'Constraint check failed: negative leave balance was allowed!';
EXCEPTION
    WHEN check_violation THEN
        RAISE NOTICE 'SUCCESS: Negative leave balance correctly rejected';
END $$;
"
```

**Expected Output**: All tests should raise "SUCCESS" notices
**If Failed**: Constraints not working properly - investigate

#### Step 2.7: Apply Migration #4 - Column Level Encryption

```bash
echo "=========================================="
echo "Applying Migration #4: AddColumnLevelEncryption"
echo "=========================================="
echo "Start time: $(date)"

# Apply migration
dotnet ef database update 20251112031109_AddColumnLevelEncryption \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --connection "<CONNECTION_STRING>"

# Check exit code
if [ $? -eq 0 ]; then
    echo "✓ Migration #4 applied successfully"
    echo "Note: This is a SCHEMA-ONLY migration"
    echo "Encryption service will remain in passthrough mode until key is configured"
else
    echo "✗ Migration #4 FAILED"
    echo "Initiating rollback..."
    exit 1
fi

# Verify migration applied
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT version, applied_on
FROM tenant_default.__EFMigrationsHistory
ORDER BY applied_on DESC
LIMIT 5;
"

# Expected: 20251112031109_AddColumnLevelEncryption should be latest

echo "End time: $(date)"
echo ""
```

**Expected Duration**: 5-10 seconds
**Expected Locks**: ShareLock + AccessExclusiveLock (temporary)
**Success Criteria**: Migration recorded in __EFMigrationsHistory
**Note**: This migration consolidates previous migrations

#### Step 2.8: Verify Migration #4 - Encryption Service

```bash
./scripts/verify-migrations.sh migration-4

# Check encryption service status (should be in passthrough mode)
curl http://localhost:5000/api/health/encryption

# Expected response:
# {
#   "encryptionEnabled": false,
#   "mode": "passthrough",
#   "keyVersion": "v1",
#   "message": "Encryption service in passthrough mode - data not encrypted"
# }

# Verify existing data still accessible
curl -H "Authorization: Bearer <TOKEN>" \
  http://localhost:5000/api/employees?pageSize=10

# Expected: Employees data returned successfully (plaintext)
```

**Expected Output**: Encryption in passthrough mode, data accessible
**If Failed**: Application connectivity issues - check logs

---

### Phase 3: Post-Deployment (30-45 minutes)

#### Step 3.1: Comprehensive Verification

```bash
echo "=========================================="
echo "Phase 3: Post-Deployment Verification"
echo "=========================================="

# Run all verification checks
./scripts/verify-migrations.sh post-deployment

# This script will:
# 1. Verify all migrations applied
# 2. Check all indexes created
# 3. Check all constraints active
# 4. Test database connectivity
# 5. Test application functionality
# 6. Check query performance
# 7. Verify audit logs
```

**Expected Output**: All checks pass
**Duration**: 5-10 minutes
**If Failed**: Review specific failure messages

#### Step 3.2: Application Health Check

```bash
# Check application health endpoints
echo "Checking application health..."

# Overall health
curl -f http://localhost:5000/health || echo "Health check failed"

# Database health
curl -f http://localhost:5000/health/database || echo "Database health check failed"

# Encryption service health
curl -f http://localhost:5000/health/encryption || echo "Encryption health check failed"

# Check application logs for errors
docker logs hrms-api --tail 100 | grep -i error

# Or if not using Docker:
tail -n 100 /var/log/hrms/hrms-api.log | grep -i error
```

**Expected Output**: All health checks return 200 OK
**If Errors Found**: Review logs and investigate

#### Step 3.3: Smoke Testing

```bash
echo "Running smoke tests..."

# Test 1: Create employee (should succeed)
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Smoke",
    "lastName": "Test",
    "email": "smoketest@example.com",
    "nationalIdCard": "SMOKE-TEST-001",
    "basicSalary": 50000
  }'

# Test 2: Try to create duplicate (should fail)
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Smoke",
    "lastName": "Test2",
    "email": "smoketest2@example.com",
    "nationalIdCard": "SMOKE-TEST-001",
    "basicSalary": 50000
  }'

# Expected: Should return 400 Bad Request with duplicate error

# Test 3: Try to create with negative salary (should fail)
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Invalid",
    "lastName": "Test",
    "email": "invalidtest@example.com",
    "nationalIdCard": "INVALID-001",
    "basicSalary": -1000
  }'

# Expected: Should return 400 Bad Request with validation error

# Test 4: Query employees (test performance)
time curl -H "Authorization: Bearer <TOKEN>" \
  'http://localhost:5000/api/employees?pageSize=100&orderBy=firstName'

# Expected: Response time < 500ms

# Test 5: Query attendance (test indexes)
time curl -H "Authorization: Bearer <TOKEN>" \
  'http://localhost:5000/api/attendance?startDate=2025-11-01&endDate=2025-11-30'

# Expected: Response time < 1000ms

# Cleanup test data
curl -X DELETE http://localhost:5000/api/employees/<SMOKE_TEST_ID> \
  -H "Authorization: Bearer <TOKEN>"
```

**Expected Results**:
- Test 1: Success (201 Created)
- Test 2: Failure (400 Bad Request - duplicate)
- Test 3: Failure (400 Bad Request - validation)
- Test 4: Success with fast response
- Test 5: Success with improved performance

#### Step 3.4: Performance Monitoring

```bash
# Monitor query performance for 15 minutes
echo "Monitoring database performance..."

# Check slow queries
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
WHERE query LIKE '%tenant_default%'
  AND mean_exec_time > 100
ORDER BY mean_exec_time DESC
LIMIT 20;
"

# Check index usage
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
  AND indexname LIKE 'IX_%'
ORDER BY idx_scan DESC
LIMIT 20;
"

# Expected: New indexes should show usage (idx_scan > 0)

# Check cache hit ratio
psql -h <DB_HOST> -U postgres -d hrms_db -c "
SELECT
    'cache hit ratio' as metric,
    ROUND(100.0 * SUM(blks_hit) / NULLIF(SUM(blks_hit + blks_read), 0), 2) as percentage
FROM pg_stat_database
WHERE datname = 'hrms_db';
"

# Expected: Cache hit ratio > 95%
```

**Monitoring Duration**: 15-30 minutes
**Red Flags**:
- Slow queries > 1000ms
- Index not being used (idx_scan = 0)
- Cache hit ratio < 90%
- High number of sequential scans

#### Step 3.5: Disable Maintenance Mode

```bash
# Re-enable application (take out of maintenance mode)
echo "Disabling maintenance mode..."

# Option 1: Using load balancer health check
curl -X POST http://localhost:5000/admin/maintenance/disable

# Option 2: Using feature flag
# Update appsettings.json or environment variable:
# "MaintenanceMode": { "Enabled": false }

# Verify application accessible
curl http://localhost:5000/health
# Expected: Returns 200 OK

echo "Application re-enabled successfully!"
```

#### Step 3.6: Update Documentation

```bash
# Record deployment completion
cat >> /var/log/hrms/deployments.log << EOF
========================================
Deployment: Database Migrations
Timestamp: $(date)
Operator: ${USER}
Migrations Applied:
  1. 20251112_AddNationalIdUniqueConstraint
  2. 20251112_AddMissingCompositeIndexes
  3. 20251112_AddDataValidationConstraints
  4. 20251112031109_AddColumnLevelEncryption
Status: SUCCESS
Duration: ${DEPLOYMENT_DURATION} minutes
Backup Location: /backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump
Issues: None
========================================
EOF

# Send completion notification
echo "Migration deployment completed successfully!" | \
  mail -s "HRMS Migration Deployment Complete" operations@morishr.com
```

---

## Rollback Procedures

### When to Rollback

**IMMEDIATE ROLLBACK Required If**:
- Migration fails with error
- Data corruption detected
- Application critical functionality broken
- Performance degradation > 50%
- Unique constraint violations in production

**Consider Rollback If**:
- Unexpected behavior in application
- Slow query performance (> 2x normal)
- Increased error rate in logs
- Stakeholder escalation

**Do NOT Rollback If**:
- Minor UI issues (not migration-related)
- Expected temporary slowdown during index build
- Non-critical feature issues

### Rollback Decision Tree

```
Is the application functional?
  ├─ NO → IMMEDIATE ROLLBACK
  └─ YES → Can data be accessed?
            ├─ NO → IMMEDIATE ROLLBACK
            └─ YES → Is performance acceptable?
                      ├─ NO → Investigate (may rollback)
                      └─ YES → Monitor and continue
```

### Rollback Procedure

```bash
# ==========================================
# EMERGENCY ROLLBACK PROCEDURE
# ==========================================

echo "=========================================="
echo "INITIATING EMERGENCY ROLLBACK"
echo "=========================================="
echo "Rollback started at: $(date)"

# Step 1: Enable maintenance mode
curl -X POST http://localhost:5000/admin/maintenance/enable

# Step 2: Execute automated rollback
cd /workspaces/HRAPP
./scripts/rollback-migrations.sh

# The rollback script will:
# - Revert all 4 migrations in reverse order
# - Verify each rollback step
# - Restore from backup if needed

# Step 3: Verify rollback successful
./scripts/verify-migrations.sh rollback-verification

# Step 4: Test application
curl http://localhost:5000/health
curl -H "Authorization: Bearer <TOKEN>" \
  http://localhost:5000/api/employees?pageSize=10

# Step 5: If rollback failed, restore from backup
if [ $? -ne 0 ]; then
    echo "Rollback failed - restoring from backup"

    # Stop application
    systemctl stop hrms-api

    # Restore database
    pg_restore -h <DB_HOST> -U postgres -d hrms_db -c \
      "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

    # Restart application
    systemctl start hrms-api

    # Verify restoration
    curl http://localhost:5000/health
fi

# Step 6: Disable maintenance mode
curl -X POST http://localhost:5000/admin/maintenance/disable

echo "Rollback completed at: $(date)"
echo "=========================================="
```

**Rollback Duration**: 15-30 minutes
**Data Loss**: Minimal (only data inserted during migration window)

### Manual Rollback (If Automated Fails)

```bash
# Revert migrations manually in reverse order

# Revert Migration #4
dotnet ef database update 20251112_AddDataValidationConstraints \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext

# Revert Migration #3
dotnet ef database update 20251112_AddMissingCompositeIndexes \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext

# Revert Migration #2
dotnet ef database update 20251112_AddNationalIdUniqueConstraint \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext

# Revert Migration #1 (back to previous stable state)
dotnet ef database update <PREVIOUS_MIGRATION_NAME> \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext
```

### Post-Rollback Actions

1. **Root Cause Analysis**: Investigate why migration failed
2. **Data Validation**: Verify data integrity after rollback
3. **Stakeholder Communication**: Notify all parties of rollback
4. **Incident Report**: Document failure and resolution
5. **Fix and Retry**: Address issues and schedule new deployment

---

## Post-Deployment Verification

### Verification Checklist

- [ ] All 4 migrations recorded in __EFMigrationsHistory
- [ ] All 6 unique indexes created on Employees table
- [ ] All 13 composite indexes created across tables
- [ ] All 30+ CHECK constraints active
- [ ] Encryption service operational (passthrough mode)
- [ ] Application health checks passing
- [ ] No error spikes in logs
- [ ] Query performance improved (payroll, attendance, leaves)
- [ ] Database backup completed and verified
- [ ] Monitoring dashboards updated
- [ ] Documentation updated
- [ ] Stakeholders notified of completion

### Monitoring Metrics (First 24 Hours)

**Application Metrics**:
- Request rate (should be normal)
- Error rate (should be < 0.1%)
- Response time (should improve or stay same)
- CPU usage (may spike during index usage, then normalize)
- Memory usage (should be stable)

**Database Metrics**:
- Query execution time (should improve for indexed queries)
- Index scans vs sequential scans (index scans should increase)
- Lock waits (should be minimal)
- Cache hit ratio (should be > 95%)
- Connection pool usage (should be normal)

**Business Metrics**:
- User login success rate
- Employee record access
- Attendance report generation time
- Payroll calculation time
- Leave application processing

### Long-Term Monitoring (First Week)

- Query performance trends
- Index effectiveness (usage statistics)
- Constraint violations logged
- Application error patterns
- User-reported issues

---

## Troubleshooting Guide

### Issue 1: Migration Fails with "Duplicate Key Violation"

**Symptoms**: Migration #1 fails during unique index creation

**Cause**: Duplicate data exists in database

**Resolution**:
1. Run data quality checks (Step 1.4)
2. Identify duplicate records
3. Review duplicates with business stakeholders
4. Clean duplicates (Step 1.5)
5. Retry migration

**SQL to Find Duplicates**:
```sql
SELECT
    \"NationalIdCard\",
    STRING_AGG(\"Id\"::text || ' (' || \"FirstName\" || ' ' || \"LastName\" || ')', ', ') as employees,
    COUNT(*) as count
FROM tenant_default.\"Employees\"
WHERE \"NationalIdCard\" IS NOT NULL
  AND \"IsDeleted\" = false
GROUP BY \"NationalIdCard\"
HAVING COUNT(*) > 1
ORDER BY count DESC;
```

---

### Issue 2: Migration Fails with "Check Constraint Violated"

**Symptoms**: Migration #3 fails during constraint addition

**Cause**: Invalid data exists in database (negative values, invalid dates)

**Resolution**:
1. Check error message for specific constraint name
2. Run data validation for that constraint
3. Clean invalid data
4. Retry migration

**Common Invalid Data Queries**:
```sql
-- Negative salaries
SELECT \"Id\", \"FirstName\", \"LastName\", \"BasicSalary\"
FROM tenant_default.\"Employees\"
WHERE \"BasicSalary\" < 0;

-- Negative leave balances
SELECT \"Id\", \"FirstName\", \"LastName\",
       \"AnnualLeaveBalance\", \"SickLeaveBalance\", \"CasualLeaveBalance\"
FROM tenant_default.\"Employees\"
WHERE \"AnnualLeaveBalance\" < 0
   OR \"SickLeaveBalance\" < 0
   OR \"CasualLeaveBalance\" < 0;

-- Invalid months
SELECT \"Id\", \"Year\", \"Month\", \"Status\"
FROM tenant_default.\"PayrollCycles\"
WHERE \"Month\" < 1 OR \"Month\" > 12;
```

---

### Issue 3: Index Creation Timeout

**Symptoms**: Migration hangs during index creation, eventually times out

**Cause**: Large table, high write activity, insufficient resources

**Resolution**:
1. Increase command timeout in connection string
2. Schedule during off-peak hours
3. Ensure sufficient disk space
4. Check for long-running transactions blocking index creation

**Check for Blocking Transactions**:
```sql
SELECT
    pid,
    usename,
    application_name,
    state,
    query_start,
    state_change,
    wait_event_type,
    wait_event,
    query
FROM pg_stat_activity
WHERE state != 'idle'
  AND pid != pg_backend_pid()
ORDER BY query_start;
```

**Increase Timeout**:
```bash
# In connection string, add:
# CommandTimeout=300  (5 minutes)

# Or in migration command:
dotnet ef database update --timeout 300
```

---

### Issue 4: Application Can't Connect After Migration

**Symptoms**: Application returns 500 errors, can't connect to database

**Cause**: Connection string issue, migration state mismatch

**Resolution**:
1. Check database is running: `pg_isready -h <DB_HOST>`
2. Test connection manually: `psql -h <DB_HOST> -U postgres -d hrms_db`
3. Check connection pool: May be exhausted
4. Restart application: `systemctl restart hrms-api`
5. Check migration status matches application

---

### Issue 5: Slow Query Performance After Migration

**Symptoms**: Queries slower than before migration

**Cause**:
- Indexes not being used (query not matching index)
- Statistics outdated
- Insufficient resources during index usage

**Resolution**:
1. Update statistics: `ANALYZE tenant_default.\"Employees\";`
2. Check if indexes are being used:
```sql
EXPLAIN ANALYZE
SELECT * FROM tenant_default.\"Employees\"
WHERE \"NationalIdCard\" = 'TEST-123'
  AND \"IsDeleted\" = false;
```
3. Look for "Index Scan using IX_..." in output
4. If "Sequential Scan" appears, index not being used
5. May need to adjust query to match index

---

### Issue 6: Encryption Service Not Working

**Symptoms**: Encryption shows "disabled" even after migration

**Cause**: Encryption key not configured (expected for initial deployment)

**Resolution**: This is expected behavior for zero-downtime deployment

**Explanation**:
- Migration #4 only updates schema
- Encryption service runs in "passthrough mode" initially
- Existing plaintext data remains accessible
- New data is also stored as plaintext until key configured
- This prevents breaking existing application

**To Enable Encryption** (separate operation, after migration):
1. Generate encryption key: `openssl rand -base64 32`
2. Store in Google Secret Manager:
```bash
echo "<KEY>" | gcloud secrets create ENCRYPTION_KEY_V1 --data-file=-
```
3. Update appsettings.json: `"Encryption": { "Enabled": true }`
4. Restart application
5. Verify encryption enabled: `curl http://localhost:5000/api/health/encryption`
6. New/updated data will be encrypted
7. Run progressive encryption job for existing data (separate process)

---

### Issue 7: Rollback Fails

**Symptoms**: Rollback script errors out

**Cause**:
- Data created during migration window
- Constraint prevents rollback
- Backup corrupted

**Resolution**:
1. Check rollback error message
2. If constraint issue: Temporarily disable constraint
3. Complete rollback
4. If rollback still fails: Restore from backup

**Emergency Backup Restore**:
```bash
# Stop application
systemctl stop hrms-api

# Drop and recreate database
psql -h <DB_HOST> -U postgres -c "DROP DATABASE hrms_db;"
psql -h <DB_HOST> -U postgres -c "CREATE DATABASE hrms_db;"

# Restore from backup
pg_restore -h <DB_HOST> -U postgres -d hrms_db \
  "/backup/database/hrms_db_pre_migration_${DEPLOYMENT_TIMESTAMP}.dump"

# Restart application
systemctl start hrms-api

# Verify
curl http://localhost:5000/health
```

---

## Emergency Contacts

### Primary Contacts

**Database Administrator**:
- Name: [DBA Name]
- Phone: [Phone Number]
- Email: dba@morishr.com
- Role: Database migration execution, rollback

**DevOps Lead**:
- Name: [DevOps Name]
- Phone: [Phone Number]
- Email: devops@morishr.com
- Role: Infrastructure, monitoring, deployment

**Application Architect**:
- Name: [Architect Name]
- Phone: [Phone Number]
- Email: architect@morishr.com
- Role: Application compatibility, code issues

### Escalation Path

1. **Level 1** (0-15 min): DevOps engineer attempts resolution
2. **Level 2** (15-30 min): Escalate to DBA and DevOps lead
3. **Level 3** (30-60 min): Escalate to Application Architect
4. **Level 4** (60+ min): Initiate rollback, notify CTO

### Support Resources

- **Documentation**: /docs/database-migrations/
- **Runbooks**: /runbooks/emergency-procedures/
- **Monitoring Dashboard**: https://monitoring.morishr.com
- **Incident Management**: https://jira.morishr.com
- **Communication Channel**: #ops-incidents (Slack)

---

## Deployment Timeline Estimate

| Phase | Activity | Duration | Notes |
|-------|----------|----------|-------|
| Pre-Deployment | Environment check | 5 min | Automated |
| Pre-Deployment | Data quality validation | 10 min | May take longer if issues found |
| Pre-Deployment | Full backup | 15 min | Depends on DB size |
| Pre-Deployment | Data cleanup (if needed) | 30 min | Only if duplicates/invalid data |
| Deployment | Migration #1 (Unique Constraints) | 10 sec | Fast if no duplicates |
| Deployment | Verify Migration #1 | 2 min | Manual testing |
| Deployment | Migration #2 (Composite Indexes) | 60 sec | Depends on table sizes |
| Deployment | Verify Migration #2 | 5 min | Performance testing |
| Deployment | Migration #3 (Validation Constraints) | 15 sec | Fast if no invalid data |
| Deployment | Verify Migration #3 | 2 min | Constraint testing |
| Deployment | Migration #4 (Encryption Schema) | 10 sec | Schema only |
| Deployment | Verify Migration #4 | 2 min | Service check |
| Post-Deployment | Comprehensive verification | 10 min | Automated + manual |
| Post-Deployment | Smoke testing | 10 min | Critical path testing |
| Post-Deployment | Performance monitoring | 15 min | Initial observation |
| Post-Deployment | Documentation | 5 min | Recording results |

**Best Case** (no issues): 90 minutes
**Expected Case** (minor cleanup): 2-3 hours
**Worst Case** (major cleanup needed): 4-5 hours

**Recommended Deployment Window**: 4 hours (with buffer)

---

## Success Criteria

### Technical Success

- [ ] All 4 migrations applied successfully
- [ ] All indexes created and being utilized
- [ ] All constraints active and enforcing rules
- [ ] No data loss or corruption
- [ ] Application functioning normally
- [ ] Query performance improved
- [ ] Backup completed and verified
- [ ] Rollback plan tested (in staging)

### Business Success

- [ ] No user-facing downtime (or minimal, within SLA)
- [ ] All critical business operations functional
- [ ] No increase in error rates
- [ ] Improved system performance
- [ ] Audit trail complete
- [ ] Stakeholders informed

### Documentation Success

- [ ] Deployment runbook followed
- [ ] All steps documented
- [ ] Issues and resolutions recorded
- [ ] Monitoring baselines updated
- [ ] Knowledge base articles created
- [ ] Lessons learned documented

---

## Post-Deployment Report Template

```markdown
# Database Migration Deployment Report

**Deployment Date**: YYYY-MM-DD
**Deployment Window**: HH:MM - HH:MM (Duration: X hours Y minutes)
**Operator**: [Name]

## Executive Summary
[Brief summary of deployment - success/failure, any issues]

## Migrations Applied
1. 20251112_AddNationalIdUniqueConstraint - SUCCESS
2. 20251112_AddMissingCompositeIndexes - SUCCESS
3. 20251112_AddDataValidationConstraints - SUCCESS
4. 20251112031109_AddColumnLevelEncryption - SUCCESS

## Pre-Deployment Findings
- Data quality issues found: [Yes/No]
- Duplicates cleaned: [Count]
- Invalid data fixed: [Count]
- Backup size: [Size]
- Backup duration: [Duration]

## Deployment Timeline
| Time | Activity | Status | Duration | Notes |
|------|----------|--------|----------|-------|
| [Time] | Pre-deployment checks | SUCCESS | [Duration] | |
| [Time] | Migration #1 | SUCCESS | [Duration] | |
| [Time] | Migration #2 | SUCCESS | [Duration] | |
| [Time] | Migration #3 | SUCCESS | [Duration] | |
| [Time] | Migration #4 | SUCCESS | [Duration] | |
| [Time] | Post-deployment verification | SUCCESS | [Duration] | |

## Issues Encountered
[List any issues and resolutions]

## Performance Metrics
### Before Migration
- Average query time: [Time]
- Payroll processing: [Time]
- Attendance reports: [Time]

### After Migration
- Average query time: [Time] ([+/-]X% change)
- Payroll processing: [Time] ([+/-]X% change)
- Attendance reports: [Time] ([+/-]X% change)

## Verification Results
- [X] All migrations applied
- [X] All indexes created
- [X] All constraints active
- [X] Application health checks passing
- [X] Smoke tests passed
- [X] Performance tests passed

## Rollback Plan
- Rollback tested in staging: [Yes/No]
- Rollback duration estimated: [Duration]
- Backup location: [Path]

## Lessons Learned
[What went well, what could be improved]

## Recommendations
[Any recommendations for future migrations]

## Sign-off
- Database Administrator: [Name] [Date]
- DevOps Lead: [Name] [Date]
- Application Architect: [Name] [Date]
```

---

## Appendix

### A. Connection String Examples

**Development**:
```
Host=localhost;Port=5432;Database=hrms_db;Username=postgres;Password=<PASSWORD>;CommandTimeout=300;
```

**Staging**:
```
Host=staging-db.morishr.com;Port=5432;Database=hrms_db;Username=postgres;Password=<PASSWORD>;CommandTimeout=300;SSL Mode=Require;
```

**Production**:
```
Host=/cloudsql/<PROJECT>:<REGION>:<INSTANCE>;Database=hrms_db;Username=postgres;Password=<PASSWORD>;CommandTimeout=300;SSL Mode=Require;
```

### B. Useful SQL Queries

**Check Database Size**:
```sql
SELECT
    pg_size_pretty(pg_database_size('hrms_db')) as database_size,
    (SELECT count(*) FROM tenant_default.\"Employees\") as employee_count,
    (SELECT count(*) FROM tenant_default.\"Attendances\") as attendance_count,
    (SELECT count(*) FROM tenant_default.\"Payslips\") as payslip_count;
```

**Check Index Usage**:
```sql
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
ORDER BY idx_scan DESC;
```

**Check Constraint Violations Logged**:
```sql
SELECT
    log_time,
    message
FROM pg_log
WHERE message LIKE '%constraint%'
  AND log_time > NOW() - INTERVAL '24 hours'
ORDER BY log_time DESC;
```

### C. Monitoring Dashboard Widgets

Recommended widgets for migration monitoring:

1. **Database Performance**:
   - Query execution time (P50, P95, P99)
   - Active connections
   - Lock waits
   - Cache hit ratio

2. **Application Metrics**:
   - Request rate
   - Error rate
   - Response time
   - CPU/Memory usage

3. **Business Metrics**:
   - Employee API calls
   - Attendance processing rate
   - Payroll calculation time
   - User session count

### D. Glossary

- **ShareLock**: Read lock, allows concurrent reads but blocks writes
- **AccessExclusiveLock**: Exclusive lock, blocks all operations
- **Composite Index**: Index on multiple columns
- **Partial Index**: Index with WHERE clause, only indexes subset of rows
- **CHECK Constraint**: Database constraint that validates data
- **Passthrough Mode**: Encryption service passes data through without encryption
- **Progressive Encryption**: Gradual encryption of existing data over time

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-12 | Claude Code | Initial version |

---

**END OF RUNBOOK**
