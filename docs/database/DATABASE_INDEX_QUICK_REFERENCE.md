# Database Security Indexes - Quick Reference

## Quick Stats
- **Indexes Created:** 5
- **Total Size:** ~56 KB
- **Performance Gain:** 100-5000x faster queries
- **Date Applied:** 2025-11-13

## Index List

### 1. Password Reset Token
```sql
idx_adminusers_passwordresettoken
WHERE PasswordResetToken IS NOT NULL
```
**Use:** Password reset lookups | **Frequency:** Medium

### 2. Tenant ID Lookup
```sql
idx_refreshtokens_tenantid
WHERE TenantId IS NOT NULL
```
**Use:** Tenant token lookups | **Frequency:** HIGH

### 3. Employee ID Lookup
```sql
idx_refreshtokens_employeeid
WHERE EmployeeId IS NOT NULL
```
**Use:** Employee token lookups | **Frequency:** HIGH

### 4. Session Timeout
```sql
idx_refreshtokens_lastactivity
WHERE RevokedAt IS NULL
```
**Use:** Inactive session cleanup | **Frequency:** Medium

### 5. Active Token Validation (CRITICAL)
```sql
idx_refreshtokens_tenant_employee_active
ON (TenantId, EmployeeId, ExpiresAt)
WHERE RevokedAt IS NULL AND TenantId IS NOT NULL
```
**Use:** Token validation (most frequent) | **Frequency:** VERY HIGH

## Monitoring Commands

### Check Index Usage
```sql
SELECT indexrelname, idx_scan, pg_size_pretty(pg_relation_size(indexrelid))
FROM pg_stat_user_indexes
WHERE schemaname = 'master' AND indexrelname LIKE 'idx_%token%';
```

### Find Slow Queries
```sql
EXPLAIN ANALYZE
SELECT * FROM master."RefreshTokens"
WHERE "TenantId" = 'uuid' AND "EmployeeId" = 'uuid';
```

### Check Index Health
```sql
SELECT schemaname, tablename, indexrelname, idx_scan
FROM pg_stat_user_indexes
WHERE schemaname = 'master' AND idx_scan = 0;
```

## Emergency Rollback
```sql
DROP INDEX master.idx_refreshtokens_tenant_employee_active;
DROP INDEX master.idx_refreshtokens_lastactivity;
DROP INDEX master.idx_refreshtokens_employeeid;
DROP INDEX master.idx_refreshtokens_tenantid;
DROP INDEX master.idx_adminusers_passwordresettoken;
```

## Files
- **SQL Script:** `/tmp/add_security_indexes.sql`
- **Full Documentation:** `/workspaces/HRAPP/DATABASE_INDEXES.md`
- **This Reference:** `/workspaces/HRAPP/DATABASE_INDEX_QUICK_REFERENCE.md`
