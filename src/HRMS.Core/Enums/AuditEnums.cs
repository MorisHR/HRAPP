namespace HRMS.Core.Enums;

/// <summary>
/// High-level categorization of audit events for filtering and compliance reporting
/// Supports Mauritius Data Protection Act and Workers' Rights Act requirements
/// </summary>
public enum AuditCategory
{
    /// <summary>User login, logout, password changes, MFA events</summary>
    AUTHENTICATION = 1,

    /// <summary>Permission checks, role assignments, access denials</summary>
    AUTHORIZATION = 2,

    /// <summary>All CRUD operations on business entities with before/after tracking</summary>
    DATA_CHANGE = 3,

    /// <summary>Tenant lifecycle operations (create, activate, suspend, delete)</summary>
    TENANT_LIFECYCLE = 4,

    /// <summary>SuperAdmin platform administration actions</summary>
    SYSTEM_ADMIN = 5,

    /// <summary>Security-related events (failed logins, suspicious activity, account lockouts)</summary>
    SECURITY_EVENT = 6,

    /// <summary>Background jobs, scheduled tasks, system integrations</summary>
    SYSTEM_EVENT = 7,

    /// <summary>Compliance-specific events (data export, audit log access, retention actions)</summary>
    COMPLIANCE = 8
}

/// <summary>
/// Severity level for prioritization, alerting, and compliance reporting
/// Aligns with industry standards (NIST, ISO 27001) and supports real-time monitoring
/// </summary>
public enum AuditSeverity
{
    /// <summary>Normal operations, routine activities - no action required</summary>
    INFO = 1,

    /// <summary>Potentially concerning activity - review recommended (e.g., multiple failed logins)</summary>
    WARNING = 2,

    /// <summary>Security incidents requiring immediate attention (e.g., unauthorized access attempts)</summary>
    CRITICAL = 3,

    /// <summary>Severe security breaches or system failures requiring emergency response</summary>
    EMERGENCY = 4
}

/// <summary>
/// Standardized action types for consistent logging across the application
/// Comprehensive list covering all MorisHR business operations
/// Supports compliance reporting and detailed audit trail requirements
/// </summary>
public enum AuditActionType
{
    // ============================================
    // AUTHENTICATION ACTIONS (1-10)
    // ============================================

    /// <summary>Successful user login (tenant or SuperAdmin)</summary>
    LOGIN_SUCCESS = 1,

    /// <summary>Failed login attempt (wrong credentials, locked account)</summary>
    LOGIN_FAILED = 2,

    /// <summary>User logout (manual or automatic)</summary>
    LOGOUT = 3,

    /// <summary>User password changed successfully</summary>
    PASSWORD_CHANGED = 4,

    /// <summary>Password reset request initiated</summary>
    PASSWORD_RESET_REQUESTED = 5,

    /// <summary>Password reset completed</summary>
    PASSWORD_RESET_COMPLETED = 6,

    /// <summary>Password reset failed (invalid token, expired token, etc.)</summary>
    PASSWORD_RESET_FAILED = 150,

    /// <summary>MFA setup initiated</summary>
    MFA_SETUP_STARTED = 7,

    /// <summary>MFA setup completed successfully</summary>
    MFA_SETUP_COMPLETED = 8,

    /// <summary>MFA verification succeeded</summary>
    MFA_VERIFICATION_SUCCESS = 9,

    /// <summary>MFA verification failed</summary>
    MFA_VERIFICATION_FAILED = 10,

    /// <summary>MFA disabled by admin</summary>
    MFA_DISABLED = 137,

    // ============================================
    // AUTHORIZATION ACTIONS (11-20)
    // ============================================

    /// <summary>Access granted to a resource</summary>
    ACCESS_GRANTED = 11,

    /// <summary>Access denied to a resource (insufficient permissions)</summary>
    ACCESS_DENIED = 12,

    /// <summary>User role assigned</summary>
    ROLE_ASSIGNED = 13,

    /// <summary>User role revoked</summary>
    ROLE_REVOKED = 14,

    /// <summary>Permission granted to user/role</summary>
    PERMISSION_GRANTED = 15,

    /// <summary>Permission revoked from user/role</summary>
    PERMISSION_REVOKED = 16,

    /// <summary>Account locked due to security policy</summary>
    ACCOUNT_LOCKED = 17,

    /// <summary>Account unlocked by administrator</summary>
    ACCOUNT_UNLOCKED = 18,

    /// <summary>Session timeout due to inactivity</summary>
    SESSION_TIMEOUT = 19,

    /// <summary>Token refresh request</summary>
    TOKEN_REFRESHED = 20,

    /// <summary>Session expired (token expired)</summary>
    SESSION_EXPIRED = 151,

    /// <summary>Permission denied (unauthorized access attempt)</summary>
    PERMISSION_DENIED = 152,

    // ============================================
    // TENANT LIFECYCLE ACTIONS (21-30)
    // ============================================

    /// <summary>New tenant created by SuperAdmin</summary>
    TENANT_CREATED = 21,

    /// <summary>Tenant updated (name, settings, etc.)</summary>
    TENANT_UPDATED = 22,

    /// <summary>Tenant activated (can access system)</summary>
    TENANT_ACTIVATED = 23,

    /// <summary>Tenant suspended (access revoked temporarily)</summary>
    TENANT_SUSPENDED = 24,

    /// <summary>Tenant deleted (soft delete)</summary>
    TENANT_DELETED = 25,

    /// <summary>Tenant subscription plan changed</summary>
    TENANT_SUBSCRIPTION_CHANGED = 26,

    /// <summary>Tenant email verified</summary>
    TENANT_EMAIL_VERIFIED = 27,

    /// <summary>Tenant database schema created</summary>
    TENANT_SCHEMA_CREATED = 28,

    /// <summary>Tenant configuration updated</summary>
    TENANT_CONFIGURATION_UPDATED = 29,

    /// <summary>Tenant data export requested</summary>
    TENANT_DATA_EXPORTED = 30,

    // ============================================
    // SUPERADMIN SPECIFIC ACTIONS (121-135) - FORTUNE 500 COMPLIANCE
    // ============================================

    /// <summary>Tenant reactivated after suspension or soft delete</summary>
    TENANT_REACTIVATED = 121,

    /// <summary>Tenant permanently deleted (hard delete - IRREVERSIBLE)</summary>
    TENANT_HARD_DELETED = 122,

    /// <summary>Tenant employee tier/pricing changed</summary>
    TENANT_TIER_UPDATED = 123,

    /// <summary>New SuperAdmin account created</summary>
    SUPERADMIN_CREATED = 124,

    /// <summary>SuperAdmin account deleted</summary>
    SUPERADMIN_DELETED = 125,

    /// <summary>SuperAdmin permissions modified</summary>
    SUPERADMIN_PERMISSION_CHANGED = 126,

    /// <summary>SuperAdmin started tenant impersonation session</summary>
    TENANT_IMPERSONATION_STARTED = 127,

    /// <summary>SuperAdmin ended tenant impersonation session</summary>
    TENANT_IMPERSONATION_ENDED = 128,

    /// <summary>SuperAdmin unlocked a locked account</summary>
    SUPERADMIN_UNLOCKED_ACCOUNT = 129,

    /// <summary>SuperAdmin forced password reset for user</summary>
    SUPERADMIN_FORCED_PASSWORD_RESET = 130,

    /// <summary>SuperAdmin accessed audit logs (meta-audit)</summary>
    SUPERADMIN_AUDIT_LOG_ACCESS = 131,

    /// <summary>SuperAdmin performed bulk operation</summary>
    SUPERADMIN_BULK_OPERATION = 132,

    /// <summary>Password expired due to rotation policy</summary>
    PASSWORD_EXPIRED = 133,

    /// <summary>Security setting modified by SuperAdmin</summary>
    SECURITY_SETTING_CHANGED = 134,

    /// <summary>System-wide setting modified by SuperAdmin</summary>
    SYSTEM_WIDE_SETTING_CHANGED = 135,

    /// <summary>Password change attempt failed (validation failure, wrong current password, password reuse)</summary>
    PASSWORD_CHANGE_FAILED = 136,

    // ============================================
    // EMPLOYEE LIFECYCLE ACTIONS (31-40)
    // ============================================

    /// <summary>New employee created in system</summary>
    EMPLOYEE_CREATED = 31,

    /// <summary>Employee profile updated</summary>
    EMPLOYEE_UPDATED = 32,

    /// <summary>Employee soft deleted</summary>
    EMPLOYEE_DELETED = 33,

    /// <summary>Employee activated (can access system)</summary>
    EMPLOYEE_ACTIVATED = 34,

    /// <summary>Employee deactivated (access revoked)</summary>
    EMPLOYEE_DEACTIVATED = 35,

    /// <summary>Employee role/position changed</summary>
    EMPLOYEE_ROLE_CHANGED = 36,

    /// <summary>Employee department transferred</summary>
    EMPLOYEE_DEPARTMENT_CHANGED = 37,

    /// <summary>Employee salary updated (sensitive operation)</summary>
    EMPLOYEE_SALARY_UPDATED = 38,

    /// <summary>Employee document uploaded</summary>
    EMPLOYEE_DOCUMENT_UPLOADED = 39,

    /// <summary>Employee document deleted</summary>
    EMPLOYEE_DOCUMENT_DELETED = 40,

    // ============================================
    // LEAVE MANAGEMENT ACTIONS (41-50)
    // ============================================

    /// <summary>Leave request submitted by employee</summary>
    LEAVE_REQUEST_CREATED = 41,

    /// <summary>Leave request updated</summary>
    LEAVE_REQUEST_UPDATED = 42,

    /// <summary>Leave request cancelled by employee</summary>
    LEAVE_REQUEST_CANCELLED = 43,

    /// <summary>Leave request approved by manager</summary>
    LEAVE_REQUEST_APPROVED = 44,

    /// <summary>Leave request rejected by manager</summary>
    LEAVE_REQUEST_REJECTED = 45,

    /// <summary>Leave balance adjusted manually</summary>
    LEAVE_BALANCE_ADJUSTED = 46,

    /// <summary>Leave type created/updated</summary>
    LEAVE_TYPE_CONFIGURED = 47,

    /// <summary>Leave policy updated</summary>
    LEAVE_POLICY_UPDATED = 48,

    /// <summary>Bulk leave approval performed</summary>
    LEAVE_BULK_APPROVED = 49,

    /// <summary>Leave carryover processed</summary>
    LEAVE_CARRYOVER_PROCESSED = 50,

    // ============================================
    // PAYROLL ACTIONS (51-60)
    // ============================================

    /// <summary>Payroll cycle initiated</summary>
    PAYROLL_CYCLE_INITIATED = 51,

    /// <summary>Payroll calculated for period</summary>
    PAYROLL_CALCULATED = 52,

    /// <summary>Payroll approved for payment</summary>
    PAYROLL_APPROVED = 53,

    /// <summary>Payroll processed and finalized</summary>
    PAYROLL_PROCESSED = 54,

    /// <summary>Payslip generated for employee</summary>
    PAYSLIP_GENERATED = 55,

    /// <summary>Payslip viewed by employee</summary>
    PAYSLIP_VIEWED = 56,

    /// <summary>Payslip downloaded by employee</summary>
    PAYSLIP_DOWNLOADED = 57,

    /// <summary>Payroll adjustment made</summary>
    PAYROLL_ADJUSTMENT = 58,

    /// <summary>Tax configuration updated</summary>
    TAX_CONFIGURATION_UPDATED = 59,

    /// <summary>Payroll exported for banking integration</summary>
    PAYROLL_EXPORTED = 60,

    // ============================================
    // TIMESHEET ACTIONS (61-70)
    // ============================================

    /// <summary>Timesheet entry created</summary>
    TIMESHEET_CREATED = 61,

    /// <summary>Timesheet entry updated</summary>
    TIMESHEET_UPDATED = 62,

    /// <summary>Timesheet entry deleted</summary>
    TIMESHEET_DELETED = 63,

    /// <summary>Timesheet submitted for approval</summary>
    TIMESHEET_SUBMITTED = 64,

    /// <summary>Timesheet approved by manager</summary>
    TIMESHEET_APPROVED = 65,

    /// <summary>Timesheet rejected by manager</summary>
    TIMESHEET_REJECTED = 66,

    /// <summary>Clock-in recorded</summary>
    TIME_CLOCK_IN = 67,

    /// <summary>Clock-out recorded</summary>
    TIME_CLOCK_OUT = 68,

    /// <summary>Bulk timesheet approval</summary>
    TIMESHEET_BULK_APPROVED = 69,

    /// <summary>Timesheet exported for payroll</summary>
    TIMESHEET_EXPORTED = 70,

    // ============================================
    // DOCUMENT MANAGEMENT ACTIONS (71-80)
    // ============================================

    /// <summary>Document uploaded to system</summary>
    DOCUMENT_UPLOADED = 71,

    /// <summary>Document viewed/accessed</summary>
    DOCUMENT_VIEWED = 72,

    /// <summary>Document downloaded</summary>
    DOCUMENT_DOWNLOADED = 73,

    /// <summary>Document updated/replaced</summary>
    DOCUMENT_UPDATED = 74,

    /// <summary>Document deleted</summary>
    DOCUMENT_DELETED = 75,

    /// <summary>Document access permission granted</summary>
    DOCUMENT_PERMISSION_GRANTED = 76,

    /// <summary>Document access permission revoked</summary>
    DOCUMENT_PERMISSION_REVOKED = 77,

    /// <summary>Document category/metadata updated</summary>
    DOCUMENT_METADATA_UPDATED = 78,

    /// <summary>Document moved to different folder/category</summary>
    DOCUMENT_MOVED = 79,

    /// <summary>Bulk document operation performed</summary>
    DOCUMENT_BULK_OPERATION = 80,

    // ============================================
    // SYSTEM ADMIN ACTIONS (81-90)
    // ============================================

    /// <summary>System configuration updated</summary>
    SYSTEM_CONFIGURATION_UPDATED = 81,

    /// <summary>System setting changed</summary>
    SYSTEM_SETTING_CHANGED = 82,

    /// <summary>Database backup initiated</summary>
    DATABASE_BACKUP_INITIATED = 83,

    /// <summary>Database restore performed</summary>
    DATABASE_RESTORE_PERFORMED = 84,

    /// <summary>System maintenance mode enabled</summary>
    MAINTENANCE_MODE_ENABLED = 85,

    /// <summary>System maintenance mode disabled</summary>
    MAINTENANCE_MODE_DISABLED = 86,

    /// <summary>Email template updated</summary>
    EMAIL_TEMPLATE_UPDATED = 87,

    /// <summary>Integration API key generated</summary>
    API_KEY_GENERATED = 88,

    /// <summary>Integration API key revoked</summary>
    API_KEY_REVOKED = 89,

    /// <summary>System health check performed</summary>
    SYSTEM_HEALTH_CHECK = 90,

    // ============================================
    // SECURITY EVENTS (91-100)
    // ============================================

    /// <summary>Suspicious activity detected</summary>
    SUSPICIOUS_ACTIVITY_DETECTED = 91,

    /// <summary>Multiple failed login attempts</summary>
    MULTIPLE_FAILED_LOGINS = 92,

    /// <summary>Unauthorized access attempt</summary>
    UNAUTHORIZED_ACCESS_ATTEMPT = 93,

    /// <summary>Data breach detected</summary>
    DATA_BREACH_DETECTED = 94,

    /// <summary>Security policy violation</summary>
    SECURITY_POLICY_VIOLATION = 95,

    /// <summary>Audit log accessed (meta-audit)</summary>
    AUDIT_LOG_ACCESSED = 96,

    /// <summary>Security alert triggered</summary>
    SECURITY_ALERT_TRIGGERED = 97,

    /// <summary>IP address blacklisted</summary>
    IP_BLACKLISTED = 98,

    /// <summary>Session hijacking detected</summary>
    SESSION_HIJACKING_DETECTED = 99,

    /// <summary>Brute force attack detected</summary>
    BRUTE_FORCE_DETECTED = 100,

    // ============================================
    // COMPLIANCE ACTIONS (101-110)
    // ============================================

    /// <summary>Data export requested for compliance</summary>
    COMPLIANCE_DATA_EXPORT = 101,

    /// <summary>Data retention policy applied</summary>
    DATA_RETENTION_APPLIED = 102,

    /// <summary>Data anonymized for privacy</summary>
    DATA_ANONYMIZED = 103,

    /// <summary>GDPR/Data Protection request processed</summary>
    DATA_PROTECTION_REQUEST = 104,

    /// <summary>Compliance report generated</summary>
    COMPLIANCE_REPORT_GENERATED = 105,

    /// <summary>Audit log archived</summary>
    AUDIT_LOG_ARCHIVED = 106,

    /// <summary>Audit trail exported</summary>
    AUDIT_TRAIL_EXPORTED = 107,

    /// <summary>External audit access granted</summary>
    EXTERNAL_AUDIT_ACCESS_GRANTED = 108,

    /// <summary>External audit access revoked</summary>
    EXTERNAL_AUDIT_ACCESS_REVOKED = 109,

    /// <summary>Legal hold applied to data</summary>
    LEGAL_HOLD_APPLIED = 110,

    // ============================================
    // GENERAL DATA OPERATIONS (111-120)
    // ============================================

    /// <summary>Generic record created (catch-all)</summary>
    RECORD_CREATED = 111,

    /// <summary>Generic record updated (catch-all)</summary>
    RECORD_UPDATED = 112,

    /// <summary>Generic record deleted (catch-all)</summary>
    RECORD_DELETED = 113,

    /// <summary>Generic record viewed (catch-all)</summary>
    RECORD_VIEWED = 114,

    /// <summary>Bulk operation performed</summary>
    BULK_OPERATION = 115,

    /// <summary>Data import performed</summary>
    DATA_IMPORTED = 116,

    /// <summary>Data export performed</summary>
    DATA_EXPORTED = 117,

    /// <summary>Report generated</summary>
    REPORT_GENERATED = 118,

    /// <summary>Notification sent</summary>
    NOTIFICATION_SENT = 119,

    /// <summary>Scheduled job executed</summary>
    SCHEDULED_JOB_EXECUTED = 120
}

/// <summary>
/// Security alert types for real-time threat detection and monitoring
/// Supports Fortune 500 compliance requirements (SOX, GDPR, ISO 27001, PCI-DSS)
/// </summary>
public enum SecurityAlertType
{
    /// <summary>Multiple failed login attempts detected (possible brute force)</summary>
    FAILED_LOGIN_THRESHOLD = 1,

    /// <summary>Unauthorized access attempt to restricted resource</summary>
    UNAUTHORIZED_ACCESS = 2,

    /// <summary>Mass data export detected (possible data exfiltration)</summary>
    MASS_DATA_EXPORT = 3,

    /// <summary>After-hours access to sensitive data</summary>
    AFTER_HOURS_ACCESS = 4,

    /// <summary>Salary or financial data modification</summary>
    SALARY_CHANGE = 5,

    /// <summary>Role or permission escalation attempt</summary>
    PRIVILEGE_ESCALATION = 6,

    /// <summary>Login from unusual geographic location</summary>
    GEOGRAPHIC_ANOMALY = 7,

    /// <summary>Rapid successive high-risk actions</summary>
    RAPID_HIGH_RISK_ACTIONS = 8,

    /// <summary>Account locked due to security policy violation</summary>
    ACCOUNT_LOCKOUT = 9,

    /// <summary>Multiple simultaneous sessions from different locations</summary>
    IMPOSSIBLE_TRAVEL = 10,

    /// <summary>API rate limit exceeded</summary>
    RATE_LIMIT_EXCEEDED = 11,

    /// <summary>SQL injection attempt detected</summary>
    SQL_INJECTION_ATTEMPT = 12,

    /// <summary>XSS attack attempt detected</summary>
    XSS_ATTEMPT = 13,

    /// <summary>CSRF token validation failure</summary>
    CSRF_FAILURE = 14,

    /// <summary>Session hijacking detected</summary>
    SESSION_HIJACK = 15,

    /// <summary>Suspicious file upload detected</summary>
    MALICIOUS_FILE_UPLOAD = 16,

    /// <summary>Data breach indicators detected</summary>
    DATA_BREACH = 17,

    /// <summary>System integrity violation</summary>
    INTEGRITY_VIOLATION = 18,

    /// <summary>Compliance policy violation</summary>
    COMPLIANCE_VIOLATION = 19,

    /// <summary>Anomaly detected by machine learning model</summary>
    ML_ANOMALY = 20,

    /// <summary>Generic security event requiring review</summary>
    GENERAL_SECURITY_EVENT = 99
}

/// <summary>
/// Security alert status for workflow management
/// Supports alert acknowledgement, investigation, and resolution tracking
/// </summary>
public enum SecurityAlertStatus
{
    /// <summary>Alert created, not yet reviewed</summary>
    NEW = 1,

    /// <summary>Alert acknowledged by security team</summary>
    ACKNOWLEDGED = 2,

    /// <summary>Investigation in progress</summary>
    IN_PROGRESS = 3,

    /// <summary>Alert resolved - threat mitigated</summary>
    RESOLVED = 4,

    /// <summary>Alert dismissed as false positive</summary>
    FALSE_POSITIVE = 5,

    /// <summary>Alert escalated to senior security team</summary>
    ESCALATED = 6,

    /// <summary>Alert requires further investigation</summary>
    PENDING_REVIEW = 7,

    /// <summary>Alert closed without action</summary>
    CLOSED = 8
}
