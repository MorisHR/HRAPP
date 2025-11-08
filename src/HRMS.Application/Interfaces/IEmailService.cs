namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a simple text email
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body);

    /// <summary>
    /// Sends an HTML email
    /// </summary>
    Task SendHtmlEmailAsync(string to, string subject, string htmlBody);

    /// <summary>
    /// Sends email to multiple recipients
    /// </summary>
    Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody);

    /// <summary>
    /// Sends document expiry alert email
    /// </summary>
    Task SendDocumentExpiryAlertAsync(string employeeName, string employeeEmail, string documentType,
        DateTime expiryDate, int daysRemaining, string urgencyLevel);

    /// <summary>
    /// Sends leave approval notification
    /// </summary>
    Task SendLeaveApprovalNotificationAsync(string employeeEmail, string employeeName,
        string leaveType, DateTime startDate, DateTime endDate, bool isApproved, string? rejectionReason = null);

    /// <summary>
    /// Sends payslip ready notification
    /// </summary>
    Task SendPayslipReadyNotificationAsync(string employeeEmail, string employeeName,
        string month, int year, decimal netSalary);

    /// <summary>
    /// Sends welcome email to new employee
    /// </summary>
    Task SendWelcomeEmailAsync(string employeeEmail, string employeeName, string username, string temporaryPassword);

    /// <summary>
    /// Sends attendance correction notification
    /// </summary>
    Task SendAttendanceCorrectionNotificationAsync(string employeeEmail, string employeeName,
        DateTime date, bool isApproved, string? rejectionReason = null);

    /// <summary>
    /// Sends tenant activation email with activation link
    /// </summary>
    Task<bool> SendTenantActivationEmailAsync(string toEmail, string tenantName,
        string activationToken, string adminFirstName);

    /// <summary>
    /// Sends welcome email after successful tenant activation
    /// </summary>
    Task<bool> SendTenantWelcomeEmailAsync(string toEmail, string tenantName,
        string adminFirstName, string subdomain);

    /// <summary>
    /// Sends password reset email
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string firstName);

    /// <summary>
    /// Sends subscription expiry reminder email
    /// </summary>
    Task<bool> SendExpiryReminderAsync(string toEmail, string tenantName,
        int daysRemaining, string adminFirstName);
}
