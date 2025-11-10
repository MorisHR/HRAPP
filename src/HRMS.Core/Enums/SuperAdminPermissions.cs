namespace HRMS.Core.Enums;

/// <summary>
/// Granular SuperAdmin permissions for role-based access control (RBAC)
/// FORTUNE 500 PATTERN: AWS IAM, Azure RBAC, Google Cloud IAM
///
/// SECURITY PRINCIPLE: Principle of Least Privilege (PoLP)
/// Each SuperAdmin should only have permissions necessary for their role
///
/// AUDIT COMPLIANCE: All permission checks are logged for SOC 2/ISO 27001
/// </summary>
public enum SuperAdminPermission
{
    // ============================================
    // TENANT MANAGEMENT PERMISSIONS (1-10)
    // ============================================

    /// <summary>Create new tenant organizations</summary>
    TENANT_CREATE = 1,

    /// <summary>View tenant details and configurations</summary>
    TENANT_READ = 2,

    /// <summary>Update tenant settings (name, tier, etc.)</summary>
    TENANT_UPDATE = 3,

    /// <summary>Suspend tenant access (reversible)</summary>
    TENANT_SUSPEND = 4,

    /// <summary>Reactivate suspended tenants</summary>
    TENANT_REACTIVATE = 5,

    /// <summary>Soft delete tenants (recoverable)</summary>
    TENANT_SOFT_DELETE = 6,

    /// <summary>CRITICAL: Hard delete tenants (IRREVERSIBLE)</summary>
    TENANT_HARD_DELETE = 7,

    /// <summary>Modify tenant subscription tiers and billing</summary>
    TENANT_BILLING_MANAGE = 8,

    /// <summary>Impersonate tenant admin for support (HIGH RISK)</summary>
    TENANT_IMPERSONATE = 9,

    /// <summary>Export tenant data for compliance</summary>
    TENANT_DATA_EXPORT = 10,

    // ============================================
    // SUPERADMIN USER MANAGEMENT PERMISSIONS (11-20)
    // ============================================

    /// <summary>Create new SuperAdmin accounts</summary>
    SUPERADMIN_CREATE = 11,

    /// <summary>View SuperAdmin user list and details</summary>
    SUPERADMIN_READ = 12,

    /// <summary>Update SuperAdmin profiles and settings</summary>
    SUPERADMIN_UPDATE = 13,

    /// <summary>Deactivate SuperAdmin accounts</summary>
    SUPERADMIN_DEACTIVATE = 14,

    /// <summary>Delete SuperAdmin accounts</summary>
    SUPERADMIN_DELETE = 15,

    /// <summary>Assign/modify SuperAdmin permissions (HIGH RISK)</summary>
    SUPERADMIN_PERMISSIONS_MANAGE = 16,

    /// <summary>Force password reset for SuperAdmins</summary>
    SUPERADMIN_PASSWORD_RESET = 17,

    /// <summary>Unlock locked SuperAdmin accounts</summary>
    SUPERADMIN_UNLOCK = 18,

    /// <summary>Configure IP whitelists for SuperAdmins</summary>
    SUPERADMIN_IP_CONFIG = 19,

    /// <summary>View SuperAdmin activity logs</summary>
    SUPERADMIN_ACTIVITY_READ = 20,

    // ============================================
    // SECURITY & AUDIT PERMISSIONS (21-30)
    // ============================================

    /// <summary>Access audit logs across all tenants</summary>
    AUDIT_LOGS_READ = 21,

    /// <summary>Export audit logs for compliance reporting</summary>
    AUDIT_LOGS_EXPORT = 22,

    /// <summary>View security alerts and incidents</summary>
    SECURITY_ALERTS_READ = 23,

    /// <summary>Manage security alert configurations</summary>
    SECURITY_ALERTS_MANAGE = 24,

    /// <summary>Configure system-wide security policies</summary>
    SECURITY_POLICY_MANAGE = 25,

    /// <summary>Access sensitive security settings</summary>
    SECURITY_SETTINGS_READ = 26,

    /// <summary>Modify critical security settings (HIGH RISK)</summary>
    SECURITY_SETTINGS_WRITE = 27,

    /// <summary>Perform security audits and reviews</summary>
    SECURITY_AUDIT_CONDUCT = 28,

    /// <summary>Manage compliance reports and certifications</summary>
    COMPLIANCE_MANAGE = 29,

    /// <summary>Access personally identifiable information (PII)</summary>
    PII_ACCESS = 30,

    // ============================================
    // SYSTEM ADMINISTRATION PERMISSIONS (31-40)
    // ============================================

    /// <summary>View system configuration and settings</summary>
    SYSTEM_CONFIG_READ = 31,

    /// <summary>Modify system-wide configuration (HIGH RISK)</summary>
    SYSTEM_CONFIG_WRITE = 32,

    /// <summary>Initiate database backups</summary>
    DATABASE_BACKUP = 33,

    /// <summary>Restore from database backups (CRITICAL)</summary>
    DATABASE_RESTORE = 34,

    /// <summary>Enable/disable maintenance mode</summary>
    MAINTENANCE_MODE_TOGGLE = 35,

    /// <summary>View system health metrics and logs</summary>
    SYSTEM_MONITORING_READ = 36,

    /// <summary>Manage email templates and notifications</summary>
    EMAIL_TEMPLATES_MANAGE = 37,

    /// <summary>Generate and revoke API keys</summary>
    API_KEYS_MANAGE = 38,

    /// <summary>Configure integrations and webhooks</summary>
    INTEGRATIONS_MANAGE = 39,

    /// <summary>Access system error logs and diagnostics</summary>
    SYSTEM_DIAGNOSTICS_READ = 40,

    // ============================================
    // BILLING & FINANCIAL PERMISSIONS (41-45)
    // ============================================

    /// <summary>View billing data and invoices</summary>
    BILLING_READ = 41,

    /// <summary>Manage billing and payment settings</summary>
    BILLING_MANAGE = 42,

    /// <summary>Process refunds and adjustments</summary>
    BILLING_REFUNDS = 43,

    /// <summary>View financial reports</summary>
    FINANCIAL_REPORTS_READ = 44,

    /// <summary>Export financial data</summary>
    FINANCIAL_DATA_EXPORT = 45,

    // ============================================
    // SUPPORT & OPERATIONS PERMISSIONS (46-50)
    // ============================================

    /// <summary>Access customer support tickets</summary>
    SUPPORT_TICKETS_READ = 46,

    /// <summary>Respond to support tickets</summary>
    SUPPORT_TICKETS_RESPOND = 47,

    /// <summary>View tenant support requests</summary>
    TENANT_SUPPORT_READ = 48,

    /// <summary>Perform bulk operations (HIGH RISK)</summary>
    BULK_OPERATIONS = 49,

    /// <summary>ALL PERMISSIONS: Master admin (EXTREME RISK)</summary>
    FULL_ACCESS = 50
}

/// <summary>
/// Predefined SuperAdmin roles with permission sets
/// FORTUNE 500 PATTERN: AWS IAM Policies, Azure RBAC Roles
/// </summary>
public static class SuperAdminRoles
{
    /// <summary>
    /// Master Administrator: All permissions (owner/founder only)
    /// RISK LEVEL: EXTREME - Should be limited to 1-2 accounts
    /// </summary>
    public static readonly SuperAdminPermission[] MasterAdmin =
    {
        SuperAdminPermission.FULL_ACCESS
    };

    /// <summary>
    /// Tenant Operations: Manage tenants and basic operations
    /// RISK LEVEL: HIGH - Standard SuperAdmin role
    /// </summary>
    public static readonly SuperAdminPermission[] TenantOperations =
    {
        SuperAdminPermission.TENANT_CREATE,
        SuperAdminPermission.TENANT_READ,
        SuperAdminPermission.TENANT_UPDATE,
        SuperAdminPermission.TENANT_SUSPEND,
        SuperAdminPermission.TENANT_REACTIVATE,
        SuperAdminPermission.TENANT_SOFT_DELETE,
        SuperAdminPermission.TENANT_BILLING_MANAGE,
        SuperAdminPermission.AUDIT_LOGS_READ,
        SuperAdminPermission.SECURITY_ALERTS_READ,
        SuperAdminPermission.SYSTEM_CONFIG_READ,
        SuperAdminPermission.SYSTEM_MONITORING_READ
    };

    /// <summary>
    /// Support Administrator: Customer support and troubleshooting
    /// RISK LEVEL: MEDIUM - Limited to support functions
    /// </summary>
    public static readonly SuperAdminPermission[] SupportAdmin =
    {
        SuperAdminPermission.TENANT_READ,
        SuperAdminPermission.TENANT_IMPERSONATE,
        SuperAdminPermission.SUPPORT_TICKETS_READ,
        SuperAdminPermission.SUPPORT_TICKETS_RESPOND,
        SuperAdminPermission.TENANT_SUPPORT_READ,
        SuperAdminPermission.AUDIT_LOGS_READ,
        SuperAdminPermission.SYSTEM_DIAGNOSTICS_READ
    };

    /// <summary>
    /// Security Auditor: Security monitoring and compliance
    /// RISK LEVEL: MEDIUM - Read-only security access
    /// </summary>
    public static readonly SuperAdminPermission[] SecurityAuditor =
    {
        SuperAdminPermission.AUDIT_LOGS_READ,
        SuperAdminPermission.AUDIT_LOGS_EXPORT,
        SuperAdminPermission.SECURITY_ALERTS_READ,
        SuperAdminPermission.SECURITY_POLICY_MANAGE,
        SuperAdminPermission.SECURITY_SETTINGS_READ,
        SuperAdminPermission.SECURITY_AUDIT_CONDUCT,
        SuperAdminPermission.COMPLIANCE_MANAGE,
        SuperAdminPermission.SYSTEM_MONITORING_READ
    };

    /// <summary>
    /// Billing Manager: Financial and billing operations
    /// RISK LEVEL: MEDIUM - Financial data access
    /// </summary>
    public static readonly SuperAdminPermission[] BillingManager =
    {
        SuperAdminPermission.TENANT_READ,
        SuperAdminPermission.TENANT_BILLING_MANAGE,
        SuperAdminPermission.BILLING_READ,
        SuperAdminPermission.BILLING_MANAGE,
        SuperAdminPermission.BILLING_REFUNDS,
        SuperAdminPermission.FINANCIAL_REPORTS_READ,
        SuperAdminPermission.FINANCIAL_DATA_EXPORT
    };

    /// <summary>
    /// Read-Only Analyst: View-only access for reporting
    /// RISK LEVEL: LOW - No write permissions
    /// </summary>
    public static readonly SuperAdminPermission[] ReadOnlyAnalyst =
    {
        SuperAdminPermission.TENANT_READ,
        SuperAdminPermission.AUDIT_LOGS_READ,
        SuperAdminPermission.SECURITY_ALERTS_READ,
        SuperAdminPermission.SYSTEM_CONFIG_READ,
        SuperAdminPermission.SYSTEM_MONITORING_READ,
        SuperAdminPermission.BILLING_READ,
        SuperAdminPermission.FINANCIAL_REPORTS_READ
    };
}
