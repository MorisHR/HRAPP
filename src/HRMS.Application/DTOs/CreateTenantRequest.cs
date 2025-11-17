using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Request DTO for creating a new tenant
/// Employee tier-based pricing model
/// </summary>
public class CreateTenantRequest
{
    // Company Information
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    // Employee Tier (replaces old subscription plan)
    public EmployeeTier EmployeeTier { get; set; }

    // Resource Limits (auto-populated from tier on backend)
    public int MaxUsers { get; set; }
    public int MaxStorageGB { get; set; }
    public int ApiCallsPerMonth { get; set; }

    /// <summary>
    /// FORTUNE 500 PATTERN: Yearly subscription price in Mauritian Rupees
    /// Annual billing reduces churn (Salesforce, HubSpot, Zendesk pattern)
    /// </summary>
    public decimal YearlyPriceMUR { get; set; }

    // Admin User Details (for activation email)
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;

    // Tenant Type
    public bool IsGovernmentEntity { get; set; } = false;

    /// <summary>
    /// FORTUNE 500 PATTERN: Industry sector for compliance and reporting
    /// Nullable = backwards compatible (existing API calls work)
    /// References: master.IndustrySectors table
    /// </summary>
    public int? SectorId { get; set; }

    // Subscription Period (optional - for trial/paid setup)
    public DateTime? TrialEndDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}
