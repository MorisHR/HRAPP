using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Platform-wide announcements and notifications
/// FORTUNE 500 PATTERN: Salesforce In-App Notifications, AWS Service Health Dashboard
/// </summary>
public class PlatformAnnouncement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; } // INFO, WARNING, MAINTENANCE, CRITICAL
    public AnnouncementAudience Audience { get; set; } // ALL, SUPERADMIN, TENANTS

    // Scheduling
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDismissible { get; set; }

    // Targeting (JSON array of tenant IDs, null = all tenants)
    public string? TargetTenantIds { get; set; }

    // Metadata
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int Priority { get; set; } = 0; // Higher = more important

    // Audit fields
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
