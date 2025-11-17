# Monitoring Performance Optimization Deployment Checklist

**Deployment Date:** _____________
**Deployed By:** _____________
**Environment:** Production
**Estimated Duration:** 15 minutes (zero downtime)

---

## Pre-Deployment Checklist

- [ ] Read full audit report: `MONITORING_PERFORMANCE_AUDIT_REPORT.md`
- [ ] Review optimization script: `monitoring/database/002_performance_optimizations.sql`
- [ ] Verify database backup exists
- [ ] Confirm maintenance window (optional - zero downtime deployment)
- [ ] Alert monitoring team of deployment
- [ ] Verify PostgreSQL version >= 12 (required for CONCURRENTLY)

---

## Deployment Steps

### Step 1: Backup Database (5 minutes)
```bash
# Create full database backup
pg_dump -U postgres -d hrms_db -F c -f /backup/hrms_backup_$(date +%Y%m%d_%H%M%S).dump

# Verify backup was created
ls -lh /backup/hrms_backup_*.dump | tail -1

# Expected: File size > 100MB
```
**Status:** [ ] Complete | **Time:** _______

---

### Step 2: Apply Performance Optimizations (8 minutes)
```bash
# Navigate to project directory
cd /workspaces/HRAPP

# Apply optimizations (zero downtime - uses CREATE INDEX CONCURRENTLY)
psql -U postgres -d hrms_db -f monitoring/database/002_performance_optimizations.sql

# Monitor output for errors
# Expected: "Fortune 50 Monitoring System - Performance Optimizations Applied"
```
**Status:** [ ] Complete | **Time:** _______

**Expected Output:**
```
CREATE INDEX (4 indexes)
CREATE MATERIALIZED VIEW (1 view)
CREATE FUNCTION (2 functions)
ALTER TABLE (multiple schema enhancements)
Performance Optimizations Applied
```

---

### Step 3: Verify Index Creation (2 minutes)
```sql
-- Connect to database
psql -U postgres -d hrms_db

-- Verify indexes were created
SELECT
    indexname,
    pg_size_pretty(pg_relation_size(indexname::regclass)) AS index_size
FROM pg_indexes
WHERE schemaname = 'monitoring'
AND indexname LIKE 'idx_%'
ORDER BY indexname;

-- Expected output: 8-10 indexes (including 4 new ones)
```
**Expected Indexes:**
- [ ] `idx_api_perf_occurred_tenant` (~15MB)
- [ ] `idx_security_events_composite` (~5MB)
- [ ] `idx_alert_history_active` (~2MB)
- [ ] `idx_tenant_activity_composite` (~8MB)

**Status:** [ ] Complete | **Time:** _______

---

### Step 4: Test Performance Improvements (3 minutes)
```sql
-- Test 1: Dashboard metrics query
EXPLAIN ANALYZE
SELECT * FROM monitoring.get_dashboard_metrics();

-- Expected execution time: <200ms (was 1200ms)
-- Planning Time: ~2ms
-- Execution Time: <200ms
```
**Execution Time:** _______ ms | **Pass/Fail:** [ ]

```sql
-- Test 2: Slow queries function
EXPLAIN ANALYZE
SELECT * FROM monitoring.get_slow_queries(10);

-- Expected execution time: <100ms
```
**Execution Time:** _______ ms | **Pass/Fail:** [ ]

```sql
-- Test 3: Security events (should use index)
EXPLAIN ANALYZE
SELECT * FROM monitoring.security_events
WHERE severity = 'Critical'
AND is_reviewed = false
ORDER BY detected_at DESC
LIMIT 20;

-- Expected: Index Scan using idx_security_events_composite
```
**Uses Index:** [ ] Yes | [ ] No | **Time:** _______ ms

**Status:** [ ] Complete | **Time:** _______

---

### Step 5: Verify Application Health (2 minutes)
```bash
# Check API health endpoint
curl -X GET https://api.hrms.com/health
# Expected: {"status": "healthy"}

# Test monitoring dashboard
curl -X GET https://admin.hrms.com/api/monitoring/dashboard \
  -H "Authorization: Bearer $SUPERADMIN_TOKEN"

# Expected: Response time < 300ms
# Check response time in headers: X-Response-Time
```
**API Status:** [ ] Healthy | **Dashboard Response Time:** _______ ms

**Status:** [ ] Complete | **Time:** _______

---

## Post-Deployment Monitoring (24 hours)

### Metrics to Monitor

#### Hour 1-4: Immediate Impact
- [ ] Dashboard response time reduced by >70%
- [ ] Database connection count stable
- [ ] No increase in error rates
- [ ] Cache hit rate remains >95%

#### Hour 4-12: Stability Check
- [ ] Average query time trending downward
- [ ] No connection pool exhaustion
- [ ] Background jobs completing successfully
- [ ] Memory usage stable or reduced

#### Hour 12-24: Performance Validation
- [ ] P95 response time <200ms for all monitoring endpoints
- [ ] Database CPU usage reduced by 15-30%
- [ ] No slow query alerts
- [ ] User reports improved dashboard performance

---

## Performance Benchmarks

### Expected Improvements

| Metric | Before | Target | Actual |
|--------|--------|--------|--------|
| Dashboard Query | 1200ms | <200ms | _____ ms |
| Infrastructure Health | 450ms | <150ms | _____ ms |
| Security Events | 320ms | <80ms | _____ ms |
| Active Alerts | 280ms | <30ms | _____ ms |
| Tenant Activity | 410ms | <100ms | _____ ms |
| DB Connections/Request | 4 | 1 | _____ |

---

## Rollback Procedure (If Needed)

**Only use if critical issues detected**

### Signs Requiring Rollback:
- [ ] Error rate increases by >5%
- [ ] Response times worse than before
- [ ] Database connection errors
- [ ] Index creation failures

### Rollback Steps:
```sql
-- 1. Drop new indexes (fast - no data loss)
DROP INDEX CONCURRENTLY IF EXISTS monitoring.idx_api_perf_occurred_tenant;
DROP INDEX CONCURRENTLY IF EXISTS monitoring.idx_security_events_composite;
DROP INDEX CONCURRENTLY IF EXISTS monitoring.idx_alert_history_active;
DROP INDEX CONCURRENTLY IF EXISTS monitoring.idx_tenant_activity_composite;

-- 2. Drop materialized view
DROP MATERIALIZED VIEW IF EXISTS monitoring.api_performance_summary;

-- 3. Drop new functions
DROP FUNCTION IF EXISTS monitoring.get_dashboard_metrics();
DROP FUNCTION IF EXISTS monitoring.get_slow_queries(INTEGER);

-- 4. Restore from backup (LAST RESORT - only if schema corruption)
pg_restore -U postgres -d hrms_db /backup/hrms_backup_YYYYMMDD_HHMMSS.dump
```

**Rollback Executed:** [ ] Yes | [ ] No | **Time:** _______

---

## Sign-Off

### Deployment Team

**Deployed By:** ___________________ | **Date:** ___________

**Verified By:** ___________________ | **Date:** ___________

**Approved By (CTO/Lead):** ___________________ | **Date:** ___________

### Post-Deployment Status

**Overall Status:** [ ] Success | [ ] Partial Success | [ ] Rollback Required

**Performance Improvements Achieved:** [ ] Yes | [ ] No

**Issues Encountered:**
_________________________________________________________________
_________________________________________________________________

**Next Steps:**
_________________________________________________________________
_________________________________________________________________

---

## Support Contacts

**Performance Engineering Team:** performance-eng@company.com
**Database Team:** database-ops@company.com
**On-Call Engineer:** oncall@company.com
**Incident Slack Channel:** #incidents-production

---

**Checklist Version:** 1.0
**Last Updated:** 2025-11-17
**Document Location:** `/workspaces/HRAPP/MONITORING_DEPLOYMENT_CHECKLIST.md`
