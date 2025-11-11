# Quick Start Testing Guide
**Complete Manual Testing After SMTP2GO Configuration**

---

## Prerequisites

Before you start testing:
- ✅ SMTP2GO account created and sender email verified
- ✅ SMTP credentials configured in `src/HRMS.API/appsettings.json`
- ✅ Backend builds successfully
- ✅ Frontend builds successfully

---

## Part 1: Start the Application (5 minutes)

### Step 1: Start PostgreSQL Database

```bash
# Check if PostgreSQL is running
sudo service postgresql status

# If not running, start it
sudo service postgresql start

# Verify connection
psql -h localhost -U postgres -d hrms_master -c "SELECT 1;"
```

---

### Step 2: Start Redis (Optional - for caching)

```bash
# Start Redis
sudo service redis-server start

# Verify Redis is running
redis-cli ping
# Expected output: PONG
```

---

### Step 3: Start Backend API

```bash
# Navigate to API directory
cd /workspaces/HRAPP/src/HRMS.API

# Run the application
dotnet run

# Expected output:
# Now listening on: http://localhost:5090
# Now listening on: https://localhost:7090
```

**Keep this terminal open!** The API needs to stay running.

---

### Step 4: Start Frontend (New Terminal)

Open a new terminal:

```bash
# Navigate to frontend directory
cd /workspaces/HRAPP/hrms-frontend

# Start development server
npm start

# Expected output:
# Application bundle generation complete.
# Watch mode enabled. Watching for file changes...
# ➜  Local:   http://localhost:4200/
```

**Keep this terminal open too!**

---

## Part 2: Email Configuration Testing (10 minutes)

### Test 1: Verify Email Configuration

```bash
# In a new terminal
cd /workspaces/HRAPP

# Check email configuration status
curl -X GET http://localhost:5090/api/admin/emailtest/config-status \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Expected Response:**
```json
{
  "smtpServer": "mail.smtp2go.com",
  "smtpPort": 2525,
  "fromEmail": "noreply@yourdomain.com",
  "enableSsl": true,
  "credentialsConfigured": true,
  "status": "Ready to send emails"
}
```

---

### Test 2: Send Test Email (Using Script)

```bash
# Run the automated testing script
./test-email-smtp2go.sh your-email@example.com

# Follow the prompts:
# 1. Enter SuperAdmin email
# 2. Enter SuperAdmin password
# 3. Script will send test email
# 4. Check your inbox!
```

---

### Test 3: Send Test Email (Manual cURL)

If you prefer manual testing:

```bash
# Step 1: Login as SuperAdmin
curl -X POST http://localhost:5090/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@morishr.com",
    "password": "YourSuperAdminPassword"
  }' | jq

# Copy the JWT token from the response

# Step 2: Send test email
curl -X POST http://localhost:5090/api/admin/emailtest/send-test \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }' | jq

# Expected response:
# {
#   "success": true,
#   "message": "Test email sent successfully to your-email@example.com"
# }
```

---

### Test 4: Test All Subscription Email Templates

```bash
curl -X POST http://localhost:5090/api/admin/emailtest/send-subscription-templates \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "toEmail": "your-email@example.com"
  }' | jq
```

**Check your inbox** - you should receive 4 emails:
1. ✉️ 30-Day Renewal Reminder
2. ✉️ 7-Day Expiring Warning
3. ✉️ Subscription Expired Notice
4. ✉️ Account Suspended Alert

---

## Part 3: Subscription System Testing (30 minutes)

### Test 5: Access SuperAdmin Dashboard

1. Open browser: http://localhost:4200
2. Login with SuperAdmin credentials
3. Navigate to: **Admin** → **Subscriptions**
4. You should see:
   - 6 analytics cards (ARR, MRR, Churn Rate, LTV, Active Subscriptions, Total Revenue)
   - 2 charts (Revenue Trend, Tier Distribution)
   - 4 tabs (Overdue Payments, Pending Payments, Upcoming Renewals, Suspended Tenants)

---

### Test 6: Create Test Tenant with Subscription

```bash
# Create a test tenant
curl -X POST http://localhost:5090/api/tenants \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Test Company Ltd",
    "subdomain": "testcompany",
    "adminEmail": "admin@testcompany.com",
    "adminFirstName": "Test",
    "adminLastName": "Admin",
    "subscriptionPlan": "Professional",
    "subscriptionStartDate": "2025-11-11T00:00:00Z",
    "subscriptionEndDate": "2025-12-11T00:00:00Z"
  }' | jq

# Response will include tenant ID - save it!
```

---

### Test 7: Verify Subscription Payment Auto-Created

```bash
# Get payments for the tenant (replace {tenantId} with actual ID)
curl -X GET http://localhost:5090/api/subscription-payments/tenant/{tenantId} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" | jq

# Expected: You should see a payment record with:
# - Status: "Pending"
# - Plan: "Professional"
# - Amount calculated with 15% VAT (Mauritius)
```

---

### Test 8: Check Database Directly

```bash
# Connect to database
psql -h localhost -U postgres -d hrms_master

# View all subscription payments
SELECT
    id,
    "TenantId",
    "SubscriptionPlan",
    "Amount",
    "Status",
    "DueDate",
    "CreatedAt"
FROM master."SubscriptionPayments"
ORDER BY "CreatedAt" DESC
LIMIT 10;

# Exit psql
\q
```

---

### Test 9: Access Hangfire Dashboard

1. Open browser: http://localhost:5090/hangfire
2. You should see Hangfire dashboard
3. Navigate to **Recurring Jobs**
4. Find job: `subscription-notifications`
5. Verify:
   - Schedule: `0 6 * * *` (daily at 6:00 AM)
   - Next execution time shown
   - Status: Scheduled

---

### Test 10: Manually Trigger Subscription Notification Job

```bash
# In Hangfire dashboard (http://localhost:5090/hangfire):
# 1. Go to "Recurring jobs"
# 2. Find "subscription-notifications"
# 3. Click "Trigger now"
# 4. Wait 30-60 seconds
# 5. Check application logs for email sending

# Or check logs in terminal where backend is running
# Look for:
# [Information] Processing subscription notifications...
# [Information] Email sent successfully to: [email]
```

---

### Test 11: Record Payment

```bash
# Get a pending payment ID from the dashboard or database

# Record payment
curl -X POST http://localhost:5090/api/subscription-payments/{paymentId}/record \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountPaid": 575.00,
    "paymentDate": "2025-11-11T10:00:00Z",
    "paymentMethod": "Bank Transfer",
    "transactionReference": "TEST-TXN-001",
    "notes": "Test payment recorded"
  }' | jq

# Expected response:
# {
#   "success": true,
#   "message": "Payment recorded successfully"
# }

# Check that tenant receives "Renewal Confirmation" email
```

---

### Test 12: Test Tier Upgrade with Pro-ration

```bash
# Upgrade tenant from Basic to Professional
curl -X PUT http://localhost:5090/api/tenants/{tenantId}/upgrade \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPlan": "Professional",
    "effectiveDate": "2025-11-11T00:00:00Z"
  }' | jq

# Verify pro-rated payment created
curl -X GET http://localhost:5090/api/subscription-payments/tenant/{tenantId} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" | jq

# Should see new payment with pro-rated amount
```

---

## Part 4: Frontend Dashboard Testing (15 minutes)

### Test 13: Verify Analytics Cards Display

1. Navigate to: http://localhost:4200/admin/subscriptions
2. Verify all 6 cards show data:
   - Annual Recurring Revenue (ARR)
   - Monthly Recurring Revenue (MRR)
   - Churn Rate
   - Customer Lifetime Value (LTV)
   - Active Subscriptions
   - Total Revenue

---

### Test 14: Verify Charts Render

1. Check **Revenue Trend Chart** (line chart):
   - X-axis: Months
   - Y-axis: Revenue
   - Line shows trend over time

2. Check **Tier Distribution Chart** (bar chart):
   - Shows count per subscription tier
   - Basic, Professional, Enterprise

---

### Test 15: Test Payment Tables

**Tab 1: Overdue Payments**
- Shows payments past due date
- Displays "Days Overdue" badge
- Action buttons: Record Payment, Send Reminder, View Details

**Tab 2: Pending Payments**
- Shows upcoming payments (not yet due)
- Displays "Days Until Due"
- Same action buttons

**Tab 3: Upcoming Renewals**
- Shows renewals in next 30 days
- Sorted by renewal date
- Quick actions available

**Tab 4: Suspended Tenants**
- Shows tenants suspended due to non-payment
- Displays suspension date
- Options to reactivate

---

### Test 16: Test Payment Detail Modal

1. Click "View Details" on any payment
2. Modal should show:
   - Tenant information
   - Payment details (amount, VAT, status)
   - Payment history
   - Timeline of events
   - Action buttons
3. Test quick actions:
   - Record payment
   - Send reminder
   - Close modal

---

### Test 17: Test Mobile Responsiveness

1. Open browser DevTools (F12)
2. Toggle device toolbar (mobile view)
3. Test on different screen sizes:
   - Mobile: 375px width
   - Tablet: 768px width
   - Desktop: 1920px width
4. Verify:
   - Cards stack vertically on mobile
   - Tables scroll horizontally
   - Charts resize properly
   - Buttons remain accessible

---

## Part 5: End-to-End Subscription Flow (20 minutes)

### Complete Flow Test

```bash
# 1. Create tenant
TENANT_RESPONSE=$(curl -s -X POST http://localhost:5090/api/tenants \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "E2E Test Company",
    "subdomain": "e2etest",
    "adminEmail": "admin@e2etest.com",
    "adminFirstName": "E2E",
    "adminLastName": "Test",
    "subscriptionPlan": "Basic",
    "subscriptionStartDate": "2025-11-11T00:00:00Z",
    "subscriptionEndDate": "2025-12-11T00:00:00Z"
  }')

TENANT_ID=$(echo $TENANT_RESPONSE | jq -r '.id')
echo "Created tenant: $TENANT_ID"

# 2. Verify payment auto-created
sleep 2
curl -X GET http://localhost:5090/api/subscription-payments/tenant/$TENANT_ID \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" | jq

# 3. View in dashboard
echo "Check dashboard: http://localhost:4200/admin/subscriptions"

# 4. Manually trigger notification job (in Hangfire)
echo "Trigger job: http://localhost:5090/hangfire"

# 5. Check email sent (check logs)
echo "Check backend logs for email sending"

# 6. Record payment
PAYMENT_ID=$(curl -s -X GET http://localhost:5090/api/subscription-payments/tenant/$TENANT_ID \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" | jq -r '.[0].id')

curl -X POST http://localhost:5090/api/subscription-payments/$PAYMENT_ID/record \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountPaid": 287.50,
    "paymentDate": "2025-11-11T10:00:00Z",
    "paymentMethod": "Credit Card",
    "transactionReference": "E2E-TEST-001"
  }' | jq

# 7. Verify renewal confirmation email sent
echo "Check inbox for renewal confirmation email"

# 8. Refresh dashboard and verify payment marked as paid
echo "Refresh dashboard and verify payment status updated"
```

---

## Part 6: Performance & Load Testing (Optional)

### Test 18: Revenue Analytics Performance

```bash
# Test with cache
time curl -X GET http://localhost:5090/api/subscription-payments/revenue-analytics \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" | jq

# Should complete in < 500ms (with cache)
```

---

### Test 19: Test Notification Deduplication

```bash
# Manually trigger notification job multiple times rapidly
# Verify in database that duplicate emails are NOT sent

psql -h localhost -U postgres -d hrms_master -c \
  "SELECT \"TenantId\", \"NotificationType\", COUNT(*)
   FROM master.\"SubscriptionNotificationLogs\"
   GROUP BY \"TenantId\", \"NotificationType\"
   HAVING COUNT(*) > 1;"

# Should return no results (no duplicates)
```

---

## Part 7: Verification Checklist

### Backend Verification

- [ ] Backend builds without errors
- [ ] Database migrations applied
- [ ] Email configuration validated
- [ ] Test email sent successfully
- [ ] All 4 subscription templates sent
- [ ] Hangfire dashboard accessible
- [ ] Background job scheduled correctly
- [ ] API endpoints respond (11 endpoints)
- [ ] Audit logs created for all actions

---

### Frontend Verification

- [ ] Frontend builds without errors
- [ ] Can access subscription dashboard
- [ ] All 6 analytics cards display
- [ ] Both charts render correctly
- [ ] All 4 payment tables work
- [ ] Payment detail modal opens
- [ ] Quick actions work (record, remind)
- [ ] Mobile responsive design works
- [ ] Dark mode supported (if applicable)

---

### Email Verification

- [ ] SMTP2GO account created
- [ ] Sender email verified
- [ ] SMTP credentials configured
- [ ] Test email received in inbox
- [ ] All subscription templates received
- [ ] Emails not in spam folder
- [ ] Email formatting looks professional
- [ ] Unsubscribe links work (if applicable)

---

### Subscription Flow Verification

- [ ] Tenant creation auto-creates payment
- [ ] Payment amount calculated with VAT (15%)
- [ ] Pro-rated upgrades calculate correctly
- [ ] Payment recording updates status
- [ ] Renewal confirmation email sent
- [ ] Notification job runs successfully
- [ ] Email deduplication works
- [ ] Grace period system works
- [ ] Auto-suspension after grace period
- [ ] Dashboard reflects real-time data

---

## Troubleshooting Common Issues

### Issue: Backend won't start

**Error:** `Unable to connect to database`

**Solution:**
```bash
# Start PostgreSQL
sudo service postgresql start

# Verify connection
psql -h localhost -U postgres -d hrms_master -c "SELECT 1;"
```

---

### Issue: Email sending fails

**Error:** `SMTP authentication failed`

**Solution:**
1. Verify credentials in `appsettings.json`
2. Check sender email is verified in SMTP2GO
3. Generate new SMTP password in SMTP2GO
4. Update `SmtpUsername` and `SmtpPassword`
5. Restart backend

---

### Issue: Frontend can't connect to backend

**Error:** `CORS policy error`

**Solution:**
```bash
# Ensure backend is running on port 5090
# Ensure frontend is running on port 4200
# Check CORS settings in appsettings.json allow http://localhost:4200
```

---

### Issue: Hangfire dashboard shows no jobs

**Solution:**
```bash
# Jobs are registered at startup
# Restart backend:
# Ctrl+C in backend terminal
cd src/HRMS.API
dotnet run
```

---

### Issue: Dashboard shows no data

**Solution:**
1. Create test tenants and payments
2. Wait for cache refresh (5 minutes)
3. Or restart backend to clear cache
4. Check browser console for errors (F12)

---

## Next Steps After Successful Testing

Once all tests pass:

1. **Document any issues found** and create bug tickets
2. **Set up production environment**
   - See: `PRODUCTION_DEPLOYMENT.md`
3. **Configure production email provider**
   - See: `docs/SMTP2GO_SETUP.md`
4. **Deploy to staging first** for UAT
5. **Run full regression testing** in staging
6. **Deploy to production** when ready
7. **Monitor email delivery** in SMTP2GO dashboard
8. **Set up alerts** for payment failures

---

## Quick Reference Commands

```bash
# Start everything
sudo service postgresql start
sudo service redis-server start
cd src/HRMS.API && dotnet run &
cd hrms-frontend && npm start &

# Run email test
./test-email-smtp2go.sh your-email@example.com

# Check logs
tail -f src/HRMS.API/Logs/hrms-*.log

# Access dashboards
# Frontend: http://localhost:4200
# Hangfire: http://localhost:5090/hangfire
# Swagger: http://localhost:5090/swagger
```

---

## Support & Documentation

**Guides:**
- SMTP2GO Setup: `docs/SMTP2GO_SETUP.md`
- Production Deployment: `PRODUCTION_DEPLOYMENT.md`
- Email Infrastructure: `docs/EMAIL_INFRASTRUCTURE_SUMMARY.md`
- Testing Checklist: `SUBSCRIPTION_TESTING_CHECKLIST.md`

**Dashboards:**
- Application: http://localhost:4200
- API Swagger: http://localhost:5090/swagger
- Hangfire: http://localhost:5090/hangfire
- SMTP2GO: https://app.smtp2go.com

---

**Last Updated:** 2025-11-11
**Ready for Testing:** ✅ Yes
**Estimated Time:** 1-2 hours for complete testing
