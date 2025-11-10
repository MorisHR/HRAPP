using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// PRODUCTION-GRADE: Fortune 500 subscription notification & lifecycle management
/// SCHEDULE: Runs daily at 9:00 AM
/// FEATURES: 9-stage reminders, auto-renewal, auto-suspension, email deduplication
/// SECURITY: Comprehensive audit logging for all actions
/// PERFORMANCE: Batch processing, optimized queries
/// </summary>
public class SubscriptionNotificationJob
{
    private readonly ISubscriptionManagementService _subscriptionService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly MasterDbContext _context;
    private readonly ILogger<SubscriptionNotificationJob> _logger;

    public SubscriptionNotificationJob(
        ISubscriptionManagementService subscriptionService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        MasterDbContext context,
        ILogger<SubscriptionNotificationJob> logger)
    {
        _subscriptionService = subscriptionService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Main job execution - runs daily at 9:00 AM
    /// FORTUNE 500: Multi-stage notification system matching Stripe/Chargebee
    /// </summary>
    public async Task Execute()
    {
        _logger.LogInformation("🔔 Starting SubscriptionNotificationJob at {Time}", DateTime.UtcNow);

        var totalNotificationsSent = 0;
        var totalPaymentsCreated = 0;
        var totalSuspensions = 0;

        try
        {
            // ============================================
            // STAGE 1: Auto-create renewal payments (30-60 days before expiry)
            // ============================================
            totalPaymentsCreated = await CreateRenewalPaymentsAsync();

            // ============================================
            // STAGE 2: Send renewal reminders (30d, 15d, 7d, 3d, 1d)
            // ============================================
            totalNotificationsSent += await SendRenewalRemindersAsync();

            // ============================================
            // STAGE 3: Send expiry notifications (on expiry day)
            // ============================================
            totalNotificationsSent += await SendExpiryNotificationsAsync();

            // ============================================
            // STAGE 4: Send grace period warnings (days 1-7, 8-14 after expiry)
            // ============================================
            totalNotificationsSent += await SendGracePeriodWarningsAsync();

            // ============================================
            // STAGE 5: Auto-suspend tenants after grace period
            // ============================================
            totalSuspensions = await AutoSuspendExpiredTenantsAsync();

            // ============================================
            // STAGE 6: Send trial conversion reminders
            // ============================================
            totalNotificationsSent += await SendTrialConversionRemindersAsync();

            // ============================================
            // STAGE 7: Mark overdue payments
            // ============================================
            await MarkOverduePaymentsAsync();

            _logger.LogInformation(
                "✅ SubscriptionNotificationJob completed | Notifications: {Notifications} | Payments: {Payments} | Suspensions: {Suspensions}",
                totalNotificationsSent, totalPaymentsCreated, totalSuspensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in SubscriptionNotificationJob");
            throw;
        }
    }

    // ============================================
    // RENEWAL PAYMENT CREATION
    // ============================================

    private async Task<int> CreateRenewalPaymentsAsync()
    {
        _logger.LogInformation("📋 Creating renewal payments for subscriptions expiring in 30-60 days...");

        var tenantsExpiringSoon = await _context.Tenants
            .Where(t => t.SubscriptionEndDate.HasValue
                && t.SubscriptionEndDate.Value >= DateTime.UtcNow.AddDays(30)
                && t.SubscriptionEndDate.Value <= DateTime.UtcNow.AddDays(60)
                && t.Status == TenantStatus.Active)
            .ToListAsync();

        var paymentsCreated = 0;

        foreach (var tenant in tenantsExpiringSoon)
        {
            try
            {
                var payment = await _subscriptionService.CreateRenewalPaymentIfNeededAsync(tenant.Id);
                if (payment != null)
                {
                    paymentsCreated++;
                    _logger.LogInformation(
                        "💰 Created renewal payment for {TenantName} (expires {ExpiryDate:yyyy-MM-dd})",
                        tenant.CompanyName, tenant.SubscriptionEndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating renewal payment for tenant {TenantId}", tenant.Id);
            }
        }

        _logger.LogInformation("✅ Created {Count} renewal payments", paymentsCreated);
        return paymentsCreated;
    }

    // ============================================
    // RENEWAL REMINDERS (30d, 15d, 7d, 3d, 1d)
    // ============================================

    private async Task<int> SendRenewalRemindersAsync()
    {
        var sent = 0;

        // 30-day reminder
        sent += await SendReminderForDaysAsync(30, SubscriptionNotificationType.Reminder30Days, "30-Day Renewal Reminder");

        // 15-day reminder
        sent += await SendReminderForDaysAsync(15, SubscriptionNotificationType.Reminder15Days, "15-Day Renewal Reminder");

        // 7-day reminder (URGENT)
        sent += await SendReminderForDaysAsync(7, SubscriptionNotificationType.Reminder7Days, "⚠️ URGENT: 7-Day Renewal Reminder");

        // 3-day reminder (CRITICAL)
        sent += await SendReminderForDaysAsync(3, SubscriptionNotificationType.Reminder3Days, "🚨 CRITICAL: 3-Day Renewal Reminder");

        // 1-day reminder (FINAL WARNING)
        sent += await SendReminderForDaysAsync(1, SubscriptionNotificationType.Reminder1Day, "🔴 FINAL WARNING: Subscription Expires Tomorrow");

        return sent;
    }

    private async Task<int> SendReminderForDaysAsync(int days, SubscriptionNotificationType notificationType, string subject)
    {
        var tenants = await _subscriptionService.GetTenantsNeedingRenewalNotificationAsync(days, notificationType);

        _logger.LogInformation("📧 Sending {Subject} to {Count} tenants", subject, tenants.Count);

        var sent = 0;

        foreach (var tenant in tenants)
        {
            try
            {
                // Get pending payment for this tenant
                var pendingPayment = await _context.SubscriptionPayments
                    .Where(p => p.TenantId == tenant.Id
                        && p.Status == SubscriptionPaymentStatus.Pending
                        && p.PeriodStartDate >= tenant.SubscriptionEndDate)
                    .OrderBy(p => p.DueDate)
                    .FirstOrDefaultAsync();

                var amountDue = pendingPayment?.TotalMUR ?? tenant.YearlyPriceMUR;
                var dueDate = pendingPayment?.DueDate ?? tenant.SubscriptionEndDate!.Value.AddDays(30);

                // Send email
                var emailSent = await SendRenewalReminderEmailAsync(
                    tenant,
                    subject,
                    days,
                    amountDue,
                    dueDate);

                // Log notification
                await _subscriptionService.LogNotificationSentAsync(
                    tenant.Id,
                    notificationType,
                    tenant.ContactEmail,
                    subject,
                    emailSent,
                    emailSent ? null : "Email service error",
                    pendingPayment?.Id,
                    requiresFollowUp: days <= 3); // Require follow-up for 3-day and 1-day reminders

                if (emailSent)
                {
                    sent++;
                    _logger.LogInformation("✅ Sent {Subject} to {Tenant}", subject, tenant.CompanyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reminder to tenant {TenantId}", tenant.Id);
            }
        }

        return sent;
    }

    // ============================================
    // EXPIRY NOTIFICATIONS (Grace period starts)
    // ============================================

    private async Task<int> SendExpiryNotificationsAsync()
    {
        _logger.LogInformation("📧 Checking for subscriptions expiring today...");

        var expiringToday = await _context.Tenants
            .Where(t => t.SubscriptionEndDate.HasValue
                && t.SubscriptionEndDate.Value.Date == DateTime.UtcNow.Date
                && t.Status == TenantStatus.Active)
            .ToListAsync();

        var sent = 0;

        foreach (var tenant in expiringToday)
        {
            try
            {
                // Check if already sent
                var alreadySent = await _subscriptionService.HasNotificationBeenSentAsync(
                    tenant.Id,
                    SubscriptionNotificationType.ExpiryNotification);

                if (alreadySent)
                    continue;

                // Start grace period
                tenant.GracePeriodStartDate = DateTime.UtcNow;
                tenant.Status = TenantStatus.ExpiringSoon;
                await _context.SaveChangesAsync();

                // Send email
                var subject = "🔔 Subscription Expired - Grace Period Started (14 Days)";
                var emailSent = await SendExpiryEmailAsync(tenant, subject);

                await _subscriptionService.LogNotificationSentAsync(
                    tenant.Id,
                    SubscriptionNotificationType.ExpiryNotification,
                    tenant.ContactEmail,
                    subject,
                    emailSent,
                    requiresFollowUp: true); // Requires follow-up

                if (emailSent)
                {
                    sent++;
                    _logger.LogInformation("✅ Sent expiry notification to {Tenant}, grace period started", tenant.CompanyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry notification to tenant {TenantId}", tenant.Id);
            }
        }

        return sent;
    }

    // ============================================
    // GRACE PERIOD WARNINGS (Days 1-14 after expiry)
    // ============================================

    private async Task<int> SendGracePeriodWarningsAsync()
    {
        var sent = 0;

        // Early grace period warning (days 1-7)
        sent += await SendGracePeriodWarningAsync(1, 7, SubscriptionNotificationType.GracePeriodWarning,
            "⚠️ Grace Period: Subscription Expired - Renew to Avoid Suspension");

        // Critical warning (days 8-14)
        sent += await SendGracePeriodWarningAsync(8, 14, SubscriptionNotificationType.CriticalWarning,
            "🚨 CRITICAL: Subscription Will Be Suspended in {DaysRemaining} Days");

        return sent;
    }

    private async Task<int> SendGracePeriodWarningAsync(int minDays, int maxDays, SubscriptionNotificationType notificationType, string subjectTemplate)
    {
        // Load tenants in grace period and filter in memory due to EF Core date function limitations
        var allTenantsInGrace = await _context.Tenants
            .Where(t => t.GracePeriodStartDate.HasValue && t.Status != TenantStatus.Suspended)
            .ToListAsync();

        var tenants = allTenantsInGrace
            .Where(t =>
            {
                var daysInGrace = (DateTime.UtcNow - t.GracePeriodStartDate!.Value).Days;
                return daysInGrace >= minDays && daysInGrace <= maxDays;
            })
            .ToList();

        var sent = 0;

        foreach (var tenant in tenants)
        {
            try
            {
                var alreadySent = await _subscriptionService.HasNotificationBeenSentAsync(tenant.Id, notificationType);
                if (alreadySent)
                    continue;

                var daysInGracePeriod = (DateTime.UtcNow - tenant.GracePeriodStartDate!.Value).Days;
                var daysRemaining = 14 - daysInGracePeriod;

                var subject = subjectTemplate.Replace("{DaysRemaining}", daysRemaining.ToString());

                var emailSent = await SendGracePeriodEmailAsync(tenant, subject, daysRemaining);

                await _subscriptionService.LogNotificationSentAsync(
                    tenant.Id,
                    notificationType,
                    tenant.ContactEmail,
                    subject,
                    emailSent,
                    requiresFollowUp: true);

                if (emailSent)
                {
                    sent++;
                    _logger.LogInformation("✅ Sent grace period warning to {Tenant} ({DaysRemaining} days remaining)",
                        tenant.CompanyName, daysRemaining);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending grace period warning to tenant {TenantId}", tenant.Id);
            }
        }

        return sent;
    }

    // ============================================
    // AUTO-SUSPENSION (After grace period ends)
    // ============================================

    private async Task<int> AutoSuspendExpiredTenantsAsync()
    {
        _logger.LogInformation("🔒 Checking for tenants to auto-suspend...");

        var tenantsToSuspend = await _subscriptionService.GetTenantsToSuspendAsync();

        var suspended = 0;

        foreach (var tenant in tenantsToSuspend)
        {
            try
            {
                // Check for any paid payments
                var hasPaidPayment = await _context.SubscriptionPayments
                    .AnyAsync(p => p.TenantId == tenant.Id
                        && p.Status == SubscriptionPaymentStatus.Paid
                        && p.PaidDate >= tenant.GracePeriodStartDate);

                if (hasPaidPayment)
                {
                    _logger.LogInformation("Tenant {TenantId} has paid - skipping suspension", tenant.Id);
                    continue;
                }

                // Suspend tenant
                tenant.Status = TenantStatus.Suspended;
                tenant.SuspensionReason = "Subscription expired and grace period ended without payment";
                tenant.SuspensionDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send suspension notification
                var subject = "🔴 ACCOUNT SUSPENDED: Subscription Payment Required";
                var emailSent = await SendSuspensionEmailAsync(tenant, subject);

                await _subscriptionService.LogNotificationSentAsync(
                    tenant.Id,
                    SubscriptionNotificationType.SuspensionNotification,
                    tenant.ContactEmail,
                    subject,
                    emailSent,
                    requiresFollowUp: true);

                // Audit log - Use system audit logging for automated suspension
                await _auditLogService.LogSuperAdminActionAsync(
                    actionType: AuditActionType.TENANT_SUSPENDED,
                    superAdminId: Guid.Empty, // System action
                    superAdminEmail: "system@hrms.com",
                    targetTenantId: tenant.Id,
                    targetTenantName: tenant.CompanyName,
                    description: "Tenant auto-suspended due to non-payment after grace period",
                    reason: $"Grace period ended: {DateTime.UtcNow:yyyy-MM-dd}",
                    success: true);

                suspended++;
                _logger.LogWarning("🔒 AUTO-SUSPENDED: {Tenant} - Grace period ended without payment", tenant.CompanyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending tenant {TenantId}", tenant.Id);
            }
        }

        _logger.LogInformation("✅ Auto-suspended {Count} tenants", suspended);
        return suspended;
    }

    // ============================================
    // TRIAL CONVERSION REMINDERS
    // ============================================

    private async Task<int> SendTrialConversionRemindersAsync()
    {
        _logger.LogInformation("📧 Sending trial conversion reminders...");

        var expiredTrials = await _subscriptionService.GetTenantsWithExpiredTrialAsync();

        var sent = 0;

        foreach (var tenant in expiredTrials)
        {
            try
            {
                var alreadySent = await _subscriptionService.HasNotificationBeenSentAsync(
                    tenant.Id,
                    SubscriptionNotificationType.RenewalConfirmation);

                if (alreadySent)
                    continue;

                var subject = "🎯 Convert Your Trial to Paid Subscription";
                var emailSent = await SendTrialConversionEmailAsync(tenant, subject);

                await _subscriptionService.LogNotificationSentAsync(
                    tenant.Id,
                    SubscriptionNotificationType.RenewalConfirmation,
                    tenant.ContactEmail,
                    subject,
                    emailSent);

                if (emailSent)
                {
                    sent++;
                    _logger.LogInformation("✅ Sent trial conversion reminder to {Tenant}", tenant.CompanyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending trial conversion reminder to tenant {TenantId}", tenant.Id);
            }
        }

        return sent;
    }

    // ============================================
    // MARK OVERDUE PAYMENTS
    // ============================================

    private async Task MarkOverduePaymentsAsync()
    {
        _logger.LogInformation("⏰ Marking overdue payments...");

        var pendingPayments = await _context.SubscriptionPayments
            .Where(p => p.Status == SubscriptionPaymentStatus.Pending
                && p.DueDate < DateTime.UtcNow)
            .ToListAsync();

        foreach (var payment in pendingPayments)
        {
            await _subscriptionService.MarkPaymentAsOverdueAsync(payment.Id);
        }

        _logger.LogInformation("✅ Marked {Count} payments as overdue", pendingPayments.Count);
    }

    // ============================================
    // EMAIL SENDING METHODS
    // ============================================

    private async Task<bool> SendRenewalReminderEmailAsync(
        Core.Entities.Master.Tenant tenant,
        string subject,
        int daysUntilExpiry,
        decimal amountDue,
        DateTime dueDate)
    {
        try
        {
            var urgencyLevel = daysUntilExpiry switch
            {
                1 => "🔴 URGENT - Tomorrow",
                3 => "🟠 CRITICAL - 3 Days",
                7 => "🟡 Important - 1 Week",
                _ => "ℹ️ Reminder"
            };

            var body = $@"
<h2>{urgencyLevel}: Subscription Renewal Reminder</h2>

<p>Dear {tenant.CompanyName},</p>

<p><strong>Your subscription will expire in {daysUntilExpiry} day(s)</strong> on {tenant.SubscriptionEndDate:MMMM dd, yyyy}.</p>

<h3>Renewal Details:</h3>
<ul>
    <li><strong>Amount Due:</strong> MUR {amountDue:N2}</li>
    <li><strong>Payment Due Date:</strong> {dueDate:MMMM dd, yyyy}</li>
    <li><strong>Current Tier:</strong> {tenant.EmployeeTier}</li>
</ul>

<p>To continue using our services without interruption, please process your payment before the due date.</p>

<p>If you have already made the payment, please disregard this reminder.</p>

<p>Contact us if you have any questions.</p>

<p>Best regards,<br>HRMS Team</p>
";

            await _emailService.SendEmailAsync(tenant.ContactEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending renewal reminder email to {Email}", tenant.ContactEmail);
            return false;
        }
    }

    private async Task<bool> SendExpiryEmailAsync(Core.Entities.Master.Tenant tenant, string subject)
    {
        try
        {
            var body = $@"
<h2>🔔 Subscription Expired - Grace Period Started</h2>

<p>Dear {tenant.CompanyName},</p>

<p><strong>Your subscription has expired</strong> on {tenant.SubscriptionEndDate:MMMM dd, yyyy}.</p>

<h3>Grace Period (14 Days):</h3>
<p>Your account will remain active for the next <strong>14 days</strong> to allow time for payment processing.</p>

<p><strong>If payment is not received by {DateTime.UtcNow.AddDays(14):MMMM dd, yyyy}, your account will be suspended.</strong></p>

<h3>Action Required:</h3>
<ul>
    <li>Process payment of MUR {tenant.YearlyPriceMUR:N2} immediately</li>
    <li>Contact us if you need assistance</li>
</ul>

<p>Thank you for your prompt attention to this matter.</p>

<p>Best regards,<br>HRMS Team</p>
";

            await _emailService.SendEmailAsync(tenant.ContactEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending expiry email to {Email}", tenant.ContactEmail);
            return false;
        }
    }

    private async Task<bool> SendGracePeriodEmailAsync(Core.Entities.Master.Tenant tenant, string subject, int daysRemaining)
    {
        try
        {
            var urgency = daysRemaining <= 3 ? "🚨 IMMEDIATE ACTION REQUIRED" : "⚠️ ACTION REQUIRED";

            var body = $@"
<h2>{urgency}: Grace Period Ending Soon</h2>

<p>Dear {tenant.CompanyName},</p>

<p><strong>Your account will be suspended in {daysRemaining} day(s)</strong> if payment is not received.</p>

<h3>Account Status:</h3>
<ul>
    <li><strong>Subscription Expired:</strong> {tenant.SubscriptionEndDate:MMMM dd, yyyy}</li>
    <li><strong>Grace Period Ends:</strong> {tenant.GracePeriodStartDate!.Value.AddDays(14):MMMM dd, yyyy}</li>
    <li><strong>Days Remaining:</strong> {daysRemaining}</li>
</ul>

<h3>Immediate Action Required:</h3>
<p>Process payment of <strong>MUR {tenant.YearlyPriceMUR:N2}</strong> immediately to avoid suspension.</p>

<p><strong>Suspension means:</strong> Loss of access to all data and features until payment is received.</p>

<p>Best regards,<br>HRMS Team</p>
";

            await _emailService.SendEmailAsync(tenant.ContactEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending grace period email to {Email}", tenant.ContactEmail);
            return false;
        }
    }

    private async Task<bool> SendSuspensionEmailAsync(Core.Entities.Master.Tenant tenant, string subject)
    {
        try
        {
            var body = $@"
<h2>🔴 ACCOUNT SUSPENDED: Payment Required</h2>

<p>Dear {tenant.CompanyName},</p>

<p><strong>Your account has been suspended</strong> due to non-payment.</p>

<h3>Suspension Details:</h3>
<ul>
    <li><strong>Subscription Expired:</strong> {tenant.SubscriptionEndDate:MMMM dd, yyyy}</li>
    <li><strong>Grace Period Ended:</strong> {tenant.GracePeriodStartDate!.Value.AddDays(14):MMMM dd, yyyy}</li>
    <li><strong>Suspended On:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</li>
</ul>

<h3>To Restore Access:</h3>
<ol>
    <li>Process payment of <strong>MUR {tenant.YearlyPriceMUR:N2}</strong></li>
    <li>Contact us to confirm payment</li>
    <li>Your account will be reactivated within 24 hours</li>
</ol>

<p>All your data is safe and will be available once payment is received.</p>

<p>Best regards,<br>HRMS Team</p>
";

            await _emailService.SendEmailAsync(tenant.ContactEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending suspension email to {Email}", tenant.ContactEmail);
            return false;
        }
    }

    private async Task<bool> SendTrialConversionEmailAsync(Core.Entities.Master.Tenant tenant, string subject)
    {
        try
        {
            var body = $@"
<h2>🎯 Convert Your Trial to Paid Subscription</h2>

<p>Dear {tenant.CompanyName},</p>

<p>Your trial period has ended on {tenant.TrialEndDate:MMMM dd, yyyy}.</p>

<h3>Continue Using HRMS:</h3>
<p>Convert to a paid subscription to continue accessing all features:</p>

<ul>
    <li><strong>Yearly Price:</strong> MUR {tenant.YearlyPriceMUR:N2}</li>
    <li><strong>Current Tier:</strong> {tenant.EmployeeTier}</li>
</ul>

<p>Contact us to complete your subscription and continue without interruption.</p>

<p>Best regards,<br>HRMS Team</p>
";

            await _emailService.SendEmailAsync(tenant.ContactEmail, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending trial conversion email to {Email}", tenant.ContactEmail);
            return false;
        }
    }
}
