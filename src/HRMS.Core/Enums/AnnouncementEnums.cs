namespace HRMS.Core.Enums;

public enum AnnouncementType
{
    INFO,
    WARNING,
    MAINTENANCE,
    CRITICAL,
    SUCCESS
}

public enum AnnouncementAudience
{
    ALL,
    SUPERADMIN,
    TENANTS,
    SPECIFIC_TENANTS
}
