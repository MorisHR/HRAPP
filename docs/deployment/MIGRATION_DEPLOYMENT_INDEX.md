# Database Migration Deployment - Complete Documentation Index

This is your complete guide to deploying 4 database migrations to the HRMS production system.

---

## Quick Start

**New to this deployment?** Start here:

1. Read the [Deployment Summary](MIGRATION_DEPLOYMENT_SUMMARY.md) - 10 minutes
2. Review the [Deployment Runbook](MIGRATION_DEPLOYMENT_RUNBOOK.md) - 30 minutes
3. Test in staging using [deploy-migrations-staging.sh](scripts/deploy-migrations-staging.sh)
4. Deploy to production using [deploy-migrations-production.sh](scripts/deploy-migrations-production.sh)

---

## Documentation Structure

### 1. Overview Documents

#### [MIGRATION_DEPLOYMENT_SUMMARY.md](MIGRATION_DEPLOYMENT_SUMMARY.md)
**Purpose**: Executive summary and quick reference
**Audience**: All stakeholders
**Read Time**: 10 minutes

**Contains**:
- High-level overview of all 4 migrations
- Timeline estimates (90 min - 4 hours)
- Risk assessment matrix
- Success criteria
- Quick commands reference
- FAQ section

**When to Use**:
- First time reading about deployment
- Need quick reference during deployment
- Explaining deployment to stakeholders
- Creating deployment approval

---

#### [MIGRATION_DEPLOYMENT_RUNBOOK.md](MIGRATION_DEPLOYMENT_RUNBOOK.md)
**Purpose**: Detailed step-by-step deployment procedures
**Audience**: DBAs, DevOps engineers
**Read Time**: 30 minutes (reference during deployment)

**Contains**:
- Complete migration analysis
- Pre-deployment checklist (10 steps)
- Step-by-step deployment guide (detailed commands)
- Post-deployment verification (comprehensive)
- Rollback procedures (multiple scenarios)
- Troubleshooting guide (7 common issues)
- Emergency contacts template
- 72-page comprehensive runbook

**When to Use**:
- During actual deployment
- Planning deployment steps
- Troubleshooting issues
- Training new team members
- Post-mortem analysis

---

### 2. Deployment Scripts

#### [scripts/deploy-migrations-staging.sh](scripts/deploy-migrations-staging.sh)
**Purpose**: Automated staging deployment
**Audience**: DevOps engineers, DBAs
**Lines of Code**: 500+

**Features**:
- Automated pre-deployment validation
- Data quality checks with auto-fix
- Backup creation and verification
- Sequential migration deployment
- Post-deployment verification
- Automatic rollback on failure
- Color-coded logging
- Comprehensive error handling

**Usage**:
```bash
export DB_HOST="staging-db.example.com"
export DB_PASSWORD="<password>"
./scripts/deploy-migrations-staging.sh
```

**Output**:
- Log file: `/tmp/backups/migration_staging_YYYYMMDD_HHMMSS.log`
- Backup: `/tmp/backups/hrms_staging_pre_migration_YYYYMMDD_HHMMSS.dump`

---

#### [scripts/deploy-migrations-production.sh](scripts/deploy-migrations-production.sh)
**Purpose**: Production deployment with safety features
**Audience**: Senior DBAs, DevOps leads
**Lines of Code**: 800+

**Features** (All staging features PLUS):
- Maintenance mode support
- Email/Slack notifications
- Confirmation prompts (type "DEPLOY" to proceed)
- Enhanced error logging
- Stakeholder notifications
- Detailed timeline tracking
- Backup integrity verification
- Emergency rollback procedures

**Usage**:
```bash
export DB_HOST="production-db.example.com"
export DB_PASSWORD="<password>"
export NOTIFICATION_EMAIL="ops@example.com"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/..."
./scripts/deploy-migrations-production.sh
```

**Safety Features**:
- Requires typing "DEPLOY" to confirm
- Creates safety backup before starting
- Sends notifications at key milestones
- Automatic rollback on any failure
- Comprehensive audit trail

---

#### [scripts/verify-migrations.sh](scripts/verify-migrations.sh)
**Purpose**: Verify migrations applied correctly
**Audience**: All technical staff
**Lines of Code**: 700+

**Check Types**:
```bash
./scripts/verify-migrations.sh pre-check           # Pre-deployment
./scripts/verify-migrations.sh data-quality        # Data validation
./scripts/verify-migrations.sh migration-1         # Unique constraints
./scripts/verify-migrations.sh migration-2         # Composite indexes
./scripts/verify-migrations.sh migration-3         # Validation constraints
./scripts/verify-migrations.sh migration-4         # Encryption schema
./scripts/verify-migrations.sh post-deployment     # Complete verification
./scripts/verify-migrations.sh rollback-verification # After rollback
./scripts/verify-migrations.sh all                 # Everything (default)
```

**Verification Checks**:
- Database connectivity
- Migration history
- Index creation (19 indexes)
- Constraint creation (30+ constraints)
- Index usage statistics
- Data quality
- Application health
- Performance metrics

**Output**:
- Color-coded pass/fail results
- Summary report with counts
- Exit code 0 (success) or 1 (failure)

---

#### [scripts/rollback-migrations.sh](scripts/rollback-migrations.sh)
**Purpose**: Emergency rollback procedures
**Audience**: Senior DBAs, incident response team
**Lines of Code**: 600+

**Rollback Types**:
```bash
./scripts/rollback-migrations.sh single            # Last migration only
./scripts/rollback-migrations.sh all               # All 4 migrations
./scripts/rollback-migrations.sh backup            # Restore from backup
./scripts/rollback-migrations.sh to-migration <name> # To specific migration
```

**Safety Features**:
- Creates backup before rollback
- Requires confirmation (type "ROLLBACK" or "RESTORE")
- Verifies backup integrity
- Terminates active connections safely
- Comprehensive verification after rollback

**Typical Scenarios**:
- **Migration failed**: Use `single` to rollback last migration
- **Multiple migrations failed**: Use `all` to rollback everything
- **Data corruption**: Use `backup` to restore from backup
- **Need specific state**: Use `to-migration` for targeted rollback

---

### 3. Migration Files (Read-Only Reference)

Located in: `/workspaces/HRAPP/src/HRMS.Infrastructure/Data/Migrations/Tenant/`

#### Migration 1: AddNationalIdUniqueConstraint
**File**: `20251112_AddNationalIdUniqueConstraint.cs`
**Changes**: 6 unique partial indexes
**Risk**: HIGH (duplicate data)
**Tables**: Employees

#### Migration 2: AddMissingCompositeIndexes
**File**: `20251112_AddMissingCompositeIndexes.cs`
**Changes**: 13 composite indexes
**Risk**: LOW
**Tables**: PayrollCycles, LeaveBalances, Attendances, Timesheets, Employees, TimesheetEntries, LeaveApplications, BiometricPunchRecords

#### Migration 3: AddDataValidationConstraints
**File**: `20251112_AddDataValidationConstraints.cs`
**Changes**: 30+ CHECK constraints
**Risk**: MEDIUM (invalid data)
**Tables**: Employees, LeaveBalances, Attendances, PayrollCycles, Payslips, Timesheets, TimesheetEntries, SalaryComponents, LeaveEncashments, AttendanceAnomalies, DeviceApiKeys

#### Migration 4: AddColumnLevelEncryption
**File**: `20251112031109_AddColumnLevelEncryption.cs`
**Changes**: Schema consolidation + encryption prep
**Risk**: MEDIUM
**Note**: Does not encrypt existing data; see "Encryption Strategy" below

---

## Deployment Workflow

### Standard Deployment Path

```
┌─────────────────────────────────────────────────────────────┐
│ PHASE 1: PRE-DEPLOYMENT                                     │
├─────────────────────────────────────────────────────────────┤
│ 1. Read documentation (Summary + Runbook)                   │
│ 2. Schedule deployment window (4 hours)                     │
│ 3. Notify stakeholders                                      │
│ 4. Test in staging:                                         │
│    └─> ./scripts/deploy-migrations-staging.sh              │
│ 5. Run test suite in staging                               │
│ 6. Verify performance in staging                           │
│ 7. Review data quality:                                     │
│    └─> ./scripts/verify-migrations.sh data-quality         │
│ 8. Clean duplicate/invalid data (if needed)                │
│ 9. Create backup:                                           │
│    └─> pg_dump -F c -f backup.dump                         │
│ 10. Final go/no-go decision                                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 2: DEPLOYMENT                                         │
├─────────────────────────────────────────────────────────────┤
│ 1. Enable maintenance mode (optional)                       │
│ 2. Run production deployment:                               │
│    └─> ./scripts/deploy-migrations-production.sh           │
│                                                             │
│ Script will automatically:                                  │
│ ├─> Validate environment                                    │
│ ├─> Check data quality                                      │
│ ├─> Create backup                                           │
│ ├─> Apply migration #1 (unique constraints)                 │
│ ├─> Verify migration #1                                     │
│ ├─> Apply migration #2 (composite indexes)                  │
│ ├─> Verify migration #2                                     │
│ ├─> Apply migration #3 (validation constraints)             │
│ ├─> Verify migration #3                                     │
│ ├─> Apply migration #4 (encryption schema)                  │
│ └─> Verify migration #4                                     │
│                                                             │
│ 3. Review deployment logs                                   │
│ 4. Disable maintenance mode                                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 3: POST-DEPLOYMENT                                    │
├─────────────────────────────────────────────────────────────┤
│ 1. Run comprehensive verification:                          │
│    └─> ./scripts/verify-migrations.sh post-deployment      │
│ 2. Test application health:                                 │
│    └─> curl http://api/health                              │
│ 3. Run smoke tests                                          │
│ 4. Monitor performance (15-30 minutes)                      │
│ 5. Check for errors in logs                                 │
│ 6. Verify business operations                              │
│ 7. Update documentation                                     │
│ 8. Notify stakeholders of completion                        │
│ 9. Monitor for 24 hours                                     │
│ 10. Create deployment report                               │
└─────────────────────────────────────────────────────────────┘
```

---

### Emergency Rollback Path

```
┌─────────────────────────────────────────────────────────────┐
│ ISSUE DETECTED                                              │
├─────────────────────────────────────────────────────────────┤
│ • Migration failed                                          │
│ • Data corruption                                           │
│ • Application broken                                        │
│ • Performance degradation > 50%                             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ DECISION: Which Rollback?                                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ Last migration failed only?                         │   │
│ │ └─> ./scripts/rollback-migrations.sh single        │   │
│ │     Duration: 5-10 minutes                          │   │
│ └─────────────────────────────────────────────────────┘   │
│                       OR                                    │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ Multiple migrations need rollback?                  │   │
│ │ └─> ./scripts/rollback-migrations.sh all           │   │
│ │     Duration: 15-30 minutes                         │   │
│ └─────────────────────────────────────────────────────┘   │
│                       OR                                    │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ Data corruption / Total failure?                    │   │
│ │ └─> ./scripts/rollback-migrations.sh backup        │   │
│ │     Duration: 30-60 minutes                         │   │
│ └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ POST-ROLLBACK VERIFICATION                                  │
├─────────────────────────────────────────────────────────────┤
│ 1. Verify rollback:                                         │
│    └─> ./scripts/verify-migrations.sh rollback-verification│
│ 2. Test application health                                  │
│ 3. Verify data integrity                                    │
│ 4. Document failure                                         │
│ 5. Notify stakeholders                                      │
│ 6. Root cause analysis                                      │
│ 7. Fix issues and reschedule                               │
└─────────────────────────────────────────────────────────────┘
```

---

## Encryption Strategy

### Why Encryption is Special

Migration #4 (AddColumnLevelEncryption) is SCHEMA ONLY:
- Updates database schema to support encryption
- Does NOT encrypt existing data
- Encryption service runs in "passthrough mode"
- Allows zero-downtime deployment

### Three-Phase Encryption Approach

```
┌─────────────────────────────────────────────────────────────┐
│ PHASE 1: Schema Migration (This Deployment)                │
├─────────────────────────────────────────────────────────────┤
│ • Add encryption service to application                     │
│ • Service configured but disabled (passthrough mode)        │
│ • No impact on existing data                               │
│ • No application restart required                           │
│ • Duration: Included in main deployment                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 2: Enable Encryption (Separate Operation)            │
├─────────────────────────────────────────────────────────────┤
│ When: 1-2 weeks after schema migration                     │
│                                                             │
│ Steps:                                                      │
│ 1. Generate encryption key:                                │
│    └─> openssl rand -base64 32                             │
│                                                             │
│ 2. Store in Google Secret Manager:                         │
│    └─> gcloud secrets create ENCRYPTION_KEY_V1             │
│                                                             │
│ 3. Update configuration:                                    │
│    └─> "Encryption": { "Enabled": true }                   │
│                                                             │
│ 4. Restart application:                                     │
│    └─> systemctl restart hrms-api                          │
│                                                             │
│ 5. Verify encryption enabled:                              │
│    └─> curl /api/health/encryption                         │
│                                                             │
│ Result:                                                     │
│ • New data will be encrypted                                │
│ • Existing plaintext data still readable                    │
│ • No downtime                                              │
│                                                             │
│ Duration: 30 minutes                                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 3: Progressive Encryption (Background Job)           │
├─────────────────────────────────────────────────────────────┤
│ When: After Phase 2, over several weeks                    │
│                                                             │
│ Approach:                                                   │
│ • Background job encrypts existing data progressively       │
│ • Data encrypted during normal updates                     │
│ • Monitor encryption progress                              │
│ • No user impact                                           │
│ • No downtime                                              │
│                                                             │
│ Duration: 2-4 weeks (background)                           │
└─────────────────────────────────────────────────────────────┘
```

### Encrypted Fields

When encryption is enabled (Phase 2), these fields will be encrypted:
- Employee.NationalIdCard
- Employee.PassportNumber
- Employee.BankAccountNumber
- Employee.TaxIdNumber
- Employee.NPFNumber
- Employee.NSFNumber
- Employee.Phone
- Employee.Email (optional)
- Salary components (BasicSalary, etc.)

---

## Cheat Sheet

### Environment Variables

```bash
# Required for all scripts
export DB_HOST="database.example.com"
export DB_PORT="5432"                    # Default: 5432
export DB_NAME="hrms_db"                 # Default: hrms_db
export DB_USER="postgres"                # Default: postgres
export DB_PASSWORD="<your_password>"     # REQUIRED

# Optional (production only)
export BACKUP_DIR="/backup/database"     # Default: /backup/database
export API_URL="http://localhost:5000"   # Default: http://localhost:5000
export NOTIFICATION_EMAIL="ops@example.com"
export SLACK_WEBHOOK_URL="https://hooks.slack.com/..."
```

### Quick Commands

```bash
# Pre-flight checks
./scripts/verify-migrations.sh pre-check
./scripts/verify-migrations.sh data-quality

# Staging deployment
./scripts/deploy-migrations-staging.sh

# Production deployment
./scripts/deploy-migrations-production.sh

# Post-deployment verification
./scripts/verify-migrations.sh post-deployment

# Emergency rollback
./scripts/rollback-migrations.sh backup

# Check application health
curl http://localhost:5000/health
curl http://localhost:5000/api/health/encryption
```

### Important File Paths

```
/workspaces/HRAPP/
├── MIGRATION_DEPLOYMENT_SUMMARY.md      # Executive summary
├── MIGRATION_DEPLOYMENT_RUNBOOK.md      # Detailed runbook
├── MIGRATION_DEPLOYMENT_INDEX.md        # This file
│
├── scripts/
│   ├── deploy-migrations-staging.sh     # Staging deployment
│   ├── deploy-migrations-production.sh  # Production deployment
│   ├── verify-migrations.sh             # Verification
│   └── rollback-migrations.sh           # Rollback
│
└── src/HRMS.Infrastructure/Data/Migrations/Tenant/
    ├── 20251112_AddNationalIdUniqueConstraint.cs
    ├── 20251112_AddMissingCompositeIndexes.cs
    ├── 20251112_AddDataValidationConstraints.cs
    └── 20251112031109_AddColumnLevelEncryption.cs
```

---

## Success Metrics

### Technical Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| All migrations applied | 4/4 | `verify-migrations.sh post-deployment` |
| Indexes created | 19/19 | Query `pg_indexes` |
| Constraints active | 30+/30+ | Query `pg_constraint` |
| No data loss | 100% | Compare row counts before/after |
| Application health | 200 OK | `curl /health` |
| Query performance | +30-60% | Compare query times |
| Deployment duration | < 3 hours | Log file timestamps |
| Rollback capability | < 30 min | Tested in staging |

### Business Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Downtime | < 1 hour | Monitoring logs |
| Data integrity | 100% | Audit reports |
| User satisfaction | No complaints | Support tickets |
| Error rate | No increase | Application logs |
| Critical ops functional | 100% | Smoke tests |

---

## Common Issues and Solutions

### Issue 1: "Duplicate key value violates unique constraint"
**Cause**: Duplicate National IDs, Passports, or Bank Accounts exist
**Solution**:
```bash
# Find duplicates
./scripts/verify-migrations.sh data-quality

# Review duplicates with business team
# Clean duplicates manually
# Retry deployment
```

### Issue 2: "Check constraint violated"
**Cause**: Invalid data (negative salaries, invalid dates)
**Solution**:
```bash
# Auto-fix is attempted by deployment script
# If manual fix needed:
psql -c "UPDATE ... SET BasicSalary = 0 WHERE BasicSalary < 0"
```

### Issue 3: "Index creation timeout"
**Cause**: Large table, high activity
**Solution**:
```bash
# Increase timeout in connection string
# Deploy during off-peak hours
# Check for blocking queries:
psql -c "SELECT * FROM pg_stat_activity WHERE state != 'idle'"
```

### Issue 4: "Application returns 500 errors"
**Cause**: Connection issues, migration state mismatch
**Solution**:
```bash
# Check database connectivity
psql -c "SELECT 1"

# Restart application
systemctl restart hrms-api

# Verify migration state
./scripts/verify-migrations.sh post-deployment
```

### Issue 5: "Slow performance after migration"
**Cause**: Statistics not updated, indexes not being used
**Solution**:
```bash
# Update statistics
psql -c "ANALYZE"

# Check index usage
psql -c "SELECT * FROM pg_stat_user_indexes WHERE schemaname = 'tenant_default'"

# Verify queries use indexes
psql -c "EXPLAIN ANALYZE SELECT ..."
```

---

## Decision Trees

### Should I Deploy?

```
┌─ Have you tested in staging?
│   ├─ NO → DO NOT DEPLOY (test in staging first)
│   └─ YES ↓
│
├─ Did staging tests pass 100%?
│   ├─ NO → DO NOT DEPLOY (fix issues first)
│   └─ YES ↓
│
├─ Is backup storage available (10GB+)?
│   ├─ NO → DO NOT DEPLOY (free up space first)
│   └─ YES ↓
│
├─ Are data quality checks passing?
│   ├─ NO → Clean data first, then deploy
│   └─ YES ↓
│
├─ Is deployment window scheduled (4+ hours)?
│   ├─ NO → Schedule window first
│   └─ YES ↓
│
├─ Are stakeholders notified?
│   ├─ NO → Notify stakeholders first
│   └─ YES ↓
│
├─ Is rollback authority available?
│   ├─ NO → Wait for senior engineer
│   └─ YES ↓
│
└─ GO FOR DEPLOYMENT! ✓
```

### Should I Rollback?

```
┌─ Is the application functional?
│   ├─ NO → IMMEDIATE ROLLBACK (backup restoration)
│   └─ YES ↓
│
├─ Can users access their data?
│   ├─ NO → IMMEDIATE ROLLBACK (backup restoration)
│   └─ YES ↓
│
├─ Is error rate elevated (> 10% increase)?
│   ├─ YES → Consider rollback (investigate first)
│   └─ NO ↓
│
├─ Is performance degraded (> 50% slower)?
│   ├─ YES → Consider rollback (investigate first)
│   └─ NO ↓
│
├─ Are critical operations working?
│   ├─ NO → Consider rollback (test thoroughly first)
│   └─ YES ↓
│
└─ DO NOT ROLLBACK - Monitor closely
```

---

## Resources

### Documentation
- Deployment Summary: [MIGRATION_DEPLOYMENT_SUMMARY.md](MIGRATION_DEPLOYMENT_SUMMARY.md)
- Detailed Runbook: [MIGRATION_DEPLOYMENT_RUNBOOK.md](MIGRATION_DEPLOYMENT_RUNBOOK.md)
- This Index: [MIGRATION_DEPLOYMENT_INDEX.md](MIGRATION_DEPLOYMENT_INDEX.md)

### Scripts
- Staging Deployment: [scripts/deploy-migrations-staging.sh](scripts/deploy-migrations-staging.sh)
- Production Deployment: [scripts/deploy-migrations-production.sh](scripts/deploy-migrations-production.sh)
- Verification: [scripts/verify-migrations.sh](scripts/verify-migrations.sh)
- Rollback: [scripts/rollback-migrations.sh](scripts/rollback-migrations.sh)

### Migration Files
- Migration 1: [src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddNationalIdUniqueConstraint.cs](src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddNationalIdUniqueConstraint.cs)
- Migration 2: [src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddMissingCompositeIndexes.cs](src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddMissingCompositeIndexes.cs)
- Migration 3: [src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddDataValidationConstraints.cs](src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112_AddDataValidationConstraints.cs)
- Migration 4: [src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112031109_AddColumnLevelEncryption.cs](src/HRMS.Infrastructure/Data/Migrations/Tenant/20251112031109_AddColumnLevelEncryption.cs)

### External Resources
- PostgreSQL Documentation: https://www.postgresql.org/docs/
- Entity Framework Migrations: https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- Database Migration Best Practices: https://docs.google.com/...

---

## Support

### Need Help?

**Before Deployment**:
- Review this index
- Read the Summary document
- Study the Runbook
- Test in staging
- Ask questions in team chat

**During Deployment**:
- Follow the Runbook step-by-step
- Check verification scripts after each step
- Monitor logs continuously
- Communicate status regularly
- Don't hesitate to abort if uncertain

**After Deployment**:
- Complete all verification steps
- Monitor for 24 hours
- Document any issues
- Create deployment report
- Share lessons learned

**Emergency**:
- Use rollback scripts immediately
- Contact senior engineer
- Notify stakeholders
- Document everything
- Post-mortem after resolution

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-12 | Claude Code | Initial version - Complete deployment package created |

---

**IMPORTANT**: Do NOT deploy to production without:
1. Testing in staging first
2. Passing all data quality checks
3. Creating a full backup
4. Reading the Runbook
5. Having rollback authority available

---

**END OF INDEX**
