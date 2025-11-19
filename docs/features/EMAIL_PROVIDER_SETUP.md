# Email Provider Setup Guide
**Complete Configuration Guide for Production Email Delivery**

---

## Table of Contents
1. [Gmail with App Password](#gmail-with-app-password)
2. [SendGrid](#sendgrid)
3. [AWS Simple Email Service (SES)](#aws-simple-email-service-ses)
4. [Azure Communication Services](#azure-communication-services)
5. [Mailgun](#mailgun)
6. [Testing Email Configuration](#testing-email-configuration)
7. [Troubleshooting](#troubleshooting)

---

## Gmail with App Password

**Best For:** Small deployments, development/staging environments
**Cost:** Free (with limitations)
**Limitations:** 500 emails/day, 500 recipients per email

### Step-by-Step Setup

#### 1. Enable 2-Factor Authentication

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Find "2-Step Verification" section
3. Click "Get started" and follow the prompts
4. Complete phone verification

#### 2. Generate App Password

1. Go to [App Passwords](https://myaccount.google.com/apppasswords)
2. Select "Mail" from the "Select app" dropdown
3. Select "Other (Custom name)" from the "Select device" dropdown
4. Enter name: `MorisHR Production`
5. Click "Generate"
6. Copy the 16-character password (spaces included, but you can remove them)
7. **Important:** Save this password securely - you won't see it again

#### 3. Configure Google Secret Manager

```bash
# Set SMTP username (your Gmail address)
echo -n "your-email@gmail.com" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-

# Set SMTP password (the 16-character app password)
echo -n "abcd efgh ijkl mnop" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-
```

#### 4. Update appsettings.Production.json

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "your-email@gmail.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false,
    "EnableEmailSending": true,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

#### 5. Test Configuration

```bash
curl -X POST https://your-domain.com/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{"toEmail":"test@example.com"}'
```

### Gmail Best Practices

- **Use a dedicated Gmail account** for the application (not your personal email)
- **Monitor daily sending limits** in Gmail dashboard
- **Set up email forwarding** to monitor bounces
- **Enable less secure app access** is NOT required with App Passwords
- **Rotate App Passwords** every 90 days for security

### Gmail Troubleshooting

**Error: "Username and Password not accepted"**
- Verify App Password is correct (no spaces)
- Ensure 2FA is enabled
- Generate a new App Password

**Error: "Daily sending quota exceeded"**
- You've hit the 500 emails/day limit
- Wait 24 hours or upgrade to SendGrid/SES
- Consider batching non-critical emails

**Emails going to spam:**
- Add SPF record to your domain DNS
- Set up DKIM authentication
- Use a professional "From" address

---

## SendGrid

**Best For:** Production deployments, scalable email sending
**Cost:** Free (100 emails/day), Essentials $19.95/mo (50,000 emails/month)
**Limitations:** None on free tier (after sender verification)

### Step-by-Step Setup

#### 1. Create SendGrid Account

1. Go to [SendGrid Signup](https://signup.sendgrid.com/)
2. Complete registration
3. Verify email address
4. Complete account setup questionnaire

#### 2. Verify Sender Identity

**Option A: Single Sender Verification (Quick)**
1. Go to Settings > Sender Authentication
2. Click "Verify a Single Sender"
3. Enter email details:
   - From Name: `MorisHR`
   - From Email: `noreply@yourdomain.com`
   - Reply To: `support@yourdomain.com`
4. Check email and click verification link

**Option B: Domain Authentication (Recommended for Production)**
1. Go to Settings > Sender Authentication
2. Click "Authenticate Your Domain"
3. Select DNS host provider
4. Add provided DNS records (CNAME, TXT)
5. Wait for DNS propagation (up to 48 hours)
6. Verify in SendGrid dashboard

#### 3. Generate API Key

1. Go to Settings > API Keys
2. Click "Create API Key"
3. Name: `MorisHR Production`
4. Permissions: Select "Restricted Access"
5. Enable only: **Mail Send** permission
6. Click "Create & View"
7. **Copy the API key immediately** (you won't see it again)

#### 4. Configure Google Secret Manager

```bash
# Set SMTP username (literally the word "apikey")
echo -n "apikey" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-

# Set SMTP password (your SendGrid API key)
echo -n "SG.your_actual_api_key_here" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-
```

#### 5. Update appsettings.Production.json

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false,
    "EnableEmailSending": true,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

#### 6. Configure Email Analytics

1. Go to Settings > Mail Settings
2. Enable "Event Webhook" for delivery tracking
3. Set webhook URL: `https://your-domain.com/api/webhooks/sendgrid`
4. Select events to track:
   - Delivered
   - Bounced
   - Spam Reports
   - Unsubscribes

### SendGrid DNS Configuration

Add these DNS records for better deliverability:

**SPF Record:**
```
Type: TXT
Host: @
Value: v=spf1 include:sendgrid.net ~all
TTL: 3600
```

**DKIM Records** (provided by SendGrid after domain authentication):
```
Type: CNAME
Host: s1._domainkey.yourdomain.com
Value: s1.domainkey.u1234567.wl001.sendgrid.net
TTL: 3600

Type: CNAME
Host: s2._domainkey.yourdomain.com
Value: s2.domainkey.u1234567.wl001.sendgrid.net
TTL: 3600
```

### SendGrid Best Practices

- **Warm up your sending** - Start with low volume, gradually increase
- **Monitor bounce rates** - Keep below 5%
- **Handle unsubscribes** - Implement unsubscribe links
- **Track email analytics** - Review open/click rates
- **Use templates** - Create reusable email templates in SendGrid

### SendGrid Pricing Tiers

| Plan | Price | Emails/Month | Support |
|------|-------|--------------|---------|
| Free | $0 | 100/day (3,000/month) | Email |
| Essentials | $19.95 | 50,000 | Email |
| Pro | $89.95 | 100,000 | Email + Chat |
| Premier | Custom | Custom | Dedicated CSM |

---

## AWS Simple Email Service (SES)

**Best For:** High-volume production deployments, cost-sensitive applications
**Cost:** $0.10 per 1,000 emails (+ data transfer)
**Limitations:** None (after production access approval)

### Step-by-Step Setup

#### 1. Create AWS Account

1. Go to [AWS Console](https://console.aws.amazon.com/)
2. Sign up or log in
3. Navigate to SES service
4. Select your preferred region (e.g., us-east-1)

#### 2. Verify Email/Domain

**Verify Email Address:**
```bash
# Using AWS CLI
aws ses verify-email-identity \
  --email-address noreply@yourdomain.com \
  --region us-east-1

# Check email and click verification link
```

**Verify Domain (Recommended):**
1. Go to SES Console > Verified Identities
2. Click "Create Identity"
3. Select "Domain"
4. Enter your domain: `yourdomain.com`
5. Enable DKIM signing
6. Copy DNS records provided
7. Add to your DNS provider:

```
Type: TXT
Host: _amazonses.yourdomain.com
Value: [verification token from AWS]
TTL: 1800

Type: CNAME (DKIM 1)
Host: [selector]._domainkey.yourdomain.com
Value: [value from AWS]
TTL: 1800

Type: CNAME (DKIM 2)
Host: [selector]._domainkey.yourdomain.com
Value: [value from AWS]
TTL: 1800

Type: MX
Host: yourdomain.com
Value: 10 feedback-smtp.us-east-1.amazonses.com
TTL: 1800
```

#### 3. Request Production Access

**Important:** By default, SES is in "Sandbox Mode" (limited sending)

1. Go to SES Console > Account Dashboard
2. Click "Request production access"
3. Fill out the form:
   - Use case: `Transactional emails for HR management system`
   - Website URL: `https://yourdomain.com`
   - Expected daily volume: `[your estimate]`
   - Bounce handling: `We monitor bounces and remove invalid addresses`
4. Submit request
5. Wait for approval (typically 24-48 hours)

#### 4. Create SMTP Credentials

1. Go to SES Console > SMTP Settings
2. Click "Create My SMTP Credentials"
3. IAM User Name: `hrms-ses-smtp`
4. Click "Create"
5. **Download credentials** (you won't see them again)
6. Note the SMTP endpoint for your region

#### 5. Configure Google Secret Manager

```bash
# Set SMTP username (from AWS SMTP credentials)
echo -n "YOUR_AWS_SMTP_USERNAME" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-

# Set SMTP password (from AWS SMTP credentials)
echo -n "YOUR_AWS_SMTP_PASSWORD" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-
```

#### 6. Update appsettings.Production.json

```json
{
  "EmailSettings": {
    "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false,
    "EnableEmailSending": true,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

#### 7. Configure SNS for Bounce/Complaint Handling

```bash
# Create SNS topic for bounces
aws sns create-topic --name hrms-ses-bounces --region us-east-1

# Subscribe your API endpoint
aws sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:YOUR_ACCOUNT_ID:hrms-ses-bounces \
  --protocol https \
  --notification-endpoint https://your-domain.com/api/webhooks/ses-bounces

# Configure SES to publish to SNS
aws ses set-identity-notification-topic \
  --identity yourdomain.com \
  --notification-type Bounce \
  --sns-topic arn:aws:sns:us-east-1:YOUR_ACCOUNT_ID:hrms-ses-bounces
```

### AWS SES Best Practices

- **Monitor reputation metrics** - Keep bounce rate < 5%, complaint rate < 0.1%
- **Implement bounce handling** - Remove bounced addresses automatically
- **Use Configuration Sets** - Track email events
- **Set up dedicated IP** (for high volume) - Improves deliverability
- **Monitor sending quotas** - AWS limits can be increased on request

### AWS SES Pricing

- **Email sending:** $0.10 per 1,000 emails
- **Received emails:** $0.10 per 1,000 emails
- **Dedicated IP:** $24.95/month per IP
- **Data transfer:** Standard AWS rates apply
- **Free tier:** 62,000 emails/month (when sending from EC2)

### AWS SES Regions

Choose region closest to your users:

| Region | SMTP Endpoint |
|--------|---------------|
| US East (N. Virginia) | email-smtp.us-east-1.amazonaws.com |
| US West (Oregon) | email-smtp.us-west-2.amazonaws.com |
| EU (Ireland) | email-smtp.eu-west-1.amazonaws.com |
| EU (Frankfurt) | email-smtp.eu-central-1.amazonaws.com |
| Asia Pacific (Singapore) | email-smtp.ap-southeast-1.amazonaws.com |
| Asia Pacific (Sydney) | email-smtp.ap-southeast-2.amazonaws.com |

---

## Azure Communication Services

**Best For:** Microsoft Azure users, integrated Azure solutions
**Cost:** $0.03 per 1,000 emails
**Limitations:** None

### Quick Setup

1. Create Azure Communication Services resource
2. Get connection string from Azure portal
3. Configure managed identity for authentication
4. Use Azure SDK (not SMTP)

**Note:** Azure Communication Services uses REST API instead of SMTP. Implementation requires custom email service adapter.

---

## Mailgun

**Best For:** Developers, flexible API usage
**Cost:** Free (5,000 emails/month), Flex starting at $35/mo
**Limitations:** Credit card required for free tier

### Quick Setup

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.mailgun.org",
    "SmtpPort": 587,
    "SmtpUsername": "postmaster@mg.yourdomain.com",
    "SmtpPassword": "your-mailgun-smtp-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  }
}
```

---

## Testing Email Configuration

### 1. Configuration Validation

```bash
# Test configuration without sending email
curl -X GET https://your-domain.com/api/admin/emailtest/validate \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT"
```

Expected response:
```json
{
  "success": true,
  "isValid": true,
  "validationResults": [
    {"field": "SmtpServer", "status": "ok", "value": "smtp.sendgrid.net"},
    {"field": "SmtpPort", "status": "ok", "value": 587},
    {"field": "FromEmail", "status": "ok", "value": "noreply@yourdomain.com"},
    {"field": "Credentials", "status": "ok", "message": "Credentials configured"},
    {"field": "EnableSsl", "status": "ok", "value": true}
  ]
}
```

### 2. Send Test Email

```bash
# Send a single test email
curl -X POST https://your-domain.com/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }'
```

### 3. Test All Email Templates

```bash
# Send all subscription notification templates
curl -X POST https://your-domain.com/api/admin/emailtest/send-subscription-templates \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }'
```

### 4. Monitor Email Logs

```bash
# View email sending logs
sudo journalctl -u hrms-api | grep -i "email"

# Or for Cloud Run
gcloud logging read "resource.type=cloud_run_revision AND textPayload=~'email'" \
  --limit 50 \
  --format json
```

---

## Troubleshooting

### Common Issues

#### Issue: "SMTP Authentication Failed"

**Causes:**
- Incorrect username/password
- Secret Manager configuration error
- 2FA not enabled (Gmail)
- API key expired (SendGrid)

**Solutions:**
```bash
# Verify secrets exist
gcloud secrets list | grep Email

# View secret metadata (not value)
gcloud secrets describe EmailSettings__SmtpPassword

# Update secret with new value
echo -n "NEW_PASSWORD" | gcloud secrets versions add EmailSettings__SmtpPassword --data-file=-

# Restart application to load new secrets
sudo systemctl restart hrms-api
```

#### Issue: "Connection Timeout"

**Causes:**
- Firewall blocking port 587
- Network connectivity issues
- Incorrect SMTP server

**Solutions:**
```bash
# Test SMTP connectivity
telnet smtp.sendgrid.net 587
# Should see: 220 smtp.sendgrid.net ESMTP

# Test with openssl
openssl s_client -connect smtp.sendgrid.net:587 -starttls smtp

# Check firewall rules (GCP)
gcloud compute firewall-rules list | grep egress
```

#### Issue: "Emails Going to Spam"

**Causes:**
- Missing SPF/DKIM records
- Poor sender reputation
- Spam-like content

**Solutions:**
1. **Add SPF Record:**
```
Type: TXT
Host: @
Value: v=spf1 include:sendgrid.net ~all
```

2. **Enable DKIM:** Follow provider-specific instructions

3. **Check sender reputation:**
   - [Google Postmaster Tools](https://postmaster.google.com/)
   - [Microsoft SNDS](https://postmaster.live.com/snds/)

4. **Test email deliverability:**
   - [Mail-Tester](https://www.mail-tester.com/)
   - Send email to Mail-Tester address
   - Review score and recommendations

#### Issue: "Daily Sending Quota Exceeded"

**Gmail:**
- Wait 24 hours or upgrade to SendGrid/SES

**SendGrid:**
- Upgrade to higher tier

**AWS SES:**
```bash
# Check current quotas
aws ses get-send-quota --region us-east-1

# Request quota increase
# Go to AWS Support > Create Case > Service Limit Increase > SES Sending Quota
```

---

## Email Provider Comparison

| Feature | Gmail | SendGrid | AWS SES | Mailgun |
|---------|-------|----------|---------|---------|
| **Free Tier** | 500/day | 100/day | 62K/month* | 5K/month |
| **Cost (per 1K)** | Free | $0.40 | $0.10 | $0.80 |
| **Setup Difficulty** | Easy | Easy | Medium | Easy |
| **Deliverability** | Good | Excellent | Excellent | Excellent |
| **Analytics** | Basic | Advanced | Basic | Advanced |
| **Support** | Limited | Good | Good | Good |
| **Best For** | Dev/Staging | Production | High Volume | Developers |

*AWS SES free tier only when sending from EC2

---

## Recommendations

### For Development/Staging:
- **Gmail with App Password** - Quick setup, no cost

### For Small Production (<10K emails/month):
- **SendGrid Free Tier** - 100 emails/day, good analytics

### For Medium Production (10K-100K emails/month):
- **SendGrid Essentials** - $19.95/mo, 50K emails included
- **AWS SES** - Pay-as-you-go, lower cost

### For Large Production (>100K emails/month):
- **AWS SES** - Most cost-effective at scale
- **SendGrid Pro** - Better analytics and support

---

## Next Steps

After configuring your email provider:

1. ✅ Test email delivery to multiple providers (Gmail, Outlook, Yahoo)
2. ✅ Verify emails are not going to spam
3. ✅ Set up bounce/complaint handling
4. ✅ Configure email analytics and monitoring
5. ✅ Document your email configuration
6. ✅ Set up alerts for delivery failures
7. ✅ Train team on email best practices

---

**Last Updated:** 2025-01-11
**For Support:** See PRODUCTION_DEPLOYMENT.md
