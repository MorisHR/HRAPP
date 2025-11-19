# FORTUNE 500 SECURITY FIXES - IMPLEMENTATION GUIDE

## Overview
This document outlines all critical security fixes needed to bring the HRMS system
to production-ready status (8.5/10 security score).

## CRITICAL FIXES (Week 1)

### FIX #1: TOTP Verification Window (IMMEDIATE - 30 min)
**File:** `/src/HRMS.Infrastructure/Services/MfaService.cs`
**Line:** ~103-106
**Change:** `previous: 480` → `previous: 1` and `future: 480` → `future: 1`
**Impact:** Reduces MFA vulnerability window from 8 hours to 90 seconds

### FIX #2: Remove Hardcoded Secret Path (IMMEDIATE - 1 hour)
**Files:**
- `/src/HRMS.API/Controllers/AuthController.cs` - Remove hardcoded endpoint
- `/src/HRMS.API/Program.cs` - Read SUPERADMIN_SECRET_PATH from environment

**Current (INSECURE):**
```csharp
[HttpPost("system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d")]
public async Task<IActionResult> SecretLogin([FromBody] LoginRequest request)
```

**Fixed:**
```csharp
private readonly string _secretPath;

public AuthController(IConfiguration config, ...)
{
    _secretPath = config["Auth:SuperAdminSecretPath"] 
        ?? throw new InvalidOperationException(
            "SUPERADMIN_SECRET_PATH environment variable not set");
}

[HttpPost("system-{secretPath}")]
[AllowAnonymous]
public async Task<IActionResult> SecretLogin(
    string secretPath,
    [FromBody] LoginRequest request)
{
    if (secretPath != _secretPath)
        return Unauthorized();
    
    // Existing login logic...
}
```

### FIX #3: Password Reset Flow (2-3 days)
**Components to create:**
1. Database fields on AdminUser entity
2. DTOs for requests/responses
3. AuthService methods (ForgotPasswordAsync, ResetPasswordAsync)
4. AuthController endpoints (/api/auth/forgot-password, /api/auth/reset-password)
5. Email template
6. Database migration
7. Frontend pages and components

**See detailed implementation below...**

### FIX #4: Tenant Refresh Token (2 hours)
**Files to modify:**
1. TenantAuthService - return RefreshToken in login response
2. AuthController - add refresh endpoint for tenant employees
3. Frontend - use RefreshToken for tenant employees
4. Database - store refresh tokens for tenants

### FIX #5: Admin Permissions Enforcement (1-2 days)
**Implement:**
1. ISuperAdminPermissionService.CheckPermissionAsync()
2. Permission validation middleware
3. [Authorize(Permissions = "...")] custom attribute
4. Audit logging for permission denials

---

## DETAILED: PASSWORD RESET IMPLEMENTATION

### Step 1: Add Database Fields to AdminUser Entity

File: `/src/HRMS.Core/Entities/Master/AdminUser.cs`

Add after StatusNotes field:
```csharp
/// <summary>
/// Password Reset: Token for password reset flow
/// Single-use, time-limited (1 hour expiry)
/// </summary>
public string? PasswordResetToken { get; set; }

/// <summary>
/// Password Reset: Token expiration timestamp
/// </summary>
public DateTime? PasswordResetTokenExpiry { get; set; }
```

### Step 2: Create DTOs

File: `/src/HRMS.Application/DTOs/ForgotPasswordRequest.cs`
```csharp
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
```

File: `/src/HRMS.Application/DTOs/ForgotPasswordResponse.cs`
```csharp
public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

File: `/src/HRMS.Application/DTOs/ResetPasswordRequest.cs`
```csharp
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

File: `/src/HRMS.Application/DTOs/ResetPasswordResponse.cs`
```csharp
public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### Step 3: Add Service Methods

File: `/src/HRMS.Infrastructure/Services/AuthService.cs`

Add these methods to IAuthService interface:
```csharp
Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword);
```

Implementation in AuthService:
```csharp
public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
{
    try
    {
        // Don't reveal if email exists (security)
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        
        if (adminUser == null)
            return (true, "If email exists, password reset link will be sent");
        
        // Generate reset token
        var resetToken = Guid.NewGuid().ToString("N");  // 32 chars
        var tokenExpiry = DateTime.UtcNow.AddHours(1);   // 1 hour expiry
        
        adminUser.PasswordResetToken = resetToken;
        adminUser.PasswordResetTokenExpiry = tokenExpiry;
        await _context.SaveChangesAsync();
        
        // Send email (implement in EmailService)
        var resetUrl = $"{_frontendUrl}/reset-password?token={resetToken}";
        var emailSent = await _emailService.SendPasswordResetEmailAsync(
            adminUser.Email,
            resetToken,
            adminUser.FirstName);
        
        if (!emailSent)
            _logger.LogWarning("Failed to send password reset email to {Email}", adminUser.Email);
        
        // Audit log
        _ = _auditLogService.LogAuthenticationAsync(
            AuditActionType.PASSWORD_RESET_REQUESTED,
            userId: adminUser.Id,
            userEmail: adminUser.Email,
            success: true);
        
        return (true, "If email exists, password reset link will be sent");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in ForgotPasswordAsync");
        return (false, "An error occurred. Please try again.");
    }
}

public async Task<(bool Success, string Message)> ResetPasswordAsync(
    string token, string newPassword)
{
    try
    {
        // Find admin with valid reset token
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        
        if (adminUser == null)
        {
            // Audit log failed attempt
            _ = _auditLogService.LogSecurityEventAsync(
                AuditActionType.PASSWORD_RESET_FAILED,
                AuditSeverity.WARNING,
                description: "Invalid password reset token");
            
            return (false, "Invalid or expired password reset link");
        }
        
        // Check token expiry
        if (adminUser.PasswordResetTokenExpiry == null || 
            adminUser.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            return (false, "Password reset link has expired. Please request a new one.");
        }
        
        // Validate new password complexity
        var (isValid, validationMessage) = ValidatePasswordComplexity(newPassword);
        if (!isValid)
            return (false, validationMessage);
        
        // Check password history (no reuse of last 5)
        var newPasswordHash = _passwordHasher.HashPassword(newPassword);
        
        if (!string.IsNullOrWhiteSpace(adminUser.PasswordHistory))
        {
            var passwordHistory = JsonSerializer.Deserialize<List<string>>(
                adminUser.PasswordHistory) ?? new();
            
            foreach (var oldHash in passwordHistory)
            {
                if (_passwordHasher.VerifyPassword(newPassword, oldHash))
                {
                    return (false, "Cannot reuse previous passwords. Choose a different password.");
                }
            }
            
            // Update history
            passwordHistory.Insert(0, adminUser.PasswordHash);
            if (passwordHistory.Count > 5)
                passwordHistory = passwordHistory.Take(5).ToList();
            adminUser.PasswordHistory = JsonSerializer.Serialize(passwordHistory);
        }
        else
        {
            adminUser.PasswordHistory = JsonSerializer.Serialize(
                new List<string> { adminUser.PasswordHash });
        }
        
        // Update password
        adminUser.PasswordHash = newPasswordHash;
        adminUser.LastPasswordChangeDate = DateTime.UtcNow;
        adminUser.PasswordExpiresAt = DateTime.UtcNow.AddDays(90);
        adminUser.MustChangePassword = false;
        
        // Revoke reset token
        adminUser.PasswordResetToken = null;
        adminUser.PasswordResetTokenExpiry = null;
        
        // Reset lockout
        adminUser.AccessFailedCount = 0;
        adminUser.LockoutEnd = null;
        
        adminUser.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Audit log success
        _ = _auditLogService.LogAuthenticationAsync(
            AuditActionType.PASSWORD_RESET_COMPLETED,
            userId: adminUser.Id,
            userEmail: adminUser.Email,
            success: true,
            eventData: new Dictionary<string, object>
            {
                { "passwordExpiryDate", adminUser.PasswordExpiresAt!.Value },
                { "method", "reset_token" }
            });
        
        return (true, "Password reset successfully. You can now login with your new password.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in ResetPasswordAsync");
        return (false, "An error occurred. Please try again.");
    }
}
```

### Step 4: Add Controller Endpoints

File: `/src/HRMS.API/Controllers/AuthController.cs`

Add these endpoints:
```csharp
/// <summary>
/// Request password reset email
/// Public endpoint - no authentication required
/// </summary>
[HttpPost("forgot-password")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    try
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid email" });
        
        var (success, message) = await _authService.ForgotPasswordAsync(request.Email);
        
        return Ok(new { success, message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in ForgotPassword");
        return StatusCode(500, new
        {
            success = false,
            message = "An error occurred. Please try again later."
        });
    }
}

/// <summary>
/// Reset password using token from email
/// Public endpoint - no authentication required
/// </summary>
[HttpPost("reset-password")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    try
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request" });
        
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new
            {
                success = false,
                message = "Passwords do not match"
            });
        
        var (success, message) = await _authService.ResetPasswordAsync(
            request.Token,
            request.NewPassword);
        
        return success ? Ok(new { success, message }) :
               BadRequest(new { success, message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in ResetPassword");
        return StatusCode(500, new
        {
            success = false,
            message = "An error occurred. Please try again later."
        });
    }
}
```

### Step 5: Add Email Template

In `EmailService.cs`, add:
```csharp
private string GetPasswordResetTemplate(string firstName, string resetUrl)
{
    return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Reset Your Password</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                padding: 40px 30px; text-align: center; color: #ffffff;'>
        <h1 style='margin: 0; font-size: 28px;'>Password Reset</h1>
        <p style='margin: 10px 0 0 0;'>MorisHR System</p>
    </div>
    
    <div style='padding: 40px 30px; max-width: 600px; margin: 0 auto;'>
        <h2 style='color: #333333;'>Reset Your Password</h2>
        <p>Hello {firstName},</p>
        <p>We received a request to reset your MorisHR password. 
           Click the button below to create a new password.</p>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetUrl}' style='display: inline-block; padding: 16px 40px; 
               background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
               color: #ffffff; text-decoration: none; border-radius: 5px; 
               font-weight: bold; font-size: 16px;'>
                Reset Password
            </a>
        </div>
        
        <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px;'>
            <p><strong>Important:</strong> This link will expire in 1 hour. 
               If you didn't request this, you can safely ignore this email.</p>
        </div>
        
        <p style='color: #666666; font-size: 12px; margin-top: 40px;'>
            If you can't click the button, copy and paste this link in your browser:<br/>
            {resetUrl}
        </p>
    </div>
</body>
</html>";
}
```

### Step 6: Database Migration

Create EF Core migration:
```bash
dotnet ef migrations add AddPasswordResetTokens -p src/HRMS.Infrastructure -s src/HRMS.API
```

The migration will add:
- AdminUser.PasswordResetToken (string nullable)
- AdminUser.PasswordResetTokenExpiry (DateTime nullable)

### Step 7: Frontend Implementation

Create `/hrms-frontend/src/app/auth/forgot-password/forgot-password.component.ts`:
```typescript
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  form: FormGroup;
  loading = false;
  submitted = false;
  successMessage = '';
  errorMessage = '';

  constructor() {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get f() { return this.form.controls; }

  onSubmit() {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.form.invalid) return;

    this.loading = true;
    this.authService.forgotPassword(this.form.value.email).subscribe({
      next: (response) => {
        this.loading = false;
        this.successMessage = response.message ||
          'If this email is registered, you will receive a password reset link.';
        this.form.reset();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'An error occurred.';
      }
    });
  }
}
```

Create `/hrms-frontend/src/app/auth/reset-password/reset-password.component.ts`:
```typescript
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  form: FormGroup;
  loading = false;
  submitted = false;
  successMessage = '';
  errorMessage = '';
  invalidToken = false;
  token = '';

  constructor() {
    this.form = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(12)]],
      confirmPassword: ['', Validators.required]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.invalidToken = true;
      this.errorMessage = 'Invalid or missing reset token.';
    }
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');
    return newPassword && confirmPassword && newPassword.value !== confirmPassword.value
      ? { passwordMismatch: true }
      : null;
  }

  get f() { return this.form.controls; }

  onSubmit() {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    if (this.form.invalid) return;

    this.loading = true;
    this.authService.resetPassword({
      token: this.token,
      newPassword: this.form.value.newPassword,
      confirmPassword: this.form.value.confirmPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Password reset successfully! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/auth/superadmin']), 2000);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message ||
          'Failed to reset password. Please try again.';
      }
    });
  }
}
```

---

## TESTING CHECKLIST

- [ ] Password reset email sends with valid token
- [ ] Reset link expires after 1 hour
- [ ] Invalid token shows appropriate error
- [ ] Password complexity validated
- [ ] Password history prevents reuse
- [ ] Successful reset redirects to login
- [ ] Audit log records all attempts
- [ ] Email doesn't reveal existence (generic message)
- [ ] Multiple reset requests don't leak information
- [ ] SQL injection attempts blocked
- [ ] XSS attempts blocked

---

## DEPLOYMENT NOTES

1. Run database migration before deploying
2. Configure email SMTP settings
3. Set JWT_SECRET in Secret Manager
4. Test with real email account
5. Verify frontend URLs match backend configuration
6. Set CORS AllowedDomains for frontend origin

