namespace HRMS.Core.Enums;

/// <summary>
/// Types of anomalies detected in the system
/// FORTUNE 500 PATTERN: Splunk Security, AWS GuardDuty, Azure Sentinel
/// </summary>
public enum AnomalyType
{
    // Authentication Anomalies
    UNUSUAL_LOGIN_LOCATION,
    IMPOSSIBLE_TRAVEL,           // Login from 2 countries within impossible timeframe
    MULTIPLE_FAILED_LOGINS,
    BRUTE_FORCE_ATTEMPT,
    CONCURRENT_SESSIONS,
    AFTER_HOURS_ACCESS,

    // Data Access Anomalies
    MASS_DATA_EXPORT,           // >100 records
    UNUSUAL_DATA_ACCESS,
    PRIVILEGE_ESCALATION,
    UNAUTHORIZED_ACCESS_ATTEMPT,

    // Behavioral Anomalies
    UNUSUAL_ACTIVITY_PATTERN,
    RAPID_HIGH_RISK_ACTIONS,
    ABNORMAL_ACCESS_FREQUENCY,

    // Financial Anomalies
    LARGE_SALARY_CHANGE,        // >50% increase/decrease
    UNUSUAL_PAYROLL_MODIFICATION,

    // Configuration Anomalies
    SECURITY_SETTING_DISABLED,
    AUDIT_LOG_ACCESS,           // Viewing own audit logs
    SYSTEM_CONFIGURATION_CHANGE
}

/// <summary>
/// Risk level of detected anomaly
/// </summary>
public enum AnomalyRiskLevel
{
    LOW = 1,
    MEDIUM = 2,
    HIGH = 3,
    CRITICAL = 4,
    EMERGENCY = 5
}

/// <summary>
/// Status of anomaly investigation
/// </summary>
public enum AnomalyStatus
{
    NEW,
    INVESTIGATING,
    CONFIRMED_THREAT,
    FALSE_POSITIVE,
    RESOLVED,
    IGNORED
}
