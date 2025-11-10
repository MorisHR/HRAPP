namespace HRMS.Core.Enums;

/// <summary>
/// PRODUCTION-GRADE: Subscription payment status tracking
/// FORTUNE 500 PATTERN: Salesforce, HubSpot, Zendesk billing systems
/// Used for manual payment tracking by SuperAdmin
/// </summary>
public enum SubscriptionPaymentStatus
{
    /// <summary>
    /// Payment is pending/awaiting confirmation (gray)
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment confirmed and processed successfully (green)
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Payment overdue but within grace period (orange)
    /// </summary>
    Overdue = 2,

    /// <summary>
    /// Payment failed or rejected (red)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment refunded (blue)
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Payment partially paid (yellow)
    /// </summary>
    PartiallyPaid = 5,

    /// <summary>
    /// Payment cancelled/waived by SuperAdmin (purple)
    /// </summary>
    Waived = 6
}
