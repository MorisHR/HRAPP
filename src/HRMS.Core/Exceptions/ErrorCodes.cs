namespace HRMS.Core.Exceptions;

/// <summary>
/// Centralized error codes for the entire HRMS application
/// Format: [MODULE]_[NUMBER] (e.g., AUTH_001, EMP_404)
/// </summary>
public static class ErrorCodes
{
    // ============================================
    // AUTHENTICATION & AUTHORIZATION (AUTH_xxx)
    // ============================================
    public const string AUTH_INVALID_CREDENTIALS = "AUTH_001";
    public const string AUTH_ACCOUNT_LOCKED = "AUTH_002";
    public const string AUTH_ACCOUNT_SUSPENDED = "AUTH_003";
    public const string AUTH_SESSION_EXPIRED = "AUTH_004";
    public const string AUTH_TOKEN_INVALID = "AUTH_005";
    public const string AUTH_MFA_REQUIRED = "AUTH_006";
    public const string AUTH_MFA_INVALID = "AUTH_007";
    public const string AUTH_PASSWORD_EXPIRED = "AUTH_008";
    public const string AUTH_INSUFFICIENT_PERMISSIONS = "AUTH_009";
    public const string AUTH_TENANT_NOT_FOUND = "AUTH_010";
    public const string AUTH_TENANT_INACTIVE = "AUTH_011";

    // ============================================
    // EMPLOYEE MANAGEMENT (EMP_xxx)
    // ============================================
    public const string EMP_NOT_FOUND = "EMP_001";
    public const string EMP_DUPLICATE_EMAIL = "EMP_002";
    public const string EMP_DUPLICATE_CODE = "EMP_003";
    public const string EMP_INVALID_DATA = "EMP_004";
    public const string EMP_CANNOT_DELETE_HAS_RECORDS = "EMP_005";
    public const string EMP_DRAFT_NOT_FOUND = "EMP_006";
    public const string EMP_DRAFT_INVALID = "EMP_007";
    public const string EMP_INACTIVE = "EMP_008";

    // ============================================
    // ATTENDANCE (ATT_xxx)
    // ============================================
    public const string ATT_NOT_FOUND = "ATT_001";
    public const string ATT_DUPLICATE_RECORD = "ATT_002";
    public const string ATT_INVALID_TIME = "ATT_003";
    public const string ATT_UNAUTHORIZED_ACCESS = "ATT_004";
    public const string ATT_CORRECTION_PENDING = "ATT_005";
    public const string ATT_CORRECTION_NOT_FOUND = "ATT_006";
    public const string ATT_ALREADY_CHECKED_IN = "ATT_007";
    public const string ATT_ALREADY_CHECKED_OUT = "ATT_008";
    public const string ATT_NO_CHECKIN = "ATT_009";
    public const string ATT_ALREADY_COMPLETED = "ATT_010";

    // ============================================
    // PAYROLL (PAY_xxx)
    // ============================================
    public const string PAY_CYCLE_NOT_FOUND = "PAY_001";
    public const string PAY_SLIP_NOT_FOUND = "PAY_002";
    public const string PAY_COMPONENT_NOT_FOUND = "PAY_003";
    public const string PAY_ALREADY_PROCESSED = "PAY_004";
    public const string PAY_CALCULATION_ERROR = "PAY_005";

    // ============================================
    // LEAVE MANAGEMENT (LEV_xxx)
    // ============================================
    public const string LEV_NOT_FOUND = "LEV_001";
    public const string LEV_INSUFFICIENT_BALANCE = "LEV_002";
    public const string LEV_OVERLAP_EXISTS = "LEV_003";
    public const string LEV_INVALID_DATES = "LEV_004";
    public const string LEV_ALREADY_APPROVED = "LEV_005";

    // ============================================
    // LOCATION (LOC_xxx)
    // ============================================
    public const string LOC_NOT_FOUND = "LOC_001";
    public const string LOC_DUPLICATE_NAME = "LOC_002";
    public const string LOC_HAS_EMPLOYEES = "LOC_003";
    public const string LOC_INVALID_HIERARCHY = "LOC_004";

    // ============================================
    // BIOMETRIC DEVICES (DEV_xxx)
    // ============================================
    public const string DEV_NOT_FOUND = "DEV_001";
    public const string DEV_DUPLICATE_SERIAL = "DEV_002";
    public const string DEV_OFFLINE = "DEV_003";
    public const string DEV_SYNC_FAILED = "DEV_004";
    public const string DEV_INVALID_DATA = "DEV_005";

    // ============================================
    // TENANT MANAGEMENT (TEN_xxx)
    // ============================================
    public const string TEN_NOT_FOUND = "TEN_001";
    public const string TEN_DUPLICATE_SUBDOMAIN = "TEN_002";
    public const string TEN_INACTIVE = "TEN_003";
    public const string TEN_SUBSCRIPTION_EXPIRED = "TEN_004";
    public const string TEN_LIMIT_EXCEEDED = "TEN_005";

    // ============================================
    // SECURITY & AUDIT (SEC_xxx)
    // ============================================
    public const string SEC_ALERT_NOT_FOUND = "SEC_001";
    public const string SEC_UNAUTHORIZED_ACTION = "SEC_002";
    public const string SEC_SUSPICIOUS_ACTIVITY = "SEC_003";
    public const string SEC_LEGAL_HOLD_ACTIVE = "SEC_004";

    // ============================================
    // VALIDATION (VAL_xxx)
    // ============================================
    public const string VAL_REQUIRED_FIELD = "VAL_001";
    public const string VAL_INVALID_FORMAT = "VAL_002";
    public const string VAL_OUT_OF_RANGE = "VAL_003";
    public const string VAL_INVALID_EMAIL = "VAL_004";
    public const string VAL_INVALID_PHONE = "VAL_005";

    // ============================================
    // SYSTEM (SYS_xxx)
    // ============================================
    public const string SYS_DATABASE_ERROR = "SYS_001";
    public const string SYS_EXTERNAL_SERVICE_ERROR = "SYS_002";
    public const string SYS_CONFIGURATION_ERROR = "SYS_003";
    public const string SYS_UNEXPECTED_ERROR = "SYS_999";
}
