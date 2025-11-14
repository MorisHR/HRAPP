-- =====================================================
-- AUDIT LOGS TABLE PARTITIONING
-- =====================================================
-- Purpose: Convert AuditLogs to partitioned table by month
-- Impact: Improves query performance and enables efficient archival
-- Estimated Performance Gain: 40-60% on date-range queries
-- =====================================================

-- Step 1: Check current AuditLogs data distribution
SELECT
    DATE_TRUNC('month', "PerformedAt") AS month,
    COUNT(*) AS record_count,
    pg_size_pretty(SUM(pg_column_size("AuditLogs".*))) AS estimated_size
FROM master."AuditLogs"
GROUP BY DATE_TRUNC('month', "PerformedAt")
ORDER BY month DESC;

-- Step 2: Create new partitioned table
CREATE TABLE IF NOT EXISTS master."AuditLogs_Partitioned" (
    LIKE master."AuditLogs" INCLUDING ALL
) PARTITION BY RANGE ("PerformedAt");

-- Step 3: Create partitions for existing data
-- Note: Adjust date ranges based on your actual data
-- Current month partition
CREATE TABLE IF NOT EXISTS master."AuditLogs_2025_11" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2025-11-01') TO ('2025-12-01');

-- Previous month partition
CREATE TABLE IF NOT EXISTS master."AuditLogs_2025_10" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2025-10-01') TO ('2025-11-01');

-- Older data partition (catch-all for historical data)
CREATE TABLE IF NOT EXISTS master."AuditLogs_2025_09_and_earlier" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2000-01-01') TO ('2025-10-01');

-- Future partitions (next 3 months)
CREATE TABLE IF NOT EXISTS master."AuditLogs_2025_12" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2025-12-01') TO ('2026-01-01');

CREATE TABLE IF NOT EXISTS master."AuditLogs_2026_01" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE IF NOT EXISTS master."AuditLogs_2026_02" PARTITION OF master."AuditLogs_Partitioned"
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');

-- Step 4: Copy indexes to partitioned table (will be inherited by all partitions)
-- Primary Key
ALTER TABLE master."AuditLogs_Partitioned"
    ADD CONSTRAINT "PK_AuditLogs_Partitioned" PRIMARY KEY ("Id", "PerformedAt");

-- Composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_TenantId_Category_PerformedAt"
    ON master."AuditLogs_Partitioned" ("TenantId", "Category", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_TenantId_PerformedAt"
    ON master."AuditLogs_Partitioned" ("TenantId", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_UserId_PerformedAt"
    ON master."AuditLogs_Partitioned" ("UserId", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_Category_PerformedAt"
    ON master."AuditLogs_Partitioned" ("Category", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_Success_PerformedAt"
    ON master."AuditLogs_Partitioned" ("Success", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_IsArchived_PerformedAt"
    ON master."AuditLogs_Partitioned" ("IsArchived", "PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_EntityType_EntityId"
    ON master."AuditLogs_Partitioned" ("EntityType", "EntityId");

-- Single-column indexes
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_ActionType"
    ON master."AuditLogs_Partitioned" ("ActionType");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_Category"
    ON master."AuditLogs_Partitioned" ("Category");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_PerformedAt"
    ON master."AuditLogs_Partitioned" ("PerformedAt");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_SessionId"
    ON master."AuditLogs_Partitioned" ("SessionId");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_Severity"
    ON master."AuditLogs_Partitioned" ("Severity");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Part_CorrelationId"
    ON master."AuditLogs_Partitioned" ("CorrelationId");

-- Step 5: Create trigger for immutability (same as original)
CREATE OR REPLACE FUNCTION master.prevent_audit_log_modification_partitioned()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Audit logs cannot be modified or deleted. Table: %, Operation: %',
        TG_TABLE_NAME, TG_OP;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_log_immutability_trigger_partitioned
    BEFORE DELETE OR UPDATE ON master."AuditLogs_Partitioned"
    FOR EACH ROW
    EXECUTE FUNCTION master.prevent_audit_log_modification_partitioned();

-- Step 6: Migration plan (to be executed during maintenance window)
-- NOTE: This is a MANUAL step - review before executing!

/*
-- MIGRATION STEPS (Execute in transaction):
BEGIN;

-- 6.1: Rename original table
ALTER TABLE master."AuditLogs" RENAME TO "AuditLogs_Old";

-- 6.2: Rename partitioned table to original name
ALTER TABLE master."AuditLogs_Partitioned" RENAME TO "AuditLogs";

-- 6.3: Copy data from old table to new partitioned table
INSERT INTO master."AuditLogs"
SELECT * FROM master."AuditLogs_Old"
ORDER BY "PerformedAt";

-- 6.4: Verify data integrity
SELECT
    'Old Table' AS source, COUNT(*) AS count FROM master."AuditLogs_Old"
UNION ALL
SELECT
    'New Table' AS source, COUNT(*) AS count FROM master."AuditLogs";

-- 6.5: If counts match, commit; otherwise, rollback
-- COMMIT; -- Only after verification
-- ROLLBACK; -- If there are issues

-- 6.6: After successful migration, drop old table (after backup!)
-- DROP TABLE master."AuditLogs_Old";
*/

-- Step 7: Verify partitions
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE tablename LIKE 'AuditLogs%'
ORDER BY tablename;

-- Step 8: Query to verify partition pruning is working
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM master."AuditLogs_Partitioned"
WHERE "PerformedAt" >= '2025-11-01'
  AND "PerformedAt" < '2025-12-01';

-- Expected: Should show "Partitions pruned: X" in the query plan
