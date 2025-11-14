# pg_stat_statements Implementation - Complete Package

## Overview

This package contains everything needed to safely configure, deploy, and maintain PostgreSQL's pg_stat_statements extension for query performance tracking.

**Status:** READY FOR DEPLOYMENT
**PostgreSQL Version:** 16.10
**Last Updated:** 2025-11-14

---

## Quick Start

### One-Command Installation

```bash
sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
```

This will handle everything automatically:
- Backup configuration
- Update settings
- Restart PostgreSQL (with confirmation)
- Install extension in databases
- Verify installation

**Time Required:** 2-3 minutes

---

## Documentation Files

### 1. Main Setup Guide (COMPREHENSIVE)
**File:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md`

**Contains:**
- Complete installation instructions
- Step-by-step manual procedures
- Configuration parameters explained
- Usage examples and queries
- Troubleshooting guide
- Performance considerations
- Security best practices
- Multi-tenant guidance

**Use this for:** Detailed understanding of the complete setup process

---

### 2. Quick Reference Guide
**File:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_QUICK_REFERENCE.md`

**Contains:**
- Quick start commands
- Essential queries
- Common issues and solutions
- Configuration snippets
- Script reference
- Maintenance tasks
- Integration examples

**Use this for:** Day-to-day operations and quick lookups

---

### 3. Admin Report (EXECUTIVE)
**File:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_ADMIN_REPORT.md`

**Contains:**
- Executive summary
- Current system status
- Risk assessment
- Implementation procedures
- Rollback procedures
- Security considerations
- Approval checklist
- Post-implementation verification

**Use this for:** Management review and approval process

---

### 4. Index (THIS FILE)
**File:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_INDEX.md`

**Contains:**
- Package overview
- Quick navigation
- File descriptions
- Command reference

**Use this for:** Finding the right documentation or script

---

## Installation Scripts

All scripts are located in `/workspaces/HRAPP/scripts/`

### 1. Complete Automated Setup
**File:** `enable_pg_stat_statements.sh`
**Requires:** sudo
**Interactive:** Yes (2-3 confirmations)

```bash
sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
```

**What it does:**
1. Backs up configuration
2. Updates postgresql.conf
3. Restarts PostgreSQL
4. Installs extension in databases
5. Verifies everything works

**Use when:** You want a complete, guided setup

---

### 2. Configuration Update Only
**File:** `update_postgresql_config.sh`
**Requires:** sudo
**Interactive:** No

```bash
sudo bash /workspaces/HRAPP/scripts/update_postgresql_config.sh
```

**What it does:**
1. Creates backup
2. Updates postgresql.conf with pg_stat_statements settings
3. Provides restart instructions

**Use when:** You only want to update the config (manual control)

---

### 3. Extension Installation
**File:** `install_pg_stat_statements_extension.sh`
**Requires:** No sudo
**Interactive:** Yes (database selection)

```bash
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh
```

**What it does:**
1. Verifies shared_preload_libraries is configured
2. Lists available databases
3. Installs extension in selected databases
4. Tests functionality

**Use when:** Config is updated, PostgreSQL restarted, now need extension installed

---

### 4. Verification Script
**File:** `verify_pg_stat_statements.sh`
**Requires:** No sudo
**Interactive:** No

```bash
bash /workspaces/HRAPP/scripts/verify_pg_stat_statements.sh
```

**What it does:**
1. Tests PostgreSQL connection
2. Verifies shared_preload_libraries
3. Checks extension availability
4. Validates configuration parameters
5. Tests functionality
6. Reports comprehensive status

**Use when:** After installation, or troubleshooting

---

### 5. Rollback Script
**File:** `rollback_pg_stat_statements.sh`
**Requires:** sudo
**Interactive:** Yes (backup selection)

```bash
sudo bash /workspaces/HRAPP/scripts/rollback_pg_stat_statements.sh
```

**What it does:**
1. Lists available backups
2. Allows selection of backup to restore
3. Creates safety backup
4. Restores configuration
5. Offers to restart PostgreSQL
6. Verifies rollback

**Use when:** Need to undo changes or restore previous configuration

---

### 6. Interactive Query Tester
**File:** `test_pg_stat_statements_queries.sh`
**Requires:** No sudo
**Interactive:** Yes (menu-driven)

```bash
bash /workspaces/HRAPP/scripts/test_pg_stat_statements_queries.sh [database_name]
```

**What it does:**
1. Provides interactive menu with 20+ query options
2. Demonstrates useful pg_stat_statements queries
3. Allows testing of functionality
4. Generates performance reports
5. Educational tool for learning the extension

**Use when:** Learning the extension, testing queries, generating reports

---

## Configuration Details

### Current System Status

```
PostgreSQL Version: 16.10 (Ubuntu 16.10-0ubuntu0.24.04.1)
Cluster: 16/main
Port: 5432
Config File: /etc/postgresql/16/main/postgresql.conf
Data Directory: /var/lib/postgresql/16/main
Extension Available: Yes (version 1.10)
Extension Installed: No (not yet)
shared_preload_libraries: Empty (needs configuration)
```

### Required Configuration Changes

**File:** `/etc/postgresql/16/main/postgresql.conf`
**Line:** 747

**Change from:**
```
#shared_preload_libraries = ''
```

**Change to:**
```
shared_preload_libraries = 'pg_stat_statements'

# pg_stat_statements configuration
pg_stat_statements.max = 10000
pg_stat_statements.track = all
pg_stat_statements.track_utility = on
pg_stat_statements.track_planning = on
```

### Restart Required

Yes, PostgreSQL must be restarted for shared_preload_libraries changes.

**Restart command (choose one):**
```bash
sudo service postgresql restart              # Container/service
sudo systemctl restart postgresql@16-main   # Systemd
sudo pg_ctlcluster 16 main restart         # pg_ctlcluster
```

**Expected downtime:** 2-5 seconds

---

## Common Operations

### Check Current Configuration

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -c "SHOW shared_preload_libraries;"
```

### Check Extension Status

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
SELECT * FROM pg_extension WHERE extname = 'pg_stat_statements';"
```

### View Top 10 Slowest Queries

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
SELECT calls, mean_exec_time::numeric(10,2) as avg_ms, query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC LIMIT 10;"
```

### Reset Statistics

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
SELECT pg_stat_statements_reset();"
```

---

## Backup and Rollback

### Backup Location

All configuration backups: `/etc/postgresql/16/main/backups/`

Format: `postgresql.conf.backup.YYYYMMDD_HHMMSS`

### List Backups

```bash
ls -lah /etc/postgresql/16/main/backups/
```

### Manual Restore

```bash
sudo cp /etc/postgresql/16/main/backups/postgresql.conf.backup.YYYYMMDD_HHMMSS \
     /etc/postgresql/16/main/postgresql.conf
sudo service postgresql restart
```

---

## Troubleshooting

### Run Verification

```bash
bash /workspaces/HRAPP/scripts/verify_pg_stat_statements.sh
```

### Check PostgreSQL Logs

```bash
sudo tail -100 /var/log/postgresql/postgresql-16-main.log
```

### Check PostgreSQL Status

```bash
pg_isready -h localhost
pg_lsclusters
sudo service postgresql status
```

### Common Issues

| Issue | Quick Fix |
|-------|-----------|
| Extension not available | Verify config, restart PostgreSQL |
| Relation does not exist | CREATE EXTENSION pg_stat_statements |
| No statistics appearing | Run queries, wait a moment, check again |
| Permission denied | Use postgres user or grant pg_read_all_stats |

---

## File Permissions

All scripts are executable:

```bash
ls -lah /workspaces/HRAPP/scripts/*pg_stat_statements*.sh
```

If not executable:

```bash
chmod +x /workspaces/HRAPP/scripts/*.sh
```

---

## Package Contents Summary

### Documentation (4 files)
- ✅ PG_STAT_STATEMENTS_SETUP.md (Comprehensive guide)
- ✅ PG_STAT_STATEMENTS_QUICK_REFERENCE.md (Quick reference)
- ✅ PG_STAT_STATEMENTS_ADMIN_REPORT.md (Executive report)
- ✅ PG_STAT_STATEMENTS_INDEX.md (This file)

### Scripts (6 files)
- ✅ enable_pg_stat_statements.sh (Complete setup)
- ✅ update_postgresql_config.sh (Config update only)
- ✅ install_pg_stat_statements_extension.sh (Extension installer)
- ✅ verify_pg_stat_statements.sh (Verification)
- ✅ rollback_pg_stat_statements.sh (Rollback)
- ✅ test_pg_stat_statements_queries.sh (Query tester)

### Backup Directory
- ✅ /etc/postgresql/16/main/backups/ (Created automatically)

**Total:** 10 deliverables

---

## Next Steps

### For First-Time Setup

1. **Review Documentation**
   - Read: `PG_STAT_STATEMENTS_ADMIN_REPORT.md`
   - Understand risks and benefits

2. **Run Installation**
   ```bash
   sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
   ```

3. **Verify**
   ```bash
   bash /workspaces/HRAPP/scripts/verify_pg_stat_statements.sh
   ```

4. **Test**
   ```bash
   bash /workspaces/HRAPP/scripts/test_pg_stat_statements_queries.sh
   ```

### For Ongoing Maintenance

1. **Weekly Review**
   - Check slow queries
   - Identify optimization opportunities

2. **Monthly Maintenance**
   - Export statistics
   - Reset statistics
   - Review configuration

3. **Troubleshooting**
   - Run verification script
   - Check logs
   - Consult documentation

---

## Support Resources

### Internal Documentation
- Comprehensive Setup Guide (PG_STAT_STATEMENTS_SETUP.md)
- Quick Reference (PG_STAT_STATEMENTS_QUICK_REFERENCE.md)
- Admin Report (PG_STAT_STATEMENTS_ADMIN_REPORT.md)

### PostgreSQL Documentation
- Official: https://www.postgresql.org/docs/16/pgstatstatements.html
- Wiki: https://wiki.postgresql.org/wiki/Pg_stat_statements

### Tools
- Verification script: verify_pg_stat_statements.sh
- Query tester: test_pg_stat_statements_queries.sh
- Rollback script: rollback_pg_stat_statements.sh

---

## Version Information

**Package Version:** 1.0
**Created:** 2025-11-14
**PostgreSQL Version:** 16.10
**Extension Version:** 1.10
**Environment:** Ubuntu 24.04 LTS

---

## Safety Checklist

Before running any scripts:

- [ ] PostgreSQL is running and accessible
- [ ] You have appropriate permissions (sudo for config changes)
- [ ] You understand what the script does
- [ ] You have reviewed the documentation
- [ ] You know the rollback procedure
- [ ] Backups will be created automatically
- [ ] Maintenance window scheduled (if required)

After running scripts:

- [ ] Run verification script
- [ ] Check PostgreSQL logs
- [ ] Test basic functionality
- [ ] Verify no performance degradation
- [ ] Update monitoring dashboards
- [ ] Document any issues or deviations

---

## Quick Navigation

| I want to... | Use this... |
|--------------|-------------|
| Install everything now | `enable_pg_stat_statements.sh` |
| Understand the complete process | `PG_STAT_STATEMENTS_SETUP.md` |
| Get management approval | `PG_STAT_STATEMENTS_ADMIN_REPORT.md` |
| Find a specific command | `PG_STAT_STATEMENTS_QUICK_REFERENCE.md` |
| Only update configuration | `update_postgresql_config.sh` |
| Only install extension | `install_pg_stat_statements_extension.sh` |
| Check if it's working | `verify_pg_stat_statements.sh` |
| Test queries interactively | `test_pg_stat_statements_queries.sh` |
| Undo changes | `rollback_pg_stat_statements.sh` |
| Troubleshoot issues | Check logs + run verify script |

---

**For questions or support, refer to the comprehensive documentation files included in this package.**
