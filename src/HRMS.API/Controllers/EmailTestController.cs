using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using Microsoft.Extensions.Options;
using HRMS.Core.Settings;

namespace HRMS.API.Controllers;

/// <summary>
/// Email testing controller for SuperAdmin to verify email configuration
/// PRODUCTION READY: Includes validation, error handling, and comprehensive testing
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailTestController> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailTestController(
        IEmailService emailService,
        ILogger<EmailTestController> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _emailService = emailService;
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// Get current email configuration status (without exposing credentials)
    /// GET /api/admin/emailtest/config-status
    /// </summary>
    [HttpGet("config-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigurationStatus()
    {
        var configStatus = new
        {
            SmtpServer = _emailSettings.SmtpServer,
            SmtpPort = _emailSettings.SmtpPort,
            FromEmail = _emailSettings.FromEmail,
            FromName = _emailSettings.FromName,
            EnableSsl = _emailSettings.EnableSsl,
            HasUsername = !string.IsNullOrEmpty(_emailSettings.SmtpUsername),
            HasPassword = !string.IsNullOrEmpty(_emailSettings.SmtpPassword),
            IsConfigured = !string.IsNullOrEmpty(_emailSettings.SmtpServer) &&
                          !string.IsNullOrEmpty(_emailSettings.FromEmail),
            Recommendation = GetConfigurationRecommendation()
        };

        return Ok(new
        {
            success = true,
            data = configStatus
        });
    }

    /// <summary>
    /// Send a test email to verify email configuration
    /// POST /api/admin/emailtest/send-test
    /// </summary>
    [HttpPost("send-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new
            {
                success = false,
                message = "Email address is required"
            });
        }

        // Validate email format
        if (!IsValidEmail(request.ToEmail))
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid email address format"
            });
        }

        try
        {
            _logger.LogInformation("Sending test email to {Email}", request.ToEmail);

            var subject = "HRMS Email Configuration Test";
            var htmlBody = GetTestEmailTemplate(request.ToEmail);

            await _emailService.SendHtmlEmailAsync(request.ToEmail, subject, htmlBody);

            _logger.LogInformation("Test email sent successfully to {Email}", request.ToEmail);

            return Ok(new
            {
                success = true,
                message = $"Test email sent successfully to {request.ToEmail}",
                sentAt = DateTime.UtcNow,
                configuration = new
                {
                    smtpServer = _emailSettings.SmtpServer,
                    smtpPort = _emailSettings.SmtpPort,
                    fromEmail = _emailSettings.FromEmail
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {Email}", request.ToEmail);

            return StatusCode(500, new
            {
                success = false,
                message = "Failed to send test email",
                error = ex.Message,
                troubleshooting = new[]
                {
                    "Check SMTP server address and port",
                    "Verify username and password credentials",
                    "Ensure SSL/TLS settings are correct",
                    "Check if sender email is verified with provider",
                    "Review firewall and network settings",
                    "Check Google Secret Manager configuration"
                }
            });
        }
    }

    /// <summary>
    /// Send test emails for all subscription notification types
    /// POST /api/admin/emailtest/send-subscription-templates
    /// </summary>
    [HttpPost("send-subscription-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendSubscriptionTemplates([FromBody] TestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new { success = false, message = "Email address is required" });
        }

        var results = new List<object>();

        try
        {
            // Test 1: 30-day renewal reminder
            await _emailService.SendExpiryReminderAsync(
                request.ToEmail,
                "Test Company Ltd",
                30,
                "Admin"
            );
            results.Add(new { template = "30-day renewal reminder", status = "sent" });

            // Test 2: 7-day expiring warning
            await _emailService.SendExpiryReminderAsync(
                request.ToEmail,
                "Test Company Ltd",
                7,
                "Admin"
            );
            results.Add(new { template = "7-day expiring warning", status = "sent" });

            // Test 3: 1-day final warning
            await _emailService.SendExpiryReminderAsync(
                request.ToEmail,
                "Test Company Ltd",
                1,
                "Admin"
            );
            results.Add(new { template = "1-day final warning", status = "sent" });

            return Ok(new
            {
                success = true,
                message = $"All subscription email templates sent to {request.ToEmail}",
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription template emails");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to send one or more template emails",
                error = ex.Message,
                sentTemplates = results
            });
        }
    }

    /// <summary>
    /// Validate email configuration without sending emails
    /// GET /api/admin/emailtest/validate
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ValidateConfiguration()
    {
        var validationResults = new List<object>();
        var isValid = true;

        // Check SMTP Server
        if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
        {
            validationResults.Add(new { field = "SmtpServer", status = "error", message = "SMTP server is not configured" });
            isValid = false;
        }
        else
        {
            validationResults.Add(new { field = "SmtpServer", status = "ok", value = _emailSettings.SmtpServer });
        }

        // Check SMTP Port
        if (_emailSettings.SmtpPort <= 0)
        {
            validationResults.Add(new { field = "SmtpPort", status = "error", message = "Invalid SMTP port" });
            isValid = false;
        }
        else
        {
            validationResults.Add(new { field = "SmtpPort", status = "ok", value = _emailSettings.SmtpPort });
        }

        // Check From Email
        if (string.IsNullOrEmpty(_emailSettings.FromEmail) || !IsValidEmail(_emailSettings.FromEmail))
        {
            validationResults.Add(new { field = "FromEmail", status = "error", message = "From email is invalid or not configured" });
            isValid = false;
        }
        else
        {
            validationResults.Add(new { field = "FromEmail", status = "ok", value = _emailSettings.FromEmail });
        }

        // Check Credentials
        if (string.IsNullOrEmpty(_emailSettings.SmtpUsername) || string.IsNullOrEmpty(_emailSettings.SmtpPassword))
        {
            validationResults.Add(new { field = "Credentials", status = "warning", message = "SMTP credentials not configured (may work for localhost)" });
        }
        else
        {
            validationResults.Add(new { field = "Credentials", status = "ok", message = "Credentials configured" });
        }

        // Check SSL/TLS
        if (_emailSettings.SmtpPort == 587 && !_emailSettings.EnableSsl)
        {
            validationResults.Add(new { field = "EnableSsl", status = "warning", message = "Port 587 typically requires SSL/TLS" });
        }
        else if (_emailSettings.SmtpPort == 465 && !_emailSettings.EnableSsl)
        {
            validationResults.Add(new { field = "EnableSsl", status = "error", message = "Port 465 requires SSL/TLS" });
            isValid = false;
        }
        else
        {
            validationResults.Add(new { field = "EnableSsl", status = "ok", value = _emailSettings.EnableSsl });
        }

        return Ok(new
        {
            success = true,
            isValid,
            validationResults,
            recommendations = GetConfigurationRecommendations()
        });
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private string GetConfigurationRecommendation()
    {
        if (_emailSettings.SmtpServer == "localhost")
        {
            return "Development mode - using localhost SMTP (MailHog). Configure production SMTP for production deployment.";
        }

        if (_emailSettings.SmtpServer?.Contains("gmail") == true)
        {
            return "Gmail configured - ensure you're using App Password (not regular password)";
        }

        if (_emailSettings.SmtpServer?.Contains("sendgrid") == true)
        {
            return "SendGrid configured - ensure API key is valid and sender is verified";
        }

        if (_emailSettings.SmtpServer?.Contains("amazonaws") == true)
        {
            return "AWS SES configured - ensure IAM credentials and verified sender";
        }

        return "Custom SMTP provider - ensure all settings are correct";
    }

    private List<string> GetConfigurationRecommendations()
    {
        var recommendations = new List<string>();

        if (_emailSettings.SmtpServer == "localhost")
        {
            recommendations.Add("For production, configure a production-grade SMTP provider (Gmail, SendGrid, or AWS SES)");
        }

        if (!_emailSettings.EnableSsl && _emailSettings.SmtpPort != 1025)
        {
            recommendations.Add("Enable SSL/TLS for secure email transmission");
        }

        if (string.IsNullOrEmpty(_emailSettings.SmtpUsername))
        {
            recommendations.Add("Configure SMTP authentication credentials");
        }

        if (_emailSettings.FromEmail?.EndsWith("@example.com") == true ||
            _emailSettings.FromEmail?.EndsWith("@localhost") == true)
        {
            recommendations.Add("Use a real, verified sender email address");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Configuration looks good! Send a test email to verify.");
        }

        return recommendations;
    }

    private string GetTestEmailTemplate(string recipientEmail)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>HRMS Email Test</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; border-collapse: collapse; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">MorisHR Email Test</h1>
                            <p style=""color: #ffffff; margin: 10px 0 0 0; font-size: 14px; opacity: 0.9;"">Email Configuration Verified</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <div style=""background-color: #d4edda; border-left: 4px solid #28a745; padding: 20px; margin: 20px 0;"">
                                <h2 style=""color: #155724; margin: 0 0 10px 0; font-size: 20px;"">✓ Success!</h2>
                                <p style=""color: #155724; font-size: 16px; margin: 0;"">
                                    Your HRMS email configuration is working correctly.
                                </p>
                            </div>

                            <h3 style=""color: #333333; margin: 30px 0 15px 0; font-size: 18px;"">Test Details</h3>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold; width: 40%;"">Sent To:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{recipientEmail}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">SMTP Server:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{_emailSettings.SmtpServer}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">SMTP Port:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{_emailSettings.SmtpPort}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">From Email:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{_emailSettings.FromEmail}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">SSL Enabled:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{(_emailSettings.EnableSsl ? "Yes" : "No")}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef; font-weight: bold;"">Test Time:</td>
                                    <td style=""padding: 10px; border-bottom: 1px solid #e9ecef;"">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td>
                                </tr>
                            </table>

                            <div style=""background-color: #e7f3ff; border-left: 4px solid #2196f3; padding: 15px; margin: 30px 0;"">
                                <p style=""color: #0d47a1; font-size: 14px; margin: 0;"">
                                    <strong>Next Steps:</strong><br/>
                                    • Test subscription notification emails<br/>
                                    • Verify email delivery to all admin users<br/>
                                    • Monitor email logs for any issues<br/>
                                    • Configure email templates for production
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #6c757d; font-size: 12px; margin: 0 0 10px 0;"">
                                This is an automated test email from HRMS
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
}

public class TestEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
}
