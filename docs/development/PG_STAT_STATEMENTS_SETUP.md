# PostgreSQL pg_stat_statements Configuration Guide

## Executive Summary

This document provides comprehensive instructions for enabling and configuring the `pg_stat_statements` extension in PostgreSQL 16 for query performance tracking and monitoring.

**Current Status:**
- PostgreSQL Version: 16.10 (Ubuntu 16.10-0ubuntu0.24.04.1)
- Cluster: 16/main running on port 5432
- Config File: `/etc/postgresql/16/main/postgresql.conf`
- Data Directory: `/var/lib/postgresql/16/main`
- Extension Available: Yes (version 1.10)
- Extension Installed: No
- shared_preload_libraries: Currently empty (not configured)

## What is pg_stat_statements?

`pg_stat_statements` is a PostgreSQL extension that tracks planning and execution statistics for all SQL statements executed by the server. It provides invaluable insights into:

- Query performance and execution times
- Most frequently executed queries
- Most resource-intensive queries
- Query plan statistics
- I/O statistics per query

## Configuration Changes Required

### Step 1: Update postgresql.conf

The `shared_preload_libraries` parameter must be set to preload the pg_stat_statements library. This parameter requires a PostgreSQL restart to take effect.

**Location:** `/etc/postgresql/16/main/postgresql.conf`

**Find line 747:**
```
#shared_preload_libraries = ''	# (change requires restart)
```

**Change to:**
```
shared_preload_libraries = 'pg_stat_statements'	# (change requires restart)
```

### Step 2: Optional Configuration Parameters

You can also add these optional parameters to customize pg_stat_statements behavior. Add these lines after the shared_preload_libraries setting:

```
# pg_stat_statements configuration
pg_stat_statements.max = 10000                    # Maximum number of statements tracked
pg_stat_statements.track = all                     # Track all statements (top-level and nested)
pg_stat_statements.track_utility = on              # Track utility commands (CREATE, DROP, etc.)
pg_stat_statements.track_planning = on             # Track query planning statistics
```

**Parameter Explanations:**

- `pg_stat_statements.max`: Maximum number of statements tracked (default: 5000)
  - Increase for larger applications with many unique queries
  - Each entry uses approximately 600 bytes of shared memory

- `pg_stat_statements.track`: Which statements to track
  - `top`: Only top-level statements (default)
  - `all`: All statements including nested (functions, triggers)
  - `none`: Disable tracking

- `pg_stat_statements.track_utility`: Track utility commands
  - `on`: Track DDL statements (CREATE, DROP, ALTER, etc.)
  - `off`: Only track DML statements (SELECT, INSERT, UPDATE, DELETE)

- `pg_stat_statements.track_planning`: Track query planning time
  - `on`: Track both planning and execution time
  - `off`: Only track execution time

## Installation Steps

### Automated Installation (Recommended)

Use the provided script for safe, automated configuration:

```bash
bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh
```

### Manual Installation

If you prefer manual installation, follow these steps:

#### 1. Backup Current Configuration

```bash
sudo cp /etc/postgresql/16/main/postgresql.conf /etc/postgresql/16/main/postgresql.conf.backup.$(date +%Y%m%d_%H%M%S)
```

#### 2. Update postgresql.conf

You can use the update script or edit manually:

**Option A: Using the update script**
```bash
bash /workspaces/HRAPP/scripts/update_postgresql_config.sh
```

**Option B: Manual edit**
```bash
sudo nano /etc/postgresql/16/main/postgresql.conf
# Find line 747 and uncomment/modify as shown above
```

#### 3. Restart PostgreSQL

**Important:** This will cause a brief service interruption!

For systems with systemd:
```bash
sudo systemctl restart postgresql@16-main
```

For container environments (like this one):
```bash
sudo service postgresql restart
```

Or using pg_ctlcluster:
```bash
sudo pg_ctlcluster 16 main restart
```

#### 4. Verify Configuration

After restart, verify the configuration:

```bash
PGPASSWORD=postgres psql -h localhost -U postgres -d postgres -c "SHOW shared_preload_libraries;"
```

Expected output:
```
 shared_preload_libraries
--------------------------
 pg_stat_statements
(1 row)
```

#### 5. Create the Extension

The extension must be created in each database where you want to use it:

```sql
-- Connect to your database
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database_name

-- Create the extension
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Verify installation
SELECT * FROM pg_stat_statements LIMIT 1;
```

For the HRMS application, you'll need to create it in each tenant database. Use the helper script:

```bash
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh
```

### Verification Steps

#### 1. Check Extension is Loaded

```sql
SELECT * FROM pg_available_extensions WHERE name = 'pg_stat_statements';
```

#### 2. Check Extension is Installed in Database

```sql
SELECT extname, extversion FROM pg_extension WHERE extname = 'pg_stat_statements';
```

#### 3. Test Query Tracking

```sql
-- Run a test query
SELECT COUNT(*) FROM pg_stat_statements;

-- View top 10 queries by total execution time
SELECT
    calls,
    total_exec_time,
    mean_exec_time,
    query
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;
```

## Using pg_stat_statements

### Basic Queries

#### Top 10 Slowest Queries (by average time)

```sql
SELECT
    calls,
    mean_exec_time::numeric(10,2) as avg_ms,
    total_exec_time::numeric(10,2) as total_ms,
    (total_exec_time / sum(total_exec_time) OVER ()) * 100 as pct_total,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

#### Most Frequently Executed Queries

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

#### Queries with Most Total Time

```sql
SELECT
    calls,
    total_exec_time::numeric(10,2) as total_ms,
    mean_exec_time::numeric(10,2) as avg_ms,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY total_exec_time DESC
LIMIT 10;
```

#### Queries with Most I/O

```sql
SELECT
    calls,
    shared_blks_read + shared_blks_written as total_io,
    query
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY (shared_blks_read + shared_blks_written) DESC
LIMIT 10;
```

### Reset Statistics

To reset all statistics:

```sql
SELECT pg_stat_statements_reset();
```

### View All Available Columns

```sql
\d pg_stat_statements
```

## Rollback Instructions

If you need to revert the changes:

### 1. Restore Original Configuration

```bash
# Find your backup file
ls -la /etc/postgresql/16/main/postgresql.conf.backup.*

# Restore the backup
sudo cp /etc/postgresql/16/main/postgresql.conf.backup.YYYYMMDD_HHMMSS /etc/postgresql/16/main/postgresql.conf
```

### 2. Restart PostgreSQL

```bash
sudo service postgresql restart
```

### 3. Optionally Remove Extension from Databases

```sql
DROP EXTENSION IF EXISTS pg_stat_statements;
```

## Risks and Considerations

### Performance Impact

**Minimal Impact:**
- CPU overhead: < 1% in most cases
- Memory usage: ~600 bytes per tracked statement
- Disk I/O: Negligible

**Recommended Settings for Production:**
- Set `pg_stat_statements.max` based on your query diversity
- Start with 10,000 for medium applications
- Monitor shared memory usage

### Memory Considerations

Total memory used = `pg_stat_statements.max * 600 bytes`

Examples:
- 5,000 statements = ~3 MB
- 10,000 statements = ~6 MB
- 50,000 statements = ~30 MB

### Query Text Truncation

- Queries longer than `track_activity_query_size` (default: 1024 bytes) will be truncated
- Increase `track_activity_query_size` if needed (requires restart)
- Balance between visibility and memory usage

### Security Considerations

1. **Query Exposure:** pg_stat_statements shows actual queries with parameter values
   - Ensure only authorized users have access
   - Consider using `pg_stat_statements_reset()` periodically for sensitive data

2. **Permissions:** Grant appropriate access:
   ```sql
   -- Grant to monitoring user
   GRANT EXECUTE ON FUNCTION pg_stat_statements_reset() TO monitoring_user;
   ```

3. **Monitoring Tools:** Integrate with monitoring solutions:
   - Grafana + Prometheus
   - pgAdmin
   - DataDog
   - New Relic

### Restart Requirement

**CRITICAL:** Changing `shared_preload_libraries` requires a full PostgreSQL restart, which will:
- Disconnect all active connections
- Cause brief service downtime (typically 2-5 seconds)
- Interrupt running transactions

**Mitigation:**
- Schedule during maintenance window
- Notify users in advance
- Ensure application has connection retry logic
- Use connection pooling (pgBouncer, PgPool-II)

## Multi-Tenant Considerations

For the HRMS application with tenant databases:

### Install in All Tenant Databases

```bash
# Use the provided script
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh

# Or manually for each tenant
PGPASSWORD=postgres psql -h localhost -U postgres -d tenant_db_name -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements;"
```

### Query Across All Tenants

You'll need to query each database separately, or use a monitoring database with foreign data wrappers (advanced setup).

## Monitoring and Maintenance

### Regular Maintenance Tasks

1. **Review Statistics Weekly**
   - Identify slow queries
   - Find optimization opportunities
   - Track query pattern changes

2. **Reset Statistics Monthly**
   - Prevents unbounded growth
   - Provides fresh baseline
   - Recommended before major releases

3. **Export Statistics for Analysis**
   ```bash
   PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "COPY (SELECT * FROM pg_stat_statements) TO STDOUT CSV HEADER" > pg_stats_$(date +%Y%m%d).csv
   ```

### Integration with Monitoring Tools

Example Grafana query:
```sql
SELECT
  query,
  calls,
  mean_exec_time,
  stddev_exec_time
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 20;
```

## Troubleshooting

### Issue: Extension not available after restart

**Solution:**
```bash
# Verify the library file exists
ls -la /usr/lib/postgresql/16/lib/pg_stat_statements.so

# Check PostgreSQL logs
sudo tail -100 /var/log/postgresql/postgresql-16-main.log

# Verify configuration
PGPASSWORD=postgres psql -h localhost -U postgres -c "SHOW shared_preload_libraries;"
```

### Issue: Permission denied when creating extension

**Solution:**
```bash
# Must be superuser or database owner
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "CREATE EXTENSION pg_stat_statements;"
```

### Issue: Statistics not appearing

**Solution:**
```sql
-- Ensure extension is created in the current database
\dx

-- Run some queries to generate statistics
SELECT 1;

-- Check again
SELECT COUNT(*) FROM pg_stat_statements;
```

## Additional Resources

- Official Documentation: https://www.postgresql.org/docs/16/pgstatstatements.html
- Performance Tuning: https://www.postgresql.org/docs/16/runtime-config-query.html
- Best Practices: https://wiki.postgresql.org/wiki/Query_Optimization

## Support and Questions

For questions or issues:
1. Check PostgreSQL logs: `/var/log/postgresql/postgresql-16-main.log`
2. Review configuration: `/etc/postgresql/16/main/postgresql.conf`
3. Verify cluster status: `pg_lsclusters`
4. Test connection: `pg_isready -h localhost -p 5432`

## Summary of Commands

```bash
# Backup configuration
sudo cp /etc/postgresql/16/main/postgresql.conf /etc/postgresql/16/main/postgresql.conf.backup.$(date +%Y%m%d_%H%M%S)

# Run automated setup (recommended)
bash /workspaces/HRAPP/scripts/enable_pg_stat_statements.sh

# Or manual steps:
bash /workspaces/HRAPP/scripts/update_postgresql_config.sh
sudo service postgresql restart
PGPASSWORD=postgres psql -h localhost -U postgres -d postgres -c "SHOW shared_preload_libraries;"
bash /workspaces/HRAPP/scripts/install_pg_stat_statements_extension.sh

# Verify
PGPASSWORD=postgres psql -h localhost -U postgres -d your_database -c "SELECT COUNT(*) FROM pg_stat_statements;"
```

---

**Document Version:** 1.0
**Last Updated:** 2025-11-14
**PostgreSQL Version:** 16.10
**Environment:** Ubuntu 24.04 LTS
