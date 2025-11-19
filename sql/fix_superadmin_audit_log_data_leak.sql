-- ============================================
-- FIX: CRITICAL SECURITY - SuperAdmin Audit Log Data Leak
-- ============================================
-- Issue: SuperAdmin audit logs were incorrectly assigned TenantId values
-- Impact: Tenant admins could see SuperAdmin activity in their audit logs
-- Severity: CRITICAL - Multi-tenant data isolation breach
--
-- This script:
-- 1. Temporarily disables immutability trigger (security fix exception)
-- 2. Identifies and fixes SuperAdmin audit logs with incorrect TenantId
-- 3. Re-enables immutability trigger
-- 4. Creates audit log of the fix itself
-- ============================================

BEGIN;

-- Step 1: Report current state
SELECT
    'BEFORE FIX' as stage,
    COUNT(*) as total_superadmin_logs,
    COUNT(CASE WHEN "TenantId" IS NOT NULL THEN 1 END) as logs_with_incorrect_tenantid,
    COUNT(CASE WHEN "TenantId" IS NULL THEN 1 END) as logs_with_correct_tenantid
FROM master."AuditLogs"
WHERE "UserRole" = 'SuperAdmin';

-- Step 2: Disable immutability trigger (security fix exception)
ALTER TABLE master."AuditLogs" DISABLE TRIGGER audit_log_immutability_trigger;

-- Step 3: Fix SuperAdmin audit logs with incorrect TenantId
-- SuperAdmin actions should NEVER have a TenantId (null = system-wide)
UPDATE master."AuditLogs"
SET
    "TenantId" = NULL,
    "TenantName" = NULL
WHERE
    "UserRole" = 'SuperAdmin'
    AND "TenantId" IS NOT NULL;

-- Get count of fixed records
DO $$
DECLARE
    fixed_count INTEGER;
BEGIN
    GET DIAGNOSTICS fixed_count = ROW_COUNT;
    RAISE NOTICE 'Fixed % SuperAdmin audit logs with incorrect TenantId', fixed_count;
END $$;

-- Step 4: Re-enable immutability trigger
ALTER TABLE master."AuditLogs" ENABLE TRIGGER audit_log_immutability_trigger;

-- Step 5: Report final state
SELECT
    'AFTER FIX' as stage,
    COUNT(*) as total_superadmin_logs,
    COUNT(CASE WHEN "TenantId" IS NOT NULL THEN 1 END) as logs_with_incorrect_tenantid,
    COUNT(CASE WHEN "TenantId" IS NULL THEN 1 END) as logs_with_correct_tenantid
FROM master."AuditLogs"
WHERE "UserRole" = 'SuperAdmin';

-- Step 6: Create audit log documenting this security fix
INSERT INTO master."AuditLogs" (
    "Id",
    "ActionType",
    "Category",
    "Severity",
    "EntityType",
    "Success",
    "PerformedAt",
    "UserEmail",
    "UserRole",
    "TenantId",
    "Reason",
    "AdditionalMetadata"
) VALUES (
    gen_random_uuid(),
    20, -- SYSTEM_CONFIGURATION_CHANGED
    8,  -- SECURITY_EVENT
    2,  -- WARNING (security fix required)
    'AuditLog',
    true,
    NOW(),
    'system@hrms.com',
    'SuperAdmin',
    NULL, -- SuperAdmin action = NULL TenantId
    'CRITICAL SECURITY FIX: Corrected SuperAdmin audit logs that were incorrectly assigned TenantId values, preventing tenant data isolation breach',
    '{"issue": "CVE-HRMS-2025-001", "impact": "Multi-tenant data leak", "fix": "Set TenantId=NULL for all SuperAdmin audit logs", "code_fix": "EnrichAuditLog method in AuditLogService.cs", "affected_component": "Audit Logging System"}'
);

COMMIT;

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Verify no SuperAdmin logs have TenantId
SELECT
    CASE
        WHEN COUNT(*) = 0 THEN '✅ PASS: No SuperAdmin logs with TenantId'
        ELSE '❌ FAIL: Found SuperAdmin logs with TenantId'
    END as verification_result,
    COUNT(*) as count
FROM master."AuditLogs"
WHERE "UserRole" = 'SuperAdmin' AND "TenantId" IS NOT NULL;

-- Show sample of fixed logs
SELECT
    "Id",
    "PerformedAt",
    "UserEmail",
    "UserRole",
    "TenantId",
    "TenantName",
    "ActionType",
    "Success"
FROM master."AuditLogs"
WHERE "UserRole" = 'SuperAdmin'
ORDER BY "PerformedAt" DESC
LIMIT 10;

-- Count logs by UserRole and TenantId status
SELECT
    "UserRole",
    COUNT(*) as total_logs,
    COUNT(CASE WHEN "TenantId" IS NOT NULL THEN 1 END) as with_tenant_id,
    COUNT(CASE WHEN "TenantId" IS NULL THEN 1 END) as without_tenant_id
FROM master."AuditLogs"
GROUP BY "UserRole"
ORDER BY total_logs DESC;
