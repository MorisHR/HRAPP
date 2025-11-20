using HRMS.Application.DTOs.DepartmentIntelligenceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Caching;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Department intelligence service with caching and query optimization
/// Implements scalable analytics for thousands of concurrent requests
/// FORTUNE 500 GRADE: Multi-tenant aware with distributed caching
/// </summary>
public class DepartmentIntelligenceService : TenantAwareServiceBase<DepartmentIntelligenceService>, IDepartmentIntelligenceService
{
    private readonly TenantDbContext _context;
    private readonly IDistributedCacheService _cache;

    private const int HEALTH_SCORE_CACHE_MINUTES = 15;
    private const int TURNOVER_RISK_CACHE_MINUTES = 15;
    private const int WORKLOAD_CACHE_MINUTES = 5; // More dynamic

    public DepartmentIntelligenceService(
        TenantDbContext context,
        IDistributedCacheService cache,
        ITenantContext tenantContext,
        ILogger<DepartmentIntelligenceService> logger)
        : base(tenantContext, logger)
    {
        _context = context;
        _cache = cache;
    }

    #region Public Methods

    public async Task<DepartmentHealthScoreDto> GetHealthScoreAsync(Guid departmentId)
    {
        return await GetCachedOrComputeAsync(
            cacheKey: $"dept_health:{departmentId}",
            computeFunc: () => ComputeHealthScoreAsync(departmentId),
            cacheDurationMinutes: HEALTH_SCORE_CACHE_MINUTES
        );
    }

    public async Task<TurnoverRiskAnalysisDto> GetTurnoverRiskAsync(Guid departmentId)
    {
        return await GetCachedOrComputeAsync(
            cacheKey: $"turnover_risk:{departmentId}",
            computeFunc: () => ComputeTurnoverRiskAsync(departmentId),
            cacheDurationMinutes: TURNOVER_RISK_CACHE_MINUTES
        );
    }

    public async Task<WorkloadDistributionDto> GetWorkloadDistributionAsync(Guid departmentId)
    {
        return await GetCachedOrComputeAsync(
            cacheKey: $"workload_dist:{departmentId}",
            computeFunc: () => ComputeWorkloadDistributionAsync(departmentId),
            cacheDurationMinutes: WORKLOAD_CACHE_MINUTES
        );
    }

    #endregion

    #region Health Score Computation

    private async Task<DepartmentHealthScoreDto> ComputeHealthScoreAsync(Guid departmentId)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get department name
        var department = await _context.Departments
            .Where(d => d.Id == departmentId && !d.IsDeleted)
            .Select(d => new { d.Name })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (department == null)
        {
            return new DepartmentHealthScoreDto
            {
                DepartmentId = departmentId,
                HealthScore = 0,
                HealthStatus = "not_found"
            };
        }

        // OPTIMIZED: Single query with aggregations
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var now = DateTime.UtcNow;

        // Get employees data - calculate tenure in C# to avoid DateDiffDay ambiguity
        var employees = await _context.Employees
            .Where(e => e.DepartmentId == departmentId && !e.IsDeleted)
            .Select(e => new
            {
                e.IsOffboarded,
                e.TerminationDate,
                e.JoiningDate,
                e.BasicSalary
            })
            .AsNoTracking()
            .ToListAsync();

        if (employees.Count == 0)
        {
            return new DepartmentHealthScoreDto
            {
                DepartmentId = departmentId,
                DepartmentName = department.Name,
                HealthScore = 0,
                HealthStatus = "no_data",
                ComputedAt = DateTime.UtcNow,
                CacheExpiresAt = DateTime.UtcNow.AddMinutes(HEALTH_SCORE_CACHE_MINUTES)
            };
        }

        // Compute aggregations in C#
        var data = new
        {
            TotalEmployees = employees.Count,
            ActiveEmployees = employees.Count(e => !e.IsOffboarded),
            TerminatedLast12Months = employees.Count(e =>
                e.TerminationDate.HasValue &&
                e.TerminationDate.Value >= oneYearAgo),
            AvgTenureDays = employees.Average(e => (now - e.JoiningDate).TotalDays),
            TotalSalaries = employees.Sum(e => e.BasicSalary)
        };

        if (data == null || data.ActiveEmployees == 0)
        {
            return new DepartmentHealthScoreDto
            {
                DepartmentId = departmentId,
                DepartmentName = department.Name,
                HealthScore = 0,
                HealthStatus = "no_data",
                ComputedAt = DateTime.UtcNow,
                CacheExpiresAt = DateTime.UtcNow.AddMinutes(HEALTH_SCORE_CACHE_MINUTES)
            };
        }

        // Calculate metrics
        var turnoverRate = (data.TerminatedLast12Months / (double)data.ActiveEmployees) * 100;
        var avgTenureYears = data.AvgTenureDays / 365.25;

        // Calculate health score components
        var turnoverScore = Math.Max(0, 100 - (turnoverRate * 5)); // Lower turnover = higher score
        var tenureScore = avgTenureYears switch
        {
            < 1 => 50,
            >= 1 and < 2 => 70,
            >= 2 and < 5 => 100,
            >= 5 and < 7 => 90,
            _ => 70
        };

        // Weighted health score
        var healthScore = (int)((turnoverScore * 0.6) + (tenureScore * 0.4));

        // Identify risk factors
        var riskFactors = new List<RiskFactorDto>();
        var recommendations = new List<string>();

        var companyAvgTurnover = 12.0m; // Industry benchmark

        if ((decimal)turnoverRate > companyAvgTurnover)
        {
            var exceeds = ((decimal)turnoverRate - companyAvgTurnover) / companyAvgTurnover * 100;
            riskFactors.Add(new RiskFactorDto
            {
                Factor = "high_turnover",
                Severity = turnoverRate > 20 ? "high" : "medium",
                Value = (decimal)turnoverRate,
                Benchmark = companyAvgTurnover,
                Description = $"Turnover rate {exceeds:F0}% above company average"
            });
            recommendations.Add("Conduct exit interviews to identify retention issues");
            recommendations.Add("Review compensation and benefits package");
        }

        if (avgTenureYears < 1.5)
        {
            riskFactors.Add(new RiskFactorDto
            {
                Factor = "low_tenure",
                Severity = "medium",
                Value = (decimal)avgTenureYears,
                Benchmark = 2.5m,
                Description = "Average tenure below optimal range"
            });
            recommendations.Add("Implement mentorship program to improve retention");
            recommendations.Add("Review onboarding process for new hires");
        }

        stopwatch.Stop();
        Logger.LogInformation(
            "Computed health score for department {DeptId} in {Ms}ms: Score={Score}",
            departmentId, stopwatch.ElapsedMilliseconds, healthScore);

        return new DepartmentHealthScoreDto
        {
            DepartmentId = departmentId,
            DepartmentName = department.Name,
            HealthScore = healthScore,
            HealthStatus = GetHealthStatus(healthScore),
            Metrics = new DepartmentMetricsDto
            {
                TurnoverRate = (decimal)turnoverRate,
                AvgTenureYears = (decimal)avgTenureYears,
                TotalEmployees = data.TotalEmployees,
                ActiveEmployees = data.ActiveEmployees,
                EmployeeSatisfaction = 75, // Placeholder
                BudgetVariance = 0, // Placeholder
                AvgPerformance = 3.5m // Placeholder
            },
            RiskFactors = riskFactors,
            Recommendations = recommendations,
            ComputedAt = DateTime.UtcNow,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(HEALTH_SCORE_CACHE_MINUTES)
        };
    }

    private string GetHealthStatus(int score) => score switch
    {
        >= 80 => "excellent",
        >= 60 => "good",
        >= 40 => "moderate",
        >= 20 => "poor",
        _ => "critical"
    };

    #endregion

    #region Turnover Risk Computation

    private async Task<TurnoverRiskAnalysisDto> ComputeTurnoverRiskAsync(Guid departmentId)
    {
        var stopwatch = Stopwatch.StartNew();

        var department = await _context.Departments
            .Where(d => d.Id == departmentId && !d.IsDeleted)
            .Select(d => new { d.Name })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (department == null)
        {
            return new TurnoverRiskAnalysisDto
            {
                DepartmentId = departmentId,
                TurnoverRisk = "not_found",
                RiskScore = 0
            };
        }

        // OPTIMIZED: Single query with all needed data
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
        var now = DateTime.UtcNow;
        var employeeData = await _context.Employees
            .Where(e => e.DepartmentId == departmentId && !e.IsDeleted && !e.IsOffboarded)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.JoiningDate,
                e.BasicSalary,
                e.IsActive
            })
            .AsNoTracking()
            .ToListAsync();

        // Calculate tenure in C# to avoid DateDiffDay ambiguity
        var employeesWithTenure = employeeData.Select(e => new
        {
            e.Id,
            e.FirstName,
            e.LastName,
            e.JoiningDate,
            e.BasicSalary,
            e.IsActive,
            TenureYears = (now - e.JoiningDate).TotalDays / 365.25
        }).ToList();

        // Count recent terminations
        var recentTerminations = await _context.Employees
            .Where(e => e.DepartmentId == departmentId &&
                       e.TerminationDate.HasValue &&
                       e.TerminationDate.Value >= ninetyDaysAgo)
            .CountAsync();

        // Calculate risk for each employee
        var atRiskEmployees = new List<AtRiskEmployeeDto>();
        foreach (var emp in employeesWithTenure)
        {
            var riskFactors = new List<string>();
            var riskScore = 0;

            // Factor 1: Tenure (2-3 years = flight risk)
            if (emp.TenureYears >= 2 && emp.TenureYears <= 3)
            {
                riskScore += 30;
                riskFactors.Add("Flight risk tenure (2-3 years)");
            }

            // Factor 2: Recent terminations in department
            if (recentTerminations >= 3)
            {
                riskScore += 20;
                riskFactors.Add("Multiple recent departures in department");
            }

            // Factor 3: Inactive status
            if (!emp.IsActive)
            {
                riskScore += 25;
                riskFactors.Add("Employee marked as inactive");
            }

            if (riskScore >= 40)
            {
                atRiskEmployees.Add(new AtRiskEmployeeDto
                {
                    EmployeeId = emp.Id,
                    EmployeeName = $"{emp.FirstName} {emp.LastName}",
                    RiskLevel = riskScore >= 60 ? "high" : "medium",
                    RiskScore = riskScore,
                    RiskReasons = riskFactors,
                    RecommendedAction = riskScore >= 60
                        ? "URGENT: Conduct retention conversation within 7 days"
                        : "Schedule 1-on-1 to discuss career goals"
                });
            }
        }

        // Calculate overall risk
        var totalEmployees = employeeData.Count;
        var atRiskCount = atRiskEmployees.Count;
        var riskPercentage = totalEmployees > 0 ? (atRiskCount / (decimal)totalEmployees) * 100 : 0;

        var overallRisk = riskPercentage switch
        {
            >= 30 => "critical",
            >= 20 => "high",
            >= 10 => "medium",
            >= 5 => "low",
            _ => "none"
        };

        var overallRiskScore = (int)Math.Min(100, riskPercentage * 3);

        // Generate risk factors
        var riskFactorsList = new List<TurnoverRiskFactorDto>();
        var recommendationsList = new List<string>();

        if (recentTerminations >= 3)
        {
            riskFactorsList.Add(new TurnoverRiskFactorDto
            {
                Factor = "recent_terminations",
                Impact = "high",
                Description = $"{recentTerminations} employees left in last 90 days",
                AffectedEmployees = totalEmployees
            });
            recommendationsList.Add("Conduct exit interviews to identify root causes");
        }

        if (atRiskCount > 0)
        {
            recommendationsList.Add($"URGENT: Conduct retention conversations with {atRiskCount} at-risk employees");
            recommendationsList.Add("Review compensation against market rates");
            recommendationsList.Add("Implement stay interviews for key talent");
        }

        stopwatch.Stop();
        Logger.LogInformation(
            "Computed turnover risk for department {DeptId} in {Ms}ms: {AtRisk} at risk out of {Total}",
            departmentId, stopwatch.ElapsedMilliseconds, atRiskCount, totalEmployees);

        return new TurnoverRiskAnalysisDto
        {
            DepartmentId = departmentId,
            DepartmentName = department.Name,
            TurnoverRisk = overallRisk,
            RiskScore = overallRiskScore,
            AtRiskEmployees = atRiskCount,
            TotalEmployees = totalEmployees,
            RiskPercentage = riskPercentage,
            Predictions = new TurnoverPredictionDto
            {
                ExpectedExitsNext90Days = (int)Math.Ceiling(atRiskCount * 0.3m), // 30% of at-risk may leave
                ConfidenceLevel = 0.65m,
                EstimatedCost = atRiskCount * 75000 // Avg replacement cost
            },
            RiskFactors = riskFactorsList,
            AtRiskEmployeesList = atRiskEmployees.OrderByDescending(e => e.RiskScore).Take(10).ToList(),
            Recommendations = recommendationsList,
            ComputedAt = DateTime.UtcNow,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(TURNOVER_RISK_CACHE_MINUTES)
        };
    }

    #endregion

    #region Workload Distribution Computation

    private async Task<WorkloadDistributionDto> ComputeWorkloadDistributionAsync(Guid departmentId)
    {
        var stopwatch = Stopwatch.StartNew();

        var department = await _context.Departments
            .Where(d => d.Id == departmentId && !d.IsDeleted)
            .Select(d => new { d.Name })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (department == null)
        {
            return new WorkloadDistributionDto
            {
                DepartmentId = departmentId,
                WorkloadBalance = "not_found"
            };
        }

        // OPTIMIZED: Query timesheet data for last 8 weeks
        var eightWeeksAgo = DateTime.UtcNow.AddDays(-56);
        var workloadData = await _context.TimesheetEntries
            .Where(t => t.Timesheet != null && t.Timesheet.Employee != null &&
                       t.Timesheet.Employee.DepartmentId == departmentId &&
                       t.Date >= eightWeeksAgo &&
                       !t.Timesheet.Employee.IsDeleted &&
                       !t.Timesheet.Employee.IsOffboarded)
            .GroupBy(t => new
            {
                EmployeeId = t.Timesheet!.EmployeeId,
                EmployeeName = t.Timesheet.Employee!.FirstName + " " + t.Timesheet.Employee.LastName
            })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.EmployeeName,
                TotalHours = g.Sum(t => t.ActualHours),
                WeekCount = g.Select(t => t.Date.Date).Distinct().Count() / 7.0
            })
            .AsNoTracking()
            .ToListAsync();

        var overloaded = new List<OverloadedEmployeeDto>();
        var underutilized = new List<UnderutilizedEmployeeDto>();
        var balanced = 0;

        var threshold = 40m; // Standard 40-hour work week

        foreach (var emp in workloadData)
        {
            var avgWeeklyHours = emp.WeekCount > 0 ? (decimal)(emp.TotalHours / (decimal)emp.WeekCount) : 0;

            if (avgWeeklyHours > threshold * 1.15m) // 15% over threshold
            {
                var overloadPercentage = ((avgWeeklyHours - threshold) / threshold) * 100;
                var burnoutRisk = avgWeeklyHours switch
                {
                    >= 60 => "critical",
                    >= 55 => "high",
                    >= 50 => "medium",
                    _ => "low"
                };

                overloaded.Add(new OverloadedEmployeeDto
                {
                    EmployeeId = emp.EmployeeId,
                    Name = emp.EmployeeName,
                    AvgWeeklyHours = avgWeeklyHours,
                    Threshold = threshold,
                    OverloadPercentage = overloadPercentage,
                    ConsecutiveWeeks = 8, // Simplified - would need week-by-week analysis
                    BurnoutRisk = burnoutRisk,
                    RecommendedAction = burnoutRisk == "critical"
                        ? "URGENT: Mandate time off immediately"
                        : "Redistribute workload or hire additional resources"
                });
            }
            else if (avgWeeklyHours < threshold * 0.80m) // 20% under threshold
            {
                var utilizationPercentage = (avgWeeklyHours / threshold) * 100;
                var availableHours = threshold - avgWeeklyHours;

                underutilized.Add(new UnderutilizedEmployeeDto
                {
                    EmployeeId = emp.EmployeeId,
                    Name = emp.EmployeeName,
                    AvgWeeklyHours = avgWeeklyHours,
                    Threshold = threshold,
                    UtilizationPercentage = utilizationPercentage,
                    AvailableHours = availableHours,
                    Suggestion = $"Can handle {availableHours:F1} more hours per week"
                });
            }
            else
            {
                balanced++;
            }
        }

        var totalEmployees = workloadData.Count;
        var avgHours = totalEmployees > 0 ? workloadData.Average(e => e.TotalHours / (decimal)e.WeekCount) : 0;

        // Calculate imbalance score
        var imbalanceScore = totalEmployees > 0
            ? (int)(((overloaded.Count + underutilized.Count) / (double)totalEmployees) * 100)
            : 0;

        var workloadBalance = imbalanceScore switch
        {
            < 10 => "excellent",
            < 25 => "good",
            < 40 => "fair",
            < 60 => "poor",
            _ => "critical"
        };

        var recommendations = new List<string>();
        if (overloaded.Any())
        {
            recommendations.Add($"Redistribute workload for {overloaded.Count} overloaded employees");
            if (overloaded.Any(e => e.BurnoutRisk == "critical"))
            {
                recommendations.Add("URGENT: Mandate time off for employees at critical burnout risk");
            }
        }
        if (overloaded.Any() && underutilized.Any())
        {
            var totalOverload = overloaded.Sum(e => e.AvgWeeklyHours - e.Threshold);
            var totalAvailable = underutilized.Sum(e => e.AvailableHours);
            recommendations.Add($"Can redistribute {Math.Min(totalOverload, totalAvailable):F1} hours from overloaded to underutilized");
        }

        stopwatch.Stop();
        Logger.LogInformation(
            "Computed workload distribution for department {DeptId} in {Ms}ms: {Overloaded} overloaded, {Underutilized} underutilized",
            departmentId, stopwatch.ElapsedMilliseconds, overloaded.Count, underutilized.Count);

        return new WorkloadDistributionDto
        {
            DepartmentId = departmentId,
            DepartmentName = department.Name,
            WorkloadBalance = workloadBalance,
            Summary = new WorkloadSummaryDto
            {
                TotalEmployees = totalEmployees,
                OverloadedCount = overloaded.Count,
                UnderutilizedCount = underutilized.Count,
                BalancedCount = balanced,
                AvgWeeklyHours = (decimal)avgHours,
                TargetWeeklyHours = threshold,
                ImbalanceScore = imbalanceScore
            },
            OverloadedEmployees = overloaded.OrderByDescending(e => e.AvgWeeklyHours).ToList(),
            UnderutilizedEmployees = underutilized.OrderBy(e => e.AvgWeeklyHours).ToList(),
            Recommendations = recommendations,
            ComputedAt = DateTime.UtcNow,
            CacheExpiresAt = DateTime.UtcNow.AddMinutes(WORKLOAD_CACHE_MINUTES)
        };
    }

    #endregion

    #region Caching Helper

    /// <summary>
    /// Generic cache-or-compute helper with multi-tenant support
    /// SECURITY: Uses tenant context from TenantAwareServiceBase for proper isolation
    /// </summary>
    private async Task<T> GetCachedOrComputeAsync<T>(
        string cacheKey,
        Func<Task<T>> computeFunc,
        int cacheDurationMinutes) where T : class
    {
        // Get tenant ID securely from base class
        var tenantId = GetCurrentTenantIdOrThrow(nameof(GetCachedOrComputeAsync));

        // Multi-tenant cache key with proper tenant isolation
        var fullKey = $"tenant:{tenantId}:dept-intelligence:{cacheKey}";

        // Try cache first
        var cached = await _cache.GetAsync<T>(fullKey);
        if (cached != null)
        {
            Logger.LogDebug("Cache HIT: {Key}", fullKey);
            return cached;
        }

        Logger.LogDebug("Cache MISS: {Key} - computing...", fullKey);

        // Compute
        var result = await computeFunc();

        // Cache result with proper TTL (passing int minutes directly)
        await _cache.SetAsync(fullKey, result, absoluteExpirationMinutes: cacheDurationMinutes);

        return result;
    }

    #endregion
}
