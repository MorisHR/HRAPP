# Database Rollback Procedures for Security Enhancements Migration

## Migration Information
- **Migration Name**: `20251113040317_AddSecurityEnhancements`
- **Migration File**: `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Master/20251113040317_AddSecurityEnhancements.cs`
- **Applied Date**: 2025-11-13 04:03:17 UTC
- **Database**: hrms_master
- **Schema**: master
- **Context**: MasterDbContext
- **Rollback Target**: `20251111125329_InitialMasterDb`

## Pre-Rollback Checklist

### Critical Pre-Rollback Steps
- [ ] **STOP all application servers** - Ensure no active connections to database
- [ ] **Verify backup exists and is valid** - Check backup file integrity
- [ ] **Take a new backup before rollback** - Safety net for rollback failures
- [ ] **Notify all users of maintenance** - Set maintenance window (recommend 30-60 minutes)
- [ ] **Document current database state** - Run verification queries below
- [ ] **Verify no active sessions** - Check for active user sessions and refresh tokens
- [ ] **Review rollback SQL script** - `/tmp/rollback_script.sql`
- [ ] **Test rollback in staging environment first** - NEVER rollback production without testing

### Pre-Rollback Verification Queries

```sql
-- Check current migration status
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 5;

-- Verify columns exist before rollback
\d master."AdminUsers"
\d master."RefreshTokens"

-- Check data that will be lost
SELECT COUNT(*) as password_reset_tokens
FROM master."AdminUsers"
WHERE "PasswordResetToken" IS NOT NULL;

SELECT COUNT(*) as activation_tokens_with_expiry
FROM master."AdminUsers"
WHERE "ActivationTokenExpiry" IS NOT NULL;

SELECT COUNT(*) as employee_refresh_tokens
FROM master."RefreshTokens"
WHERE "EmployeeId" IS NOT NULL;

SELECT COUNT(*) as tenant_refresh_tokens
FROM master."RefreshTokens"
WHERE "TenantId" IS NOT NULL;

SELECT COUNT(*) as null_admin_user_tokens
FROM master."RefreshTokens"
WHERE "AdminUserId" IS NULL;

-- Check active sessions
SELECT COUNT(*) as active_sessions
FROM master."RefreshTokens"
WHERE "ExpiresAt" > NOW() AND "RevokedAt" IS NULL;
```

## Backup Procedures

### Create Database Backup Before Rollback

```bash
# Set timestamp for backup file
BACKUP_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="/tmp/hrms_master_backup_${BACKUP_TIMESTAMP}.backup"

# Create backup using pg_dump custom format
pg_dump -h localhost -U postgres -d hrms_master -F c -f "${BACKUP_FILE}"

# Verify backup was created
ls -lh "${BACKUP_FILE}"

# Test backup integrity
pg_restore --list "${BACKUP_FILE}" | head -20

# Store backup location for recovery
echo "Backup created at: ${BACKUP_FILE}" | tee /tmp/backup_location.txt
```

### Backup Verification

```bash
# Verify backup file size (should be several MB)
du -h /tmp/hrms_master_backup_*.backup

# List backup contents
pg_restore --list /tmp/hrms_master_backup_*.backup 2>&1 | head -20

# Check backup includes our tables
pg_restore --list /tmp/hrms_master_backup_*.backup 2>&1 | grep -E "(AdminUsers|RefreshTokens)"
```

## Rollback Execution

### Method 1: Using EF Core Migrations (Recommended)

```bash
# Navigate to project directory
cd /workspaces/HRAPP

# Set required environment variables
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-minimum-32-characters"

# Dry run - Generate rollback script without executing
dotnet ef migrations script AddSecurityEnhancements InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext \
  --output /tmp/rollback_script.sql

# Review the generated SQL script
cat /tmp/rollback_script.sql

# Execute rollback to previous migration
dotnet ef database update InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext

# Verify rollback completed
dotnet ef migrations list \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Method 2: Direct SQL Execution (For Emergency Rollback)

```bash
# Execute rollback script directly via psql
psql -h localhost -U postgres -d hrms_master -f /tmp/rollback_script.sql

# Or execute manually with transaction control
psql -h localhost -U postgres -d hrms_master << 'EOSQL'
BEGIN;

-- Verify we're about to rollback the right migration
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251113040317_AddSecurityEnhancements';

-- Execute rollback
ALTER TABLE master."RefreshTokens" DROP COLUMN "EmployeeId";
ALTER TABLE master."RefreshTokens" DROP COLUMN "LastActivityAt";
ALTER TABLE master."RefreshTokens" DROP COLUMN "SessionTimeoutMinutes";
ALTER TABLE master."RefreshTokens" DROP COLUMN "TenantId";
ALTER TABLE master."AdminUsers" DROP COLUMN "ActivationTokenExpiry";
ALTER TABLE master."AdminUsers" DROP COLUMN "PasswordResetToken";
ALTER TABLE master."AdminUsers" DROP COLUMN "PasswordResetTokenExpiry";

-- Restore AdminUserId constraint
UPDATE master."RefreshTokens" SET "AdminUserId" = '00000000-0000-0000-0000-000000000000'
WHERE "AdminUserId" IS NULL;
ALTER TABLE master."RefreshTokens" ALTER COLUMN "AdminUserId" SET NOT NULL;
ALTER TABLE master."RefreshTokens" ALTER COLUMN "AdminUserId" SET DEFAULT '00000000-0000-0000-0000-000000000000';

-- Remove migration from history
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251113040317_AddSecurityEnhancements';

-- If everything looks good, commit
COMMIT;
-- If problems occur, run: ROLLBACK;
EOSQL
```

## What Gets Rolled Back

### Columns Dropped from AdminUsers Table
1. **PasswordResetToken** (text, nullable)
   - Purpose: Stored password reset tokens for admin users
   - Data Loss: All active password reset requests will be lost

2. **PasswordResetTokenExpiry** (timestamp with time zone, nullable)
   - Purpose: Expiration timestamp for password reset tokens
   - Data Loss: Expiry tracking for password reset tokens

3. **ActivationTokenExpiry** (timestamp with time zone, nullable)
   - Purpose: Expiration timestamp for account activation tokens
   - Data Loss: Expiry tracking for activation tokens

### Columns Dropped from RefreshTokens Table
1. **TenantId** (uuid, nullable)
   - Purpose: Associated tenant for multi-tenant refresh tokens
   - Data Loss: Tenant associations for refresh tokens

2. **EmployeeId** (uuid, nullable)
   - Purpose: Associated employee for employee refresh tokens
   - Data Loss: Employee associations for refresh tokens

3. **SessionTimeoutMinutes** (integer, not null, default 0)
   - Purpose: Custom session timeout per token
   - Data Loss: Custom timeout configurations

4. **LastActivityAt** (timestamp with time zone, not null, default DateTime.MinValue)
   - Purpose: Track last user activity for session management
   - Data Loss: Activity tracking data

### Columns Modified in RefreshTokens Table
1. **AdminUserId** - Changed from NULLABLE to NOT NULL
   - Action: All NULL values set to '00000000-0000-0000-0000-000000000000'
   - Warning: This may create orphaned tokens pointing to non-existent admin users

## Data Loss Warning

### CRITICAL DATA THAT WILL BE PERMANENTLY LOST

- **Active Password Reset Tokens**: Users with in-progress password reset requests will need to restart the process
- **Activation Token Expiry Data**: Account activation time limits will be lost
- **Tenant Refresh Tokens**: All tenant-specific refresh tokens (TenantId associations)
- **Employee Refresh Tokens**: All employee-specific refresh tokens (EmployeeId associations)
- **Session Timeout Settings**: Custom session timeout configurations per token
- **Last Activity Timestamps**: User activity tracking data for session management

### Impact Assessment

```sql
-- Run this query before rollback to assess impact
SELECT
    'Password Reset Tokens' as feature,
    COUNT(*) as records_affected
FROM master."AdminUsers"
WHERE "PasswordResetToken" IS NOT NULL

UNION ALL

SELECT
    'Activation Token Expiry',
    COUNT(*)
FROM master."AdminUsers"
WHERE "ActivationTokenExpiry" IS NOT NULL

UNION ALL

SELECT
    'Employee Refresh Tokens',
    COUNT(*)
FROM master."RefreshTokens"
WHERE "EmployeeId" IS NOT NULL

UNION ALL

SELECT
    'Tenant Refresh Tokens',
    COUNT(*)
FROM master."RefreshTokens"
WHERE "TenantId" IS NOT NULL

UNION ALL

SELECT
    'Session Timeout Configs',
    COUNT(*)
FROM master."RefreshTokens"
WHERE "SessionTimeoutMinutes" > 0

UNION ALL

SELECT
    'NULL AdminUserId (will become empty GUID)',
    COUNT(*)
FROM master."RefreshTokens"
WHERE "AdminUserId" IS NULL;
```

## Post-Rollback Verification

### Database Schema Verification

```sql
-- Verify columns were dropped from AdminUsers
\d master."AdminUsers"

-- Should NOT see: PasswordResetToken, PasswordResetTokenExpiry, ActivationTokenExpiry

-- Verify columns were dropped from RefreshTokens
\d master."RefreshTokens"

-- Should NOT see: TenantId, EmployeeId, SessionTimeoutMinutes, LastActivityAt
-- AdminUserId should be NOT NULL

-- Verify migration history
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC LIMIT 5;

-- Should NOT see: 20251113040317_AddSecurityEnhancements
-- Should see: 20251111125329_InitialMasterDb as latest
```

### Application Verification

```bash
# Test application startup
cd /workspaces/HRAPP
dotnet run --project src/HRMS.API --environment Production

# Check application logs for errors
tail -f src/HRMS.API/Logs/hrms-*.log

# Test health endpoint
curl -X GET http://localhost:5000/health

# Test database connectivity
curl -X GET http://localhost:5000/health/ready
```

### Functional Testing Checklist
- [ ] Application starts without errors
- [ ] Admin user login works
- [ ] Password reset functionality works (without token expiry)
- [ ] Account activation works (without token expiry)
- [ ] Refresh token generation works
- [ ] Session management works
- [ ] No database errors in logs

## Post-Rollback Actions

1. **Verify columns dropped successfully**
   ```sql
   \d master."AdminUsers"
   \d master."RefreshTokens"
   ```

2. **Check RefreshTokens constraints restored**
   ```sql
   SELECT
       conname AS constraint_name,
       contype AS constraint_type,
       conrelid::regclass AS table_name,
       a.attname AS column_name,
       con.convalidated
   FROM pg_constraint con
   JOIN pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = ANY(con.conkey)
   WHERE conrelid = 'master."RefreshTokens"'::regclass
   AND a.attname = 'AdminUserId';
   ```

3. **Restart all application services**
   ```bash
   # Restart API servers
   systemctl restart hrms-api  # Adjust based on your deployment

   # Clear application cache if using Redis
   redis-cli FLUSHDB
   ```

4. **Test critical user workflows**
   - Admin login
   - Password reset request
   - Account activation
   - Token refresh

5. **Monitor application logs** for the first 24 hours
   ```bash
   tail -f /var/log/hrms/api.log
   ```

6. **Notify users system is back online**
   - Send email notification
   - Update status page
   - Inform support team

7. **Document the rollback**
   - Why was rollback needed?
   - What was the impact?
   - What was learned?
   - Update runbooks based on experience

## Recovery from Failed Rollback

### If Rollback Fails Partway Through

```bash
# DO NOT PANIC - Data is not lost if you have a backup

# Step 1: Stop all application connections
# Step 2: Assess the current state
psql -h localhost -U postgres -d hrms_master -c "\d master.\"AdminUsers\""
psql -h localhost -U postgres -d hrms_master -c "\d master.\"RefreshTokens\""
psql -h localhost -U postgres -d hrms_master -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"

# Step 3: Restore from backup
BACKUP_FILE=$(ls -t /tmp/hrms_master_backup_*.backup | head -1)
echo "Restoring from: ${BACKUP_FILE}"

# Drop and recreate database (CAUTION!)
psql -h localhost -U postgres -c "DROP DATABASE hrms_master;"
psql -h localhost -U postgres -c "CREATE DATABASE hrms_master;"

# Restore from backup
pg_restore -h localhost -U postgres -d hrms_master -c -v "${BACKUP_FILE}"

# Step 4: Verify restoration
psql -h localhost -U postgres -d hrms_master -c "SELECT COUNT(*) FROM master.\"AdminUsers\";"
psql -h localhost -U postgres -d hrms_master -c "SELECT COUNT(*) FROM master.\"RefreshTokens\";"

# Step 5: Try rollback again with lessons learned
```

### If Application Won't Start After Rollback

```bash
# Check for schema mismatches
dotnet ef migrations list --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext

# Verify database connection
psql -h localhost -U postgres -d hrms_master -c "SELECT version();"

# Check application logs
tail -100 src/HRMS.API/Logs/hrms-*.log

# If schema is correct but app still fails, restore from backup and investigate
```

## Emergency Contacts

### Database Team
- **DBA Primary**: dba@morishr.com | +230-XXXX-XXXX
- **DBA Backup**: dba-backup@morishr.com | +230-XXXX-XXXX
- **24/7 Pager**: database-oncall@morishr.com

### System Administration
- **SysAdmin Primary**: sysadmin@morishr.com | +230-XXXX-XXXX
- **DevOps Lead**: devops@morishr.com | +230-XXXX-XXXX
- **Infrastructure Manager**: infrastructure@morishr.com

### Emergency Escalation
- **CTO**: cto@morishr.com | +230-XXXX-XXXX
- **CEO**: ceo@morishr.com | +230-XXXX-XXXX
- **Security Team**: security@morishr.com

### External Support
- **PostgreSQL Support**: (if you have enterprise support)
- **Cloud Provider Support**: (Google Cloud, AWS, etc.)
- **Microsoft .NET Support**: (if you have enterprise support)

## Rollback Decision Matrix

| Scenario | Rollback? | Notes |
|----------|-----------|-------|
| Critical production bug affecting all users | YES | Immediate rollback, investigate in dev |
| Security vulnerability introduced | YES | Rollback, patch, re-deploy |
| Data corruption detected | YES | Rollback, restore from backup |
| Performance degradation >50% | YES | Rollback, investigate and optimize |
| Minor bug affecting <5% of users | NO | Deploy hotfix instead |
| Feature not working as expected | NO | Deploy fix forward |
| Non-critical schema issue | NO | Fix with new migration |

## Lessons Learned Template

After completing rollback, document:

```markdown
## Rollback Incident Report: [Date]

### Incident Summary
- Date/Time:
- Duration:
- Impact:
- Root Cause:

### Rollback Details
- Rollback Started:
- Rollback Completed:
- Method Used:
- Issues Encountered:

### Data Impact
- Records Affected:
- Data Lost:
- Data Recovered:

### Prevention Measures
1.
2.
3.

### Process Improvements
1.
2.
3.
```

## Related Documentation
- Migration Runbook: `/workspaces/HRAPP/DATABASE_MIGRATION_RUNBOOK.md`
- Quick Reference: `/workspaces/HRAPP/MIGRATION_QUICK_REFERENCE.md`
- Security Enhancement Migration: `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Master/20251113040317_AddSecurityEnhancements.cs`
- Rollback SQL Script: `/tmp/rollback_script.sql`

---
**Last Updated**: 2025-11-13 04:03:17 UTC
**Document Version**: 1.0
**Reviewed By**: Database Disaster Recovery Specialist
