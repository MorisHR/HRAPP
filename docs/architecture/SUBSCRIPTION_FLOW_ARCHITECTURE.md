# Fortune 500 Yearly Subscription Flow - Architecture & Implementation Plan

**Design Pattern:** Salesforce / HubSpot / Zendesk Manual Yearly Billing
**Status:** 🟡 **PARTIALLY IMPLEMENTED** - Entities ready, services/jobs/migration pending

---

## 🎯 Business Requirements

### Subscription Model
- **Billing Cycle:** YEARLY (not monthly) - reduces churn, Fortune 500 standard
- **Currency:** Mauritian Rupees (MUR)
- **Payment Method:** MANUAL - SuperAdmin marks payments as received
- **Tiers:** Employee-based (1-50, 51-200, 201-500, 501-1000, 1000+)
- **Grace Period:** 14 days after expiry before suspension

### Notification Timeline (Multi-Stage)
1. **30 days before expiry** - First reminder
2. **15 days before expiry** - Second reminder
3. **7 days before expiry** - Urgent reminder
4. **3 days before expiry** - Critical warning
5. **1 day before expiry** - Final warning
6. **Expiry day** - Grace period starts (14 days)
7. **Day 1-7 after expiry** - Grace period warning
8. **Day 8-14 after expiry** - Critical warning
9. **Day 15 after expiry** - Auto-suspension

### Fortune 500 Features
- ✅ **Email Deduplication** - Never send same notification twice
- ✅ **Complete Audit Trail** - All payments logged immutably
- ✅ **Notification History** - Track every email sent
- ✅ **Follow-up Tracking** - SuperAdmin can mark follow-ups completed
- ✅ **Multi-Stage Reminders** - Progressive urgency
- ✅ **Graceful Degradation** - System works even if email fails

---

## 📊 Database Schema (Already Configured)

### Tenant Entity Fields (READY ✅)
```csharp
// Subscription tracking
public decimal YearlyPriceMUR { get; set; }  // ✅ YEARLY (not monthly)
public DateTime SubscriptionStartDate { get; set; }
public DateTime? SubscriptionEndDate { get; set; }
public DateTime? TrialEndDate { get; set; }

// Notification deduplication (Stripe/Chargebee pattern)
public DateTime? LastNotificationSent { get; set; }
public SubscriptionNotificationType? LastNotificationType { get; set; }

// Grace period tracking
public DateTime? GracePeriodStartDate { get; set; }
public int GracePeriodDays { get; set; } = 30; // Can be 14 for production

// Navigation properties
public virtual ICollection<SubscriptionPayment> SubscriptionPayments { get; set; }
public virtual ICollection<SubscriptionNotificationLog> SubscriptionNotificationLogs { get; set; }
```

### SubscriptionPayment Entity (READY ✅)
```csharp
public Guid TenantId { get; set; }
public DateTime PeriodStartDate { get; set; }       // Subscription year start
public DateTime PeriodEndDate { get; set; }         // Subscription year end (+365 days)
public decimal AmountMUR { get; set; }              // Amount in MUR
public SubscriptionPaymentStatus Status { get; set; } // Pending, Paid, Overdue, etc.
public DateTime? PaidDate { get; set; }             // When SuperAdmin marked as paid
public string? ProcessedBy { get; set; }            // SuperAdmin who processed
public string? PaymentReference { get; set; }       // Invoice/receipt number
public string? PaymentMethod { get; set; }          // Bank Transfer, Cash, Cheque
public DateTime DueDate { get; set; }               // Payment due date
public EmployeeTier EmployeeTier { get; set; }      // Tier at time of payment

// Computed properties
public bool IsOverdue => Status != Paid && DueDate < DateTime.UtcNow;
public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;
public bool IsInGracePeriod => IsOverdue && DaysUntilDue >= -14;
```

### SubscriptionNotificationLog Entity (READY ✅)
```csharp
public Guid TenantId { get; set; }
public SubscriptionNotificationType NotificationType { get; set; }
public DateTime SentDate { get; set; }
public string RecipientEmail { get; set; }
public string EmailSubject { get; set; }
public bool DeliverySuccess { get; set; }
public string? DeliveryError { get; set; }
public DateTime SubscriptionEndDateAtNotification { get; set; }
public int DaysUntilExpiryAtNotification { get; set; }
public Guid? SubscriptionPaymentId { get; set; }
public bool RequiresFollowUp { get; set; }
public DateTime? FollowUpCompletedDate { get; set; }
public string? FollowUpNotes { get; set; }
```

### Enums (READY ✅)

**SubscriptionPaymentStatus:**
- Pending = 0
- Paid = 1
- Overdue = 2
- Failed = 3
- Refunded = 4
- PartiallyPaid = 5
- Waived = 6

**SubscriptionNotificationType:**
- Reminder30Days = 0
- Reminder15Days = 1
- Reminder7Days = 2
- Reminder3Days = 3
- Reminder1Day = 4
- ExpiryNotification = 5
- GracePeriodWarning = 6
- CriticalWarning = 7
- FinalWarning = 8
- SuspensionNotification = 9
- RenewalConfirmation = 10
- AdminPaymentReminder = 11

---

## 🔧 What Needs to Be Fixed (Inconsistencies)

### 1. CreateTenantRequest DTO ❌
**Current (WRONG):**
```csharp
public decimal MonthlyPrice { get; set; }  // ❌ OLD MONTHLY PATTERN
```

**Should Be (CORRECT):**
```csharp
public decimal YearlyPriceMUR { get; set; }  // ✅ FORTUNE 500 YEARLY PATTERN
```

### 2. TenantManagementService.cs ❌
**Lines to Fix:**
- Line 92: `MonthlyPrice = request.MonthlyPrice` → should be `YearlyPriceMUR`
- Line 340: `tenant.MonthlyPrice = monthlyPrice` → should be `YearlyPriceMUR = yearlyPrice`
- Line 368: `MonthlyPrice = tenant.MonthlyPrice` → should be `YearlyPriceMUR`

**Method signature to fix:**
```csharp
// CURRENT (WRONG)
public async Task<(bool, string)> UpdateEmployeeTierAsync(
    ...,
    decimal monthlyPrice,  // ❌ WRONG
    ...)

// SHOULD BE (CORRECT)
public async Task<(bool, string)> UpdateEmployeeTierAsync(
    ...,
    decimal yearlyPriceMUR,  // ✅ CORRECT
    ...)
```

### 3. TenantDto (need to check)
Should have `YearlyPriceMUR` not `MonthlyPrice`

### 4. TenantsController.cs (need to check)
UpdateEmployeeTier endpoint should pass `yearlyPriceMUR`

---

## 🏗️ What Needs to Be Built

### 1. Database Migration ⏳
**File:** `src/HRMS.Infrastructure/Data/Migrations/Master/YYYYMMDD_AddSubscriptionManagement.cs`

**What it creates:**
- `master.SubscriptionPayments` table
- `master.SubscriptionNotificationLogs` table
- Indexes for performance
- Foreign key relationships
- Add subscription fields to Tenant table (if not exists)

**Command:**
```bash
cd src/HRMS.API
dotnet ef migrations add AddSubscriptionManagement \
  --project ../HRMS.Infrastructure \
  --context MasterDbContext \
  --output-dir Data/Migrations/Master
```

### 2. Subscription Management Service Interface ⏳
**File:** `src/HRMS.Application/Interfaces/ISubscriptionManagementService.cs`

```csharp
public interface ISubscriptionManagementService
{
    // Payment Management
    Task<SubscriptionPayment> CreatePaymentRecordAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        decimal amountMUR,
        DateTime dueDate);

    Task<bool> MarkPaymentAsPaidAsync(
        Guid paymentId,
        string processedBy,
        string paymentReference,
        string paymentMethod);

    Task<List<SubscriptionPayment>> GetOverduePaymentsAsync();
    Task<List<SubscriptionPayment>> GetUpcomingPaymentsAsync(int daysAhead = 30);

    // Notification Management
    Task<bool> ShouldSendNotificationAsync(
        Guid tenantId,
        SubscriptionNotificationType notificationType);

    Task LogNotificationSentAsync(
        Guid tenantId,
        SubscriptionNotificationType notificationType,
        string recipientEmail,
        string subject,
        bool success,
        string? error = null);

    // Subscription Lifecycle
    Task<List<Tenant>> GetTenantsNeedingRenewalNotificationAsync(int daysUntilExpiry);
    Task<List<Tenant>> GetExpiredTenantsInGracePeriodAsync();
    Task<List<Tenant>> GetTenantsToSuspendAsync(); // Grace period ended
    Task RenewSubscriptionAsync(Guid tenantId, int years = 1);
}
```

### 3. Subscription Management Service Implementation ⏳
**File:** `src/HRMS.Infrastructure/Services/SubscriptionManagementService.cs`

**Key Methods:**
- `CreatePaymentRecordAsync()` - Create payment when subscription starts/renews
- `MarkPaymentAsPaidAsync()` - SuperAdmin marks payment as received
- `ShouldSendNotificationAsync()` - Check if notification was already sent (deduplication)
- `LogNotificationSentAsync()` - Record notification in audit log
- `GetTenantsNeedingRenewalNotificationAsync()` - Find tenants X days before expiry
- `RenewSubscriptionAsync()` - Extend subscription by 1 year

### 4. Subscription Notification Background Job ⏳
**File:** `src/HRMS.BackgroundJobs/Jobs/SubscriptionNotificationJob.cs`

**Schedule:** Runs daily at 9:00 AM
**Logic:**
```csharp
public async Task Execute()
{
    // 1. Check tenants expiring in 30 days
    var tenants30d = await _service.GetTenantsNeedingRenewalNotificationAsync(30);
    foreach (var tenant in tenants30d)
    {
        if (await _service.ShouldSendNotificationAsync(tenant.Id, Reminder30Days))
        {
            await SendReminderEmail(tenant, Reminder30Days);
            await _service.LogNotificationSentAsync(...);
        }
    }

    // 2. Repeat for 15d, 7d, 3d, 1d
    // 3. Check expired tenants in grace period
    // 4. Auto-suspend tenants after grace period
    // 5. Send admin reminders for overdue payments
}
```

### 5. Subscription Payment Controller ⏳
**File:** `src/HRMS.API/Controllers/SubscriptionPaymentController.cs`

**Endpoints:**
```csharp
[HttpGet("payments")]  // Get all payments (filtered by status, tenant, date)
[HttpGet("payments/{id}")]  // Get payment details
[HttpPost("payments")]  // Create payment record (when tenant created/renewed)
[HttpPut("payments/{id}/mark-paid")]  // SuperAdmin marks payment as paid
[HttpPut("payments/{id}/mark-overdue")]  // Mark payment as overdue
[HttpGet("payments/overdue")]  // Get overdue payments dashboard
[HttpGet("notifications/{tenantId}")]  // Get notification history for tenant
```

### 6. Email Templates ⏳
**Files:**
- `EmailTemplates/SubscriptionReminder30Days.html`
- `EmailTemplates/SubscriptionReminder15Days.html`
- `EmailTemplates/SubscriptionReminder7Days.html`
- `EmailTemplates/SubscriptionExpiryNotice.html`
- `EmailTemplates/GracePeriodWarning.html`
- `EmailTemplates/SuspensionNotice.html`
- `EmailTemplates/RenewalConfirmation.html`

### 7. Service Registration ⏳
**File:** `src/HRMS.API/Program.cs`

```csharp
builder.Services.AddScoped<ISubscriptionManagementService, SubscriptionManagementService>();

// Register background job
RecurringJob.AddOrUpdate<SubscriptionNotificationJob>(
    "subscription-notifications",
    job => job.Execute(),
    "0 9 * * *"  // Daily at 9:00 AM
);
```

---

## 🔄 Complete Flow Example

### Scenario: New Tenant Created

1. **SuperAdmin creates tenant** via TenantsController.CreateTenant()
   - Tenant created with `YearlyPriceMUR = 15000` (example)
   - `SubscriptionStartDate = Today`
   - `SubscriptionEndDate = Today + 365 days`

2. **System creates payment record** automatically
   ```csharp
   await _subscriptionService.CreatePaymentRecordAsync(
       tenantId: newTenant.Id,
       periodStart: DateTime.UtcNow,
       periodEnd: DateTime.UtcNow.AddYears(1),
       amountMUR: 15000,
       dueDate: DateTime.UtcNow.AddDays(30)  // 30 days to pay
   );
   ```
   - Status: `Pending`

3. **Tenant receives activation email** (existing flow)

4. **After 30 days, if not paid:**
   - Background job marks payment as `Overdue`
   - Sends overdue notice to tenant
   - Sends reminder to SuperAdmin

5. **SuperAdmin receives bank transfer:**
   - Goes to `/api/subscription-payments/{id}/mark-paid`
   - System updates:
     - Status: `Paid`
     - PaidDate: `DateTime.UtcNow`
     - ProcessedBy: `admin@hrms.com`
     - PaymentReference: `INV-2025-001`
   - Sends confirmation email to tenant

6. **335 days later (30 days before expiry):**
   - Background job detects expiry in 30 days
   - Checks: `ShouldSendNotificationAsync(tenantId, Reminder30Days)` → true
   - Sends reminder email
   - Logs notification in `SubscriptionNotificationLogs`
   - Updates `Tenant.LastNotificationType = Reminder30Days`

7. **Repeat at 15d, 7d, 3d, 1d intervals**

8. **On expiry day:**
   - Send expiry notification
   - Set `GracePeriodStartDate = Today`
   - Tenant can still use system (grace period)

9. **14 days after expiry:**
   - Background job auto-suspends tenant
   - Status: `Suspended`
   - Sends suspension notice
   - SuperAdmin notified

10. **SuperAdmin manually renews:**
    - Calls `/api/subscription-payments` to create new payment
    - Or extends expiry date manually

---

## ✅ Implementation Checklist

### Phase 1: Fix Inconsistencies (IMMEDIATE)
- [ ] Update `CreateTenantRequest.cs` - change `MonthlyPrice` to `YearlyPriceMUR`
- [ ] Update `TenantDto.cs` - change `MonthlyPrice` to `YearlyPriceMUR`
- [ ] Update `TenantManagementService.cs` - fix all `MonthlyPrice` references
- [ ] Update `UpdateEmployeeTierAsync()` signature - change `monthlyPrice` to `yearlyPriceMUR`
- [ ] Update `TenantsController.cs` - fix UpdateEmployeeTier endpoint
- [ ] Update frontend DTOs if needed

### Phase 2: Database Migration
- [ ] Build project successfully (fix above errors first)
- [ ] Create migration: `dotnet ef migrations add AddSubscriptionManagement`
- [ ] Review generated migration SQL
- [ ] Apply migration: `dotnet ef database update`
- [ ] Verify tables created in PostgreSQL

### Phase 3: Build Services
- [ ] Create `ISubscriptionManagementService` interface
- [ ] Implement `SubscriptionManagementService`
- [ ] Create `SubscriptionNotificationJob` background job
- [ ] Create email templates
- [ ] Register services in `Program.cs`

### Phase 4: Build API
- [ ] Create `SubscriptionPaymentController`
- [ ] Implement all endpoints (CRUD + mark-paid)
- [ ] Add SuperAdmin authorization
- [ ] Add audit logging for all subscription actions
- [ ] Test endpoints with Postman

### Phase 5: Testing
- [ ] Unit tests for SubscriptionManagementService
- [ ] Integration tests for background job
- [ ] Test email deduplication
- [ ] Test grace period logic
- [ ] Test auto-suspension
- [ ] End-to-end manual test

---

## 📈 Success Metrics

When complete, the system will have:
- ✅ **100% Email Deduplication** - No duplicate notifications ever
- ✅ **Complete Audit Trail** - Every payment logged immutably
- ✅ **Proactive Notifications** - 9-stage reminder system
- ✅ **Graceful Degradation** - Works even if emails fail
- ✅ **SuperAdmin Control** - Manual payment approval workflow
- ✅ **Fortune 500 Grade** - Matches Salesforce/HubSpot patterns

---

**Next Steps:** Fix the inconsistencies (Phase 1), then proceed with migration and implementation.
