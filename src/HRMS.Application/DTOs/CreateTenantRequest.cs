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
    public decimal MonthlyPrice { get; set; }

    // Admin User Details
    public string AdminUserName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
