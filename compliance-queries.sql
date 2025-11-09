-- =====================================================
-- COMPLIANCE AUDIT QUERIES
-- Phase 2: Regulatory Compliance Reporting
-- =====================================================
--
-- These queries support:
-- - Mauritius Workers' Rights Act compliance
-- - Data Protection Act compliance
-- - MRA Tax audit requirements
-- - Internal security audits
--
-- =====================================================

-- =====================================================
-- QUERY 1: USER ACTIVITY REPORT (Last 30 Days)
-- =====================================================
-- Purpose: Track all actions performed by a specific user
-- Use Case: Employee termination review, security investigation
-- Compliance: Data Protection Act - Right to Access
-- =====================================================

SELECT
    al.id,
    al.performed_at,
    al.action_type,
    al.category,
    al.severity,
    al.entity_type,
    al.entity_id,
    al.old_values,
    al.new_values,
    al.changed_fields,
    al.ip_address,
    al.user_agent,
    al.success,
    al.error_message,
    t.company_name AS tenant_name,
    t.subdomain
FROM audit_logs al
LEFT JOIN tenants t ON al.tenant_id = t.id
WHERE
    al.user_id = 'REPLACE_WITH_USER_ID'::uuid  -- Replace with actual user ID
    AND al.performed_at >= NOW() - INTERVAL '30 days'
ORDER BY al.performed_at DESC
LIMIT 1000;

-- Example usage:
-- Find all actions by a specific employee:
-- WHERE al.user_id = '12345678-1234-1234-1234-123456789012'::uuid

-- =====================================================
-- QUERY 2: FAILED LOGIN ATTEMPTS (Security Monitoring)
-- =====================================================
-- Purpose: Detect brute force attacks, suspicious activity
-- Use Case: Security incident investigation, IP blocking
-- Compliance: Security best practices, incident response
-- =====================================================

SELECT
    al.performed_at,
    al.user_email,
    al.ip_address,
    al.user_agent,
    al.error_message,
    t.company_name AS tenant_name,
    t.subdomain,
    COUNT(*) OVER (PARTITION BY al.ip_address ORDER BY al.performed_at
                   ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS failed_attempts_from_ip,
    COUNT(*) OVER (PARTITION BY al.user_email ORDER BY al.performed_at
                   ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS failed_attempts_for_user
FROM audit_logs al
LEFT JOIN tenants t ON al.tenant_id = t.id
WHERE
    al.action_type = 'LOGIN_FAILED'
    AND al.performed_at >= NOW() - INTERVAL '7 days'
ORDER BY al.performed_at DESC;

-- Identify potential brute force attacks (5+ failed attempts from same IP):
-- Add: HAVING COUNT(*) OVER (...) >= 5

-- =====================================================
-- QUERY 3: SENSITIVE DATA CHANGES (Compliance Audit)
-- =====================================================
-- Purpose: Track changes to salary, banking, personal data
-- Use Case: Payroll audits, MRA tax compliance, data protection
-- Compliance: Workers' Rights Act, Data Protection Act, MRA
-- =====================================================

SELECT
    al.performed_at,
    al.user_email,
    al.action_type,
    al.entity_type,
    al.entity_id,
    al.changed_fields,
    al.old_values,
    al.new_values,
    al.reason,
    t.company_name AS tenant_name,
    t.subdomain,
    al.ip_address,
    -- Flag specific sensitive fields
    CASE
        WHEN al.changed_fields LIKE '%Salary%' OR al.changed_fields LIKE '%Compensation%'
            THEN 'SALARY_CHANGE'
        WHEN al.changed_fields LIKE '%BankAccount%' OR al.changed_fields LIKE '%IBAN%'
            THEN 'BANKING_CHANGE'
        WHEN al.changed_fields LIKE '%Termination%' OR al.changed_fields LIKE '%EmploymentStatus%'
            THEN 'EMPLOYMENT_STATUS_CHANGE'
        WHEN al.changed_fields LIKE '%Role%' OR al.changed_fields LIKE '%Permissions%'
            THEN 'SECURITY_CHANGE'
        ELSE 'OTHER_SENSITIVE'
    END AS change_type
FROM audit_logs al
LEFT JOIN tenants t ON al.tenant_id = t.id
WHERE
    al.severity = 'WARNING'  -- Sensitive fields trigger WARNING severity
    AND al.category = 'DATA_CHANGE'
    AND al.performed_at >= NOW() - INTERVAL '90 days'
ORDER BY al.performed_at DESC;

-- Filter by specific change type:
-- Add: AND change_type = 'SALARY_CHANGE'

-- Filter by specific tenant:
-- Add: AND al.tenant_id = 'REPLACE_WITH_TENANT_ID'::uuid

-- =====================================================
-- QUERY 4: SUPERADMIN ACTIVITY LOG (Accountability)
-- =====================================================
-- Purpose: Track all SuperAdmin actions for oversight
-- Use Case: Internal audits, tenant management review
-- Compliance: Administrative accountability, security oversight
-- =====================================================

SELECT
    al.performed_at,
    al.user_email,
    al.action_type,
    al.category,
    al.severity,
    al.entity_type,
    al.entity_id,
    al.old_values,
    al.new_values,
    al.reason,
    t.company_name AS affected_tenant,
    t.subdomain AS affected_subdomain,
    al.ip_address,
    al.user_agent,
    al.success,
    -- Classify admin actions
    CASE
        WHEN al.action_type LIKE '%TENANT%' THEN 'TENANT_MANAGEMENT'
        WHEN al.action_type LIKE '%SUBSCRIPTION%' THEN 'SUBSCRIPTION_MANAGEMENT'
        WHEN al.action_type LIKE '%SYSTEM%' THEN 'SYSTEM_ADMINISTRATION'
        WHEN al.action_type LIKE '%SECURITY%' THEN 'SECURITY_ADMINISTRATION'
        ELSE 'OTHER_ADMIN_ACTION'
    END AS admin_action_category
FROM audit_logs al
LEFT JOIN tenants t ON al.tenant_id = t.id
WHERE
    al.tenant_id IS NULL  -- SuperAdmin actions have null tenant_id
    OR al.user_email LIKE '%@hrms.com'  -- System admin emails
    OR al.action_type IN (
        'CREATE_TENANT', 'UPDATE_TENANT', 'DELETE_TENANT',
        'SUSPEND_TENANT', 'ACTIVATE_TENANT',
        'SYSTEM_CONFIG_CHANGE'
    )
ORDER BY al.performed_at DESC
LIMIT 500;

-- Filter by specific SuperAdmin:
-- Add: AND al.user_email = 'admin@hrms.com'

-- Filter by action category:
-- Add: AND admin_action_category = 'TENANT_MANAGEMENT'

-- =====================================================
-- QUERY 5: DATA RETENTION VERIFICATION
-- =====================================================
-- Purpose: Verify audit logs are retained for required period
-- Use Case: Compliance verification, storage planning
-- Compliance: 10-year retention requirement
-- =====================================================

SELECT
    DATE_TRUNC('month', performed_at) AS month,
    COUNT(*) AS total_logs,
    COUNT(*) FILTER (WHERE severity = 'INFO') AS info_logs,
    COUNT(*) FILTER (WHERE severity = 'WARNING') AS warning_logs,
    COUNT(*) FILTER (WHERE severity = 'CRITICAL') AS critical_logs,
    COUNT(*) FILTER (WHERE severity = 'EMERGENCY') AS emergency_logs,
    COUNT(*) FILTER (WHERE category = 'AUTHENTICATION') AS auth_logs,
    COUNT(*) FILTER (WHERE category = 'DATA_CHANGE') AS data_change_logs,
    COUNT(*) FILTER (WHERE category = 'HTTP_REQUEST') AS http_logs,
    COUNT(*) FILTER (WHERE success = true) AS successful_operations,
    COUNT(*) FILTER (WHERE success = false) AS failed_operations,
    pg_size_pretty(SUM(LENGTH(old_values::text) + LENGTH(new_values::text))::bigint) AS storage_size
FROM audit_logs
WHERE performed_at >= NOW() - INTERVAL '10 years'
GROUP BY DATE_TRUNC('month', performed_at)
ORDER BY month DESC;

-- =====================================================
-- QUERY 6: ENTITY CHANGE HISTORY
-- =====================================================
-- Purpose: Complete audit trail for a specific entity
-- Use Case: Dispute resolution, payroll verification
-- Compliance: Complete record keeping requirement
-- =====================================================

SELECT
    al.performed_at,
    al.user_email,
    al.action_type,
    al.changed_fields,
    al.old_values,
    al.new_values,
    al.reason,
    al.ip_address,
    t.company_name AS tenant_name
FROM audit_logs al
LEFT JOIN tenants t ON al.tenant_id = t.id
WHERE
    al.entity_type = 'Employee'  -- Replace with entity type
    AND al.entity_id = 'REPLACE_WITH_ENTITY_ID'::uuid  -- Replace with entity ID
ORDER BY al.performed_at DESC;

-- Example: Track all changes to employee salary:
-- WHERE al.entity_type = 'Employee'
--   AND al.entity_id = '12345678-1234-1234-1234-123456789012'::uuid
--   AND (al.changed_fields LIKE '%Salary%' OR al.changed_fields LIKE '%BaseSalary%')

-- =====================================================
-- QUERY 7: HOURLY ACTIVITY HEATMAP (Performance Monitoring)
-- =====================================================
-- Purpose: Identify peak usage times, plan maintenance windows
-- Use Case: Performance optimization, capacity planning
-- =====================================================

SELECT
    DATE_TRUNC('hour', performed_at) AS hour,
    COUNT(*) AS total_operations,
    COUNT(DISTINCT user_id) AS unique_users,
    COUNT(*) FILTER (WHERE success = true) AS successful_ops,
    COUNT(*) FILTER (WHERE success = false) AS failed_ops,
    ROUND(AVG(CASE WHEN success THEN 1 ELSE 0 END) * 100, 2) AS success_rate_pct,
    COUNT(*) FILTER (WHERE category = 'AUTHENTICATION') AS auth_ops,
    COUNT(*) FILTER (WHERE category = 'DATA_CHANGE') AS data_ops,
    COUNT(*) FILTER (WHERE category = 'HTTP_REQUEST') AS http_ops
FROM audit_logs
WHERE performed_at >= NOW() - INTERVAL '7 days'
GROUP BY DATE_TRUNC('hour', performed_at)
ORDER BY hour DESC;

-- =====================================================
-- QUERY 8: COMPLIANCE SUMMARY DASHBOARD
-- =====================================================
-- Purpose: Executive summary for compliance reporting
-- Use Case: Monthly compliance reports, board presentations
-- =====================================================

SELECT
    'Last 30 Days' AS period,
    COUNT(*) AS total_audit_events,
    COUNT(DISTINCT user_id) AS unique_users,
    COUNT(DISTINCT tenant_id) AS active_tenants,
    COUNT(*) FILTER (WHERE severity = 'WARNING' OR severity = 'CRITICAL' OR severity = 'EMERGENCY') AS high_severity_events,
    COUNT(*) FILTER (WHERE action_type LIKE '%LOGIN%') AS authentication_events,
    COUNT(*) FILTER (WHERE action_type LIKE '%CREATE%' OR action_type LIKE '%UPDATE%' OR action_type LIKE '%DELETE%') AS data_modification_events,
    COUNT(*) FILTER (WHERE changed_fields LIKE '%Salary%' OR changed_fields LIKE '%Compensation%' OR changed_fields LIKE '%BankAccount%') AS sensitive_data_changes,
    COUNT(*) FILTER (WHERE success = false) AS failed_operations,
    ROUND(AVG(CASE WHEN success THEN 1 ELSE 0 END) * 100, 2) AS overall_success_rate,
    MAX(performed_at) AS last_audit_entry,
    MIN(performed_at) AS oldest_audit_entry,
    pg_size_pretty(pg_total_relation_size('audit_logs')) AS total_storage_size
FROM audit_logs
WHERE performed_at >= NOW() - INTERVAL '30 days';

-- =====================================================
-- NOTES FOR DATABASE ADMINISTRATORS
-- =====================================================
--
-- 1. INDEXING RECOMMENDATIONS:
--    - CREATE INDEX idx_audit_logs_performed_at ON audit_logs(performed_at DESC);
--    - CREATE INDEX idx_audit_logs_user_id ON audit_logs(user_id) WHERE user_id IS NOT NULL;
--    - CREATE INDEX idx_audit_logs_tenant_id ON audit_logs(tenant_id) WHERE tenant_id IS NOT NULL;
--    - CREATE INDEX idx_audit_logs_action_type ON audit_logs(action_type);
--    - CREATE INDEX idx_audit_logs_severity ON audit_logs(severity) WHERE severity IN ('WARNING', 'CRITICAL', 'EMERGENCY');
--    - CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id) WHERE entity_type IS NOT NULL;
--
-- 2. PARTITIONING RECOMMENDATIONS:
--    - Partition by performed_at (monthly or quarterly) for large deployments
--    - Example: CREATE TABLE audit_logs_2025_01 PARTITION OF audit_logs FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');
--
-- 3. ARCHIVING STRATEGY:
--    - Keep last 2 years in hot storage (fast queries)
--    - Archive 2-10 years to warm storage (slower queries acceptable)
--    - Consider cloud object storage (S3/GCS) for 10+ year compliance retention
--
-- 4. SECURITY NOTES:
--    - Restrict access to audit_logs table (read-only for most users)
--    - Grant INSERT-only access to application service account
--    - NO DELETE OR UPDATE permissions on audit_logs (immutable requirement)
--
-- =====================================================
