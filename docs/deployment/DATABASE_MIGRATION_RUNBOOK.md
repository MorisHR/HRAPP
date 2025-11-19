# Database Migration Runbook: Security Enhancements

## Executive Summary

**Migration Name**: AddSecurityEnhancements
**Migration ID**: 20251113040317_AddSecurityEnhancements
**Target Database**: hrms_master (MasterDbContext)
**Complexity Level**: Medium
**Risk Level**: Medium-High
**Estimated Downtime**: 5-15 minutes
**Rollback Difficulty**: Easy (reversible)

### Migration Purpose
This migration enhances the security infrastructure by adding:
- Password reset token management for admin users
- Activation token expiry tracking
- Multi-tenant refresh token support
- Employee refresh token support
- Session timeout configuration per token
- User activity tracking for session management

---

## Pre-Migration Phase

### Prerequisites Checklist

#### Environment Verification
- [ ] **Database Server**: PostgreSQL 12+ running and accessible
- [ ] **Application Servers**: All instances identified and documented
- [ ] **Network**: Database connectivity verified from all app servers
- [ ] **Permissions**: DBA credentials available and tested
- [ ] **Disk Space**: Verify sufficient space for backup and migration
  ```bash
  df -h /var/lib/postgresql
  # Require at least 10GB free space
  ```

#### Backup Requirements
- [ ] **Full Database Backup**: Created and verified
- [ ] **Migration History**: Current state documented
- [ ] **Backup Retention**: Previous 7 days of backups available
- [ ] **Backup Location**: Documented and accessible to recovery team

#### Application State
- [ ] **Code Deployment**: Latest code deployed to all servers
- [ ] **Dependencies**: All NuGet packages restored
- [ ] **Environment Variables**: JWT_SECRET and connection strings configured
- [ ] **Logs**: Application logging enabled and monitored

### Stakeholder Communication

#### Notification Timeline
- **T-72 hours**: Initial migration announcement
  - To: All stakeholders, development team, support team
  - Content: Migration purpose, timing, expected impact

- **T-24 hours**: Migration reminder
  - To: Operations team, support team, management
  - Content: Final migration schedule, contact information

- **T-1 hour**: Maintenance window notification
  - To: All users, support team
  - Content: System will be unavailable, estimated duration

- **T-0**: Migration start notification
  - To: Operations team, on-call engineers
  - Content: Migration in progress, status updates

- **T+complete**: Migration completion notification
  - To: All stakeholders
  - Content: System restored, any issues encountered, next steps

#### Communication Template

```markdown
Subject: [MAINTENANCE] Database Migration - Security Enhancements

Dear Team,

We will be performing a database migration to enhance security features:

**Date**: [INSERT DATE]
**Time**: [INSERT TIME] (Mauritius Time - MUT, UTC+4)
**Duration**: Approximately 15-30 minutes
**Impact**: HRMS application will be unavailable

**What's changing**:
- Enhanced password reset security
- Improved session management
- Multi-tenant token support

**What you need to do**:
- Save all work before [START TIME]
- Log out of HRMS
- Do not attempt to log in during maintenance
- Wait for "System Available" notification

**Support**: For urgent issues, contact support@morishr.com

Thank you for your patience.

Technical Team
```

### Backup Procedures

#### Create Full Database Backup

```bash
#!/bin/bash
# Backup script: backup_before_migration.sh

# Configuration
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="hrms_master"
DB_USER="postgres"
BACKUP_DIR="/var/backups/postgresql"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/hrms_master_backup_${TIMESTAMP}.backup"
LOG_FILE="${BACKUP_DIR}/backup_${TIMESTAMP}.log"

# Create backup directory if it doesn't exist
mkdir -p "${BACKUP_DIR}"

# Log start
echo "[$(date)] Starting database backup..." | tee -a "${LOG_FILE}"

# Create backup using pg_dump custom format (allows selective restore)
pg_dump -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
  -F c -b -v -f "${BACKUP_FILE}" 2>&1 | tee -a "${LOG_FILE}"

# Verify backup was created
if [ -f "${BACKUP_FILE}" ]; then
    BACKUP_SIZE=$(du -h "${BACKUP_FILE}" | cut -f1)
    echo "[$(date)] Backup created successfully: ${BACKUP_FILE} (${BACKUP_SIZE})" | tee -a "${LOG_FILE}"
else
    echo "[$(date)] ERROR: Backup failed!" | tee -a "${LOG_FILE}"
    exit 1
fi

# Verify backup integrity
echo "[$(date)] Verifying backup integrity..." | tee -a "${LOG_FILE}"
pg_restore --list "${BACKUP_FILE}" > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "[$(date)] Backup integrity verified successfully" | tee -a "${LOG_FILE}"
else
    echo "[$(date)] ERROR: Backup integrity check failed!" | tee -a "${LOG_FILE}"
    exit 1
fi

# Store backup location for recovery
echo "${BACKUP_FILE}" > /tmp/latest_backup_location.txt

# Create checksum for verification
md5sum "${BACKUP_FILE}" > "${BACKUP_FILE}.md5"

echo "[$(date)] Backup completed successfully" | tee -a "${LOG_FILE}"
echo "Backup location: ${BACKUP_FILE}"
echo "Backup size: ${BACKUP_SIZE}"
```

#### Verify Backup

```bash
# Verify backup file exists and has content
BACKUP_FILE=$(cat /tmp/latest_backup_location.txt)
ls -lh "${BACKUP_FILE}"

# Verify backup integrity
pg_restore --list "${BACKUP_FILE}" | head -20

# Verify checksum
md5sum -c "${BACKUP_FILE}.md5"

# Count tables in backup
pg_restore --list "${BACKUP_FILE}" | grep "TABLE DATA" | wc -l

# Verify specific tables are in backup
pg_restore --list "${BACKUP_FILE}" | grep -E "(AdminUsers|RefreshTokens)"
```

### Pre-Migration Database State Documentation

```sql
-- Document current migration status
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

-- Save to file for comparison
\copy (SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId") TO '/tmp/migrations_before.csv' CSV HEADER;

-- Document table structures
\d master."AdminUsers"
\d master."RefreshTokens"

-- Count existing records
SELECT
    'AdminUsers' as table_name,
    COUNT(*) as record_count
FROM master."AdminUsers"
UNION ALL
SELECT
    'RefreshTokens',
    COUNT(*)
FROM master."RefreshTokens"
UNION ALL
SELECT
    'Tenants',
    COUNT(*)
FROM master."Tenants";

-- Document current constraints
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    conrelid::regclass AS table_name
FROM pg_constraint
WHERE conrelid IN (
    'master."AdminUsers"'::regclass,
    'master."RefreshTokens"'::regclass
)
ORDER BY table_name, constraint_name;

-- Check for NULL AdminUserId values (important for migration)
SELECT COUNT(*) as null_admin_user_ids
FROM master."RefreshTokens"
WHERE "AdminUserId" IS NULL;
```

---

## Migration Execution Phase

### Step 1: Application Shutdown

```bash
#!/bin/bash
# Shutdown script: stop_application.sh

echo "[$(date)] Stopping HRMS application servers..."

# Method 1: Systemd service (production)
sudo systemctl stop hrms-api
sudo systemctl status hrms-api

# Method 2: Docker containers
docker-compose -f /path/to/docker-compose.yml down

# Method 3: Kubernetes deployment
kubectl scale deployment hrms-api --replicas=0 -n production

# Method 4: Process kill (development/manual)
pkill -f "dotnet.*HRMS.API"

# Verify no connections to database
ACTIVE_CONNECTIONS=$(psql -h localhost -U postgres -d hrms_master -t -c "
SELECT COUNT(*)
FROM pg_stat_activity
WHERE datname = 'hrms_master'
AND state = 'active'
AND usename != 'postgres';
")

echo "Active database connections: ${ACTIVE_CONNECTIONS}"

if [ "${ACTIVE_CONNECTIONS}" -gt 0 ]; then
    echo "WARNING: Active database connections detected!"
    psql -h localhost -U postgres -d hrms_master -c "
    SELECT pid, usename, application_name, state, query_start
    FROM pg_stat_activity
    WHERE datname = 'hrms_master';
    "

    # Terminate active connections (if approved)
    read -p "Terminate active connections? (yes/no): " CONFIRM
    if [ "${CONFIRM}" = "yes" ]; then
        psql -h localhost -U postgres -d hrms_master -c "
        SELECT pg_terminate_backend(pid)
        FROM pg_stat_activity
        WHERE datname = 'hrms_master'
        AND pid <> pg_backend_pid();
        "
    fi
fi

echo "[$(date)] Application shutdown complete"
```

**Expected Output**:
```
[2025-11-13 04:00:00] Stopping HRMS application servers...
● hrms-api.service - HRMS API Service
   Loaded: loaded
   Active: inactive (dead)
Active database connections: 0
[2025-11-13 04:00:05] Application shutdown complete
```

### Step 2: Pre-Migration Verification

```bash
#!/bin/bash
# Pre-migration verification script

cd /workspaces/HRAPP

# Set environment variables
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-here"

# Verify database connectivity
echo "[$(date)] Verifying database connectivity..."
dotnet ef database drop --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext --dry-run

# List current migrations
echo "[$(date)] Listing current migrations..."
dotnet ef migrations list --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext

# Generate migration SQL script for review
echo "[$(date)] Generating migration SQL script..."
dotnet ef migrations script --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext --output /tmp/migration_script.sql

# Display migration SQL for review
echo "[$(date)] Migration SQL script:"
cat /tmp/migration_script.sql

# Verify backup exists
BACKUP_FILE=$(cat /tmp/latest_backup_location.txt)
if [ -f "${BACKUP_FILE}" ]; then
    echo "[$(date)] Backup verified: ${BACKUP_FILE}"
else
    echo "[$(date)] ERROR: Backup not found!"
    exit 1
fi

echo "[$(date)] Pre-migration verification complete"
```

**Expected Output**:
```
[2025-11-13 04:05:00] Verifying database connectivity...
Database connection successful
[2025-11-13 04:05:05] Listing current migrations...
20251031135011_InitialMasterSchema
20251111125329_InitialMasterDb (Applied)
20251113040317_AddSecurityEnhancements (Pending)
[2025-11-13 04:05:10] Generating migration SQL script...
[2025-11-13 04:05:15] Migration SQL script:
START TRANSACTION;
...
[2025-11-13 04:05:20] Backup verified: /var/backups/postgresql/hrms_master_backup_20251113_040000.backup
[2025-11-13 04:05:25] Pre-migration verification complete
```

### Step 3: Execute Migration

```bash
#!/bin/bash
# Migration execution script: execute_migration.sh

cd /workspaces/HRAPP

# Configuration
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-here"
MIGRATION_LOG="/tmp/migration_execution_$(date +%Y%m%d_%H%M%S).log"

# Start migration
echo "[$(date)] Starting database migration..." | tee -a "${MIGRATION_LOG}"

# Execute migration
dotnet ef database update --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext --verbose 2>&1 | tee -a "${MIGRATION_LOG}"

# Check exit code
if [ $? -eq 0 ]; then
    echo "[$(date)] Migration completed successfully" | tee -a "${MIGRATION_LOG}"
else
    echo "[$(date)] ERROR: Migration failed!" | tee -a "${MIGRATION_LOG}"
    echo "Check log file: ${MIGRATION_LOG}"
    exit 1
fi

# Verify migration was applied
echo "[$(date)] Verifying migration status..." | tee -a "${MIGRATION_LOG}"
dotnet ef migrations list --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext | tee -a "${MIGRATION_LOG}"

echo "Migration log: ${MIGRATION_LOG}"
```

**Expected Output**:
```
[2025-11-13 04:10:00] Starting database migration...
Build started...
Build succeeded.
Applying migration '20251113040317_AddSecurityEnhancements'.
Executed DbCommand (25ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
ALTER TABLE master."AdminUsers" ADD "PasswordResetToken" text NULL;
...
Done.
[2025-11-13 04:10:30] Migration completed successfully
[2025-11-13 04:10:35] Verifying migration status...
20251111125329_InitialMasterDb (Applied)
20251113040317_AddSecurityEnhancements (Applied)
```

### Step 4: Post-Migration Verification

```sql
-- Connect to database
psql -h localhost -U postgres -d hrms_master

-- Verify migration was recorded
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251113040317_AddSecurityEnhancements';

-- Expected: One row with migration ID and EF version

-- Verify new columns exist in AdminUsers
\d master."AdminUsers"

-- Expected columns:
-- - PasswordResetToken (text, nullable)
-- - PasswordResetTokenExpiry (timestamp with time zone, nullable)
-- - ActivationTokenExpiry (timestamp with time zone, nullable)

-- Verify new columns exist in RefreshTokens
\d master."RefreshTokens"

-- Expected columns:
-- - TenantId (uuid, nullable)
-- - EmployeeId (uuid, nullable)
-- - SessionTimeoutMinutes (integer, not null, default 0)
-- - LastActivityAt (timestamp with time zone, not null)

-- Verify AdminUserId is now nullable
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'master'
AND table_name = 'RefreshTokens'
AND column_name = 'AdminUserId';

-- Expected: is_nullable = 'YES'

-- Verify data integrity
SELECT COUNT(*) as total_admin_users FROM master."AdminUsers";
SELECT COUNT(*) as total_refresh_tokens FROM master."RefreshTokens";
SELECT COUNT(*) as null_admin_user_ids FROM master."RefreshTokens" WHERE "AdminUserId" IS NULL;

-- All counts should match pre-migration values (except null_admin_user_ids may be > 0 now)

-- Test insert operations with new columns
BEGIN;

INSERT INTO master."AdminUsers" (
    "Id", "Username", "Email", "FirstName", "LastName",
    "PasswordHash", "Role", "IsActive", "CreatedAt",
    "PasswordResetToken", "PasswordResetTokenExpiry", "ActivationTokenExpiry"
) VALUES (
    gen_random_uuid(),
    'test_migration_user',
    'test@migration.local',
    'Test', 'User',
    'temp_hash',
    'SuperAdmin',
    false,
    NOW(),
    'test_token_123',
    NOW() + INTERVAL '1 hour',
    NOW() + INTERVAL '24 hours'
);

-- Verify insert succeeded
SELECT * FROM master."AdminUsers" WHERE "Username" = 'test_migration_user';

-- Cleanup test data
ROLLBACK;

-- Test complete
\echo 'Post-migration verification complete!'
```

**Expected Verification Results**:
- Migration ID exists in `__EFMigrationsHistory`
- All new columns present with correct data types
- `AdminUserId` is nullable in `RefreshTokens`
- Record counts unchanged from pre-migration
- Test insert operations succeed

---

## Post-Migration Phase

### Step 5: Application Restart

```bash
#!/bin/bash
# Application restart script: start_application.sh

echo "[$(date)] Starting HRMS application servers..."

# Method 1: Systemd service (production)
sudo systemctl start hrms-api
sleep 5
sudo systemctl status hrms-api

# Method 2: Docker containers
docker-compose -f /path/to/docker-compose.yml up -d

# Method 3: Kubernetes deployment
kubectl scale deployment hrms-api --replicas=3 -n production

# Method 4: Manual start (development)
cd /workspaces/HRAPP
nohup dotnet run --project src/HRMS.API --environment Production > /tmp/hrms-api.log 2>&1 &

# Wait for application to be ready
echo "Waiting for application to start..."
for i in {1..30}; do
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health)
    if [ "${HTTP_STATUS}" = "200" ]; then
        echo "[$(date)] Application is healthy!"
        break
    fi
    echo "Attempt $i/30: HTTP ${HTTP_STATUS} - Waiting..."
    sleep 2
done

if [ "${HTTP_STATUS}" != "200" ]; then
    echo "[$(date)] ERROR: Application failed to start properly!"
    tail -50 /var/log/hrms/api.log
    exit 1
fi

echo "[$(date)] Application startup complete"
```

**Expected Output**:
```
[2025-11-13 04:15:00] Starting HRMS application servers...
● hrms-api.service - HRMS API Service
   Loaded: loaded
   Active: active (running)
Waiting for application to start...
Attempt 1/30: HTTP 000 - Waiting...
Attempt 3/30: HTTP 503 - Waiting...
Attempt 5/30: HTTP 200 - Application is healthy!
[2025-11-13 04:15:20] Application is healthy!
[2025-11-13 04:15:25] Application startup complete
```

### Step 6: Functional Testing

```bash
#!/bin/bash
# Functional testing script: test_migration.sh

API_URL="http://localhost:5000"
TEST_LOG="/tmp/migration_testing_$(date +%Y%m%d_%H%M%S).log"

echo "[$(date)] Starting functional testing..." | tee -a "${TEST_LOG}"

# Test 1: Health check
echo "Test 1: Health check..." | tee -a "${TEST_LOG}"
HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${API_URL}/health")
if [ "${HEALTH_STATUS}" = "200" ]; then
    echo "✓ Health check passed" | tee -a "${TEST_LOG}"
else
    echo "✗ Health check failed: ${HEALTH_STATUS}" | tee -a "${TEST_LOG}"
fi

# Test 2: Database connectivity check
echo "Test 2: Database connectivity..." | tee -a "${TEST_LOG}"
DB_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "${API_URL}/health/ready")
if [ "${DB_HEALTH}" = "200" ]; then
    echo "✓ Database connectivity passed" | tee -a "${TEST_LOG}"
else
    echo "✗ Database connectivity failed: ${DB_HEALTH}" | tee -a "${TEST_LOG}"
fi

# Test 3: Admin login (replace with actual credentials)
echo "Test 3: Admin login..." | tee -a "${TEST_LOG}"
LOGIN_RESPONSE=$(curl -s -X POST "${API_URL}/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email": "admin@test.com", "password": "Test123!"}')

if echo "${LOGIN_RESPONSE}" | grep -q "token"; then
    echo "✓ Admin login passed" | tee -a "${TEST_LOG}"
    TOKEN=$(echo "${LOGIN_RESPONSE}" | jq -r '.token')
else
    echo "✗ Admin login failed" | tee -a "${TEST_LOG}"
    echo "Response: ${LOGIN_RESPONSE}" | tee -a "${TEST_LOG}"
fi

# Test 4: Verify new migration features accessible
echo "Test 4: Password reset token functionality..." | tee -a "${TEST_LOG}"
# Add test for password reset endpoint

# Test 5: Verify session timeout functionality
echo "Test 5: Session timeout configuration..." | tee -a "${TEST_LOG}"
# Add test for session timeout

echo "[$(date)] Functional testing complete" | tee -a "${TEST_LOG}"
echo "Test log: ${TEST_LOG}"
```

### Step 7: Monitoring and Validation (First 24 Hours)

```bash
# Monitor application logs
tail -f /var/log/hrms/api.log | grep -i -E "(error|exception|failed)"

# Monitor database performance
psql -h localhost -U postgres -d hrms_master -c "
SELECT
    pid,
    now() - pg_stat_activity.query_start AS duration,
    query,
    state
FROM pg_stat_activity
WHERE (now() - pg_stat_activity.query_start) > interval '5 seconds'
ORDER BY duration DESC;
"

# Monitor database connections
watch -n 5 "psql -h localhost -U postgres -d hrms_master -t -c \"
SELECT COUNT(*) FROM pg_stat_activity WHERE datname = 'hrms_master';
\""

# Check for migration-related errors
grep -i "AdminUsers\|RefreshTokens\|PasswordReset\|ActivationToken" /var/log/hrms/api.log | tail -100

# Monitor API response times
curl -w "@curl-format.txt" -o /dev/null -s "${API_URL}/health"

# Create curl-format.txt:
cat > /tmp/curl-format.txt << 'EOF'
    time_namelookup:  %{time_namelookup}\n
       time_connect:  %{time_connect}\n
    time_appconnect:  %{time_appconnect}\n
   time_pretransfer:  %{time_pretransfer}\n
      time_redirect:  %{time_redirect}\n
 time_starttransfer:  %{time_starttransfer}\n
                    ----------\n
         time_total:  %{time_total}\n
EOF
```

### Success Criteria

- [ ] Migration applied successfully (no errors in log)
- [ ] All new columns exist in database
- [ ] Application starts without errors
- [ ] Health check endpoint returns 200 OK
- [ ] Database health check returns 200 OK
- [ ] Admin login functionality works
- [ ] Token refresh functionality works
- [ ] No database errors in application logs
- [ ] API response times within acceptable range (<200ms)
- [ ] No user-reported issues in first hour

---

## Rollback Procedures

### When to Rollback

Rollback if any of the following occur:
1. Migration fails with database errors
2. Application fails to start after migration
3. Critical functionality broken (login, token refresh)
4. Data corruption detected
5. Performance degradation >50%
6. Security vulnerability introduced

### Rollback Execution

See detailed rollback procedures in:
`/workspaces/HRAPP/DATABASE_ROLLBACK_PROCEDURES.md`

**Quick Rollback Command**:
```bash
cd /workspaces/HRAPP
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-here"

# Rollback to previous migration
dotnet ef database update InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

---

## Contact Information

### Primary Contacts (During Migration Window)
- **Migration Lead**: migrations@morishr.com | +230-XXXX-XXXX
- **DBA On-Call**: dba@morishr.com | +230-XXXX-XXXX
- **DevOps Lead**: devops@morishr.com | +230-XXXX-XXXX

### Escalation Path
1. Migration Team Lead (0-15 minutes)
2. Database Administrator (15-30 minutes)
3. CTO (30+ minutes or critical issues)

### Emergency Contacts
- **CTO**: cto@morishr.com | +230-XXXX-XXXX
- **CEO**: ceo@morishr.com | +230-XXXX-XXXX (critical business impact only)

---

## Related Documentation

- **Rollback Procedures**: `/workspaces/HRAPP/DATABASE_ROLLBACK_PROCEDURES.md`
- **Quick Reference**: `/workspaces/HRAPP/MIGRATION_QUICK_REFERENCE.md`
- **Migration File**: `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Master/20251113040317_AddSecurityEnhancements.cs`
- **Rollback SQL**: `/tmp/rollback_script.sql`
- **Migration SQL**: `/tmp/migration_script.sql`

---

**Document Version**: 1.0
**Last Updated**: 2025-11-13 04:03:17 UTC
**Prepared By**: Database Disaster Recovery Specialist
**Reviewed By**: Database Team, DevOps Team, Security Team
**Approved By**: CTO
