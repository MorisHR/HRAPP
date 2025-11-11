# Email Infrastructure Summary
**Quick Reference Guide for MorisHR Email System**

---

## Overview

The MorisHR email system is production-ready with:
- Professional HTML email templates
- Multiple email provider support (Gmail, SendGrid, AWS SES)
- Built-in testing and validation tools
- Comprehensive error handling and retry logic
- Mobile-responsive templates with unsubscribe compliance

---

## Email Templates Available

### 1. System Emails
- **Tenant Activation** - Welcome email with activation link
- **Tenant Welcome** - Post-activation confirmation
- **Password Reset** - Secure password reset link
- **Test Email** - Configuration verification

### 2. HR Operations
- **Document Expiry Alert** - Critical document expiration warnings
- **Leave Approval/Rejection** - Leave request notifications
- **Payslip Ready** - Monthly payslip availability
- **Welcome Email** - New employee onboarding
- **Attendance Correction** - Attendance approval notifications

### 3. Subscription Management (NEW)
- **30-Day Renewal Reminder** - Subscription expiring in 30 days
- **7-Day Expiring Warning** - Urgent renewal reminder
- **Subscription Expired** - Grace period started notification
- **Account Suspended** - Critical suspension alert
- **Renewal Confirmation** - Successful subscription renewal

---

## Testing Endpoints (SuperAdmin Only)

### 1. Configuration Status
```bash
GET /api/admin/emailtest/config-status
```
Returns current email configuration (without exposing secrets)

### 2. Validate Configuration
```bash
GET /api/admin/emailtest/validate
```
Validates email settings without sending emails

### 3. Send Test Email
```bash
POST /api/admin/emailtest/send-test
Body: {"toEmail": "test@example.com"}
```
Sends a single test email

### 4. Test All Subscription Templates
```bash
POST /api/admin/emailtest/send-subscription-templates
Body: {"toEmail": "test@example.com"}
```
Sends all subscription notification templates

---

## Configuration Files

### Development
**File:** `/src/HRMS.API/appsettings.json`
- Uses localhost SMTP (MailHog)
- Port 1025 (no SSL)
- No credentials required

### Production
**File:** `/src/HRMS.API/appsettings.Production.json`
- Real SMTP provider (Gmail/SendGrid/SES)
- Port 587 with SSL
- Credentials from Google Secret Manager

### Example
**File:** `/src/HRMS.API/appsettings.Production.json.example`
- Template with all options documented
- Copy and customize for your environment

---

## Google Secret Manager Secrets

Required secrets for production:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `EmailSettings__SmtpUsername` | SMTP username or API key identifier | `apikey` (SendGrid) or `your@email.com` (Gmail) |
| `EmailSettings__SmtpPassword` | SMTP password or API key | App Password or API key |
| `ConnectionStrings__DefaultConnection` | Database connection string | PostgreSQL connection string |
| `JwtSettings__Secret` | JWT signing secret | 64-char random string |
| `Redis__ConnectionString` | Redis connection string | Redis host and credentials |

---

## Email Provider Quick Setup

### Gmail (Development/Small Scale)
```json
{
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "EnableSsl": true
}
```
**Limit:** 500 emails/day
**Setup Time:** 5 minutes

### SendGrid (Recommended for Production)
```json
{
  "SmtpServer": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SmtpUsername": "apikey",
  "EnableSsl": true
}
```
**Cost:** Free (100/day) or $19.95/mo (50K/month)
**Setup Time:** 15 minutes

### AWS SES (Best for High Volume)
```json
{
  "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
  "SmtpPort": 587,
  "EnableSsl": true
}
```
**Cost:** $0.10 per 1,000 emails
**Setup Time:** 30 minutes + approval wait

---

## Production Deployment Checklist

### Pre-Deployment
- [ ] Choose email provider (Gmail/SendGrid/SES)
- [ ] Create provider account and verify sender
- [ ] Generate SMTP credentials
- [ ] Configure Google Secret Manager secrets
- [ ] Update `appsettings.Production.json`
- [ ] Configure DNS records (SPF, DKIM)

### Deployment
- [ ] Deploy application with production config
- [ ] Verify secrets loaded correctly
- [ ] Test connectivity to SMTP server
- [ ] Send test email to multiple providers
- [ ] Check emails not going to spam

### Post-Deployment
- [ ] Monitor email delivery logs
- [ ] Set up delivery failure alerts
- [ ] Configure bounce/complaint handling
- [ ] Document email configuration
- [ ] Train team on email testing tools

---

## Quick Commands

### Test SMTP Connectivity
```bash
telnet smtp.sendgrid.net 587
```

### Create Secret in Google Secret Manager
```bash
echo -n "YOUR_SECRET_VALUE" | \
gcloud secrets create SECRET_NAME --data-file=-
```

### View Application Logs
```bash
# VM deployment
sudo journalctl -u hrms-api -f | grep -i email

# Cloud Run deployment
gcloud logging read "textPayload=~'email'" --limit 50
```

### Send Test Email via API
```bash
curl -X POST https://your-domain.com/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{"toEmail":"test@example.com"}'
```

### Check Email Deliverability
Send test email to: `[random-id]@mail-tester.com`
Then visit: `https://www.mail-tester.com/[random-id]`

---

## Monitoring & Alerts

### Key Metrics to Track
1. **Email send success rate** - Target: >99.5%
2. **Email delivery time** - Target: <5 seconds
3. **Bounce rate** - Target: <5%
4. **Spam complaint rate** - Target: <0.1%
5. **Daily sending volume** - Monitor against quotas

### Set Up Alerts For
- Failed email sends (3+ consecutive failures)
- High bounce rate (>5%)
- Spam complaints
- Quota approaching limit
- SMTP authentication failures

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  HRMS Application                    │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │          IEmailService Interface           │    │
│  └────────────────────────────────────────────┘    │
│                       ↓                             │
│  ┌────────────────────────────────────────────┐    │
│  │       EmailService Implementation          │    │
│  │  • HTML Template Generation                │    │
│  │  • SMTP Connection Management              │    │
│  │  • Retry Logic (3 attempts)                │    │
│  │  • Error Handling                          │    │
│  └────────────────────────────────────────────┘    │
│                       ↓                             │
│  ┌────────────────────────────────────────────┐    │
│  │         MailKit SMTP Client                │    │
│  │  • TLS/SSL Support                         │    │
│  │  • Authentication                          │    │
│  │  • Connection Pooling                      │    │
│  └────────────────────────────────────────────┘    │
│                       ↓                             │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│              Email Provider (SMTP)                   │
├─────────────────────────────────────────────────────┤
│  • Gmail (smtp.gmail.com:587)                       │
│  • SendGrid (smtp.sendgrid.net:587)                 │
│  • AWS SES (email-smtp.REGION.amazonaws.com:587)    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│                 Email Recipients                     │
│  • Tenant Admins                                    │
│  • Employees                                        │
│  • System Administrators                            │
└─────────────────────────────────────────────────────┘
```

---

## Email Template Features

### All Templates Include
- **Mobile-responsive design** - Works on all devices
- **Professional styling** - Gradient headers, clean layout
- **Company branding** - Logo placeholder, custom colors
- **Compliance footer** - Unsubscribe link, legal notices
- **Security headers** - Prevents email spoofing
- **Plain text fallback** - For email clients that don't support HTML

### Template Customization
Templates can be customized by editing:
`/src/HRMS.Infrastructure/Services/EmailService.cs`

Look for methods:
- `GetTenantActivationTemplate()`
- `GetExpiryReminderTemplate()`
- `GetSubscriptionExpiredTemplate()`
- `GetAccountSuspendedTemplate()`
- `GetRenewalConfirmationTemplate()`

---

## Troubleshooting Guide

### Issue: Emails Not Sending

**Check:**
1. SMTP configuration correct?
2. Secrets loaded from Secret Manager?
3. Network allows outbound port 587?
4. SMTP credentials valid?

**Debug:**
```bash
# Check logs
sudo journalctl -u hrms-api | grep -i "email"

# Test SMTP connection
telnet smtp.sendgrid.net 587

# Validate configuration
curl -X GET https://your-domain.com/api/admin/emailtest/validate
```

### Issue: Emails Going to Spam

**Check:**
1. SPF record configured?
2. DKIM signatures enabled?
3. Sender domain verified?
4. "From" address professional?

**Test:**
1. Send to mail-tester.com
2. Review spam score and recommendations
3. Add missing DNS records

### Issue: Slow Email Delivery

**Check:**
1. SMTP server response time
2. Network latency
3. Email template size
4. Concurrent sending volume

**Optimize:**
1. Use async sending
2. Implement queue for bulk sends
3. Reduce email HTML size
4. Enable connection pooling

---

## Support Resources

### Documentation
- **Production Deployment:** `/PRODUCTION_DEPLOYMENT.md`
- **Email Provider Setup:** `/docs/EMAIL_PROVIDER_SETUP.md`
- **API Documentation:** `https://your-domain.com/swagger`

### Code References
- **Email Service:** `/src/HRMS.Infrastructure/Services/EmailService.cs`
- **Email Interface:** `/src/HRMS.Application/Interfaces/IEmailService.cs`
- **Test Controller:** `/src/HRMS.API/Controllers/EmailTestController.cs`
- **Email Settings:** `/src/HRMS.Core/Settings/EmailSettings.cs`

### External Resources
- [SendGrid Documentation](https://docs.sendgrid.com/)
- [AWS SES Documentation](https://docs.aws.amazon.com/ses/)
- [Gmail App Passwords](https://support.google.com/accounts/answer/185833)
- [Email Deliverability Best Practices](https://www.mailgun.com/resources/email-deliverability/)

---

## Contact

**Technical Support:** support@morishr.com
**Security Issues:** security@morishr.com
**Documentation Issues:** Submit PR to repository

---

**Created:** 2025-01-11
**Last Updated:** 2025-01-11
**Version:** 1.0.0
