using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// PRODUCTION-GRADE: Subscription management service for Fortune 500 yearly billing
/// SECURITY: All methods validate tenant ownership and SuperAdmin authorization
/// PERFORMANCE: Optimized queries with caching where appropriate
/// COMPLIANCE: Full audit trail for all payment operations
/// </summary>
public interface ISubscriptionManagementService
{
    // ============================================
    // PAYMENT MANAGEMENT
    // ============================================

    /// <summary>
    /// Create a new subscription payment record (manual payment tracking)
    /// CALLED WHEN: Tenant created, subscription renewed, tier upgraded
    /// </summary>
    Task<SubscriptionPayment> CreatePaymentRecordAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        decimal amountMUR,
        DateTime dueDate,
        EmployeeTier tier,
        string? description = null,
        bool calculateTax = true);

    /// <summary>
    /// SuperAdmin marks payment as received/paid
    /// SECURITY: Requires SuperAdmin role
    /// AUDIT: Logs who processed the payment and when
    /// </summary>
    Task<(bool Success, string Message)> MarkPaymentAsPaidAsync(
        Guid paymentId,
        string processedBy,
        string paymentReference,
        string paymentMethod,
        DateTime? paidDate = null,
        string? notes = null);

    /// <summary>
    /// SuperAdmin marks payment as overdue
    /// TRIGGER: Background job or manual action
    /// </summary>
    Task<(bool Success, string Message)> MarkPaymentAsOverdueAsync(Guid paymentId);

    /// <summary>
    /// SuperAdmin waives payment (special circumstances)
    /// SECURITY: Requires business reason
    /// AUDIT: High-severity audit log entry
    /// </summary>
    Task<(bool Success, string Message)> WaivePaymentAsync(
        Guid paymentId,
        string waivedBy,
        string reason);

    /// <summary>
    /// Get payment by ID with full details
    /// </summary>
    Task<SubscriptionPayment?> GetPaymentByIdAsync(Guid paymentId);

    /// <summary>
    /// Get all payments for a tenant (for billing history)
    /// </summary>
    Task<List<SubscriptionPayment>> GetPaymentsByTenantIdAsync(
        Guid tenantId,
        bool includeArchived = false);

    /// <summary>
    /// Get overdue payments (for dashboard and reminders)
    /// PERFORMANCE: Indexed query on DueDate and Status
    /// </summary>
    Task<List<SubscriptionPayment>> GetOverduePaymentsAsync();

    /// <summary>
    /// Get payments due within specified days (for proactive reminders)
    /// EXAMPLE: GetUpcomingPaymentsAsync(30) returns payments due in next 30 days
    /// </summary>
    Task<List<SubscriptionPayment>> GetUpcomingPaymentsAsync(int daysAhead = 30);

    /// <summary>
    /// Get pending payments (awaiting SuperAdmin confirmation)
    /// </summary>
    Task<List<SubscriptionPayment>> GetPendingPaymentsAsync();

    // ============================================
    // NOTIFICATION MANAGEMENT (Email Deduplication)
    // ============================================

    /// <summary>
    /// Check if notification has already been sent (deduplication)
    /// FORTUNE 500: Prevents duplicate emails (Stripe/Chargebee pattern)
    /// PERFORMANCE: Fast indexed lookup
    /// </summary>
    Task<bool> HasNotificationBeenSentAsync(
        Guid tenantId,
        SubscriptionNotificationType notificationType,
        DateTime? withinLastHours = null);

    /// <summary>
    /// Log that a notification was sent (for audit and deduplication)
    /// IMMUTABLE: Notification logs are never deleted
    /// </summary>
    Task LogNotificationSentAsync(
        Guid tenantId,
        SubscriptionNotificationType notificationType,
        string recipientEmail,
        string subject,
        bool success,
        string? error = null,
        Guid? paymentId = null,
        bool requiresFollowUp = false);

    /// <summary>
    /// Get notification history for a tenant
    /// </summary>
    Task<List<SubscriptionNotificationLog>> GetNotificationHistoryAsync(
        Guid tenantId,
        SubscriptionNotificationType? filterByType = null);

    /// <summary>
    /// Mark follow-up as completed (for critical notifications)
    /// </summary>
    Task MarkFollowUpCompletedAsync(Guid notificationId, string notes);

    // ============================================
    // SUBSCRIPTION LIFECYCLE
    // ============================================

    /// <summary>
    /// Get tenants requiring renewal notification at specified days before expiry
    /// CALLED BY: Background job
    /// EXAMPLE: GetTenantsNeedingRenewalNotificationAsync(30) = expiring in 30 days
    /// DEDUPLICATION: Only returns tenants who haven't received this notification yet
    /// </summary>
    Task<List<Tenant>> GetTenantsNeedingRenewalNotificationAsync(
        int daysUntilExpiry,
        SubscriptionNotificationType notificationType);

    /// <summary>
    /// Get tenants with expired subscriptions (in grace period)
    /// GRACE PERIOD: 14 days by default (configurable per tenant)
    /// </summary>
    Task<List<Tenant>> GetExpiredTenantsInGracePeriodAsync();

    /// <summary>
    /// Get tenants whose grace period has ended (ready for suspension)
    /// CRITICAL: These tenants will be auto-suspended
    /// </summary>
    Task<List<Tenant>> GetTenantsToSuspendAsync();

    /// <summary>
    /// Get tenants with expired trials (for conversion reminders)
    /// </summary>
    Task<List<Tenant>> GetTenantsWithExpiredTrialAsync();

    /// <summary>
    /// Renew subscription for specified number of years
    /// CREATES: New payment record for renewal period
    /// UPDATES: SubscriptionEndDate on tenant
    /// </summary>
    Task<(bool Success, string Message, SubscriptionPayment? Payment)> RenewSubscriptionAsync(
        Guid tenantId,
        int years = 1,
        string? processedBy = null);

    /// <summary>
    /// Auto-create renewal payment record (30-60 days before expiry)
    /// FORTUNE 500: Proactive payment generation
    /// DEDUPLICATION: Checks if renewal payment already exists
    /// </summary>
    Task<SubscriptionPayment?> CreateRenewalPaymentIfNeededAsync(Guid tenantId);

    /// <summary>
    /// Convert trial to paid subscription
    /// CREATES: First year payment record
    /// UPDATES: Tenant status and subscription dates
    /// </summary>
    Task<(bool Success, string Message, SubscriptionPayment? Payment)> ConvertTrialToPaidAsync(
        Guid tenantId,
        decimal yearlyPriceMUR);

    // ============================================
    // TIER UPGRADE / DOWNGRADE (Pro-Rated)
    // ============================================

    /// <summary>
    /// Calculate pro-rated amount for mid-subscription tier change
    /// FORMULA: (New yearly price - Old yearly price) * (Months remaining / 12)
    /// TAX: Includes Mauritius VAT calculation if applicable
    /// </summary>
    Task<(decimal SubtotalMUR, decimal TaxMUR, decimal TotalMUR, int MonthsRemaining)> CalculateProRatedAmountAsync(
        Guid tenantId,
        decimal newYearlyPriceMUR,
        bool includeTax = true);

    /// <summary>
    /// Create pro-rated payment for tier upgrade
    /// CALLED WHEN: Tenant upgrades mid-subscription
    /// EXAMPLE: Upgrade from 1-50 to 51-200 employees halfway through year
    /// </summary>
    Task<SubscriptionPayment?> CreateProRatedPaymentAsync(
        Guid tenantId,
        EmployeeTier newTier,
        decimal newYearlyPriceMUR,
        string reason);

    // ============================================
    // TAX CALCULATION (Mauritius VAT)
    // ============================================

    /// <summary>
    /// Calculate Mauritius VAT (15%) and return breakdown
    /// TAX EXEMPT: Government entities, international organizations
    /// </summary>
    Task<(decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total)> CalculateTaxAsync(
        decimal amountMUR,
        bool isGovernmentEntity);

    /// <summary>
    /// Get applicable tax rate for tenant
    /// MAURITIUS: Standard 15% VAT, with exemptions
    /// </summary>
    Task<decimal> GetTaxRateForTenantAsync(Guid tenantId);

    // ============================================
    // REVENUE ANALYTICS (SuperAdmin Dashboard)
    // ============================================

    /// <summary>
    /// Calculate Annual Recurring Revenue (ARR)
    /// FORMULA: Sum of all active subscription yearly prices
    /// </summary>
    Task<decimal> GetAnnualRecurringRevenueAsync();

    /// <summary>
    /// Get overdue amount across all tenants
    /// CRITICAL: For cash flow monitoring
    /// </summary>
    Task<decimal> GetTotalOverdueAmountAsync();

    /// <summary>
    /// Get expected revenue from upcoming renewals (next N days)
    /// EXAMPLE: GetUpcomingRevenueAsync(30) = revenue expected in next 30 days
    /// </summary>
    Task<decimal> GetUpcomingRevenueAsync(int daysAhead = 30);

    /// <summary>
    /// Calculate renewal rate (% of subscriptions that renewed)
    /// FORTUNE 500 KPI: Target 90%+
    /// </summary>
    Task<decimal> GetRenewalRateAsync(DateTime? since = null);

    /// <summary>
    /// Calculate churn rate (% of subscriptions cancelled/not renewed)
    /// FORTUNE 500 KPI: Target <5%
    /// </summary>
    Task<decimal> GetChurnRateAsync(DateTime? since = null);

    /// <summary>
    /// Get revenue dashboard metrics
    /// COMPREHENSIVE: All key financial metrics in one call
    /// PERFORMANCE: Cached for 5 minutes
    /// </summary>
    Task<RevenueDashboard> GetRevenueDashboardAsync();

    // ============================================
    // PAYMENT VALIDATION
    // ============================================

    /// <summary>
    /// Validate that payment belongs to tenant (security check)
    /// </summary>
    Task<bool> ValidatePaymentOwnershipAsync(Guid paymentId, Guid tenantId);

    /// <summary>
    /// Check if tenant has any overdue payments
    /// </summary>
    Task<bool> HasOverduePaymentsAsync(Guid tenantId);

    /// <summary>
    /// Get days until subscription expires
    /// NULL: No expiry date set (trial or lifetime?)
    /// NEGATIVE: Already expired (in grace period or suspended)
    /// </summary>
    Task<int?> GetDaysUntilSubscriptionExpiryAsync(Guid tenantId);
}

/// <summary>
/// DTO for revenue dashboard
/// </summary>
public class RevenueDashboard
{
    public int TotalActiveSubscriptions { get; set; }
    public decimal AnnualRecurringRevenueMUR { get; set; }
    public decimal MonthlyRecurringRevenueMUR { get; set; } // ARR / 12
    public decimal OverdueAmountMUR { get; set; }
    public int OverduePaymentCount { get; set; }
    public decimal UpcomingRevenue30DaysMUR { get; set; }
    public int UpcomingRenewals30DaysCount { get; set; }
    public decimal RenewalRate { get; set; } // Percentage (0-100)
    public decimal ChurnRate { get; set; } // Percentage (0-100)
    public Dictionary<EmployeeTier, int> TenantsByTier { get; set; } = new();
    public Dictionary<SubscriptionPaymentStatus, int> PaymentsByStatus { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}
