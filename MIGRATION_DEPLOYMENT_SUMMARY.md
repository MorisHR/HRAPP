# Database Migration Deployment Summary

## Overview

This document provides a high-level summary of the database migration deployment plan for the HRMS production system. For detailed procedures, refer to `MIGRATION_DEPLOYMENT_RUNBOOK.md`.

---

## Quick Reference

### Migrations to Deploy

| # | Migration Name | Type | Risk | Duration |
|---|----------------|------|------|----------|
| 1 | 20251112_AddNationalIdUniqueConstraint | Indexes | HIGH | 10s |
| 2 | 20251112_AddMissingCompositeIndexes | Indexes | LOW | 60s |
| 3 | 20251112_AddDataValidationConstraints | Constraints | MEDIUM | 15s |
| 4 | 20251112031109_AddColumnLevelEncryption | Schema | MEDIUM | 10s |

### Total Deployment Time Estimates

- **Best Case**: 90 minutes (no data quality issues)
- **Expected Case**: 2-3 hours (minor cleanup needed)
- **Worst Case**: 4-5 hours (major data cleanup required)
- **Recommended Window**: 4 hours (with buffer)

### Critical Success Factors

1. No duplicate National IDs, Passports, or Bank Accounts in database
2. No negative values in salary or leave balance fields
3. Valid data in all month/year fields
4. Full database backup completed successfully
5. Staging environment testing completed successfully

---

## Deployment Scripts

### Available Scripts

```bash
# 1. Deploy to Staging
./scripts/deploy-migrations-staging.sh

# 2. Deploy to Production
./scripts/deploy-migrations-production.sh

# 3. Verify Migrations
./scripts/verify-migrations.sh [check_type]

# 4. Rollback Migrations
./scripts/rollback-migrations.sh [rollback_type]
```

### Script Features

All deployment scripts include:
- Pre-deployment validation
- Data quality checks
- Automatic backup creation
- Step-by-step deployment with verification
- Automatic rollback on failure
- Comprehensive logging
- Status notifications (production only)

---

## Pre-Deployment Checklist

### 48 Hours Before

- [ ] Schedule deployment window (4 hours, off-peak)
- [ ] Notify all stakeholders
- [ ] Update status page with maintenance notice
- [ ] Assign on-call team
- [ ] Verify backup storage has 10GB+ free space

### 24 Hours Before

- [ ] Test migrations in staging environment
- [ ] Run full test suite in staging
- [ ] Verify staging performance after migrations
- [ ] Review rollback procedures with team
- [ ] Confirm emergency contacts available

### 1 Hour Before

- [ ] Create fresh production backup
- [ ] Verify backup integrity
- [ ] Run data quality checks
- [ ] Test database connectivity
- [ ] Verify monitoring dashboards operational
- [ ] Confirm rollback authority present

### Immediately Before

- [ ] Enable maintenance mode (optional)
- [ ] Verify no critical operations running
- [ ] Final stakeholder notification
- [ ] Create deployment log file
- [ ] Start screen recording (if required)

---

## Deployment Timeline

### Phase 1: Pre-Deployment (30-60 minutes)

| Time | Activity | Duration | Responsible |
|------|----------|----------|-------------|
| T+0 | Environment validation | 5 min | DevOps |
| T+5 | Data quality checks | 10-30 min | DBA |
| T+15 | Data cleanup (if needed) | 0-30 min | DBA |
| T+20 | Create backup | 15 min | DBA |
| T+35 | Verify backup | 5 min | DBA |

### Phase 2: Migration Deployment (15-30 minutes)

| Time | Activity | Duration | Responsible |
|------|----------|----------|-------------|
| T+40 | Migration #1: Unique Constraints | 2 min | DBA |
| T+42 | Verify Migration #1 | 2 min | DBA |
| T+44 | Migration #2: Composite Indexes | 5 min | DBA |
| T+49 | Verify Migration #2 | 5 min | DevOps |
| T+54 | Migration #3: Validation Constraints | 2 min | DBA |
| T+56 | Verify Migration #3 | 2 min | DBA |
| T+58 | Migration #4: Encryption Schema | 2 min | DBA |
| T+60 | Verify Migration #4 | 2 min | DevOps |

### Phase 3: Post-Deployment (30-45 minutes)

| Time | Activity | Duration | Responsible |
|------|----------|----------|-------------|
| T+62 | Comprehensive verification | 10 min | All |
| T+72 | Application health checks | 5 min | DevOps |
| T+77 | Smoke testing | 10 min | QA |
| T+87 | Performance monitoring | 15 min | DevOps |
| T+102 | Documentation update | 5 min | DBA |
| T+107 | Disable maintenance mode | 2 min | DevOps |

**Total Duration**: ~110 minutes (best case)

---

## Risk Assessment Matrix

### High Risk Items

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Duplicate data exists | MEDIUM | HIGH | Pre-deployment data scan |
| Constraint violations | MEDIUM | HIGH | Pre-deployment validation |
| Index creation timeout | LOW | MEDIUM | Off-peak deployment |
| Application incompatibility | LOW | HIGH | Staging testing |

### Risk Mitigation Strategies

#### 1. Duplicate Data Risk
**Mitigation**:
- Run data quality checks 24 hours before deployment
- Clean duplicates with business stakeholder review
- Re-verify immediately before deployment
- Have duplicate resolution procedures ready

**Contingency**:
- If duplicates found during deployment, abort and reschedule
- Use provided SQL scripts to identify and resolve duplicates
- Document resolution decisions for audit trail

#### 2. Constraint Violation Risk
**Mitigation**:
- Scan for negative values and invalid dates
- Auto-fix minor issues (negative salaries/balances)
- Manual review for critical data issues
- Validate constraints in staging first

**Contingency**:
- If violations found, auto-fix where safe
- Manual intervention for critical data
- Abort if cannot resolve quickly

#### 3. Performance Degradation Risk
**Mitigation**:
- Deploy during off-peak hours
- Monitor query performance in staging
- Use concurrent index creation where possible
- Set appropriate timeouts

**Contingency**:
- Extended deployment window
- Performance tuning post-deployment
- Index rebuild if needed

#### 4. Rollback Risk
**Mitigation**:
- Full backup before deployment
- Verify backup integrity
- Test rollback in staging
- Document rollback procedures
- Have rollback decision maker present

**Contingency**:
- Automated rollback script
- Manual rollback procedures
- Backup restoration as last resort

---

## Rollback Procedures

### When to Rollback

**Immediate Rollback Required**:
- Migration fails with error
- Data corruption detected
- Application critical functionality broken
- Performance degradation > 50%

**Consider Rollback**:
- Unexpected behavior
- Slow query performance (> 2x normal)
- Increased error rate
- Stakeholder escalation

### Rollback Options

#### Option 1: Automated Script Rollback (15-30 minutes)
```bash
./scripts/rollback-migrations.sh all
```
**Use when**: Migration failed but database is accessible

#### Option 2: Single Migration Rollback (5-10 minutes)
```bash
./scripts/rollback-migrations.sh single
```
**Use when**: Only latest migration needs reverting

#### Option 3: Backup Restoration (30-60 minutes)
```bash
./scripts/rollback-migrations.sh backup
```
**Use when**: Automated rollback failed or data corruption detected

### Rollback Decision Tree

```
Is the application functional?
├─ NO → IMMEDIATE ROLLBACK (Option 3: Backup)
└─ YES → Can data be accessed?
           ├─ NO → IMMEDIATE ROLLBACK (Option 3: Backup)
           └─ YES → Is performance acceptable?
                      ├─ NO → Investigate (may rollback Option 1)
                      └─ YES → Monitor and continue
```

---

## Success Criteria

### Technical Success Metrics

- [ ] All 4 migrations applied successfully
- [ ] All 19 indexes created and operational
- [ ] All 30+ CHECK constraints active
- [ ] No data loss or corruption
- [ ] Application health checks passing
- [ ] Query performance improved by 30-60%
- [ ] Backup completed and verified

### Business Success Metrics

- [ ] Downtime < 1 hour (if maintenance mode used)
- [ ] Zero data corruption incidents
- [ ] All critical operations functional
- [ ] Error rate unchanged or decreased
- [ ] User satisfaction maintained
- [ ] Audit trail complete

### Performance Targets

| Metric | Before | After | Target Improvement |
|--------|--------|-------|-------------------|
| Payroll queries | X ms | Y ms | 40-60% faster |
| Attendance reports | X ms | Y ms | 50-70% faster |
| Leave lookups | X ms | Y ms | 60-80% faster |
| Employee searches | X ms | Y ms | 50-70% faster |

---

## Post-Deployment Monitoring

### First Hour

Monitor every 5 minutes:
- Application error rate
- Database query performance
- Index usage statistics
- Constraint violations logged
- User-reported issues

### First 24 Hours

Monitor every hour:
- Query execution times
- Cache hit ratios
- Connection pool usage
- API response times
- Business metrics

### First Week

Monitor daily:
- Performance trends
- Index effectiveness
- Constraint violations
- User feedback
- Error patterns

### Red Flags

Immediate investigation required if:
- Error rate increases by > 10%
- Query times increase by > 50%
- Cache hit ratio drops below 90%
- Constraint violations logged
- User complaints spike

---

## Encryption Migration Strategy

### Why Encryption Migration is Special

The 4th migration (AddColumnLevelEncryption) is SCHEMA ONLY:
- Does not encrypt existing data
- Encryption service starts in "passthrough mode"
- Existing plaintext data remains accessible
- New data written as plaintext initially

### Zero-Downtime Approach

**Phase 1: Schema Migration** (included in deployment)
- Add encryption service to application
- Service configured but disabled
- No impact on existing data or operations

**Phase 2: Enable Encryption** (separate operation, post-migration)
- Configure encryption key in Google Secret Manager
- Enable encryption in configuration
- Restart application
- New data encrypted, old data readable

**Phase 3: Progressive Encryption** (background job, future)
- Create background job to encrypt existing data
- Encrypt data progressively during updates
- Monitor encryption progress
- No downtime required

### Encryption Enablement (Post-Migration)

```bash
# 1. Generate encryption key (if not already done)
openssl rand -base64 32

# 2. Store in Google Secret Manager
echo "<KEY>" | gcloud secrets create ENCRYPTION_KEY_V1 --data-file=-

# 3. Update application configuration
# Set "Encryption": { "Enabled": true } in appsettings.json

# 4. Restart application
systemctl restart hrms-api

# 5. Verify encryption enabled
curl http://localhost:5000/api/health/encryption
# Expected: { "encryptionEnabled": true, "keyVersion": "v1" }

# 6. Test new data encryption
# Create new employee record and verify it's encrypted in database
```

---

## Communication Plan

### Pre-Deployment Notifications

**T-48 hours**:
- Email to all stakeholders
- Status page updated
- Slack/Teams announcement
- Maintenance window confirmed

**T-24 hours**:
- Reminder email to stakeholders
- Final go/no-go decision
- On-call team briefed
- Backup procedures reviewed

**T-1 hour**:
- Final notification to users
- Maintenance mode enabled (if applicable)
- Monitoring dashboards prepared
- Emergency contacts confirmed

### During Deployment

**Every 30 minutes**:
- Status update to stakeholders
- Progress log updated
- Any issues reported immediately

**Critical events**:
- Migration start/complete
- Any errors or warnings
- Rollback initiated (if needed)
- Completion announcement

### Post-Deployment Notifications

**Immediately**:
- Completion announcement
- Success/failure status
- Known issues (if any)
- Next steps

**T+24 hours**:
- Performance report
- Lessons learned summary
- Outstanding action items

**T+1 week**:
- Final deployment report
- Metrics comparison
- Recommendations for future

---

## Emergency Contacts

### Primary Response Team

| Role | Name | Phone | Email | Responsibility |
|------|------|-------|-------|----------------|
| DBA Lead | [Name] | [Phone] | [Email] | Database operations |
| DevOps Lead | [Name] | [Phone] | [Email] | Infrastructure |
| App Architect | [Name] | [Phone] | [Email] | Application compatibility |
| QA Lead | [Name] | [Phone] | [Email] | Testing & verification |

### Escalation Path

| Level | Timeframe | Contact | Action |
|-------|-----------|---------|--------|
| 1 | 0-15 min | DevOps Engineer | Initial troubleshooting |
| 2 | 15-30 min | DBA + DevOps Lead | Advanced troubleshooting |
| 3 | 30-60 min | App Architect | Design-level issues |
| 4 | 60+ min | CTO | Rollback decision |

### Communication Channels

- **Primary**: Slack #ops-incidents
- **Secondary**: Conference call bridge: [Number]
- **Email**: ops-alerts@morishr.com
- **Status Page**: https://status.morishr.com

---

## Post-Deployment Report Template

Use this template after deployment:

```markdown
# Database Migration Deployment Report

**Date**: YYYY-MM-DD
**Duration**: HH:MM
**Status**: SUCCESS/FAILED
**Operator**: [Name]

## Summary
[Brief summary of deployment]

## Timeline
- Pre-deployment: [Duration]
- Deployment: [Duration]
- Post-deployment: [Duration]

## Issues Encountered
[List any issues and resolutions]

## Performance Metrics
- Payroll queries: [Before] → [After] ([% change])
- Attendance reports: [Before] → [After] ([% change])
- Leave lookups: [Before] → [After] ([% change])

## Verification Results
- [X] All migrations applied
- [X] All indexes created
- [X] All constraints active
- [X] Application healthy
- [X] Performance improved

## Lessons Learned
[What went well, what could be improved]

## Recommendations
[Any recommendations for future deployments]
```

---

## Key Takeaways

### What We're Deploying

4 database migrations that:
1. Enforce uniqueness on critical employee identifiers
2. Optimize query performance with composite indexes
3. Add data validation constraints
4. Prepare schema for column-level encryption

### Why It's Important

- **Data Integrity**: Prevents duplicate employee records
- **Performance**: 30-80% faster queries for critical operations
- **Compliance**: Enforces business rules at database level
- **Security**: Prepares for encryption of sensitive PII data

### What Could Go Wrong

- Duplicate data blocking migration #1
- Invalid data blocking migration #3
- Performance issues during index creation
- Application compatibility issues

### How We Mitigate Risk

- Comprehensive pre-deployment validation
- Automated data cleanup procedures
- Full backup before deployment
- Automated rollback on failure
- Staging environment testing
- Progressive deployment with verification

### What Happens After

- Performance monitoring for 24 hours
- User feedback collection
- Metrics comparison report
- Encryption enablement (separate operation)
- Progressive encryption of existing data (background job)

---

## FAQ

### Q: Can we deploy during business hours?
**A**: Not recommended. Deploy during off-peak hours (weekends/evenings) to minimize impact and reduce risk of index creation affecting performance.

### Q: What if we find duplicate data during deployment?
**A**: The deployment will abort. You must resolve duplicates manually with business stakeholder input, then retry deployment.

### Q: Will encryption be enabled immediately?
**A**: No. The migration only updates the schema. Encryption is enabled separately after migration, allowing zero-downtime deployment.

### Q: How long will rollback take if needed?
**A**: 15-30 minutes for automated script rollback, 30-60 minutes for backup restoration.

### Q: Can we partially apply migrations?
**A**: Yes, but not recommended. The deployment script applies migrations sequentially and can stop at any point, but rolling back partially applied migrations is complex.

### Q: What if staging tests fail?
**A**: Do NOT proceed to production. Investigate issues in staging, fix them, retest, and only then schedule production deployment.

### Q: Do we need downtime for this deployment?
**A**: No mandatory downtime, but maintenance mode is recommended for safety. Expect 0-60 minutes of user impact depending on approach chosen.

### Q: Will this affect existing data?
**A**: No data modification. Only schema changes (indexes, constraints). Existing data remains untouched unless it violates new constraints.

### Q: Can we test in production first?
**A**: NO. Always test in staging first. Never test database migrations directly in production.

### Q: What's the fastest we can deploy?
**A**: Best case 90 minutes, but plan for 2-3 hours to allow time for verification and any unexpected issues.

---

## Appendix: Quick Commands

### Pre-Deployment

```bash
# Check data quality
export DB_PASSWORD="<password>"
./scripts/verify-migrations.sh data-quality

# Create backup
pg_dump -h <host> -U postgres -d hrms_db -F c -f backup.dump

# Verify backup
pg_restore --list backup.dump
```

### Deployment

```bash
# Staging deployment
export DB_PASSWORD="<password>"
./scripts/deploy-migrations-staging.sh

# Production deployment
export DB_PASSWORD="<password>"
./scripts/deploy-migrations-production.sh
```

### Verification

```bash
# Quick verification
./scripts/verify-migrations.sh post-deployment

# Specific migration verification
./scripts/verify-migrations.sh migration-1
./scripts/verify-migrations.sh migration-2
./scripts/verify-migrations.sh migration-3
./scripts/verify-migrations.sh migration-4
```

### Rollback

```bash
# Rollback last migration
./scripts/rollback-migrations.sh single

# Rollback all migrations
./scripts/rollback-migrations.sh all

# Restore from backup
./scripts/rollback-migrations.sh backup
```

### Monitoring

```bash
# Check application health
curl http://localhost:5000/health

# Check encryption service
curl http://localhost:5000/api/health/encryption

# Database query performance
psql -h <host> -U postgres -d hrms_db -c "
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
WHERE query LIKE '%tenant_default%'
ORDER BY mean_exec_time DESC
LIMIT 10;
"
```

---

## Document Information

**Version**: 1.0
**Last Updated**: 2025-11-12
**Author**: Claude Code
**Review Date**: Before each deployment

**Related Documents**:
- MIGRATION_DEPLOYMENT_RUNBOOK.md (detailed procedures)
- /scripts/deploy-migrations-staging.sh (staging deployment)
- /scripts/deploy-migrations-production.sh (production deployment)
- /scripts/verify-migrations.sh (verification procedures)
- /scripts/rollback-migrations.sh (rollback procedures)

---

**END OF SUMMARY**
