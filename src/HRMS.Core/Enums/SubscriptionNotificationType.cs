namespace HRMS.Core.Enums;

/// <summary>
/// PRODUCTION-GRADE: Subscription notification types for email automation
/// FORTUNE 500 PATTERN: Multi-stage reminder system (Stripe, Chargebee, Recurly)
/// Tracks which notifications have been sent to prevent duplicates
/// </summary>
public enum SubscriptionNotificationType
{
    /// <summary>
    /// 30 days before subscription expiry
    /// </summary>
    Reminder30Days = 0,

    /// <summary>
    /// 15 days before subscription expiry
    /// </summary>
    Reminder15Days = 1,

    /// <summary>
    /// 7 days before subscription expiry
    /// </summary>
    Reminder7Days = 2,

    /// <summary>
    /// 3 days before subscription expiry
    /// </summary>
    Reminder3Days = 3,

    /// <summary>
    /// 1 day before subscription expiry
    /// </summary>
    Reminder1Day = 4,

    /// <summary>
    /// On expiry day (grace period starts)
    /// </summary>
    ExpiryNotification = 5,

    /// <summary>
    /// Grace period warning (days 1-7 after expiry)
    /// </summary>
    GracePeriodWarning = 6,

    /// <summary>
    /// Critical warning (days 8-14 after expiry)
    /// </summary>
    CriticalWarning = 7,

    /// <summary>
    /// Final warning before suspension
    /// </summary>
    FinalWarning = 8,

    /// <summary>
    /// Tenant suspended due to non-payment
    /// </summary>
    SuspensionNotification = 9,

    /// <summary>
    /// Payment received and subscription renewed
    /// </summary>
    RenewalConfirmation = 10,

    /// <summary>
    /// Payment reminder to SuperAdmin
    /// </summary>
    AdminPaymentReminder = 11
}
