namespace HRMS.Core.Enums;

/// <summary>
/// Payment status for payslips
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Pending - awaiting payment
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Paid - payment completed successfully
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Failed - payment failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Cancelled - payment cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// OnHold - payment on hold
    /// </summary>
    OnHold = 5
}
