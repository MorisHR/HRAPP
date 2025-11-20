using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Machine learning training data for intelligent project allocation
/// Learns employee work patterns over time
/// Used to generate smart suggestions
/// </summary>
public class WorkPattern : BaseEntity
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Day of week (0=Sunday, 1=Monday, etc.)
    /// null = applies to all days
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Hour of day (0-23)
    /// null = applies to all hours
    /// </summary>
    public int? HourOfDay { get; set; }

    /// <summary>
    /// How many times has this pattern occurred?
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;

    /// <summary>
    /// Average hours spent on this project during this pattern
    /// </summary>
    public decimal AverageHours { get; set; }

    /// <summary>
    /// Total hours logged for this pattern
    /// </summary>
    public decimal TotalHours { get; set; }

    /// <summary>
    /// Last time this pattern was observed
    /// </summary>
    public DateTime LastOccurrence { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// First time this pattern was observed
    /// </summary>
    public DateTime FirstOccurrence { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Pattern confidence score (0-100)
    /// Higher = more reliable pattern
    /// </summary>
    public int ConfidenceScore { get; set; } = 50;

    /// <summary>
    /// Is this pattern still active? (last seen within 30 days)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Pattern context (JSON)
    /// E.g., {"typical_task": "Code Review", "typical_start_time": "09:00"}
    /// </summary>
    public string? PatternContext { get; set; }

    /// <summary>
    /// Tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Update pattern with new observation
    /// </summary>
    public void UpdatePattern(decimal newHours)
    {
        OccurrenceCount++;
        TotalHours += newHours;
        AverageHours = TotalHours / OccurrenceCount;
        LastOccurrence = DateTime.UtcNow;

        // Recalculate confidence score
        // More occurrences = higher confidence
        // Recent occurrences = higher confidence
        var daysSinceFirst = (DateTime.UtcNow - FirstOccurrence).TotalDays;
        var recencyBonus = (DateTime.UtcNow - LastOccurrence).TotalDays < 7 ? 20 : 0;

        ConfidenceScore = Math.Min(100,
            (int)(Math.Min(OccurrenceCount * 5, 60) +
                  Math.Min(daysSinceFirst / 7, 20) +
                  recencyBonus));

        IsActive = (DateTime.UtcNow - LastOccurrence).TotalDays <= 30;
    }
}
