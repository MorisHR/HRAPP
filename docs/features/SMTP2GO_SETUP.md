# SMTP2GO Setup Guide for HRMS
**Complete Configuration Guide for SMTP2GO Email Delivery**

---

## Why SMTP2GO?

**Advantages Over Other Providers (2025):**
- ✅ **Free tier**: 1,000 emails/month, 200/day (SendGrid discontinued free plan)
- ✅ **Best deliverability**: 95.5% (vs SendGrid's 82%)
- ✅ **Highest reliability**: Score 9.4/10
- ✅ **Best support**: Score 9.2/10
- ✅ **No credit card required** for free tier
- ✅ **Larger attachments**: 50 MB per email
- ✅ **Same pricing when scaling**: $15/mo starting

---

## Quick Start (5 Minutes)

### Step 1: Create SMTP2GO Account

1. Go to https://www.smtp2go.com/pricing/
2. Click **"Start Free Trial"**
3. Fill in your details:
   - Email address
   - Password
   - Company name: `Your Company Name`
4. Click "Sign Up"
5. Check your email and verify your account

**Free Tier Details:**
- 1,000 emails per month
- 200 emails per day
- 2 team members
- No time limit
- No credit card required

---

### Step 2: Verify Sender Email

You need to verify your sender email address before sending:

1. Log in to SMTP2GO dashboard
2. Go to **"Settings"** → **"Sender Domains/Addresses"**
3. Click **"Add Sender Email Address"**
4. Enter your sender email (e.g., `noreply@yourdomain.com` or `info@yourdomain.com`)
5. Click "Send Verification Email"
6. Check your email inbox and click the verification link
7. Wait for green checkmark in dashboard

**Tips:**
- Use a professional email address (not Gmail/Hotmail)
- If you own a domain, verify the entire domain for better deliverability
- You can verify multiple sender addresses

---

### Step 3: Generate SMTP Credentials

1. In SMTP2GO dashboard, go to **"Settings"** → **"Users"**
2. Click **"Add SMTP User"**
3. Enter details:
   - **Username**: `hrms-production` (or any name you prefer)
   - **Description**: `HRMS Application Production`
4. Click **"Create User"**
5. **IMPORTANT**: Copy the generated password immediately
   - Format: Long string like `smtp2go_xxxxxxxxxxxxxxxxxxxxx`
   - You won't be able to see it again
   - Store it securely

---

### Step 4: Configure HRMS Application

#### Option A: For Local Development/Testing

Edit `src/HRMS.API/appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "mail.smtp2go.com",
    "SmtpPort": 2525,
    "SmtpUsername": "hrms-production",
    "SmtpPassword": "smtp2go_xxxxxxxxxxxxxxxxxxxxx",
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

**Important Notes:**
- `SmtpServer`: Use `mail.smtp2go.com` (NOT smtp2go.com)
- `SmtpPort`: Use `2525`, `587`, or `80` (2525 recommended)
- `FromEmail`: Must be verified in SMTP2GO dashboard
- `EnableSsl`: Must be `true`

---

#### Option B: For Production (Google Secret Manager)

```bash
# Set SMTP username
echo -n "hrms-production" | \
gcloud secrets create EmailSettings__SmtpUsername --data-file=-

# Set SMTP password (your SMTP2GO password)
echo -n "smtp2go_xxxxxxxxxxxxxxxxxxxxx" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-
```

Update `src/HRMS.API/appsettings.Production.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "mail.smtp2go.com",
    "SmtpPort": 2525,
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

---

## SMTP2GO Port Options

SMTP2GO supports multiple ports. Choose based on your environment:

| Port | Protocol | Use Case | Recommended |
|------|----------|----------|-------------|
| **2525** | SMTP/TLS | General use (best compatibility) | ✅ Yes |
| **587** | SMTP/STARTTLS | Standard SMTP submission | ✅ Yes |
| **80** | HTTP/SMTP | Firewall-restricted environments | If needed |
| **8025** | SMTP/TLS | Alternative to 2525 | If needed |
| **465** | SMTP/SSL | Legacy SSL (less common) | No |

**Recommendation**: Use port **2525** (best compatibility and reliability)

---

## Testing Email Configuration

### Method 1: Using Email Test Controller

```bash
# Start your application
cd src/HRMS.API
dotnet run

# In another terminal, test email sending
curl -X POST http://localhost:5090/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }'
```

Expected response:
```json
{
  "success": true,
  "message": "Test email sent successfully to your-email@example.com"
}
```

---

### Method 2: Test All Subscription Email Templates

```bash
curl -X POST http://localhost:5090/api/admin/emailtest/send-subscription-templates \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }'
```

This will send all 4 subscription email templates:
1. 30-day renewal reminder
2. 7-day expiring warning
3. Subscription expired notice
4. Account suspended alert

---

### Method 3: Check Configuration Status

```bash
curl -X GET http://localhost:5090/api/admin/emailtest/config-status \
  -H "Authorization: Bearer YOUR_SUPERADMIN_JWT"
```

Expected response:
```json
{
  "success": true,
  "smtpServer": "mail.smtp2go.com",
  "smtpPort": 2525,
  "fromEmail": "noreply@yourdomain.com",
  "fromName": "MorisHR",
  "enableSsl": true,
  "emailSendingEnabled": true,
  "credentialsConfigured": true,
  "status": "Ready to send emails"
}
```

---

## Verifying Email Delivery

### 1. Check Application Logs

```bash
# View real-time logs
cd src/HRMS.API
dotnet run

# Look for email sending confirmation:
# [Information] Email sent successfully to: your-email@example.com
```

---

### 2. Check SMTP2GO Dashboard

1. Log in to SMTP2GO dashboard
2. Go to **"Activity"** → **"Email Activity"**
3. You should see:
   - Email sent successfully
   - Recipient email
   - Timestamp
   - Subject line
   - Delivery status

---

### 3. Check Your Email Inbox

1. Check the inbox for the recipient email
2. Look for email from "MorisHR <noreply@yourdomain.com>"
3. If not in inbox, check spam/junk folder
4. If in spam, see "Improving Deliverability" section below

---

## Improving Email Deliverability

To ensure emails don't go to spam, configure DNS records:

### Step 1: Domain Authentication in SMTP2GO

1. Go to **"Settings"** → **"Sender Domains"**
2. Click **"Authenticate Domain"**
3. Enter your domain: `yourdomain.com`
4. SMTP2GO will provide DNS records to add

---

### Step 2: Add DNS Records

Add these records to your domain DNS (e.g., Cloudflare, GoDaddy, Namecheap):

**SPF Record:**
```
Type: TXT
Host: @
Value: v=spf1 include:smtp2go.com ~all
TTL: 3600
```

**DKIM Record** (provided by SMTP2GO):
```
Type: TXT
Host: smtp2go._domainkey
Value: [long string provided by SMTP2GO]
TTL: 3600
```

**DMARC Record** (optional but recommended):
```
Type: TXT
Host: _dmarc
Value: v=DMARC1; p=none; rua=mailto:dmarc-reports@yourdomain.com
TTL: 3600
```

---

### Step 3: Verify DNS Configuration

1. Wait 10-60 minutes for DNS propagation
2. In SMTP2GO dashboard, click "Verify DNS Records"
3. All records should show green checkmarks
4. Test email delivery again

---

## SMTP2GO Pricing & Limits

### Free Plan (Current)
- **Monthly limit**: 1,000 emails
- **Daily limit**: 200 emails
- **Cost**: $0 (no credit card required)
- **Features**: Basic analytics, 2 team members
- **Perfect for**: Testing, staging, low-volume production

### Paid Plans (When You Need to Scale)

| Plan | Monthly Emails | Price/Month | Price/Year | Daily Limit |
|------|----------------|-------------|------------|-------------|
| **Starter** | 10,000 | $15 | $144 | No limit |
| **Standard** | 50,000 | $50 | $480 | No limit |
| **Pro** | 100,000 | $90 | $864 | No limit |
| **Business** | 250,000 | $190 | $1,824 | No limit |
| **Enterprise** | Custom | Custom | Custom | No limit |

**Annual subscriptions**: Get 2 months free (17% discount)

---

## Monitoring Email Usage

### Check Current Usage

1. Log in to SMTP2GO dashboard
2. Go to **"Activity"** → **"Usage Statistics"**
3. View:
   - Emails sent today
   - Emails sent this month
   - Remaining quota
   - Usage graphs

### Set Up Usage Alerts

1. Go to **"Settings"** → **"Account Settings"**
2. Enable **"Usage Alerts"**
3. Set threshold (e.g., 80% of quota)
4. Enter notification email
5. You'll receive alerts before hitting limits

---

## Troubleshooting

### Issue: "SMTP Authentication Failed"

**Causes:**
- Incorrect username or password
- Using wrong SMTP server

**Solutions:**
```bash
# Verify credentials in config
cat src/HRMS.API/appsettings.json | grep -A 10 "EmailSettings"

# Regenerate SMTP password in SMTP2GO
# Go to Settings → Users → Delete old user → Create new user

# Test SMTP connection
telnet mail.smtp2go.com 2525
# Should see: 220 mail.smtp2go.com ESMTP
```

---

### Issue: "Connection Timeout"

**Causes:**
- Firewall blocking port
- Wrong SMTP server

**Solutions:**
```bash
# Test connectivity
telnet mail.smtp2go.com 2525

# Try alternative port
# Change SmtpPort to 587 or 80

# Check firewall rules
sudo ufw status
```

---

### Issue: "Sender Not Verified"

**Causes:**
- FromEmail not verified in SMTP2GO
- Using unverified domain

**Solutions:**
1. Log in to SMTP2GO
2. Go to Settings → Sender Domains/Addresses
3. Verify your sender email
4. Update `FromEmail` in appsettings.json to match verified email

---

### Issue: "Emails Going to Spam"

**Causes:**
- Missing SPF/DKIM records
- Sender domain not authenticated
- Spam-like content

**Solutions:**
1. **Authenticate your domain** in SMTP2GO
2. **Add DNS records** (SPF, DKIM, DMARC)
3. **Avoid spam triggers**:
   - Don't use ALL CAPS in subject
   - Avoid excessive exclamation marks
   - Include unsubscribe link
   - Use professional "From" name
4. **Test deliverability**: Use https://www.mail-tester.com/

---

### Issue: "Daily Quota Exceeded"

**Cause:**
- Sent more than 200 emails today (free plan)

**Solutions:**
1. **Wait 24 hours** for quota reset
2. **Upgrade to paid plan** (removes daily limits)
3. **Batch non-critical emails** to spread over multiple days

---

## Best Practices

### 1. Use Professional Email Addresses
```
✅ Good: noreply@yourdomain.com
✅ Good: info@yourdomain.com
❌ Bad: yourname@gmail.com
❌ Bad: test@test.com
```

### 2. Configure Proper "From" Name
```json
{
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "MorisHR"  // ✅ Professional and recognizable
}
```

### 3. Monitor Bounce Rates
- Keep bounce rate below 5%
- Remove invalid email addresses promptly
- Use SMTP2GO's bounce reports

### 4. Handle Unsubscribes
- Include unsubscribe links in marketing emails
- Honor unsubscribe requests immediately
- Use SMTP2GO's suppression list

### 5. Warm Up Sending (For New Accounts)
- Start with low volume (50-100/day)
- Gradually increase over 2-4 weeks
- Helps build sender reputation

---

## Security Recommendations

### 1. Never Commit Passwords
```bash
# ❌ BAD: Never do this
git add appsettings.json  # Contains actual password

# ✅ GOOD: Use environment variables or Secret Manager
export EmailSettings__SmtpPassword="smtp2go_xxx"
```

### 2. Use Google Secret Manager (Production)
```bash
# Store in Secret Manager
echo -n "smtp2go_xxx" | \
gcloud secrets create EmailSettings__SmtpPassword --data-file=-

# Application reads from Secret Manager automatically
```

### 3. Rotate Credentials Regularly
- Generate new SMTP password every 90 days
- Delete old SMTP users in SMTP2GO dashboard
- Update Secret Manager with new password

### 4. Restrict SMTP User Permissions
- Create separate SMTP users for dev/staging/production
- If compromised, only affects one environment
- Easier to track which environment sent what

---

## Integration with Subscription System

Your subscription system will automatically send emails via SMTP2GO:

### Automatic Emails Sent:

1. **30-Day Renewal Reminder**
   - Sent 30 days before subscription expires
   - Subject: "Your MorisHR Subscription Expires in 30 Days"

2. **7-Day Expiring Warning**
   - Sent 7 days before expiry
   - Subject: "URGENT: Your MorisHR Subscription Expires in 7 Days"

3. **Subscription Expired**
   - Sent when subscription expires
   - Subject: "Your MorisHR Subscription Has Expired"

4. **Account Suspended**
   - Sent after grace period (7 days after expiry)
   - Subject: "IMPORTANT: Your MorisHR Account Has Been Suspended"

5. **Renewal Confirmation**
   - Sent when payment is recorded
   - Subject: "Payment Confirmed - Your MorisHR Subscription is Active"

### Background Job Schedule

The subscription notification job runs daily at 6:00 AM (Mauritius time) and sends all pending emails.

View scheduled jobs:
```bash
# Navigate to Hangfire dashboard
http://localhost:5090/hangfire

# Or in production
https://yourdomain.com/hangfire
```

---

## Next Steps

After configuring SMTP2GO:

- [ ] Create SMTP2GO account (free, 1,000 emails/month)
- [ ] Verify sender email in SMTP2GO dashboard
- [ ] Generate SMTP credentials
- [ ] Update appsettings.json with SMTP2GO configuration
- [ ] Test email delivery using Email Test Controller
- [ ] Check email arrives in inbox (not spam)
- [ ] Configure DNS records for better deliverability
- [ ] Set up usage alerts in SMTP2GO
- [ ] Document your SMTP2GO configuration
- [ ] Test subscription email templates

---

## Support Resources

**SMTP2GO Documentation:**
- Knowledge Base: https://support.smtp2go.com/
- API Documentation: https://apidocs.smtp2go.com/

**Application Documentation:**
- Email Provider Setup: `docs/EMAIL_PROVIDER_SETUP.md`
- Production Deployment: `PRODUCTION_DEPLOYMENT.md`
- Email Testing: `docs/EMAIL_INFRASTRUCTURE_SUMMARY.md`

**Contact:**
- SMTP2GO Support: https://www.smtp2go.com/contact/
- Live chat available during business hours

---

**Last Updated**: 2025-01-11
**SMTP2GO Plan**: Free (1,000 emails/month)
**Recommended For**: Development, staging, and small-scale production
