using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// ML/Rules-based engine for intelligent project allocation
/// Learns from historical data to predict future allocations
/// </summary>
public class ProjectAllocationEngine : IProjectAllocationEngine
{
    private readonly TenantDbContext _context;
    private readonly ILogger<ProjectAllocationEngine> _logger;

    public ProjectAllocationEngine(
        TenantDbContext context,
        ILogger<ProjectAllocationEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AllocationSuggestion>> GenerateSuggestionsAsync(
        Guid employeeId,
        DateTime date,
        decimal totalHoursAvailable,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating allocation suggestions for Employee {EmployeeId} on {Date}",
            employeeId, date);

        var suggestions = new List<AllocationSuggestion>();

        // Strategy 1: Work Pattern Analysis (Primary source - 70% weight)
        var patternSuggestions = await GenerateFromWorkPatternsAsync(
            employeeId, date, totalHoursAvailable, tenantId, cancellationToken);
        suggestions.AddRange(patternSuggestions);

        // Strategy 2: Active Project Membership (30% weight)
        var membershipSuggestions = await GenerateFromProjectMembershipAsync(
            employeeId, date, totalHoursAvailable, tenantId, cancellationToken);

        // Merge membership suggestions with pattern suggestions
        foreach (var memberSuggestion in membershipSuggestions)
        {
            var existing = suggestions.FirstOrDefault(s => s.ProjectId == memberSuggestion.ProjectId);
            if (existing != null)
            {
                // Boost confidence if both patterns and membership suggest same project
                existing.ConfidenceScore = Math.Min(100, existing.ConfidenceScore + 15);
                existing.Source = "Hybrid";
            }
            else
            {
                suggestions.Add(memberSuggestion);
            }
        }

        // TODO: Future integrations
        // Strategy 3: Calendar Integration (check for project-related meetings)
        // Strategy 4: Git Commits (check which repos employee committed to)
        // Strategy 5: Jira/Task Management (check which tasks were worked on)

        // Normalize hours to match total available hours
        suggestions = NormalizeHoursAllocation(suggestions, totalHoursAvailable);

        // Sort by confidence score descending
        suggestions = suggestions.OrderByDescending(s => s.ConfidenceScore).ToList();

        _logger.LogInformation(
            "Generated {Count} suggestions for Employee {EmployeeId}",
            suggestions.Count, employeeId);

        return suggestions;
    }

    private async Task<List<AllocationSuggestion>> GenerateFromWorkPatternsAsync(
        Guid employeeId,
        DateTime date,
        decimal totalHoursAvailable,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        // Find active work patterns for this employee
        // PRODUCTION FIX: AsNoTracking for read-only queries + Take limit to prevent memory exhaustion
        var patterns = await _context.WorkPatterns
            .AsNoTracking()
            .Include(wp => wp.Project)
            .Where(wp => wp.EmployeeId == employeeId
                && wp.TenantId == tenantId
                && wp.IsActive
                && (wp.DayOfWeek == null || wp.DayOfWeek == dayOfWeek))
            .OrderByDescending(wp => wp.ConfidenceScore)
            .Take(100)
            .ToListAsync(cancellationToken);

        var suggestions = new List<AllocationSuggestion>();

        foreach (var pattern in patterns)
        {
            if (pattern.Project == null || !pattern.Project.AllowTimeEntry)
                continue;

            // Skip completed or cancelled projects
            if (pattern.Project.Status == "Completed" || pattern.Project.Status == "Cancelled")
                continue;

            // Check if project is over budget
            var isOverBudget = pattern.Project.IsOverBudget();

            // Calculate suggested hours based on historical average
            var suggestedHours = Math.Min(pattern.AverageHours, totalHoursAvailable);

            // Build reason string
            var reason = BuildPatternReason(pattern, date);

            suggestions.Add(new AllocationSuggestion
            {
                ProjectId = pattern.ProjectId,
                ProjectCode = pattern.Project.ProjectCode,
                ProjectName = pattern.Project.ProjectName,
                SuggestedHours = suggestedHours,
                ConfidenceScore = isOverBudget
                    ? Math.Max(pattern.ConfidenceScore - 20, 30) // Lower confidence if over budget
                    : pattern.ConfidenceScore,
                Source = "WorkPattern",
                Reason = reason,
                Evidence = new Dictionary<string, object>
                {
                    { "occurrences", pattern.OccurrenceCount },
                    { "average_hours", pattern.AverageHours },
                    { "last_seen", pattern.LastOccurrence },
                    { "is_over_budget", isOverBudget }
                }
            });
        }

        return suggestions;
    }

    private async Task<List<AllocationSuggestion>> GenerateFromProjectMembershipAsync(
        Guid employeeId,
        DateTime date,
        decimal totalHoursAvailable,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Find active project memberships for this employee
        // PRODUCTION FIX: AsNoTracking for read-only queries + Take limit to prevent memory exhaustion
        var memberships = await _context.ProjectMembers
            .AsNoTracking()
            .Include(pm => pm.Project)
            .Where(pm => pm.EmployeeId == employeeId
                && pm.IsActive
                && pm.Project!.AllowTimeEntry
                && pm.Project.Status == "Active"
                && (pm.RemovedDate == null || pm.RemovedDate > date))
            .Take(100)
            .ToListAsync(cancellationToken);

        var suggestions = new List<AllocationSuggestion>();

        foreach (var membership in memberships)
        {
            if (membership.Project == null)
                continue;

            // Calculate suggested hours based on expected hours per week
            var suggestedHours = membership.ExpectedHoursPerWeek.HasValue
                ? Math.Min(membership.ExpectedHoursPerWeek.Value / 5, totalHoursAvailable) // Divide by 5 for daily
                : totalHoursAvailable / Math.Max(memberships.Count, 1); // Equal distribution

            // Base confidence for active membership
            var confidence = 60;

            // Boost confidence if recently assigned
            var daysSinceAssignment = (date - membership.AssignedDate).TotalDays;
            if (daysSinceAssignment < 7)
                confidence += 15; // Recently assigned = likely working on it

            suggestions.Add(new AllocationSuggestion
            {
                ProjectId = membership.ProjectId,
                ProjectCode = membership.Project.ProjectCode,
                ProjectName = membership.Project.ProjectName,
                SuggestedHours = suggestedHours,
                ConfidenceScore = confidence,
                Source = "ProjectMembership",
                Reason = $"You are an active member of this project{(membership.Role != null ? $" as {membership.Role}" : "")}",
                Evidence = new Dictionary<string, object>
                {
                    { "role", membership.Role ?? "Member" },
                    { "assigned_date", membership.AssignedDate },
                    { "expected_hours_per_week", membership.ExpectedHoursPerWeek ?? 0 }
                }
            });
        }

        return suggestions;
    }

    private string BuildPatternReason(WorkPattern pattern, DateTime date)
    {
        var daysSinceLastOccurrence = (date - pattern.LastOccurrence).TotalDays;

        if (pattern.DayOfWeek != null)
        {
            var dayName = ((DayOfWeek)pattern.DayOfWeek.Value).ToString();
            return $"You typically work on this project on {dayName}s ({pattern.OccurrenceCount} times, avg {pattern.AverageHours:F1}h)";
        }

        if (daysSinceLastOccurrence <= 7)
        {
            return $"You worked on this project recently ({pattern.OccurrenceCount} times, avg {pattern.AverageHours:F1}h)";
        }

        return $"Historical pattern: {pattern.OccurrenceCount} occurrences, avg {pattern.AverageHours:F1}h";
    }

    private List<AllocationSuggestion> NormalizeHoursAllocation(
        List<AllocationSuggestion> suggestions,
        decimal totalHoursAvailable)
    {
        if (!suggestions.Any())
            return suggestions;

        var totalSuggestedHours = suggestions.Sum(s => s.SuggestedHours);

        // If total suggested hours exceed available hours, proportionally reduce
        if (totalSuggestedHours > totalHoursAvailable)
        {
            var scaleFactor = totalHoursAvailable / totalSuggestedHours;
            foreach (var suggestion in suggestions)
            {
                suggestion.SuggestedHours = Math.Round(suggestion.SuggestedHours * scaleFactor, 2);
            }
        }

        return suggestions;
    }

    public async Task LearnFromFeedbackAsync(
        Guid suggestionId,
        bool wasAccepted,
        decimal? finalHours,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _context.ProjectAllocationSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId, cancellationToken);

        if (suggestion == null)
            return;

        // Log the feedback for ML improvement
        var intelligenceEvent = TimesheetIntelligenceEvent.CreateInfo(
            wasAccepted ? "SuggestionAccepted" : "SuggestionRejected",
            $"Suggestion for {suggestion.SuggestedHours}h on project {suggestion.ProjectId} was {(wasAccepted ? "accepted" : "rejected")}",
            tenantId,
            suggestion.EmployeeId,
            new
            {
                suggestion_id = suggestionId,
                suggested_hours = suggestion.SuggestedHours,
                final_hours = finalHours,
                confidence_score = suggestion.ConfidenceScore,
                source = suggestion.SuggestionSource
            });

        intelligenceEvent.WasDecisionCorrect = wasAccepted;

        _context.TimesheetIntelligenceEvents.Add(intelligenceEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Learned from feedback: Suggestion {SuggestionId} was {Result}",
            suggestionId, wasAccepted ? "accepted" : "rejected");
    }

    public async Task UpdateWorkPatternsAsync(
        List<TimesheetProjectAllocation> confirmedAllocations,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        foreach (var allocation in confirmedAllocations)
        {
            var dayOfWeek = (int)allocation.Date.DayOfWeek;

            // Find or create work pattern
            var pattern = await _context.WorkPatterns
                .FirstOrDefaultAsync(wp =>
                    wp.EmployeeId == allocation.EmployeeId
                    && wp.ProjectId == allocation.ProjectId
                    && wp.DayOfWeek == dayOfWeek
                    && wp.TenantId == tenantId,
                    cancellationToken);

            if (pattern == null)
            {
                // Create new pattern
                pattern = new WorkPattern
                {
                    EmployeeId = allocation.EmployeeId,
                    ProjectId = allocation.ProjectId,
                    DayOfWeek = dayOfWeek,
                    TenantId = tenantId,
                    FirstOccurrence = allocation.Date,
                    LastOccurrence = allocation.Date,
                    OccurrenceCount = 1,
                    TotalHours = allocation.Hours,
                    AverageHours = allocation.Hours,
                    ConfidenceScore = 30 // Low initial confidence
                };

                _context.WorkPatterns.Add(pattern);
            }
            else
            {
                // Update existing pattern
                pattern.UpdatePattern(allocation.Hours);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated work patterns from {Count} confirmed allocations",
            confirmedAllocations.Count);
    }

    public async Task<int> GetConfidenceScoreAsync(
        Guid employeeId,
        Guid projectId,
        DateTime date,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        var pattern = await _context.WorkPatterns
            .FirstOrDefaultAsync(wp =>
                wp.EmployeeId == employeeId
                && wp.ProjectId == projectId
                && wp.DayOfWeek == dayOfWeek
                && wp.TenantId == tenantId
                && wp.IsActive,
                cancellationToken);

        return pattern?.ConfidenceScore ?? 0;
    }

    public async Task<bool> HasPatternChangedAsync(
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Check if employee has worked on different projects in last 14 vs previous 14 days
        var today = DateTime.UtcNow.Date;
        var twoWeeksAgo = today.AddDays(-14);
        var fourWeeksAgo = today.AddDays(-28);

        // PRODUCTION FIX: AsNoTracking for read-only queries + Take limit to prevent memory exhaustion
        var recentProjects = await _context.TimesheetProjectAllocations
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.TenantId == tenantId
                && a.Date >= twoWeeksAgo
                && a.Date < today)
            .Select(a => a.ProjectId)
            .Distinct()
            .Take(1000)
            .ToListAsync(cancellationToken);

        var previousProjects = await _context.TimesheetProjectAllocations
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.TenantId == tenantId
                && a.Date >= fourWeeksAgo
                && a.Date < twoWeeksAgo)
            .Select(a => a.ProjectId)
            .Distinct()
            .Take(1000)
            .ToListAsync(cancellationToken);

        // If more than 50% of projects are different, pattern has changed
        var commonProjects = recentProjects.Intersect(previousProjects).Count();
        var totalUniqueProjects = recentProjects.Union(previousProjects).Count();

        return totalUniqueProjects > 0 && ((double)commonProjects / totalUniqueProjects) < 0.5;
    }
}
