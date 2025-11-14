-- ============================================================================
-- CRITICAL FIX: Grant FULL_ACCESS Permission to Existing SuperAdmin
-- ============================================================================
-- Issue: Default SuperAdmin (admin@hrms.com) was created without permissions
-- Impact: SuperAdmin cannot access any protected endpoints (catch-22 scenario)
-- Fix: Grant FULL_ACCESS permission (enum value 50) to resolve bootstrap problem
-- Date: 2025-11-14
-- ============================================================================

-- BEFORE: Check current permissions
SELECT
    "Id",
    "Email",
    "UserName",
    "IsActive",
    "Permissions" AS "CurrentPermissions",
    "CreatedAt"
FROM master."AdminUsers"
WHERE "Email" = 'admin@hrms.com';

-- FIX: Update SuperAdmin with FULL_ACCESS permission
-- FULL_ACCESS = 50 (grants all permissions)
UPDATE master."AdminUsers"
SET
    "Permissions" = '[50]',  -- JSON array containing FULL_ACCESS enum value
    "UpdatedAt" = NOW(),
    "UpdatedBy" = 'System - Permission Bootstrap Fix'
WHERE "Email" = 'admin@hrms.com';

-- AFTER: Verify permissions were applied
SELECT
    "Id",
    "Email",
    "UserName",
    "IsActive",
    "Permissions" AS "NewPermissions",
    "UpdatedAt"
FROM master."AdminUsers"
WHERE "Email" = 'admin@hrms.com';

-- VERIFICATION QUERY
-- This should show: Permissions = '[50]'
-- 50 = SuperAdminPermission.FULL_ACCESS (grants all operations)
