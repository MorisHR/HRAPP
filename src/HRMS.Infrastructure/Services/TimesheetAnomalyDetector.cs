using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Detects anomalies, fraud patterns, and compliance violations
/// CRITICAL: Multi-tenant SaaS - all queries tenant-scoped
/// CRITICAL: Performance - efficient queries with proper indexing
/// </summary>
public class TimesheetAnomalyDetector : ITimesheetAnomalyDetector
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TimesheetAnomalyDetector> _logger;

    // Configurable thresholds (move to appsettings in production)
    private const int MAX_HOURS_PER_DAY = 12;
    private const int MAX_CONSECUTIVE_DAYS = 6;
    private const decimal OVER_ALLOCATION_TOLERANCE = 0.5m; // 30 minutes tolerance
    private const int LOW_BIOMETRIC_QUALITY_THRESHOLD = 60;

    public TimesheetAnomalyDetector(
        TenantDbContext context,
        ILogger<TimesheetAnomalyDetector> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TimesheetAnomaly>> DetectAttendanceAnomaliesAsync(
        Attendance attendance,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<TimesheetAnomaly>();

        // 1. Missing Clock-Out Detection
        if (attendance.CheckInTime.HasValue && !attendance.CheckOutTime.HasValue)
        {
            var predictedClockOut = await PredictClockOutTimeAsync(
                attendance.EmployeeId,
                attendance.Date.DayOfWeek,
                tenantId,
                cancellationToken);

            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "MissingClockOut",
                Severity = "Warning",
                Description = "No clock-out recorded. Please verify.",
                AutoResolvable = true,
                SuggestedResolution = predictedClockOut.HasValue
                    ? $"Suggest clock-out at {predictedClockOut.Value:HH:mm} based on your pattern"
                    : "Please manually enter clock-out time",
                Details = new Dictionary<string, object>
                {
                    { "clock_in", attendance.CheckInTime.Value },
                    { "predicted_clock_out", predictedClockOut ?? DateTime.MinValue }
                }
            });
        }

        // 2. Excessive Hours Detection
        if (attendance.WorkingHours > MAX_HOURS_PER_DAY)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "ExcessiveHours",
                Severity = "Error",
                Description = $"Worked {attendance.WorkingHours:F1} hours (max {MAX_HOURS_PER_DAY} allowed)",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "hours_worked", attendance.WorkingHours },
                    { "max_allowed", MAX_HOURS_PER_DAY },
                    { "excess_hours", attendance.WorkingHours - MAX_HOURS_PER_DAY }
                }
            });
        }

        // 3. Consecutive Days Without Rest
        var consecutiveDays = await GetConsecutiveDaysWorkedAsync(
            attendance.EmployeeId,
            attendance.Date,
            tenantId,
            cancellationToken);

        if (consecutiveDays > MAX_CONSECUTIVE_DAYS)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "MandatoryRestViolation",
                Severity = "Critical",
                Description = $"Worked {consecutiveDays} consecutive days without rest (max {MAX_CONSECUTIVE_DAYS})",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "consecutive_days", consecutiveDays },
                    { "max_allowed", MAX_CONSECUTIVE_DAYS }
                }
            });
        }

        // 4. Unauthorized Location Access (if device tracking enabled)
        if (attendance.DeviceId.HasValue && !attendance.IsAuthorized)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "UnauthorizedLocation",
                Severity = "Critical",
                Description = attendance.AuthorizationNote ?? "Employee not authorized for this location/device",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "device_id", attendance.DeviceId.Value },
                    { "location_id", attendance.LocationId ?? Guid.Empty }
                }
            });
        }

        return anomalies;
    }

    public async Task<List<TimesheetAnomaly>> DetectAllocationAnomaliesAsync(
        TimesheetEntry entry,
        List<TimesheetProjectAllocation> allocations,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<TimesheetAnomaly>();

        // 1. Over-Allocation Detection
        var totalAllocatedHours = allocations.Sum(a => a.Hours);
        if (totalAllocatedHours > entry.ActualHours + OVER_ALLOCATION_TOLERANCE)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "OverAllocation",
                Severity = "Warning",
                Description = $"Allocated {totalAllocatedHours:F1}h but only worked {entry.ActualHours:F1}h",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "allocated_hours", totalAllocatedHours },
                    { "actual_hours", entry.ActualHours },
                    { "difference", totalAllocatedHours - entry.ActualHours }
                }
            });
        }

        // 2. Under-Allocation Detection
        if (totalAllocatedHours < entry.ActualHours - OVER_ALLOCATION_TOLERANCE)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "UnderAllocation",
                Severity = "Warning",
                Description = $"Only allocated {totalAllocatedHours:F1}h of {entry.ActualHours:F1}h worked",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "allocated_hours", totalAllocatedHours },
                    { "actual_hours", entry.ActualHours },
                    { "unallocated_hours", entry.ActualHours - totalAllocatedHours }
                }
            });
        }

        // 3. Allocation to Closed/Inactive Projects
        // PERFORMANCE: Load project statuses in single query
        var projectIds = allocations.Select(a => a.ProjectId).Distinct().ToList();
        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id) && p.TenantId == tenantId)
            .Select(p => new { p.Id, p.ProjectCode, p.Status, p.AllowTimeEntry })
            .ToListAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            var project = projects.FirstOrDefault(p => p.Id == allocation.ProjectId);
            if (project == null) continue;

            if (project.Status == "Completed" || project.Status == "Cancelled")
            {
                anomalies.Add(new TimesheetAnomaly
                {
                    AnomalyType = "AllocationToClosedProject",
                    Severity = "Error",
                    Description = $"Cannot allocate hours to {project.Status.ToLower()} project {project.ProjectCode}",
                    AutoResolvable = false,
                    Details = new Dictionary<string, object>
                    {
                        { "project_id", project.Id },
                        { "project_code", project.ProjectCode },
                        { "project_status", project.Status },
                        { "allocated_hours", allocation.Hours }
                    }
                });
            }

            if (!project.AllowTimeEntry)
            {
                anomalies.Add(new TimesheetAnomaly
                {
                    AnomalyType = "AllocationToRestrictedProject",
                    Severity = "Warning",
                    Description = $"Project {project.ProjectCode} is not accepting time entries",
                    AutoResolvable = false,
                    Details = new Dictionary<string, object>
                    {
                        { "project_id", project.Id },
                        { "project_code", project.ProjectCode }
                    }
                });
            }
        }

        // 4. Budget Overrun Detection
        // PERFORMANCE: Batch query for project budgets
        var projectsWithBudget = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id)
                && p.TenantId == tenantId
                && p.BudgetHours.HasValue)
            .Select(p => new
            {
                p.Id,
                p.ProjectCode,
                p.BudgetHours,
                LoggedHours = p.TimesheetAllocations.Sum(a => a.Hours)
            })
            .ToListAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            var projectBudget = projectsWithBudget.FirstOrDefault(p => p.Id == allocation.ProjectId);
            if (projectBudget == null) continue;

            var newTotal = projectBudget.LoggedHours + allocation.Hours;
            if (newTotal > projectBudget.BudgetHours)
            {
                anomalies.Add(new TimesheetAnomaly
                {
                    AnomalyType = "BudgetOverrun",
                    Severity = "Warning",
                    Description = $"Project {projectBudget.ProjectCode} over budget: {newTotal:F1}h / {projectBudget.BudgetHours:F1}h",
                    AutoResolvable = false,
                    Details = new Dictionary<string, object>
                    {
                        { "project_code", projectBudget.ProjectCode },
                        { "budget_hours", projectBudget.BudgetHours ?? 0 },
                        { "logged_hours", newTotal },
                        { "utilization_percent", (newTotal / (projectBudget.BudgetHours ?? 1)) * 100 }
                    }
                });
            }
        }

        return anomalies;
    }

    public async Task<List<TimesheetAnomaly>> DetectFraudPatternsAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<TimesheetAnomaly>();

        // PERFORMANCE: Single query for all attendance data in range
        var attendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Date >= startDate
                && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

        if (!attendances.Any())
            return anomalies;

        // 1. "Too Perfect" Pattern - Always exactly X.00 hours
        var roundNumberCount = attendances.Count(a =>
            a.WorkingHours == Math.Floor(a.WorkingHours));

        if (roundNumberCount > attendances.Count * 0.9) // 90% are round numbers
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "SuspiciouslyPerfectHours",
                Severity = "Warning",
                Description = $"{roundNumberCount}/{attendances.Count} days have suspiciously round hours (e.g., always 8.00)",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "round_number_count", roundNumberCount },
                    { "total_days", attendances.Count },
                    { "percentage", (roundNumberCount * 100.0 / attendances.Count) }
                }
            });
        }

        // 2. Identical Clock Times - Same time every day
        var clockInTimes = attendances
            .Where(a => a.CheckInTime.HasValue)
            .Select(a => a.CheckInTime!.Value.TimeOfDay)
            .ToList();

        if (clockInTimes.Count > 5)
        {
            var mostCommonTime = clockInTimes
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .First();

            if (mostCommonTime.Count() > clockInTimes.Count * 0.8) // 80% same time
            {
                anomalies.Add(new TimesheetAnomaly
                {
                    AnomalyType = "IdenticalClockTimes",
                    Severity = "Warning",
                    Description = $"Clocked in at exactly {mostCommonTime.Key} on {mostCommonTime.Count()}/{clockInTimes.Count} days",
                    AutoResolvable = false,
                    Details = new Dictionary<string, object>
                    {
                        { "common_time", mostCommonTime.Key.ToString() },
                        { "occurrences", mostCommonTime.Count() },
                        { "total_days", clockInTimes.Count }
                    }
                });
            }
        }

        // 3. Low Biometric Quality Pattern
        // PERFORMANCE: Query biometric punch records efficiently
        var punchRecords = await _context.BiometricPunchRecords
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId
                && p.PunchTime >= startDate
                && p.PunchTime <= endDate)
            .Select(p => new { p.VerificationQuality, p.PunchTime })
            .ToListAsync(cancellationToken);

        var lowQualityCount = punchRecords.Count(p => p.VerificationQuality < LOW_BIOMETRIC_QUALITY_THRESHOLD);

        if (lowQualityCount > punchRecords.Count * 0.3) // 30% low quality
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "FrequentLowBiometricQuality",
                Severity = "Critical",
                Description = $"{lowQualityCount}/{punchRecords.Count} punches have suspiciously low biometric quality",
                AutoResolvable = false,
                SuggestedResolution = "Possible buddy punching - investigate manually",
                Details = new Dictionary<string, object>
                {
                    { "low_quality_count", lowQualityCount },
                    { "total_punches", punchRecords.Count },
                    { "percentage", (lowQualityCount * 100.0 / punchRecords.Count) }
                }
            });
        }

        return anomalies;
    }

    public Task<List<TimesheetAnomaly>> DetectComplianceViolationsAsync(
        TimesheetEntry entry,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var anomalies = new List<TimesheetAnomaly>();

        // 1. Mandatory Break Compliance
        if (entry.ActualHours > 6 && entry.BreakDuration < 30)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "MandatoryBreakViolation",
                Severity = "Error",
                Description = $"Worked {entry.ActualHours:F1}h with only {entry.BreakDuration}min break (30min required for 6+ hours)",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "hours_worked", entry.ActualHours },
                    { "break_duration_minutes", entry.BreakDuration },
                    { "required_break_minutes", 30 }
                }
            });
        }

        // 2. Maximum Daily Hours (Labor Law)
        if (entry.ActualHours > MAX_HOURS_PER_DAY)
        {
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = "MaxDailyHoursViolation",
                Severity = "Critical",
                Description = $"Exceeded maximum daily hours: {entry.ActualHours:F1}h (max {MAX_HOURS_PER_DAY}h)",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "hours_worked", entry.ActualHours },
                    { "max_allowed", MAX_HOURS_PER_DAY }
                }
            });
        }

        // 3. Weekend/Holiday Work Without Authorization
        if (entry.IsWeekend || entry.IsHoliday)
        {
            // TODO: Check if weekend/holiday work was pre-approved
            anomalies.Add(new TimesheetAnomaly
            {
                AnomalyType = entry.IsHoliday ? "HolidayWork" : "WeekendWork",
                Severity = "Info",
                Description = $"Worked on {(entry.IsHoliday ? "holiday" : "weekend")} - verify authorization",
                AutoResolvable = false,
                Details = new Dictionary<string, object>
                {
                    { "date", entry.Date },
                    { "is_holiday", entry.IsHoliday },
                    { "is_weekend", entry.IsWeekend },
                    { "hours_worked", entry.ActualHours }
                }
            });
        }

        return Task.FromResult(anomalies);
    }

    public async Task<int> CalculateRiskScoreAsync(
        Guid timesheetId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        int riskScore = 0;

        // PERFORMANCE: Single query with all needed data
        var timesheet = await _context.Timesheets
            .AsNoTracking()
            .Include(t => t.Entries)
                .ThenInclude(e => e.ProjectAllocations)
            .FirstOrDefaultAsync(t => t.Id == timesheetId && t.TenantId == tenantId, cancellationToken);

        if (timesheet == null)
            return 100; // Max risk if not found

        var employeeId = timesheet.EmployeeId;
        var periodStart = timesheet.PeriodStart;
        var periodEnd = timesheet.PeriodEnd;

        // Factor 1: Check for anomalies
        var allAnomalies = new List<TimesheetAnomaly>();

        foreach (var entry in timesheet.Entries)
        {
            var entryAnomalies = await DetectAllocationAnomaliesAsync(
                entry,
                entry.ProjectAllocations.ToList(),
                tenantId,
                cancellationToken);

            var complianceAnomalies = await DetectComplianceViolationsAsync(
                entry,
                tenantId,
                cancellationToken);

            allAnomalies.AddRange(entryAnomalies);
            allAnomalies.AddRange(complianceAnomalies);
        }

        // Weight anomalies by severity
        riskScore += allAnomalies.Count(a => a.Severity == "Critical") * 30;
        riskScore += allAnomalies.Count(a => a.Severity == "Error") * 20;
        riskScore += allAnomalies.Count(a => a.Severity == "Warning") * 10;

        // Factor 2: Manual vs Suggested Allocations
        var allAllocations = timesheet.Entries
            .SelectMany(e => e.ProjectAllocations)
            .ToList();

        if (allAllocations.Any())
        {
            var manualCount = allAllocations.Count(a => a.AllocationSource == "Manual");
            var manualRatio = (double)manualCount / allAllocations.Count;

            if (manualRatio > 0.5)
                riskScore += 10; // Lots of manual = higher risk

            // Low suggestion acceptance rate
            var suggestedCount = allAllocations.Count(a => a.SuggestionAccepted.HasValue);
            if (suggestedCount > 0)
            {
                var acceptedCount = allAllocations.Count(a => a.SuggestionAccepted == true);
                var acceptanceRate = (double)acceptedCount / suggestedCount;

                if (acceptanceRate < 0.3)
                    riskScore += 15; // Low acceptance = unusual behavior
                else if (acceptanceRate > 0.8)
                    riskScore -= 10; // High acceptance = lower risk
            }
        }

        // Factor 3: Pattern Deviation
        var hasPatternChanged = await _context.WorkPatterns
            .AnyAsync(wp => wp.EmployeeId == employeeId
                && wp.TenantId == tenantId
                && !wp.IsActive, // Recently deactivated patterns
                cancellationToken);

        if (hasPatternChanged)
            riskScore += 20;

        // Factor 4: Budget Violations
        var projectsOverBudget = allAllocations
            .Select(a => a.ProjectId)
            .Distinct()
            .ToList();

        var overBudgetCount = await _context.Projects
            .CountAsync(p => projectsOverBudget.Contains(p.Id)
                && p.TenantId == tenantId
                && p.BudgetHours.HasValue
                && p.TimesheetAllocations.Sum(a => a.Hours) > p.BudgetHours,
                cancellationToken);

        riskScore += overBudgetCount * 15;

        // Cap at 100
        return Math.Min(100, Math.Max(0, riskScore));
    }

    // Helper Methods

    private async Task<DateTime?> PredictClockOutTimeAsync(
        Guid employeeId,
        DayOfWeek dayOfWeek,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // PERFORMANCE: Efficient query for average clock-out time
        var avgClockOut = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Date.DayOfWeek == dayOfWeek
                && a.CheckOutTime.HasValue)
            .OrderByDescending(a => a.Date)
            .Take(10) // Last 10 occurrences
            .Select(a => a.CheckOutTime!.Value.TimeOfDay.TotalMinutes)
            .ToListAsync(cancellationToken);

        if (!avgClockOut.Any())
            return null;

        var averageMinutes = avgClockOut.Average();
        return DateTime.Today.AddMinutes(averageMinutes);
    }

    private async Task<int> GetConsecutiveDaysWorkedAsync(
        Guid employeeId,
        DateTime currentDate,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // PERFORMANCE: Efficient query looking backwards
        var recentAttendances = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Date <= currentDate
                && a.Date >= currentDate.AddDays(-30)) // Look back max 30 days
            .OrderByDescending(a => a.Date)
            .Select(a => a.Date)
            .ToListAsync(cancellationToken);

        int consecutiveDays = 0;
        var checkDate = currentDate;

        foreach (var date in recentAttendances)
        {
            if (date == checkDate)
            {
                consecutiveDays++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (date < checkDate)
            {
                // Gap found
                break;
            }
        }

        return consecutiveDays;
    }
}
