using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// PRODUCTION-GRADE: Subscription notification tracking (prevents duplicate emails)
/// FORTUNE 500 PATTERN: Stripe, Chargebee notification deduplication
/// PERFORMANCE: Indexed on TenantId, NotificationType, SentDate
/// SECURITY: Immutable logs for audit compliance
/// </summary>
public class SubscriptionNotificationLog : BaseEntity
{
    /// <summary>
    /// Foreign key to Tenant
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Type of notification sent
    /// </summary>
    public SubscriptionNotificationType NotificationType { get; set; }

    /// <summary>
    /// Date/time when notification was sent
    /// </summary>
    public DateTime SentDate { get; set; }

    /// <summary>
    /// Recipient email address
    /// AUDIT: Track exactly who received the notification
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line
    /// </summary>
    public string EmailSubject { get; set; } = string.Empty;

    /// <summary>
    /// Was the email delivered successfully?
    /// FORTUNE 500: Track delivery status for follow-up
    /// </summary>
    public bool DeliverySuccess { get; set; }

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    public string? DeliveryError { get; set; }

    /// <summary>
    /// Subscription end date at the time notification was sent
    /// AUDIT: Historical record of what was communicated
    /// </summary>
    public DateTime SubscriptionEndDateAtNotification { get; set; }

    /// <summary>
    /// Days until expiry at time of notification
    /// AUDIT: Track notification timing
    /// </summary>
    public int DaysUntilExpiryAtNotification { get; set; }

    /// <summary>
    /// Related subscription payment ID (if applicable)
    /// </summary>
    public Guid? SubscriptionPaymentId { get; set; }

    /// <summary>
    /// Should this notification trigger a follow-up?
    /// FORTUNE 500: Escalation tracking for critical warnings
    /// </summary>
    public bool RequiresFollowUp { get; set; }

    /// <summary>
    /// Follow-up completed date
    /// </summary>
    public DateTime? FollowUpCompletedDate { get; set; }

    /// <summary>
    /// Notes about follow-up action taken
    /// </summary>
    public string? FollowUpNotes { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual SubscriptionPayment? SubscriptionPayment { get; set; }
}
