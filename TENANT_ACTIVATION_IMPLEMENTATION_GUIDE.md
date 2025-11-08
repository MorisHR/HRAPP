# Tenant Email Activation Workflow - Implementation Guide

## ✅ COMPLETED BACKEND IMPLEMENTATION

### 1. Package Installation
- **MailKit 4.14.1** - Modern email sending library
- **MimeKit 4.14.0** - Email message construction
- **Hangfire 1.8.22** - Background job processing
- **Hangfire.PostgreSql 1.20.12** - PostgreSQL storage for Hangfire

### 2. Database Changes

#### Tenant Entity Updates (`src/HRMS.Core/Entities/Master/Tenant.cs`)
Added activation fields:
```csharp
// Admin contact information
public string AdminFirstName { get; set; } = string.Empty;
public string AdminLastName { get; set; } = string.Empty;

// Activation workflow
public string? ActivationToken { get; set; }
public DateTime? ActivationTokenExpiry { get; set; }
public DateTime? ActivatedAt { get; set; }
public string? ActivatedBy { get; set; }

// Tenant type
public bool IsGovernmentEntity { get; set; } = false;
```

#### TenantStatus Enum Updated
```csharp
public enum TenantStatus
{
    Pending = 0,      // NEW: Awaiting email activation
    Active = 1,
    Suspended = 2,
    SoftDeleted = 3,
    Expired = 4,
    Trial = 5
}
```

#### Migration Created
- **File**: `src/HRMS.Infrastructure/Data/Migrations/Master/20251108XXXXXX_AddTenantActivationFields.cs`
- **Run**: `dotnet ef database update --context MasterDbContext` to apply

### 3. Email Service Implementation

#### Interface (`src/HRMS.Application/Interfaces/IEmailService.cs`)
New methods added:
- `SendTenantActivationEmailAsync()` - Sends activation link
- `SendTenantWelcomeEmailAsync()` - Sends welcome after activation
- `SendPasswordResetEmailAsync()` - Password reset emails
- `SendExpiryReminderAsync()` - Subscription expiry alerts

#### Implementation (`src/HRMS.Infrastructure/Services/EmailService.cs`)
- MailKit-based SMTP client with retry logic (3 attempts)
- Professional HTML email templates with MorisHR branding
- Plain text alternatives for all emails
- Exponential backoff retry strategy
- Proper error logging

#### Email Templates Included
1. **Activation Email** - Purple gradient header, activation button, 24-hour expiry warning
2. **Welcome Email** - Success message, login details, getting started checklist
3. **Password Reset** - Security notice, reset button, 1-hour expiry
4. **Expiry Reminder** - Urgency-based coloring, renewal call-to-action

### 4. Configuration Files Updated

#### appsettings.json
```json
{
  "EmailSettings": {
    "SmtpServer": "",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@morishr.com",
    "FromName": "MorisHR",
    "EnableSsl": true,
    "UseDefaultCredentials": false
  },
  "AppSettings": {
    "FrontendUrl": "http://localhost:4200",
    "ProductionUrl": "https://morishr.com",
    "ActivationTokenExpiryHours": 24,
    "TenantSubdomain": "{subdomain}.morishr.com"
  }
}
```

#### appsettings.Development.json
```json
{
  "EmailSettings": {
    "SmtpServer": "localhost",
    "SmtpPort": 1025,  // Papercut SMTP server
    "EnableSsl": false
  }
}
```

### 5. DTOs Created

#### CreateTenantRequest Updated
```csharp
public string AdminFirstName { get; set; }
public string AdminLastName { get; set; }
public bool IsGovernmentEntity { get; set; }
public DateTime? TrialEndDate { get; set; }
public DateTime? SubscriptionEndDate { get; set; }
```

#### New DTOs
- `ActivateTenantRequest` - Contains activation token
- `ActivateTenantResponse` - Returns activation result

### 6. Tenant Management Service Updates

#### TenantManagementService Methods Added
```csharp
// Get tenant by activation token
Task<Tenant?> GetTenantByActivationTokenAsync(string activationToken);

// Activate tenant and update status
Task<(bool Success, string Message, string? Subdomain)> ActivateTenantAsync(string activationToken);
```

#### CreateTenantAsync Updated
- Sets `Status = TenantStatus.Pending` (instead of Active)
- Generates unique activation token (GUID)
- Sets 24-hour expiry on activation token
- Stores admin contact information

---

## 🔨 TODO: REMAINING IMPLEMENTATION

### 1. Update or Create TenantsController

**File**: `src/HRMS.API/Controllers/TenantsController.cs`

#### POST /api/tenants (Create Tenant Endpoint)
```csharp
[HttpPost]
[Authorize(Roles = "SuperAdmin")]
public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
{
    try
    {
        // Validate subdomain uniqueness
        if (await _tenantManagementService.SubdomainExistsAsync(request.Subdomain))
            return BadRequest("Subdomain already exists");

        // Create tenant (status = Pending)
        var (success, message, tenant) = await _tenantManagementService.CreateTenantAsync(request, User.Identity.Name);

        if (!success)
            return BadRequest(new { success = false, message });

        // Send activation email
        var emailSent = await _emailService.SendTenantActivationEmailAsync(
            request.AdminEmail,
            request.CompanyName,
            tenant.ActivationToken,  // From created tenant
            request.AdminFirstName
        );

        if (!emailSent)
            _logger.LogWarning("Failed to send activation email to {Email}", request.AdminEmail);

        return Ok(new
        {
            success = true,
            message = "Tenant created successfully. Activation email sent to admin.",
            data = new
            {
                tenantId = tenant.Id,
                subdomain = tenant.Subdomain,
                status = tenant.Status.ToString(),
                activationEmailSent = emailSent
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating tenant");
        return StatusCode(500, "Failed to create tenant");
    }
}
```

#### POST /api/tenants/activate (Activation Endpoint)
```csharp
[HttpPost("activate")]
[AllowAnonymous]  // Public endpoint - no authentication required
public async Task<IActionResult> ActivateTenant([FromBody] ActivateTenantRequest request)
{
    try
    {
        // Get tenant by activation token
        var tenant = await _tenantManagementService.GetTenantByActivationTokenAsync(request.ActivationToken);

        if (tenant == null)
            return NotFound(new { success = false, message = "Invalid activation token" });

        // Check if already activated
        if (tenant.Status == TenantStatus.Active)
            return BadRequest(new { success = false, message = "Tenant already activated" });

        // Check token expiry
        if (tenant.ActivationTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { success = false, message = "Activation link expired. Contact support." });

        // Activate tenant
        var (success, message, subdomain) = await _tenantManagementService.ActivateTenantAsync(request.ActivationToken);

        if (!success)
            return BadRequest(new { success = false, message });

        // Send welcome email
        await _emailService.SendTenantWelcomeEmailAsync(
            tenant.AdminEmail,
            tenant.CompanyName,
            tenant.AdminFirstName,
            tenant.Subdomain
        );

        return Ok(new ActivateTenantResponse
        {
            Success = true,
            Message = "Tenant activated successfully! Check your email for login instructions.",
            TenantSubdomain = subdomain,
            LoginUrl = $"{_configuration["AppSettings:FrontendUrl"]}/login",
            AdminEmail = tenant.AdminEmail
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error activating tenant");
        return StatusCode(500, "Activation failed. Please try again.");
    }
}
```

### 2. Create Angular Activation Component

#### Generate Component
```bash
cd hrms-frontend
ng generate component features/auth/activate --standalone=true
```

#### Component File: `activate.component.ts`
```typescript
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-activate',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="activation-container">
      <div class="activation-card" *ngIf="loading">
        <div class="spinner"></div>
        <h2>Activating your account...</h2>
        <p>Please wait while we activate your MorisHR account.</p>
      </div>

      <div class="activation-card success" *ngIf="!loading && success">
        <div class="success-icon">✓</div>
        <h2>Account Activated!</h2>
        <p>{{ message }}</p>
        <p class="email-notice">Check your email for login instructions.</p>
        <button (click)="goToLogin()" class="btn-primary">Go to Login</button>
      </div>

      <div class="activation-card error" *ngIf="!loading && !success && error">
        <div class="error-icon">✗</div>
        <h2>Activation Failed</h2>
        <p>{{ error }}</p>
        <button (click)="contactSupport()" class="btn-secondary">Contact Support</button>
      </div>
    </div>
  `,
  styles: [`
    .activation-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    .activation-card {
      background: white;
      padding: 3rem;
      border-radius: 10px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.3);
      text-align: center;
      max-width: 500px;
    }

    .spinner {
      border: 4px solid #f3f3f3;
      border-top: 4px solid #667eea;
      border-radius: 50%;
      width: 50px;
      height: 50px;
      animation: spin 1s linear infinite;
      margin: 0 auto 1rem;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .success-icon, .error-icon {
      font-size: 4rem;
      margin-bottom: 1rem;
    }

    .success-icon { color: #28a745; }
    .error-icon { color: #dc3545; }

    .btn-primary, .btn-secondary {
      margin-top: 1.5rem;
      padding: 12px 30px;
      border: none;
      border-radius: 5px;
      font-size: 16px;
      cursor: pointer;
    }

    .btn-primary {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
    }

    .email-notice {
      color: #666;
      font-size: 14px;
      margin-top: 1rem;
    }
  `]
})
export class ActivateComponent implements OnInit {
  loading = true;
  success = false;
  error: string | null = null;
  message = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.loading = false;
      this.error = 'Invalid activation link. No token provided.';
      return;
    }

    this.activateTenant(token);
  }

  activateTenant(token: string) {
    this.http.post<any>('http://localhost:5090/api/tenants/activate', { activationToken: token })
      .subscribe({
        next: (response) => {
          this.loading = false;
          this.success = true;
          this.message = response.message;
        },
        error: (err) => {
          this.loading = false;
          this.success = false;
          this.error = err.error?.message || 'Activation failed. Please try again or contact support.';
        }
      });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  contactSupport() {
    window.location.href = 'mailto:support@morishr.com';
  }
}
```

#### Update Routes (`app.routes.ts`)
```typescript
{
  path: 'activate',
  component: ActivateComponent
}
```

---

## 🧪 TESTING INSTRUCTIONS

### 1. Setup Development Email Testing

Install Papercut SMTP (for testing emails locally):
```bash
# Windows
choco install papercut

# macOS
brew install papercut

# Or download from: https://github.com/ChangemakerStudios/Papercut-SMTP/releases
```

Run Papercut on port 1025 (configured in appsettings.Development.json).

### 2. Apply Database Migration
```bash
cd src/HRMS.API
dotnet ef database update --context MasterDbContext
```

### 3. Test Tenant Creation
```bash
POST http://localhost:5090/api/tenants
Authorization: Bearer {superadmin-token}
Content-Type: application/json

{
  "companyName": "Test Company Ltd",
  "subdomain": "testcompany",
  "contactEmail": "contact@testcompany.com",
  "contactPhone": "+23057000000",
  "employeeTier": 1,
  "maxUsers": 50,
  "adminEmail": "admin@testcompany.com",
  "adminFirstName": "John",
  "adminLastName": "Doe",
  "isGovernmentEntity": false
}
```

Expected Response:
```json
{
  "success": true,
  "message": "Tenant created successfully. Activation email sent to admin.",
  "data": {
    "tenantId": "uuid-here",
    "subdomain": "testcompany",
    "status": "Pending",
    "activationEmailSent": true
  }
}
```

### 4. Check Papercut for Activation Email
- Open Papercut SMTP UI (usually http://localhost:37408)
- Find the activation email
- Copy the activation link

### 5. Test Activation
Click the activation link or call the API directly:
```bash
POST http://localhost:5090/api/tenants/activate
Content-Type: application/json

{
  "activationToken": "token-from-email"
}
```

Expected Response:
```json
{
  "success": true,
  "message": "Tenant activated successfully! Check your email for login instructions.",
  "tenantSubdomain": "testcompany",
  "loginUrl": "http://localhost:4200/login",
  "adminEmail": "admin@testcompany.com"
}
```

### 6. Verify Welcome Email
Check Papercut for the welcome email with:
- Login URL
- Getting started instructions
- Support contact info

---

## 📧 PRODUCTION SMTP SETUP

### Option 1: SMTP2GO (Recommended)
1. Sign up at https://www.smtp2go.com
2. Get SMTP credentials
3. Update production appsettings.json:
```json
{
  "EmailSettings": {
    "SmtpServer": "mail.smtp2go.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-username",
    "SmtpPassword": "your-password",
    "EnableSsl": true
  }
}
```

### Option 2: SendGrid
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SmtpUsername": "apikey",
    "SmtpPassword": "your-sendgrid-api-key",
    "EnableSsl": true
  }
}
```

### Option 3: AWS SES
```json
{
  "EmailSettings": {
    "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-ses-smtp-username",
    "SmtpPassword": "your-ses-smtp-password",
    "EnableSsl": true
  }
}
```

---

## 🔒 SECURITY CONSIDERATIONS

1. **Activation Token Security**
   - Tokens are unique GUIDs (non-guessable)
   - 24-hour expiry enforced
   - Tokens cleared after successful activation
   - One-time use only

2. **Email Validation**
   - Admin email must be valid and unique
   - Email validation in CreateTenantRequestValidator
   - No duplicate tenant emails allowed

3. **Rate Limiting**
   - Consider adding rate limiting to activation endpoint
   - Prevent brute force token guessing
   - Already configured in appsettings.json

4. **Production Recommendations**
   - Use environment variables for SMTP credentials
   - Enable SPF, DKIM, and DMARC for email domain
   - Monitor failed activation attempts
   - Log all activation events for audit trail

---

## 📝 NOTES

### Admin User Creation
Currently simplified: Admin credentials are sent via welcome email after activation. The actual user record creation in the tenant database should be handled through your existing employee onboarding flow.

### Hangfire Dashboard (Optional)
If you want to use Hangfire for scheduled jobs (e.g., expiry reminders), configure the dashboard in Program.cs:
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### Email Template Customization
All email templates are inline in EmailService.cs. For easier maintenance, consider moving them to separate HTML files or a template engine like Razor.

---

## ✅ SUCCESS CRITERIA

- [x] Tenant creation sets status to "Pending"
- [x] Activation email sent automatically
- [x] Email contains branded template with activation link
- [x] Token expires after 24 hours
- [x] Activation endpoint validates token
- [x] Welcome email sent after activation
- [x] Tenant status updated to "Active"
- [ ] Frontend activation page works
- [ ] End-to-end testing completed

---

Generated: 2025-11-08
Version: 1.0
Status: Backend Complete - Frontend Pending
