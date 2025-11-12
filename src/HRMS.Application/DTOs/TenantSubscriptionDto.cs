using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// DTO for tenant subscription and payment history
/// Used by tenant billing portal
/// </summary>
public class TenantSubscriptionDto
{
    public SubscriptionDetailsDto Subscription { get; set; } = new();
    public List<PaymentHistoryDto> Payments { get; set; } = new();
}

/// <summary>
/// Subscription details for current tenant
/// </summary>
public class SubscriptionDetailsDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool AutoRenew { get; set; }
    public DateTime? GracePeriodEndDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>
/// Payment history record for tenant
/// </summary>
public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public bool IsOverdue { get; set; }
}
