using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Response DTO for tenant information
/// </summary>
public class TenantDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;

    // Employee Tier (replaces SubscriptionPlan)
    public EmployeeTier EmployeeTier { get; set; }
    public string EmployeeTierDisplay { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }

    // Resource Limits and Usage
    public int MaxUsers { get; set; }
    public int CurrentUserCount { get; set; }
    public int MaxStorageGB { get; set; }
    public int CurrentStorageGB { get; set; }
    public int ApiCallsPerMonth { get; set; }

    // Computed property for employee count (derived from CurrentUserCount)
    public int EmployeeCount => CurrentUserCount;

    // Dates
    public DateTime CreatedAt { get; set; }
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }

    // Suspension/Deletion Info
    public string? SuspensionReason { get; set; }
    public DateTime? SuspensionDate { get; set; }
    public DateTime? SoftDeleteDate { get; set; }
    public string? DeletionReason { get; set; }
    public int? DaysUntilHardDelete { get; set; }

    // Admin User
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}
