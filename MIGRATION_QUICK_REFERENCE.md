# Quick Reference: Security Enhancements Migration

## Migration Overview

**Migration ID**: 20251113040317_AddSecurityEnhancements
**Target**: MasterDbContext (hrms_master database)
**Date**: 2025-11-13 04:03:17 UTC
**Risk**: Medium-High | **Complexity**: Medium | **Reversible**: Yes

---

## Quick Commands

### Check Current Migration Status

```bash
cd /workspaces/HRAPP
dotnet ef migrations list \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Apply Migration

```bash
cd /workspaces/HRAPP

# Set environment variables
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-minimum-32-characters"

# Apply migration
dotnet ef database update \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Rollback Migration

```bash
cd /workspaces/HRAPP

# Set environment variables
export DOTNET_ENVIRONMENT=Production
export JWT_SECRET="your-production-jwt-secret-minimum-32-characters"

# Rollback to previous migration
dotnet ef database update InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Generate Migration Script (Dry Run)

```bash
cd /workspaces/HRAPP

# Generate forward migration script
dotnet ef migrations script \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext \
  --output /tmp/migration_script.sql

# Generate rollback script
dotnet ef migrations script AddSecurityEnhancements InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext \
  --output /tmp/rollback_script.sql
```

---

## Database Verification

### Verify Migration Applied

```sql
-- Check migration history
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251113040317_AddSecurityEnhancements';

-- Should return 1 row if applied, 0 rows if not applied
```

### Verify Schema Changes

```sql
-- Check AdminUsers new columns
\d master."AdminUsers"

-- Should see:
--   PasswordResetToken          | text
--   PasswordResetTokenExpiry     | timestamp with time zone
--   ActivationTokenExpiry        | timestamp with time zone

-- Check RefreshTokens new columns
\d master."RefreshTokens"

-- Should see:
--   TenantId                     | uuid
--   EmployeeId                   | uuid
--   SessionTimeoutMinutes        | integer (default 0)
--   LastActivityAt               | timestamp with time zone
--   AdminUserId                  | uuid (nullable after migration)
```

### Verify Data Integrity

```sql
-- Count records (should match pre-migration counts)
SELECT
    'AdminUsers' as table_name,
    COUNT(*) as count
FROM master."AdminUsers"
UNION ALL
SELECT 'RefreshTokens', COUNT(*) FROM master."RefreshTokens";

-- Check for NULL AdminUserId (OK after migration)
SELECT COUNT(*) as null_admin_users
FROM master."RefreshTokens"
WHERE "AdminUserId" IS NULL;
```

---

## Backup & Restore

### Create Backup

```bash
# Create timestamped backup
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="/tmp/hrms_master_backup_${TIMESTAMP}.backup"

pg_dump -h localhost -U postgres -d hrms_master \
  -F c -f "${BACKUP_FILE}"

# Verify backup
ls -lh "${BACKUP_FILE}"
pg_restore --list "${BACKUP_FILE}" | head -20

# Save location
echo "${BACKUP_FILE}" > /tmp/latest_backup_location.txt
```

### Restore from Backup

```bash
# Get latest backup
BACKUP_FILE=$(cat /tmp/latest_backup_location.txt)

# Option 1: Restore to existing database (drops existing objects)
pg_restore -h localhost -U postgres -d hrms_master -c -v "${BACKUP_FILE}"

# Option 2: Restore to new database
psql -h localhost -U postgres -c "CREATE DATABASE hrms_master_restored;"
pg_restore -h localhost -U postgres -d hrms_master_restored -v "${BACKUP_FILE}"

# Option 3: Full database replacement (CAUTION!)
psql -h localhost -U postgres -c "DROP DATABASE hrms_master;"
psql -h localhost -U postgres -c "CREATE DATABASE hrms_master;"
pg_restore -h localhost -U postgres -d hrms_master -v "${BACKUP_FILE}"
```

### Verify Backup Contents

```bash
# List all tables in backup
pg_restore --list /tmp/hrms_master_backup_*.backup | grep "TABLE DATA"

# Check specific tables
pg_restore --list /tmp/hrms_master_backup_*.backup | grep -E "(AdminUsers|RefreshTokens)"

# Count objects in backup
echo "Tables: $(pg_restore --list /tmp/hrms_master_backup_*.backup | grep -c 'TABLE DATA')"
echo "Indexes: $(pg_restore --list /tmp/hrms_master_backup_*.backup | grep -c 'INDEX')"
echo "Constraints: $(pg_restore --list /tmp/hrms_master_backup_*.backup | grep -c 'CONSTRAINT')"
```

---

## Application Health Checks

### Check Application Status

```bash
# Systemd service
sudo systemctl status hrms-api

# Docker
docker ps | grep hrms

# Process check
ps aux | grep "dotnet.*HRMS.API"

# Health endpoint
curl -i http://localhost:5000/health

# Database health endpoint
curl -i http://localhost:5000/health/ready
```

### Stop Application

```bash
# Systemd
sudo systemctl stop hrms-api

# Docker
docker-compose down

# Kubernetes
kubectl scale deployment hrms-api --replicas=0 -n production

# Process kill
pkill -f "dotnet.*HRMS.API"
```

### Start Application

```bash
# Systemd
sudo systemctl start hrms-api

# Docker
docker-compose up -d

# Kubernetes
kubectl scale deployment hrms-api --replicas=3 -n production

# Manual
cd /workspaces/HRAPP
nohup dotnet run --project src/HRMS.API --environment Production > /tmp/hrms-api.log 2>&1 &
```

---

## Database Connection Management

### Check Active Connections

```sql
-- Count active connections
SELECT COUNT(*)
FROM pg_stat_activity
WHERE datname = 'hrms_master';

-- Show connection details
SELECT
    pid,
    usename,
    application_name,
    client_addr,
    state,
    query_start,
    now() - query_start AS duration
FROM pg_stat_activity
WHERE datname = 'hrms_master'
ORDER BY query_start;
```

### Terminate Connections

```sql
-- Terminate specific connection
SELECT pg_terminate_backend(12345);  -- Replace with actual PID

-- Terminate all non-superuser connections
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = 'hrms_master'
AND pid <> pg_backend_pid()
AND usename != 'postgres';
```

---

## Troubleshooting

### Migration Fails: "Unable to create DbContext"

**Cause**: JWT_SECRET not configured or too short

**Solution**:
```bash
# Set JWT secret (minimum 32 characters)
export JWT_SECRET="your-secret-key-must-be-at-least-32-chars-long"

# Or add to appsettings.Development.json
echo '{
  "JwtSettings": {
    "Secret": "your-secret-key-must-be-at-least-32-chars-long"
  }
}' > src/HRMS.API/appsettings.Development.json
```

### Migration Fails: "Connection refused"

**Cause**: PostgreSQL not running or connection string incorrect

**Solution**:
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Start PostgreSQL
sudo systemctl start postgresql

# Verify connection
psql -h localhost -U postgres -d hrms_master -c "SELECT version();"

# Check connection string in appsettings.json
cat src/HRMS.API/appsettings.json | grep "DefaultConnection"
```

### Rollback Fails: "Migration not found"

**Cause**: Target migration name incorrect

**Solution**:
```bash
# List all migrations
dotnet ef migrations list \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext

# Use exact migration name from list (case-sensitive)
dotnet ef database update InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Application Won't Start After Migration

**Cause**: Schema mismatch or missing columns

**Solution**:
```bash
# Check application logs
tail -100 src/HRMS.API/Logs/hrms-*.log

# Verify schema matches migration
psql -h localhost -U postgres -d hrms_master -c "\d master.\"AdminUsers\""
psql -h localhost -U postgres -d hrms_master -c "\d master.\"RefreshTokens\""

# If schema is wrong, rollback and investigate
dotnet ef database update InitialMasterDb \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context MasterDbContext
```

### Performance Issues After Migration

**Cause**: Missing indexes or statistics outdated

**Solution**:
```sql
-- Analyze tables to update statistics
ANALYZE master."AdminUsers";
ANALYZE master."RefreshTokens";

-- Check for missing indexes
SELECT
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'master'
AND tablename IN ('AdminUsers', 'RefreshTokens')
ORDER BY tablename, indexname;

-- Reindex if needed
REINDEX TABLE master."AdminUsers";
REINDEX TABLE master."RefreshTokens";
```

---

## One-Liner Commands

```bash
# Full migration workflow
cd /workspaces/HRAPP && export JWT_SECRET="your-jwt-secret-32-chars-minimum" && dotnet ef database update --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext

# Full rollback workflow
cd /workspaces/HRAPP && export JWT_SECRET="your-jwt-secret-32-chars-minimum" && dotnet ef database update InitialMasterDb --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext

# Backup + migrate
BACKUP_FILE="/tmp/hrms_master_backup_$(date +%Y%m%d_%H%M%S).backup" && pg_dump -h localhost -U postgres -d hrms_master -F c -f "${BACKUP_FILE}" && cd /workspaces/HRAPP && export JWT_SECRET="your-jwt-secret-32-chars-minimum" && dotnet ef database update --project src/HRMS.Infrastructure --startup-project src/HRMS.API --context MasterDbContext

# Check migration status
psql -h localhost -U postgres -d hrms_master -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"

# Verify schema changes
psql -h localhost -U postgres -d hrms_master -c "\d master.\"AdminUsers\"" && psql -h localhost -U postgres -d hrms_master -c "\d master.\"RefreshTokens\""

# Health check
curl -s http://localhost:5000/health && curl -s http://localhost:5000/health/ready

# Application restart
sudo systemctl restart hrms-api && sleep 5 && curl -s http://localhost:5000/health
```

---

## Important File Locations

| Resource | Location |
|----------|----------|
| Migration File | `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Master/20251113040317_AddSecurityEnhancements.cs` |
| Rollback Procedures | `/workspaces/HRAPP/DATABASE_ROLLBACK_PROCEDURES.md` |
| Migration Runbook | `/workspaces/HRAPP/DATABASE_MIGRATION_RUNBOOK.md` |
| Quick Reference | `/workspaces/HRAPP/MIGRATION_QUICK_REFERENCE.md` |
| Migration Script | `/tmp/migration_script.sql` (generated) |
| Rollback Script | `/tmp/rollback_script.sql` (generated) |
| Backup Files | `/tmp/hrms_master_backup_*.backup` |
| Application Logs | `/workspaces/HRAPP/src/HRMS.API/Logs/hrms-*.log` |
| appsettings | `/workspaces/HRAPP/src/HRMS.API/appsettings.json` |
| appsettings.Development | `/workspaces/HRAPP/src/HRMS.API/appsettings.Development.json` |

---

## Emergency Contacts

| Role | Contact | When to Contact |
|------|---------|-----------------|
| Migration Lead | migrations@morishr.com | Any migration questions |
| DBA On-Call | dba@morishr.com | Database issues, rollback needed |
| DevOps Lead | devops@morishr.com | Application deployment issues |
| CTO | cto@morishr.com | Critical failures, business impact |
| Support Team | support@morishr.com | User impact reporting |

---

## What Changed in This Migration

### AdminUsers Table
- **Added**: `PasswordResetToken` (text, nullable) - Stores password reset tokens
- **Added**: `PasswordResetTokenExpiry` (timestamp, nullable) - Token expiration time
- **Added**: `ActivationTokenExpiry` (timestamp, nullable) - Account activation deadline

### RefreshTokens Table
- **Added**: `TenantId` (uuid, nullable) - Multi-tenant token support
- **Added**: `EmployeeId` (uuid, nullable) - Employee token support
- **Added**: `SessionTimeoutMinutes` (integer, default 0) - Custom timeouts
- **Added**: `LastActivityAt` (timestamp, not null) - Activity tracking
- **Modified**: `AdminUserId` changed from NOT NULL to NULLABLE

### No Data Changes
- All existing data preserved
- No records added, modified, or deleted
- Only schema changes

---

## Data Impact on Rollback

Rolling back will **permanently delete**:
- Password reset tokens in progress
- Activation token expiry timestamps
- Tenant associations on refresh tokens
- Employee associations on refresh tokens
- Session timeout configurations
- User activity tracking data

**NULL AdminUserId values** will be set to `00000000-0000-0000-0000-000000000000`

---

**Version**: 1.0
**Last Updated**: 2025-11-13 04:03:17 UTC
**Maintained By**: Database Team

**Related Docs**:
- [Rollback Procedures](/workspaces/HRAPP/DATABASE_ROLLBACK_PROCEDURES.md)
- [Full Migration Runbook](/workspaces/HRAPP/DATABASE_MIGRATION_RUNBOOK.md)
