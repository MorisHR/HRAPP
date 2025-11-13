namespace HRMS.Core.Constants;

/// <summary>
/// Standard permission constants for AdminUser granular RBAC
/// Used with RequirePermissionAttribute to enforce permission-based access control
/// </summary>
public static class Permissions
{
    // ============================================
    // TENANT MANAGEMENT PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to create new tenants
    /// CRITICAL: Allows provisioning new company accounts with database schemas
    /// </summary>
    public const string TENANT_CREATE = "TENANT_CREATE";

    /// <summary>
    /// Permission to view tenant details
    /// Allows viewing tenant information and subscription details
    /// </summary>
    public const string TENANT_VIEW = "TENANT_VIEW";

    /// <summary>
    /// Permission to update tenant information
    /// Allows modifying company details, subscription tiers, and settings
    /// </summary>
    public const string TENANT_UPDATE = "TENANT_UPDATE";

    /// <summary>
    /// Permission to suspend tenants
    /// CRITICAL: Temporarily blocks tenant access
    /// </summary>
    public const string TENANT_SUSPEND = "TENANT_SUSPEND";

    /// <summary>
    /// Permission to soft delete tenants
    /// CRITICAL: Marks tenant for deletion with 30-day grace period
    /// </summary>
    public const string TENANT_DELETE = "TENANT_DELETE";

    /// <summary>
    /// Permission to permanently delete tenants
    /// HIGHLY CRITICAL: IRREVERSIBLE operation - destroys all tenant data
    /// </summary>
    public const string TENANT_HARD_DELETE = "TENANT_HARD_DELETE";

    /// <summary>
    /// Permission to reactivate suspended/deleted tenants
    /// Allows restoring tenant access
    /// </summary>
    public const string TENANT_REACTIVATE = "TENANT_REACTIVATE";

    // ============================================
    // ADMIN USER MANAGEMENT PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to create new SuperAdmin users
    /// CRITICAL: Allows creating users with platform-wide access
    /// </summary>
    public const string ADMIN_CREATE = "ADMIN_CREATE";

    /// <summary>
    /// Permission to view SuperAdmin user details
    /// Allows viewing admin user accounts and permissions
    /// </summary>
    public const string ADMIN_VIEW = "ADMIN_VIEW";

    /// <summary>
    /// Permission to update SuperAdmin users
    /// Allows modifying admin details, permissions, and settings
    /// </summary>
    public const string ADMIN_UPDATE = "ADMIN_UPDATE";

    /// <summary>
    /// Permission to delete SuperAdmin users
    /// CRITICAL: Removes platform admin access
    /// </summary>
    public const string ADMIN_DELETE = "ADMIN_DELETE";

    /// <summary>
    /// Permission to modify admin user permissions
    /// CRITICAL: Allows granting/revoking admin permissions (privilege escalation)
    /// </summary>
    public const string ADMIN_MANAGE_PERMISSIONS = "ADMIN_MANAGE_PERMISSIONS";

    // ============================================
    // SECURITY & AUDIT PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to view audit logs
    /// Allows accessing compliance and security audit trail
    /// </summary>
    public const string AUDIT_LOGS_VIEW = "AUDIT_LOGS_VIEW";

    /// <summary>
    /// Permission to export audit logs
    /// Allows downloading audit data for compliance reporting
    /// </summary>
    public const string AUDIT_LOGS_EXPORT = "AUDIT_LOGS_EXPORT";

    /// <summary>
    /// Permission to view security alerts
    /// Allows monitoring security incidents and anomalies
    /// </summary>
    public const string SECURITY_ALERTS_VIEW = "SECURITY_ALERTS_VIEW";

    /// <summary>
    /// Permission to manage security settings
    /// CRITICAL: Allows modifying security policies and configurations
    /// </summary>
    public const string SECURITY_MANAGE = "SECURITY_MANAGE";

    /// <summary>
    /// Permission to view anomaly detection results
    /// Allows accessing AI/ML-powered threat detection insights
    /// </summary>
    public const string ANOMALY_DETECTION_VIEW = "ANOMALY_DETECTION_VIEW";

    // ============================================
    // SYSTEM SETTINGS PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to modify system-wide settings
    /// CRITICAL: Allows changing platform configuration
    /// </summary>
    public const string SYSTEM_SETTINGS = "SYSTEM_SETTINGS";

    /// <summary>
    /// Permission to view system health and performance metrics
    /// Allows accessing monitoring dashboards
    /// </summary>
    public const string SYSTEM_HEALTH_VIEW = "SYSTEM_HEALTH_VIEW";

    /// <summary>
    /// Permission to perform database operations
    /// HIGHLY CRITICAL: Allows backups, restores, and schema changes
    /// </summary>
    public const string DATABASE_OPERATIONS = "DATABASE_OPERATIONS";

    /// <summary>
    /// Permission to manage background jobs
    /// Allows configuring Hangfire jobs and schedules
    /// </summary>
    public const string BACKGROUND_JOBS_MANAGE = "BACKGROUND_JOBS_MANAGE";

    // ============================================
    // BILLING & SUBSCRIPTION PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to view subscription and billing information
    /// Allows accessing payment history and pricing
    /// </summary>
    public const string BILLING_VIEW = "BILLING_VIEW";

    /// <summary>
    /// Permission to manage subscriptions and payments
    /// Allows processing payments and modifying billing
    /// </summary>
    public const string BILLING_MANAGE = "BILLING_MANAGE";

    /// <summary>
    /// Permission to update pricing and tiers
    /// CRITICAL: Allows changing subscription pricing
    /// </summary>
    public const string PRICING_MANAGE = "PRICING_MANAGE";

    // ============================================
    // COMPLIANCE & LEGAL PERMISSIONS
    // ============================================

    /// <summary>
    /// Permission to view compliance reports
    /// Allows accessing SOX, GDPR, and regulatory reports
    /// </summary>
    public const string COMPLIANCE_REPORTS_VIEW = "COMPLIANCE_REPORTS_VIEW";

    /// <summary>
    /// Permission to manage legal holds
    /// CRITICAL: Allows freezing data for legal proceedings
    /// </summary>
    public const string LEGAL_HOLD_MANAGE = "LEGAL_HOLD_MANAGE";

    /// <summary>
    /// Permission to perform e-discovery operations
    /// Allows searching and exporting data for legal requests
    /// </summary>
    public const string EDISCOVERY_PERFORM = "EDISCOVERY_PERFORM";

    // ============================================
    // SPECIAL PERMISSIONS
    // ============================================

    /// <summary>
    /// Wildcard permission - grants ALL permissions
    /// HIGHLY CRITICAL: Only for SuperAdmin role
    /// Reserved for backward compatibility and initial setup
    /// </summary>
    public const string ALL = "*";

    /// <summary>
    /// Read-only access to all data
    /// Allows viewing but not modifying any resources
    /// Useful for auditor or observer roles
    /// </summary>
    public const string READ_ONLY = "READ_ONLY";
}
