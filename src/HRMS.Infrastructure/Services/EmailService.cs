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

    public async Task<bool> SendTenantWelcomeEmailAsync(
        string toEmail,
        string tenantName,
        string adminFirstName,
        string subdomain)
    {
        try
        {
            var loginUrl = $"{_frontendUrl}/login";

            var subject = $"Welcome to MorisHR - {tenantName}";
            var htmlBody = GetTenantWelcomeTemplate(adminFirstName, tenantName, loginUrl, subdomain);

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

    private string GetTenantWelcomeTemplate(string firstName, string tenantName, string loginUrl, string subdomain)
    {
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
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">Welcome to MorisHR!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Congratulations, {firstName}!</h2>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">
                                Your account for <strong>{tenantName}</strong> has been successfully activated.
                                You can now access your HRMS dashboard and start managing your workforce.
                            </p>
                            <div style=""background-color: #e7f3ff; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #0d47a1; font-size: 14px; margin: 0 0 10px 0;"">
                                    <strong>Your Login Details:</strong>
                                </p>
                                <p style=""color: #0d47a1; font-size: 14px; margin: 0;"">
                                    Subdomain: <strong>{subdomain}</strong><br>
                                    Login URL: <a href=""{loginUrl}"" style=""color: #2196f3;"">{loginUrl}</a>
                                </p>
                            </div>
                            <table role=""presentation"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{loginUrl}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; border-radius: 5px; font-size: 16px; font-weight: bold;"">
                                            Go to Dashboard
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">What's Next?</h3>
                            <ul style=""color: #666666; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;"">
                                <li>Complete your company profile</li>
                                <li>Add departments and employee records</li>
                                <li>Configure payroll settings</li>
                                <li>Set up biometric attendance devices</li>
                                <li>Invite team members to join</li>
                            </ul>
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
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">Subscription Reminder</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <h2 style=""color: #333333; margin: 0 0 20px 0; font-size: 24px;"">Hi {firstName},</h2>
                            <div style=""background-color: #fff3cd; border-left: 4px solid {urgencyColor}; padding: 20px; margin: 20px 0;"">
                                <p style=""color: #856404; font-size: 16px; margin: 0; font-weight: bold;"">
                                    {urgencyText} Your MorisHR subscription for {tenantName} will expire in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}.
                                </p>
                            </div>
                            <p style=""color: #666666; font-size: 16px; line-height: 1.6; margin: 20px 0;"">
                                To ensure uninterrupted access to your HRMS, please renew your subscription before it expires.
                            </p>
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
