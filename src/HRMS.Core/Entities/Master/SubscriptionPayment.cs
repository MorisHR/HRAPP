using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// PRODUCTION-GRADE: Subscription payment history tracking
/// FORTUNE 500 PATTERN: Salesforce, HubSpot, Zendesk billing audit trail
/// SECURITY: Immutable payment records with full audit trail
/// PERFORMANCE: Indexed on TenantId, PaymentDate for fast queries
/// </summary>
public class SubscriptionPayment : BaseEntity
{
    /// <summary>
    /// Foreign key to Tenant
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Period start date for this subscription payment
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Period end date for this subscription payment (usually +365 days)
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Amount paid in Mauritian Rupees (MUR)
    /// PRECISION: decimal(18,2) for accurate financial calculations
    /// </summary>
    public decimal AmountMUR { get; set; }

    /// <summary>
    /// Subtotal before tax (MUR)
    /// FORTUNE 500: Separate line items for tax compliance
    /// </summary>
    public decimal SubtotalMUR { get; set; }

    /// <summary>
    /// Tax rate applied (typically 0.15 for Mauritius VAT)
    /// STORED: As decimal (0.15 = 15%)
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Tax amount in MUR (SubtotalMUR * TaxRate)
    /// PRECISION: decimal(18,2) for accurate calculations
    /// </summary>
    public decimal TaxAmountMUR { get; set; }

    /// <summary>
    /// Total amount including tax (SubtotalMUR + TaxAmountMUR)
    /// FORTUNE 500: Clear breakdown for financial reporting
    /// </summary>
    public decimal TotalMUR { get; set; }

    /// <summary>
    /// Is this payment tax-exempt? (Government entities, international orgs)
    /// MAURITIUS: Some entities exempt from 15% VAT
    /// </summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>
    /// Payment status tracking
    /// </summary>
    public SubscriptionPaymentStatus Status { get; set; }

    /// <summary>
    /// Date when payment was marked as paid by SuperAdmin
    /// NULL if payment is still pending
    /// </summary>
    public DateTime? PaidDate { get; set; }

    /// <summary>
    /// SuperAdmin who confirmed the payment
    /// SECURITY: Tracks who processed payment for audit
    /// </summary>
    public string? ProcessedBy { get; set; }

    /// <summary>
    /// Payment reference number (invoice, receipt, bank transaction)
    /// FORTUNE 500: Critical for financial reconciliation
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Payment method (Bank Transfer, Cash, Cheque, etc.)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Due date for this payment
    /// FORTUNE 500: Grace period calculations based on this
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Notes about the payment (optional)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Employee tier at the time of payment
    /// AUDIT: Tracks tier changes over time
    /// </summary>
    public EmployeeTier EmployeeTier { get; set; }

    /// <summary>
    /// Is this payment overdue?
    /// PERFORMANCE: Computed property for quick filtering
    /// </summary>
    public bool IsOverdue => Status != SubscriptionPaymentStatus.Paid &&
                             DueDate < DateTime.UtcNow;

    /// <summary>
    /// Days until/since due date
    /// PERFORMANCE: Computed property for notifications
    /// Negative = overdue, Positive = days remaining
    /// </summary>
    public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;

    /// <summary>
    /// Is payment in grace period? (0-14 days overdue)
    /// </summary>
    public bool IsInGracePeriod => IsOverdue && DaysUntilDue >= -14;

    // Navigation property
    public virtual Tenant? Tenant { get; set; }
}
