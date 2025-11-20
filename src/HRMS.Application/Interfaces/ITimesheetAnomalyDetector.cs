using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Detects anomalies in timesheet/attendance data
/// Fraud detection, compliance violations, unusual patterns
/// </summary>
public interface ITimesheetAnomalyDetector
{
    /// <summary>
    /// Detect anomalies in attendance-to-timesheet conversion
    /// E.g., Missing clock-out, impossible hours, duplicate entries
    /// </summary>
    Task<List<TimesheetAnomaly>> DetectAttendanceAnomaliesAsync(
        Attendance attendance,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect anomalies in project allocations
    /// E.g., Allocating to closed projects, over-allocation, budget violations
    /// </summary>
    Task<List<TimesheetAnomaly>> DetectAllocationAnomaliesAsync(
        TimesheetEntry entry,
        List<TimesheetProjectAllocation> allocations,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect fraud patterns across multiple days/weeks
    /// E.g., Always exactly 8.00 hours, copy-paste patterns, buddy punching
    /// </summary>
    Task<List<TimesheetAnomaly>> DetectFraudPatternsAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect compliance violations
    /// E.g., Labor law violations, mandatory breaks, overtime limits
    /// </summary>
    Task<List<TimesheetAnomaly>> DetectComplianceViolationsAsync(
        TimesheetEntry entry,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate risk score for a timesheet (0-100)
    /// Used for auto-approve vs requires-review decision
    /// </summary>
    Task<int> CalculateRiskScoreAsync(
        Guid timesheetId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A detected timesheet anomaly (DTO)
/// </summary>
public class TimesheetAnomaly
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning"; // Info, Warning, Error, Critical
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Details { get; set; } = new();
    public bool AutoResolvable { get; set; } = false;
    public string? SuggestedResolution { get; set; }
}
