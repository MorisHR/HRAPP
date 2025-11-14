# pg_stat_statements Quick Reference Guide

## Current System Status

**PostgreSQL Version:** 16.10 (Ubuntu 16.10-0ubuntu0.24.04.1)
**Configuration File:** `/etc/postgresql/16/main/postgresql.conf`
**Extension Available:** Yes (version 1.10)
**Current Status:** NOT CONFIGURED (shared_preload_libraries is empty)

## Quick Start (One Command)

```bash
sudo bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
```

This automated script will:
1. Backup your configuration
2. Update postgresql.conf
3. Restart PostgreSQL (with your confirmation)
4. Install extension in databases
5. Verify everything works

## Manual Step-by-Step

### 1. Update Configuration

```bash
sudo bash /workspaces/HRAPP/scripts/update_postgresql_config.sh
```

This will:
- Backup current configuration to `/etc/postgresql/16/main/backups/`
- Update `shared_preload_libraries = 'pg_stat_statements'`
- Add recommended configuration parameters

### 2. Restart PostgreSQL

Choose ONE method:

```bash
# For containers (recommended for this environment)
sudo service postgresql restart

# For systemd
sudo systemctl restart postgresql@16-main

# Using pg_ctlcluster
sudo pg_ctlcluster 16 main restart
```

### 3. Verify Configuration

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -c "SHOW shared_preload_libraries;"
```

Expected output: `pg_stat_statements`

### 4. Install Extension in Databases

```bash
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh
```

Or manually for a specific database:

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "CREATE EXTENSION pg_stat_statements;"
```

### 5. Verify Installation

```bash
bash /workspaces/HRAPP/scripts/verify_pg_stat_statements.sh
```

## Configuration Details

### Required Changes to postgresql.conf

Find line 747 and change:

**From:**
```
#shared_preload_libraries = ''	# (change requires restart)
```

**To:**
```
shared_preload_libraries = 'pg_stat_statements'	# (change requires restart)
```

### Recommended Additional Settings

Add after the shared_preload_libraries line:

```
# pg_stat_statements configuration
pg_stat_statements.max = 10000                    # Maximum number of statements tracked
pg_stat_statements.track = all                     # Track all statements (top-level and nested)
pg_stat_statements.track_utility = on              # Track utility commands (CREATE, DROP, etc.)
pg_stat_statements.track_planning = on             # Track query planning statistics
```

## Essential Queries

### Top 10 Slowest Queries (Average Time)

```sql
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    total_exec_time::numeric(10,2) as total_ms,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

### Most Frequently Executed Queries

```sql
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY calls DESC
LIMIT 10;
```

### Queries with Most Total Time

```sql
SELECT
    calls,
    total_exec_time::numeric(10,2) as total_ms,
    mean_exec_time::numeric(10,2) as avg_ms,
    (total_exec_time / sum(total_exec_time) OVER ()) * 100 as pct_total,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY total_exec_time DESC
LIMIT 10;
```

### Queries with High I/O

```sql
SELECT
    calls,
    (shared_blks_read + shared_blks_written) as total_io,
    shared_blks_read,
    shared_blks_written,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY (shared_blks_read + shared_blks_written) DESC
LIMIT 10;
```

### Queries with Cache Misses

```sql
SELECT
    calls,
    shared_blks_hit,
    shared_blks_read,
    CASE
        WHEN (shared_blks_hit + shared_blks_read) > 0
        THEN (shared_blks_hit::float / (shared_blks_hit + shared_blks_read) * 100)::numeric(5,2)
        ELSE 0
    END as cache_hit_ratio,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
  AND (shared_blks_hit + shared_blks_read) > 0
ORDER BY cache_hit_ratio ASC
LIMIT 10;
```

### Reset Statistics

```sql
SELECT pg_stat_statements_reset();
```

## Available Scripts

All scripts are located in `/workspaces/HRAPP/scripts/`

| Script | Purpose | Requires Sudo |
|--------|---------|---------------|
| `enable_pg_stat_statements.sh` | Complete automated setup | Yes |
| `update_postgresql_config.sh` | Update postgresql.conf only | Yes |
| `install_pg_stat_statements_extension.sh` | Install extension in databases | No |
| `verify_pg_stat_statements.sh` | Verify installation | No |
| `rollback_pg_stat_statements.sh` | Rollback configuration changes | Yes |

## Common Issues and Solutions

### Issue: "extension pg_stat_statements is not available"

**Cause:** shared_preload_libraries not configured or PostgreSQL not restarted

**Solution:**
```bash
# 1. Update config
sudo bash /workspaces/HRAPP/scripts/update_postgresql_config.sh

# 2. Restart PostgreSQL
sudo service postgresql restart

# 3. Verify
PGPASSWORD=postgres psql -h localhost -U postgres -c "SHOW shared_preload_libraries;"
```

### Issue: "relation pg_stat_statements does not exist"

**Cause:** Extension not created in the current database

**Solution:**
```bash
# Install in specific database
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "CREATE EXTENSION pg_stat_statements;"

# Or install in all databases
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh
```

### Issue: No statistics appearing

**Cause:** Just installed, no queries run yet

**Solution:** Run some queries and wait a moment:
```sql
-- Run test query
SELECT COUNT(*) FROM your_table;

-- Check statistics
SELECT COUNT(*) FROM pg_stat_statements;
```

### Issue: Want to rollback changes

**Solution:**
```bash
sudo bash /workspaces/HRAPP/scripts/rollback_pg_stat_statements.sh
```

## Performance Impact

- **CPU Overhead:** < 1% typically
- **Memory Usage:** ~600 bytes per tracked statement
  - 5,000 statements = ~3 MB
  - 10,000 statements = ~6 MB
  - 50,000 statements = ~30 MB
- **Disk I/O:** Negligible

## Security Considerations

1. **Query Visibility:** pg_stat_statements shows actual queries
2. **Access Control:** Ensure only authorized users can query it
3. **Sensitive Data:** Consider resetting statistics periodically

```sql
-- Grant access to monitoring user
GRANT pg_read_all_stats TO monitoring_user;

-- Reset statistics monthly
SELECT pg_stat_statements_reset();
```

## Rollback Process

### Quick Rollback

```bash
sudo bash /workspaces/HRAPP/scripts/rollback_pg_stat_statements.sh
```

### Manual Rollback

```bash
# 1. List backups
ls -lah /etc/postgresql/16/main/backups/

# 2. Restore specific backup
sudo cp /etc/postgresql/16/main/backups/postgresql.conf.backup.YYYYMMDD_HHMMSS /etc/postgresql/16/main/postgresql.conf

# 3. Restart PostgreSQL
sudo service postgresql restart

# 4. Verify
PGPASSWORD=postgres psql -h localhost -U postgres -c "SHOW shared_preload_libraries;"
```

### Remove Extension from Databases

```sql
DROP EXTENSION IF EXISTS pg_stat_statements;
```

## Integration Examples

### Bash Script Monitoring

```bash
#!/bin/bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 10;" > slow_queries_$(date +%Y%m%d).txt
```

### Grafana Query

```sql
SELECT
  $__timeGroup(now(), '5m') as time,
  query,
  calls,
  mean_exec_time,
  stddev_exec_time
FROM pg_stat_statements
WHERE $__timeFilter(now())
  AND query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 20
```

### Export to CSV

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
COPY (
    SELECT * FROM pg_stat_statements
) TO STDOUT CSV HEADER" > pg_stats_$(date +%Y%m%d).csv
```

## Maintenance Tasks

### Weekly

- Review top 10 slowest queries
- Identify optimization opportunities
- Check for query pattern changes

```bash
# Quick weekly report
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "
SELECT
    'Slowest Queries' as metric,
    COUNT(*) FILTER (WHERE mean_exec_time > 1000) as over_1s,
    COUNT(*) FILTER (WHERE mean_exec_time > 100) as over_100ms,
    COUNT(*) as total_tracked
FROM pg_stat_statements;"
```

### Monthly

- Export statistics for analysis
- Reset statistics for fresh baseline
- Review configuration parameters

```bash
# Monthly cleanup
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "SELECT pg_stat_statements_reset();"
```

## Additional Resources

- **Full Documentation:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md`
- **Official Docs:** https://www.postgresql.org/docs/16/pgstatstatements.html
- **PostgreSQL Performance Tuning:** https://wiki.postgresql.org/wiki/Performance_Optimization

## Support Commands

```bash
# Check PostgreSQL status
pg_isready -h localhost -p 5432
pg_lsclusters

# View PostgreSQL logs
sudo tail -100 /var/log/postgresql/postgresql-16-main.log

# Check configuration
grep "shared_preload_libraries" /etc/postgresql/16/main/postgresql.conf

# Test connection
PGPASSWORD=postgres psql -h localhost -U postgres -c "SELECT version();"
```

## Summary of Key Files

- **Main Documentation:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_SETUP.md`
- **Quick Reference:** `/workspaces/HRAPP/PG_STAT_STATEMENTS_QUICK_REFERENCE.md` (this file)
- **Config File:** `/etc/postgresql/16/main/postgresql.conf`
- **Backup Directory:** `/etc/postgresql/16/main/backups/`
- **Scripts Directory:** `/workspaces/HRAPP/scripts/`

---

**Last Updated:** 2025-11-14
**PostgreSQL Version:** 16.10
