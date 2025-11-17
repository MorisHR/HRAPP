using System.Net;
using System.Net.Mail;
using HRMS.Application.Interfaces;
using HRMS.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Email service for sending notifications
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _frontendUrl;
    private readonly int _maxRetries = 3;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _configuration = configuration;
        _frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendHtmlEmailAsync(to, subject, body);
    }

    public async Task SendHtmlEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody)
    {
        try
        {
            using var smtpClient = CreateSmtpClient();

            foreach (var recipient in recipients)
            {
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(recipient);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Bulk email sent to {To}", recipient);

                // Small delay to avoid overwhelming SMTP server
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk emails");
            throw;
        }
    }

    public async Task SendDocumentExpiryAlertAsync(
        string employeeName,
        string employeeEmail,
        string documentType,
        DateTime expiryDate,
        int daysRemaining,
        string urgencyLevel)
    {
        var subject = $"URGENT: {documentType} Expiring in {daysRemaining} Days";

        var urgencyColor = urgencyLevel switch
        {
            "Critical" => "#dc3545",
            "Urgent" => "#fd7e14",
            "Warning" => "#ffc107",
            _ => "#17a2b8"
        };

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {urgencyColor}; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; border-radius: 5px; }}
        .alert-box {{ background-color: #fff3cd; border-left: 4px solid {urgencyColor}; padding: 15px; margin: 20px 0; }}
        .details {{ margin: 20px 0; }}
        .details-table {{ width: 100%; border-collapse: collapse; }}
        .details-table td {{ padding: 10px; border-bottom: 1px solid #dee2e6; }}
        .details-table td:first-child {{ font-weight: bold; width: 40%; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
        .action-required {{ background-color: {urgencyColor}; color: white; padding: 15px; text-align: center; font-weight: bold; margin: 20px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>⚠️ Document Expiry Alert - {urgencyLevel}</h2>
        </div>

        <div class='content'>
            <p>Dear {employeeName},</p>

            <div class='action-required'>
                YOUR {documentType.ToUpper()} IS EXPIRING IN {daysRemaining} DAY{(daysRemaining != 1 ? "S" : "")}!
            </div>

            <div class='alert-box'>
                <strong>⚠️ Immediate Action Required</strong><br/>
                Please renew your {documentType} immediately to avoid any legal issues or work permit violations.
            </div>

            <div class='details'>
                <table class='details-table'>
                    <tr>
                        <td>Document Type:</td>
                        <td>{documentType}</td>
                    </tr>
                    <tr>
                        <td>Expiry Date:</td>
                        <td>{expiryDate:MMMM dd, yyyy}</td>
                    </tr>
                    <tr>
                        <td>Days Remaining:</td>
                        <td><strong>{daysRemaining} days</strong></td>
                    </tr>
                    <tr>
                        <td>Urgency Level:</td>
                        <td><span style='color: {urgencyColor}; font-weight: bold;'>{urgencyLevel}</span></td>
                    </tr>
                </table>
            </div>

            <div style='margin-top: 30px;'>
                <h3>What You Need to Do:</h3>
                <ol>
                    <li>Visit the relevant embassy/immigration office immediately</li>
                    <li>Prepare all necessary documentation for renewal</li>
                    <li>Submit your renewal application as soon as possible</li>
                    <li>Inform HR once renewal process has started</li>
                    <li>Upload the renewed document to HRMS once received</li>
                </ol>
            </div>

            <div style='background-color: #d1ecf1; padding: 15px; margin-top: 20px; border-radius: 5px;'>
                <strong>📞 Need Help?</strong><br/>
                Contact HR Department for assistance with the renewal process.
            </div>
        </div>

        <div class='footer'>
            <p>This is an automated notification from HRMS.</p>
            <p>© {DateTime.Now.Year} HRMS - Human Resource Management System</p>
        </div>
    </div>
</body>
</html>";

        await SendHtmlEmailAsync(employeeEmail, subject, htmlBody);
    }

    public async Task SendLeaveApprovalNotificationAsync(
        string employeeEmail,
        string employeeName,
        string leaveType,
        DateTime startDate,
        DateTime endDate,
        bool isApproved,
        string? rejectionReason = null)
    {
        var subject = isApproved
            ? $"Leave Approved: {leaveType}"
            : $"Leave Rejected: {leaveType}";

        var statusColor = isApproved ? "#28a745" : "#dc3545";
        var statusIcon = isApproved ? "✅" : "❌";
        var statusText = isApproved ? "APPROVED" : "REJECTED";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {statusColor}; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; border-radius: 5px; }}
        .status-badge {{ background-color: {statusColor}; color: white; padding: 10px 20px; text-align: center; font-size: 18px; font-weight: bold; margin: 20px 0; border-radius: 5px; }}
        .details-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .details-table td {{ padding: 10px; border-bottom: 1px solid #dee2e6; }}
        .details-table td:first-child {{ font-weight: bold; width: 40%; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{statusIcon} Leave Application {statusText}</h2>
        </div>

        <div class='content'>
            <p>Dear {employeeName},</p>

            <div class='status-badge'>
                Your leave application has been {statusText}
            </div>

            <table class='details-table'>
                <tr>
                    <td>Leave Type:</td>
                    <td>{leaveType}</td>
                </tr>
                <tr>
                    <td>Start Date:</td>
                    <td>{startDate:MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <td>End Date:</td>
                    <td>{endDate:MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <td>Duration:</td>
                    <td>{(endDate - startDate).Days + 1} day(s)</td>
                </tr>
                <tr>
                    <td>Status:</td>
                    <td><strong style='color: {statusColor};'>{statusText}</strong></td>
                </tr>
                {(rejectionReason != null ? $@"
                <tr>
                    <td>Rejection Reason:</td>
                    <td style='color: #dc3545;'>{rejectionReason}</td>
                </tr>" : "")}
            </table>

            {(isApproved ? @"
            <div style='background-color: #d4edda; padding: 15px; margin-top: 20px; border-radius: 5px; color: #155724;'>
                <strong>✅ Your leave has been approved!</strong><br/>
                Enjoy your time off. Please ensure a smooth handover before your leave starts.
            </div>" : @"
            <div style='background-color: #f8d7da; padding: 15px; margin-top: 20px; border-radius: 5px; color: #721c24;'>
                <strong>❌ Your leave application was not approved.</strong><br/>
                Please contact your manager if you have any questions about this decision.
            </div>")}
        </div>

        <div class='footer'>
            <p>This is an automated notification from HRMS.</p>
            <p>© {DateTime.Now.Year} HRMS - Human Resource Management System</p>
        </div>
    </div>
</body>
</html>";

        await SendHtmlEmailAsync(employeeEmail, subject, htmlBody);
    }

    public async Task SendPayslipReadyNotificationAsync(
        string employeeEmail,
        string employeeName,
        string month,
        int year,
        decimal netSalary)
    {
        var subject = $"Payslip Ready: {month} {year}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; border-radius: 5px; }}
        .salary-box {{ background-color: #28a745; color: white; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px; }}
        .salary-amount {{ font-size: 32px; font-weight: bold; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>💰 Your Payslip is Ready!</h2>
        </div>

        <div class='content'>
            <p>Dear {employeeName},</p>

            <p>Your payslip for <strong>{month} {year}</strong> has been processed and is now available.</p>

            <div class='salary-box'>
                <div>Net Salary</div>
                <div class='salary-amount'>MUR {netSalary:N2}</div>
            </div>

            <p>Login to the HRMS portal to:</p>
            <ul>
                <li>View your detailed payslip</li>
                <li>Download PDF copy</li>
                <li>Review salary breakup and deductions</li>
            </ul>

            <div style='background-color: #d1ecf1; padding: 15px; margin-top: 20px; border-radius: 5px;'>
                <strong>📌 Payment Information:</strong><br/>
                Your salary will be credited to your registered bank account shortly.
            </div>
        </div>

        <div class='footer'>
            <p>This is an automated notification from HRMS.</p>
            <p>© {DateTime.Now.Year} HRMS - Human Resource Management System</p>
        </div>
    </div>
</body>
</html>";

        await SendHtmlEmailAsync(employeeEmail, subject, htmlBody);
    }

    public async Task SendWelcomeEmailAsync(
        string employeeEmail,
        string employeeName,
        string username,
        string temporaryPassword)
    {
        var subject = "Welcome to the Team! - HRMS Account Created";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; border-radius: 5px; }}
        .credentials-box {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>🎉 Welcome to Our Team!</h2>
        </div>

        <div class='content'>
            <p>Dear {employeeName},</p>

            <p>Welcome aboard! We're excited to have you join our team.</p>

            <p>Your HRMS account has been created. You can now access the system to:</p>
            <ul>
                <li>View and update your profile</li>
                <li>Apply for leave</li>
                <li>View attendance records</li>
                <li>Access payslips</li>
                <li>And much more!</li>
            </ul>

            <div class='credentials-box'>
                <strong>🔐 Your Login Credentials:</strong><br/><br/>
                <strong>Username:</strong> {username}<br/>
                <strong>Temporary Password:</strong> {temporaryPassword}<br/><br/>
                <strong>⚠️ Please change your password immediately after first login!</strong>
            </div>

            <div style='background-color: #d1ecf1; padding: 15px; margin-top: 20px; border-radius: 5px;'>
                <strong>📋 Next Steps:</strong><br/>
                1. Login to HRMS using the credentials above<br/>
                2. Complete your profile information<br/>
                3. Upload required documents<br/>
                4. Change your password<br/>
                5. Review company policies
            </div>

            <p style='margin-top: 30px;'>If you have any questions or need assistance, please don't hesitate to contact the HR department.</p>

            <p>Once again, welcome to the team!</p>
        </div>

        <div class='footer'>
            <p>This is an automated notification from HRMS.</p>
            <p>© {DateTime.Now.Year} HRMS - Human Resource Management System</p>
        </div>
    </div>
</body>
</html>";

        await SendHtmlEmailAsync(employeeEmail, subject, htmlBody);
    }

    public async Task SendAttendanceCorrectionNotificationAsync(
        string employeeEmail,
        string employeeName,
        DateTime date,
        bool isApproved,
        string? rejectionReason = null)
    {
        var subject = isApproved
            ? "Attendance Correction Approved"
            : "Attendance Correction Rejected";

        var statusColor = isApproved ? "#28a745" : "#dc3545";
        var statusIcon = isApproved ? "✅" : "❌";
        var statusText = isApproved ? "APPROVED" : "REJECTED";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {statusColor}; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; border-radius: 5px; }}
        .status-badge {{ background-color: {statusColor}; color: white; padding: 10px 20px; text-align: center; font-size: 18px; font-weight: bold; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>{statusIcon} Attendance Correction {statusText}</h2>
        </div>

        <div class='content'>
            <p>Dear {employeeName},</p>

            <div class='status-badge'>
                Your attendance correction request has been {statusText}
            </div>

            <p><strong>Date:</strong> {date:MMMM dd, yyyy}</p>
            <p><strong>Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{statusText}</span></p>

            {(rejectionReason != null ? $@"
            <div style='background-color: #f8d7da; padding: 15px; margin-top: 20px; border-radius: 5px; color: #721c24;'>
                <strong>Rejection Reason:</strong><br/>
                {rejectionReason}
            </div>" : "")}

            {(isApproved ? @"
            <p style='margin-top: 20px;'>Your attendance record has been updated accordingly.</p>" : "")}
        </div>

        <div class='footer'>
            <p>This is an automated notification from HRMS.</p>
            <p>© {DateTime.Now.Year} HRMS - Human Resource Management System</p>
        </div>
    </div>
</body>
</html>";

        await SendHtmlEmailAsync(employeeEmail, subject, htmlBody);
    }

    #region Tenant Activation Methods (MailKit-based)

    public async Task<bool> SendTenantActivationEmailAsync(
        string toEmail,
        string tenantName,
        string activationToken,
        string adminFirstName)
    {
        try
        {
            var activationUrl = $"{_frontendUrl}/activate?token={activationToken}";
            var expiryHours = int.Parse(_configuration["AppSettings:ActivationTokenExpiryHours"] ?? "24");

            var subject = $"Activate Your MorisHR Account - {tenantName}";
            var htmlBody = GetTenantActivationTemplate(adminFirstName, tenantName, activationUrl, expiryHours);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tenant activation email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTenantActivationReminderAsync(
        string toEmail,
        string tenantName,
        string activationToken,
        string adminFirstName,
        int daysSinceCreation,
        int daysRemaining)
    {
        try
        {
            var activationUrl = $"{_frontendUrl}/activate?token={activationToken}";

            // Different subject lines based on urgency
            var subject = daysSinceCreation switch
            {
                3 => $"Haven't activated yet? We're here to help - {tenantName}",
                7 => $"Your activation link expires in {daysRemaining} days - {tenantName}",
                14 => $"Halfway to expiration - Activate your account today - {tenantName}",
                21 => $"Final reminder: Only {daysRemaining} days left to activate - {tenantName}",
                _ => $"Reminder: Activate your MorisHR account - {tenantName}"
            };

            var htmlBody = GetTenantActivationReminderTemplate(
                adminFirstName, tenantName, activationUrl, daysSinceCreation, daysRemaining);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tenant activation reminder email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTenantWelcomeEmailAsync(
        string toEmail,
        string adminFirstName,
        string subdomain,
        string? passwordResetToken = null)
    {
        try
        {
            var loginUrl = $"{_frontendUrl}/auth/subdomain";
            var passwordSetupUrl = !string.IsNullOrEmpty(passwordResetToken)
                ? $"{_frontendUrl}/set-password?token={passwordResetToken}"
                : null;

            var subject = $"Welcome to MorisHR - Your Account is Ready!";
            var htmlBody = GetTenantWelcomeTemplate(adminFirstName, subdomain, loginUrl, passwordSetupUrl);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tenant welcome email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        string firstName)
    {
        try
        {
            var resetUrl = $"{_frontendUrl}/reset-password?token={resetToken}";

            var subject = "Reset Your MorisHR Password";
            var htmlBody = GetPasswordResetTemplate(firstName, resetUrl);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendExpiryReminderAsync(
        string toEmail,
        string tenantName,
        int daysRemaining,
        string adminFirstName)
    {
        try
        {
            var subject = $"Subscription Expiring Soon - {tenantName}";
            var htmlBody = GetExpiryReminderTemplate(adminFirstName, tenantName, daysRemaining);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiry reminder email to {Email}", toEmail);
            return false;
        }
    }

    private async Task<bool> SendEmailWithMailKitAsync(string toEmail, string subject, string htmlBody)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _maxRetries)
        {
            attempt++;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = StripHtml(htmlBody)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();

                // Set timeout
                client.Timeout = 30000;

                // Connect to SMTP server
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort,
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                // Authenticate if credentials provided
                if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) && !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
                {
                    await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                }

                // Send email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email} (Subject: {Subject})", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Email send attempt {Attempt}/{MaxRetries} failed for {Email}",
                    attempt, _maxRetries, toEmail);

                if (attempt < _maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }

        _logger.LogError(lastException, "Failed to send email to {Email} after {Attempts} attempts",
            toEmail, _maxRetries);
        return false;
    }

    private string GetTenantActivationTemplate(string firstName, string tenantName, string activationUrl, int expiryHours)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Activate Your MorisHR Account</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">MorisHR</h1>
                            <p style=""color: #ffffff; margin: 10px 0 0 0; font-size: 14px; opacity: 0.9;"">Human Resource Management System</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Welcome to MorisHR, {firstName}!</h2>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                Your MorisHR account for <strong>{tenantName}</strong> has been created successfully.
                                To get started, please activate your account by clicking the button below.
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{activationUrl}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Activate Your Account
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0;"">
                                Or copy and paste this link into your browser:<br>
                                <a href=""{activationUrl}"" style=""color: #667eea; word-break: break-all;"">{activationUrl}</a>
                            </p>
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #856404; font-size: 14px; margin: 0;"">
                                    <strong>Important:</strong> This activation link will expire in {expiryHours} hours.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Contact us at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetTenantActivationReminderTemplate(
        string firstName,
        string tenantName,
        string activationUrl,
        int daysSinceCreation,
        int daysRemaining)
    {
        // Customize message based on reminder milestone
        string headline, message, urgencyColor, urgencyText;

        switch (daysSinceCreation)
        {
            case 3:
                headline = "Just checking in!";
                message = $"We noticed you haven't activated your MorisHR account yet. Need any help getting started?";
                urgencyColor = "#17a2b8"; // info blue
                urgencyText = "No rush - you still have plenty of time!";
                break;
            case 7:
                headline = "Don't miss out!";
                message = $"Your activation link will expire in <strong>{daysRemaining} days</strong>. Activate now to start managing your HR operations.";
                urgencyColor = "#ffc107"; // warning yellow
                urgencyText = $"Expires in {daysRemaining} days";
                break;
            case 14:
                headline = "Time is flying!";
                message = $"You're halfway to the expiration date. Don't let your activation link expire - it only takes a minute to activate!";
                urgencyColor = "#fd7e14"; // orange
                urgencyText = $"Only {daysRemaining} days remaining";
                break;
            case 21:
                headline = "Final reminder!";
                message = $"<strong>Your activation link expires in just {daysRemaining} days.</strong> This is your last reminder before your account is automatically deleted.";
                urgencyColor = "#dc3545"; // danger red
                urgencyText = $"URGENT: {daysRemaining} days left!";
                break;
            default:
                headline = "Activate your account";
                message = $"You have {daysRemaining} days remaining to activate your MorisHR account.";
                urgencyColor = "#667eea";
                urgencyText = $"{daysRemaining} days remaining";
                break;
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Activation Reminder - MorisHR</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">MorisHR</h1>
                            <p style=""color: #ffffff; margin: 10px 0 0 0; font-size: 14px; opacity: 0.9;"">Human Resource Management System</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">{headline}</h2>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                Hi {firstName},
                            </p>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                {message}
                            </p>
                            <div style=""background-color: {urgencyColor}; background: linear-gradient(135deg, {urgencyColor} 0%, {urgencyColor}dd 100%); color: #ffffff; padding: 20px; margin: 30px 0; border-radius: 8px; text-align: center;"">
                                <p style=""margin: 0; font-size: 18px; font-weight: bold;"">{urgencyText}</p>
                            </div>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 30px 0;"">
                                Your account for <strong>{tenantName}</strong> is almost ready. Just click the button below to activate:
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{activationUrl}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Activate Now
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0;"">
                                Or copy and paste this link into your browser:<br>
                                <a href=""{activationUrl}"" style=""color: #667eea; word-break: break-all;"">{activationUrl}</a>
                            </p>
                            <div style=""background-color: #e9ecef; border-left: 4px solid #6c757d; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #495057; font-size: 14px; margin: 0;"">
                                    <strong>Need help?</strong> If you're experiencing any issues or have questions, our support team is here to help at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                This is an automated reminder. If you've already activated, please disregard this email.
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetTenantWelcomeTemplate(string firstName, string subdomain, string loginUrl, string? passwordSetupUrl)
    {
        var passwordSetupSection = !string.IsNullOrEmpty(passwordSetupUrl)
            ? $@"
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ff9800; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #856404; font-size: 16px; margin: 0 0 15px 0; font-weight: bold;"">
                                    🔐 Set Up Your Password
                                </p>
                                <p style=""color: #856404; font-size: 14px; margin: 0 0 15px 0;"">
                                    Before you can access your account, you need to create a secure password.
                                    Click the button below to set up your password (link expires in 24 hours).
                                </p>
                                <table role=""presentation"" style=""margin: 0;"">
                                    <tr>
                                        <td align=""center"">
                                            <a href=""{passwordSetupUrl}"" style=""display: inline-block; padding: 14px 32px; background-color: #ff9800; color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 15px; font-weight: bold;"">
                                                Set Your Password
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                            </div>"
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to MorisHR</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">🎉 Welcome to MorisHR!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Congratulations, {firstName}!</h2>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                Your MorisHR account has been successfully activated! You're now ready to streamline your HR operations
                                and empower your workforce with our comprehensive HRMS platform.
                            </p>
                            {passwordSetupSection}
                            <div style=""background-color: #e7f3ff; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #0d47a1; font-size: 14px; margin: 0 0 10px 0;"">
                                    <strong>📍 Your Account Details:</strong>
                                </p>
                                <p style=""color: #0d47a1; font-size: 14px; margin: 0;"">
                                    <strong>Subdomain:</strong> {subdomain}.morishr.com<br>
                                    <strong>Login Page:</strong> <a href=""{loginUrl}"" style=""color: #2196f3;"">{loginUrl}</a>
                                </p>
                            </div>
                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">🚀 Getting Started</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li><strong>Set your password</strong> using the link above (if you haven't already)</li>
                                <li>Complete your company profile and customize settings</li>
                                <li>Add departments and organizational structure</li>
                                <li>Onboard your first employees</li>
                                <li>Configure payroll and compliance settings</li>
                                <li>Set up biometric attendance devices (optional)</li>
                            </ul>
                            <div style=""background-color: #f0f8ff; padding: 20px; margin: 30px 0; border-radius: 8px; border: 1px solid #b3d9ff;"">
                                <p style=""color: #004085; font-size: 14px; margin: 0 0 10px 0; font-weight: bold;"">
                                    💡 Pro Tip: Secure Your Account
                                </p>
                                <p style=""color: #004085; font-size: 13px; margin: 0; line-height: 1.6;"">
                                    After setting your password, we recommend enabling Two-Factor Authentication (2FA)
                                    from your account settings for enhanced security. Fortune 500 companies trust MorisHR
                                    with their HR data - we take security seriously!
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Our support team is here for you!<br>
                                📧 <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a> |
                                📞 +230-xxx-xxxx
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved. | <a href=""https://morishr.com/privacy"" style=""color: #667eea; text-decoration: none;"">Privacy Policy</a>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetPasswordResetTemplate(string firstName, string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">Password Reset</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Hi {firstName},</h2>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                We received a request to reset your MorisHR password. Click the button below to create a new password.
                            </p>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{resetUrl}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Reset Password
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #856404; font-size: 14px; margin: 0;"">
                                    <strong>Security Notice:</strong> This link will expire in 1 hour.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Contact us at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetExpiryReminderTemplate(string firstName, string tenantName, int daysRemaining)
    {
        var urgencyColor = daysRemaining <= 7 ? "#dc3545" : "#ffc107";
        var urgencyText = daysRemaining <= 7 ? "Urgent:" : "Notice:";
        var loginUrl = _configuration["AppSettings:ProductionUrl"] ?? _frontendUrl;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Subscription Expiring Soon</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">Subscription Renewal Reminder</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Hi {firstName},</h2>
                            <div style=""background-color: #fff3cd; border-left: 4px solid {urgencyColor}; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #856404; font-size: 16px; margin: 0; font-weight: bold;"">
                                    {urgencyText} Your MorisHR subscription for <strong>{tenantName}</strong> will expire in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}.
                                </p>
                            </div>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 20px 0;"">
                                To ensure uninterrupted access to your HRMS and avoid service disruption, please renew your subscription before it expires.
                            </p>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">What Happens Next?</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li>Your subscription will expire in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}</li>
                                <li>After expiry, you'll have a {(daysRemaining <= 7 ? "3-day" : "7-day")} grace period</li>
                                <li>During grace period, access will be limited</li>
                                <li>After grace period, your account will be suspended</li>
                            </ul>

                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{loginUrl}/subscription/renew"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Renew Subscription Now
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Contact us at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                            <p style=""color: #999; font-size: 10px; margin: 10px 0 0 0;"">
                                You are receiving this email because you are an administrator of {tenantName}.<br/>
                                To update your notification preferences, please log in to your account.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public async Task<bool> SendSubscriptionExpiredAsync(
        string toEmail,
        string tenantName,
        string adminFirstName,
        DateTime expiryDate,
        int gracePeriodDays)
    {
        try
        {
            var subject = $"URGENT: Subscription Expired - {tenantName}";
            var htmlBody = GetSubscriptionExpiredTemplate(adminFirstName, tenantName, expiryDate, gracePeriodDays);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription expired email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendAccountSuspendedAsync(
        string toEmail,
        string tenantName,
        string adminFirstName,
        DateTime suspensionDate,
        string reason)
    {
        try
        {
            var subject = $"CRITICAL: Account Suspended - {tenantName}";
            var htmlBody = GetAccountSuspendedTemplate(adminFirstName, tenantName, suspensionDate, reason);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account suspended email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendRenewalConfirmationAsync(
        string toEmail,
        string tenantName,
        string adminFirstName,
        DateTime newExpiryDate,
        string planName)
    {
        try
        {
            var subject = $"Subscription Renewed Successfully - {tenantName}";
            var htmlBody = GetRenewalConfirmationTemplate(adminFirstName, tenantName, newExpiryDate, planName);

            return await SendEmailWithMailKitAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send renewal confirmation email to {Email}", toEmail);
            return false;
        }
    }

    private string GetSubscriptionExpiredTemplate(string firstName, string tenantName, DateTime expiryDate, int gracePeriodDays)
    {
        var loginUrl = _configuration["AppSettings:ProductionUrl"] ?? _frontendUrl;
        var graceEndDate = expiryDate.AddDays(gracePeriodDays);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Subscription Expired</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">⚠️ Subscription Expired</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Hi {firstName},</h2>
                            <div style=""background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #721c24; font-size: 16px; margin: 0; font-weight: bold;"">
                                    Your MorisHR subscription for <strong>{tenantName}</strong> expired on {expiryDate:MMMM dd, yyyy}.
                                </p>
                            </div>

                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; margin: 20px 0;"">
                                <h3 style=""color: #856404; margin: 0 0 10px 0; font-size: 18px;"">Grace Period Active</h3>
                                <p style=""color: #856404; font-size: 14px; margin: 0;"">
                                    You have <strong>{gracePeriodDays} days</strong> of grace period until <strong>{graceEndDate:MMMM dd, yyyy}</strong>.<br/>
                                    During this time, your access is limited to read-only operations.
                                </p>
                            </div>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">Current Limitations:</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li>❌ Cannot add or modify employee records</li>
                                <li>❌ Cannot process payroll</li>
                                <li>❌ Cannot mark attendance</li>
                                <li>❌ Cannot approve leave requests</li>
                                <li>✓ Can view existing data (read-only)</li>
                                <li>✓ Can export critical reports</li>
                            </ul>

                            <div style=""background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 20px; margin: 30px 0;"">
                                <h3 style=""color: #0c5460; margin: 0 0 10px 0; font-size: 18px;"">⚡ Act Now to Avoid Account Suspension</h3>
                                <p style=""color: #0c5460; font-size: 14px; margin: 0;"">
                                    If you don't renew before <strong>{graceEndDate:MMMM dd, yyyy}</strong>, your account will be suspended and you will lose access to all data.
                                </p>
                            </div>

                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{loginUrl}/subscription/renew"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Renew Now to Restore Full Access
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Contact us at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetAccountSuspendedTemplate(string firstName, string tenantName, DateTime suspensionDate, string reason)
    {
        var loginUrl = _configuration["AppSettings:ProductionUrl"] ?? _frontendUrl;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Suspended</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #6c757d 0%, #495057 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">🚫 Account Suspended</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Hi {firstName},</h2>
                            <div style=""background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #721c24; font-size: 16px; margin: 0; font-weight: bold;"">
                                    Your MorisHR account for <strong>{tenantName}</strong> has been suspended as of {suspensionDate:MMMM dd, yyyy}.
                                </p>
                            </div>

                            <div style=""background-color: #e2e3e5; border-left: 4px solid #6c757d; padding: 20px; margin: 20px 0;"">
                                <h3 style=""color: #383d41; margin: 0 0 10px 0; font-size: 18px;"">Reason for Suspension:</h3>
                                <p style=""color: #383d41; font-size: 14px; margin: 0;"">
                                    {reason}
                                </p>
                            </div>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">What This Means:</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li>❌ No access to the HRMS portal</li>
                                <li>❌ All employee operations suspended</li>
                                <li>❌ Payroll processing unavailable</li>
                                <li>❌ Attendance tracking disabled</li>
                                <li>⚠️ Your data is preserved for 30 days</li>
                                <li>⚠️ After 30 days, data may be permanently deleted</li>
                            </ul>

                            <div style=""background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 20px; margin: 30px 0;"">
                                <h3 style=""color: #0c5460; margin: 0 0 10px 0; font-size: 18px;"">How to Restore Access</h3>
                                <p style=""color: #0c5460; font-size: 14px; margin: 0 0 10px 0;"">
                                    To reactivate your account and restore full access:
                                </p>
                                <ol style=""color: #0c5460; font-size: 14px; margin: 0; padding-left: 20px;"">
                                    <li>Contact our support team immediately</li>
                                    <li>Renew your subscription</li>
                                    <li>Complete any pending payment</li>
                                    <li>Your account will be reactivated within 24 hours</li>
                                </ol>
                            </div>

                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""mailto:support@morishr.com?subject=Account Reactivation Request - {tenantName}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #28a745 0%, #218838 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Contact Support to Reactivate
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 30px 0;"">
                                <p style=""color: #856404; font-size: 12px; margin: 0;"">
                                    <strong>⏰ Important:</strong> Your data will be retained for 30 days. After this period, all data will be permanently deleted and cannot be recovered.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Urgent assistance: <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a> | Phone: +230 5XXX XXXX
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string GetRenewalConfirmationTemplate(string firstName, string tenantName, DateTime newExpiryDate, string planName)
    {
        var loginUrl = _configuration["AppSettings:ProductionUrl"] ?? _frontendUrl;
        var daysUntilExpiry = (newExpiryDate - DateTime.UtcNow).Days;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Subscription Renewed</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #28a745 0%, #218838 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">✅ Subscription Renewed!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Congratulations, {firstName}!</h2>
                            <div style=""background-color: #d4edda; border-left: 4px solid #28a745; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #155724; font-size: 16px; margin: 0; font-weight: bold;"">
                                    Your MorisHR subscription for <strong>{tenantName}</strong> has been successfully renewed!
                                </p>
                            </div>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">Renewal Details</h3>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold; width: 40%;"">Plan:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{planName}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">New Expiry Date:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{newExpiryDate:MMMM dd, yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">Days Remaining:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{daysUntilExpiry} days</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">Status:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;""><strong style=""color: #28a745;"">✓ Active</strong></td>
                                </tr>
                            </table>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">Full Access Restored</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li>✅ All features and modules enabled</li>
                                <li>✅ Employee management fully functional</li>
                                <li>✅ Payroll processing available</li>
                                <li>✅ Attendance tracking active</li>
                                <li>✅ Leave management operational</li>
                                <li>✅ Reports and analytics accessible</li>
                            </ul>

                            <div style=""background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 20px; margin: 30px 0;"">
                                <h3 style=""color: #0c5460; margin: 0 0 10px 0; font-size: 18px;"">Thank You!</h3>
                                <p style=""color: #0c5460; font-size: 14px; margin: 0;"">
                                    Thank you for continuing to trust MorisHR for your human resource management needs. We're committed to providing you with the best HRMS experience.
                                </p>
                            </div>

                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{loginUrl}/dashboard"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Go to Dashboard
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                Need help? Contact us at <a href=""mailto:support@morishr.com"" style=""color: #667eea; text-decoration: none;"">support@morishr.com</a>
                            </p>
                            <p style=""color: #6c757d; font-size: 12px; margin: 0;"">
                                &copy; 2025 MorisHR. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Trim();
    }

    #endregion

    private System.Net.Mail.SmtpClient CreateSmtpClient()
    {
        return new System.Net.Mail.SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
            EnableSsl = _emailSettings.EnableSsl,
            UseDefaultCredentials = _emailSettings.UseDefaultCredentials
        };
    }
}
