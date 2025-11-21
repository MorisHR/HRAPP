namespace HRMS.Core.Enums;

/// <summary>
/// TIER 3: Industry classification for tenants
/// FORTUNE 500 PATTERN: Industry segmentation for analytics and customization
/// </summary>
public enum TenantIndustry
{
    NotSpecified = 0,
    Technology = 1,
    Healthcare = 2,
    Finance = 3,
    Manufacturing = 4,
    Retail = 5,
    Education = 6,
    Government = 7,
    NonProfit = 8,
    RealEstate = 9,
    Hospitality = 10,
    Transportation = 11,
    Construction = 12,
    Agriculture = 13,
    Media = 14,
    Telecommunications = 15,
    Energy = 16,
    ProfessionalServices = 17,
    Other = 99
}

/// <summary>
/// TIER 3: Company size classification
/// FORTUNE 500 PATTERN: Size-based feature enablement and pricing
/// </summary>
public enum CompanySize
{
    NotSpecified = 0,
    Micro = 1,          // 1-10 employees
    Small = 2,          // 11-50 employees
    Medium = 3,         // 51-200 employees
    Large = 4,          // 201-1000 employees
    Enterprise = 5,     // 1001+ employees
    Government = 6      // Government entities (any size)
}

/// <summary>
/// TIER 2: Tenant provisioning status
/// PRODUCTION-GRADE: Track tenant setup progress for visibility
/// </summary>
public enum TenantProvisioningStatus
{
    NotStarted = 0,
    CreatingDatabase = 1,
    RunningMigrations = 2,
    SeedingData = 3,
    ConfiguringDefaults = 4,
    ActivatingServices = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8
}

/// <summary>
/// TIER 1: Impersonation action type
/// SECURITY: Detailed tracking of what superadmin did during impersonation
/// </summary>
public enum ImpersonationAction
{
    Started = 1,
    ViewedDashboard = 2,
    ViewedEmployees = 3,
    ViewedSettings = 4,
    ModifiedData = 5,
    ExportedData = 6,
    Ended = 7,
    ForcedLogout = 8
}

/// <summary>
/// TIER 1: Health score severity levels
/// FORTUNE 500 PATTERN: Alert levels for tenant health monitoring
/// </summary>
public enum HealthScoreSeverity
{
    Excellent = 1,      // 90-100
    Good = 2,           // 70-89
    Fair = 3,           // 50-69
    Poor = 4,           // 30-49
    Critical = 5        // 0-29
}
