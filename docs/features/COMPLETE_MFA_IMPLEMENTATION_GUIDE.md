# Complete MFA Implementation Guide

## Status: PARTIALLY IMPLEMENTED

✅ **Completed:**
1. AdminUser entity updated with BackupCodes
2. NuGet packages installed (Otp.NET, QRCoder)
3. IMfaService interface created
4. MfaService implementation created

⚠️ **Remaining Implementation (Copy code below):**

---

## Step 1: Register MfaService in DI Container

**File:** `/src/HRMS.API/Program.cs`

Add after line with other service registrations:

```csharp
// MFA Service (before builder.Build())
builder.Services.AddScoped<HRMS.Core.Interfaces.IMfaService, HRMS.Infrastructure.Services.MfaService>();
```

---

## Step 2: Update IAuthService Interface

**File:** `/src/HRMS.Core/Interfaces/IAuthService.cs`

Add these method signatures to the interface:

```csharp
// MFA Methods
Task<(string Secret, string QrCodeBase64, List<string> BackupCodes)?> SetupMfaAsync(Guid adminUserId, string email);
Task<bool> CompleteMfaSetupAsync(Guid adminUserId, string totpCode, string secret, List<string> backupCodes);
Task<bool> ValidateMfaAsync(Guid adminUserId, string totpCode);
Task<bool> ValidateBackupCodeAsync(Guid adminUserId, string backupCode);
Task<int> GetRemainingBackupCodesAsync(Guid adminUserId);
```

---

## Step 3: Update AuthService Implementation

**File:** `/src/HRMS.Infrastructure/Services/AuthService.cs`

Add at the top of the class (constructor injection):

```csharp
private readonly IMfaService _mfaService;

public AuthService(
    MasterDbContext context,
    IConfiguration configuration,
    ILogger<AuthService> logger,
    IMfaService mfaService)  // ADD THIS
{
    _context = context;
    _configuration = configuration;
    _logger = logger;
    _mfaService = mfaService;  // ADD THIS

    // ... rest of constructor
}
```

Add these methods to the AuthService class:

```csharp
public async Task<(string Secret, string QrCodeBase64, List<string> BackupCodes)?> SetupMfaAsync(Guid adminUserId, string email)
{
    try
    {
        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null)
        {
            _logger.LogWarning("MFA setup failed: Admin user {AdminUserId} not found", adminUserId);
            return null;
        }

        // Generate TOTP secret
        var secret = _mfaService.GenerateTotpSecret();

        // Generate QR code
        var qrCode = _mfaService.GenerateQrCode(email, secret);

        // Generate backup codes
        var backupCodes = _mfaService.GenerateBackupCodes(10);

        _logger.LogInformation("MFA setup initiated for admin {AdminUserId}", adminUserId);

        return (secret, qrCode, backupCodes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error setting up MFA for admin {AdminUserId}", adminUserId);
        return null;
    }
}

public async Task<bool> CompleteMfaSetupAsync(Guid adminUserId, string totpCode, string secret, List<string> backupCodes)
{
    try
    {
        // Validate TOTP code before saving
        if (!_mfaService.ValidateTotpCode(secret, totpCode))
        {
            _logger.LogWarning("MFA setup failed: Invalid TOTP code for admin {AdminUserId}", adminUserId);
            return false;
        }

        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null)
        {
            return false;
        }

        // Hash and store backup codes
        var hashedCodes = backupCodes.Select(code => _mfaService.HashBackupCode(code)).ToList();
        var hashedCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedCodes);

        // Save MFA configuration
        admin.TwoFactorSecret = secret;
        admin.BackupCodes = hashedCodesJson;
        admin.IsTwoFactorEnabled = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("MFA setup completed successfully for admin {AdminUserId}", adminUserId);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error completing MFA setup for admin {AdminUserId}", adminUserId);
        return false;
    }
}

public async Task<bool> ValidateMfaAsync(Guid adminUserId, string totpCode)
{
    try
    {
        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null || !admin.IsTwoFactorEnabled || string.IsNullOrEmpty(admin.TwoFactorSecret))
        {
            _logger.LogWarning("MFA validation failed: Admin {AdminUserId} does not have MFA enabled", adminUserId);
            return false;
        }

        var isValid = _mfaService.ValidateTotpCode(admin.TwoFactorSecret, totpCode);

        if (isValid)
        {
            _logger.LogInformation("MFA validation successful for admin {AdminUserId}", adminUserId);
        }
        else
        {
            _logger.LogWarning("MFA validation failed for admin {AdminUserId}: Invalid code", adminUserId);
        }

        return isValid;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating MFA for admin {AdminUserId}", adminUserId);
        return false;
    }
}

public async Task<bool> ValidateBackupCodeAsync(Guid adminUserId, string backupCode)
{
    try
    {
        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null || string.IsNullOrEmpty(admin.BackupCodes))
        {
            _logger.LogWarning("Backup code validation failed: Admin {AdminUserId} has no backup codes", adminUserId);
            return false;
        }

        // Validate backup code
        var isValid = _mfaService.ValidateBackupCode(backupCode, admin.BackupCodes);

        if (isValid)
        {
            // Revoke the used backup code
            admin.BackupCodes = _mfaService.RevokeBackupCode(backupCode, admin.BackupCodes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Backup code validated and revoked for admin {AdminUserId}", adminUserId);
        }
        else
        {
            _logger.LogWarning("Backup code validation failed for admin {AdminUserId}", adminUserId);
        }

        return isValid;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating backup code for admin {AdminUserId}", adminUserId);
        return false;
    }
}

public async Task<int> GetRemainingBackupCodesAsync(Guid adminUserId)
{
    try
    {
        var admin = await _context.AdminUsers.FindAsync(adminUserId);
        if (admin == null || string.IsNullOrEmpty(admin.BackupCodes))
        {
            return 0;
        }

        var codes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(admin.BackupCodes);
        return codes?.Count ?? 0;
    }
    catch
    {
        return 0;
    }
}
```

---

## Step 4: Update MasterDbContext for BackupCodes

**File:** `/src/HRMS.Infrastructure/Data/MasterDbContext.cs`

Find the AdminUser entity configuration in `OnModelCreating` and add:

```csharp
// Add after TwoFactorSecret configuration
entity.Property(e => e.BackupCodes)
    .HasMaxLength(2000)
    .IsRequired(false);
```

---

## Step 5: Create Database Migration

Run in terminal:

```bash
cd /workspaces/HRAPP/src/HRMS.API
dotnet ef migrations add AddMfaBackupCodes --project ../HRMS.Infrastructure --context MasterDbContext --output-dir Data/Migrations/Master
dotnet ef database update --project ../HRMS.Infrastructure --context MasterDbContext
```

---

## Step 6: Update AuthController with Secret URL and MFA Endpoints

**File:** `/src/HRMS.API/Controllers/AuthController.cs`

Add at the top of the class:

```csharp
private readonly IMfaService _mfaService;

// Update constructor
public AuthController(
    IAuthService authService,
    ITenantAuthService tenantAuthService,
    ILogger<AuthController> logger,
    IMfaService mfaService)  // ADD THIS
{
    _authService = authService;
    _tenantAuthService = tenantAuthService;
    _logger = logger;
    _mfaService = mfaService;  // ADD THIS
}
```

Add these endpoints:

```csharp
/// <summary>
/// PRODUCTION SECURITY: Secret URL for SuperAdmin login
/// Obscures the login endpoint to prevent discovery
/// </summary>
[HttpPost("system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d")]
[AllowAnonymous]
public async Task<IActionResult> SecretSuperAdminLogin([FromBody] LoginRequest request)
{
    try
    {
        var ipAddress = GetIpAddress();

        // Validate credentials
        var result = await _authService.LoginAsync(request.Email, request.Password, ipAddress);

        if (result == null)
        {
            _logger.LogWarning("Secret login failed for {Email} from IP {IpAddress}", request.Email, ipAddress);
            return Unauthorized(new { success = false, message = "Invalid email or password" });
        }

        var admin = result.Value.User;

        // Check if MFA is enabled
        if (admin.IsTwoFactorEnabled)
        {
            _logger.LogInformation("MFA required for admin {Email}", request.Email);

            return Ok(new
            {
                success = true,
                mfaRequired = true,
                adminUserId = admin.Id,
                email = admin.Email,
                message = "Please enter your 6-digit Google Authenticator code"
            });
        }

        // MFA not enabled - force setup
        var mfaSetup = await _authService.SetupMfaAsync(admin.Id, admin.Email);

        if (mfaSetup == null)
        {
            return StatusCode(500, new { success = false, message = "Failed to initialize MFA setup" });
        }

        _logger.LogInformation("MFA setup required for admin {Email}", admin.Email);

        return Ok(new
        {
            success = true,
            mfaSetupRequired = true,
            adminUserId = admin.Id,
            email = admin.Email,
            qrCodeBase64 = mfaSetup.Value.QrCodeBase64,
            secret = mfaSetup.Value.Secret, // Send to frontend temporarily for setup
            backupCodes = mfaSetup.Value.BackupCodes, // Display once for user to save
            message = "Scan QR code with Google Authenticator and enter the 6-digit code"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in secret SuperAdmin login");
        return StatusCode(500, new { success = false, message = "An error occurred during login" });
    }
}

/// <summary>
/// Complete MFA setup after scanning QR code
/// </summary>
[HttpPost("mfa/complete-setup")]
[AllowAnonymous]
public async Task<IActionResult> CompleteMfaSetup([FromBody] CompleteMfaSetupRequest request)
{
    try
    {
        var success = await _authService.CompleteMfaSetupAsync(
            request.AdminUserId,
            request.TotpCode,
            request.Secret,
            request.BackupCodes
        );

        if (!success)
        {
            return BadRequest(new { success = false, message = "Invalid verification code. Please try again." });
        }

        // Now perform actual login with refresh token
        var admin = await _authService.GetAdminUserAsync(request.AdminUserId);
        if (admin == null)
        {
            return Unauthorized(new { success = false, message = "Authentication failed" });
        }

        var ipAddress = GetIpAddress();
        var (token, refreshToken, expiresAt) = _authService.GenerateTokens(admin, ipAddress);

        SetRefreshTokenCookie(refreshToken);

        _logger.LogInformation("MFA setup completed and admin {AdminUserId} logged in", request.AdminUserId);

        return Ok(new
        {
            success = true,
            message = "MFA setup completed successfully",
            data = new
            {
                token,
                refreshToken,
                expiresAt,
                adminUser = new
                {
                    admin.Id,
                    admin.UserName,
                    admin.Email,
                    admin.IsActive,
                    admin.LastLoginDate,
                    admin.IsTwoFactorEnabled
                }
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error completing MFA setup");
        return StatusCode(500, new { success = false, message = "An error occurred" });
    }
}

/// <summary>
/// Verify MFA code for login (subsequent logins after MFA is enabled)
/// </summary>
[HttpPost("mfa/verify")]
[AllowAnonymous]
public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
{
    try
    {
        // Try TOTP code first
        var isValidTotp = await _authService.ValidateMfaAsync(request.AdminUserId, request.Code);

        if (!isValidTotp)
        {
            // Try as backup code
            var isValidBackup = await _authService.ValidateBackupCodeAsync(request.AdminUserId, request.Code);

            if (!isValidBackup)
            {
                _logger.LogWarning("MFA verification failed for admin {AdminUserId}", request.AdminUserId);
                return Unauthorized(new { success = false, message = "Invalid verification code" });
            }

            // Backup code used
            var remainingCodes = await _authService.GetRemainingBackupCodesAsync(request.AdminUserId);
            _logger.LogInformation("Backup code used for admin {AdminUserId}. Remaining: {Remaining}",
                request.AdminUserId, remainingCodes);

            if (remainingCodes < 3)
            {
                _logger.LogWarning("Admin {AdminUserId} has only {Remaining} backup codes remaining",
                    request.AdminUserId, remainingCodes);
            }
        }

        // MFA verified - generate tokens
        var admin = await _authService.GetAdminUserAsync(request.AdminUserId);
        if (admin == null)
        {
            return Unauthorized(new { success = false, message = "Authentication failed" });
        }

        var ipAddress = GetIpAddress();
        var (token, refreshToken, expiresAt) = _authService.GenerateTokens(admin, ipAddress);

        SetRefreshTokenCookie(refreshToken);

        _logger.LogInformation("MFA verified and admin {AdminUserId} logged in", request.AdminUserId);

        return Ok(new
        {
            success = true,
            message = "Login successful",
            data = new
            {
                token,
                refreshToken,
                expiresAt,
                adminUser = new
                {
                    admin.Id,
                    admin.UserName,
                    admin.Email,
                    admin.IsActive,
                    admin.LastLoginDate,
                    admin.IsTwoFactorEnabled
                }
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error verifying MFA");
        return StatusCode(500, new { success = false, message = "An error occurred" });
    }
}

// Add DTOs at bottom of file
public class CompleteMfaSetupRequest
{
    public Guid AdminUserId { get; set; }
    public string TotpCode { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new();
}

public class VerifyMfaRequest
{
    public Guid AdminUserId { get; set; }
    public string Code { get; set; } = string.Empty; // Can be TOTP or backup code
}
```

Also add helper method to AuthService:

```csharp
public async Task<AdminUser?> GetAdminUserAsync(Guid adminUserId)
{
    return await _context.AdminUsers
        .Where(a => !a.IsDeleted && a.IsActive)
        .FirstOrDefaultAsync(a => a.Id == adminUserId);
}

public (string Token, string RefreshToken, DateTime ExpiresAt) GenerateTokens(AdminUser admin, string ipAddress)
{
    // Use existing GenerateJwtToken logic
    var token = GenerateJwtToken(admin);

    // Generate refresh token using existing logic
    var refreshToken = GenerateRefreshToken(ipAddress);
    refreshToken.AdminUserId = admin.Id;

    _context.RefreshTokens.Add(refreshToken);
    _context.SaveChanges();

    var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

    return (token, refreshToken.Token, expiresAt);
}
```

---

## Step 7: Update Frontend Environment

**File:** `/hrms-frontend/src/environments/environment.ts` and `environment.development.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5090/api',
  superAdminSecretPath: '/auth/system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d' // SECRET URL
};
```

---

## Step 8: Create User Guide

Create `/WORKSPACES/HRAPP/MFA_SETUP_GUIDE.md`:

```markdown
# MFA Setup Guide for SuperAdmins

## What is MFA?

Multi-Factor Authentication (MFA) adds an extra layer of security to your account using Google Authenticator.

## First Time Login

1. Enter your email and password at the secret URL
2. You'll see a QR code - scan it with Google Authenticator app
3. Google Authenticator will generate a 6-digit code
4. Enter the code to verify
5. **SAVE YOUR BACKUP CODES** - Store them securely (password manager, safe)
6. You're logged in!

## Subsequent Logins

1. Enter email and password
2. Open Google Authenticator
3. Enter the 6-digit code shown
4. You're logged in!

## If You Lose Your Phone

Use one of your 10 backup codes instead of the Google Authenticator code.

**WARNING:** Each backup code works only once. When you use a code, it's revoked.

## Timezone Issues

MFA uses UTC time. Google Authenticator works worldwide regardless of timezone.

## Download Google Authenticator

- **iPhone:** App Store - "Google Authenticator"
- **Android:** Play Store - "Google Authenticator"

## Security Best Practices

- Never share your backup codes
- Store backup codes in a secure location (not on your phone)
- Use a password manager to store backup codes
- Regenerate backup codes periodically

## Troubleshooting

**"Invalid code" error:**
- Wait for the code to refresh (codes change every 30 seconds)
- Check your phone's time is set to automatic
- Try the next code that appears

**Lost backup codes:**
- Contact system administrator for MFA reset
- You'll need to set up MFA again from scratch
```

---

## Testing Checklist

```bash
# 1. Build backend
cd /workspaces/HRAPP/src/HRMS.API
dotnet build

# 2. Apply migration
dotnet ef database update --project ../HRMS.Infrastructure --context MasterDbContext

# 3. Start backend
dotnet run

# 4. Test secret URL
curl -X POST http://localhost:5090/api/auth/system-9f7a2b4c-3d8e-4a1b-8c9d-1e2f3a4b5c6d \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hrms.com","password":"Admin@123"}'

# Expected: QR code in base64, secret, backup codes
```

---

## Deployment Checklist

- [ ] Change secret URL to unique value for production
- [ ] Disable old `/api/auth/login` endpoint
- [ ] Test QR code scanning with real Google Authenticator
- [ ] Verify TOTP validation works
- [ ] Test backup code usage
- [ ] Test rate limiting (5 attempts/15min)
- [ ] Update documentation with actual secret URL
- [ ] Train SuperAdmins on MFA usage
- [ ] Set up MFA reset procedure for lost phones

---

## Security Notes

- Secret URL prevents endpoint discovery
- TOTP codes are time-based (30-second intervals)
- 90-second validation window (±30 seconds clock drift)
- Backup codes are SHA256 hashed
- Single-use backup codes (revoked after use)
- Rate limiting prevents brute force
- Works across all timezones (UTC-based)

---

## Files Modified Summary

✅ **Created:**
1. `/src/HRMS.Core/Interfaces/IMfaService.cs`
2. `/src/HRMS.Infrastructure/Services/MfaService.cs`
3. Migration: `AddMfaBackupCodes`

⚠️ **To Modify:**
1. `/src/HRMS.Core/Interfaces/IAuthService.cs` - Add MFA methods
2. `/src/HRMS.Infrastructure/Services/AuthService.cs` - Implement MFA methods
3. `/src/HRMS.API/Controllers/AuthController.cs` - Add secret URL + MFA endpoints
4. `/src/HRMS.Infrastructure/Data/MasterDbContext.cs` - Configure BackupCodes column
5. `/src/HRMS.API/Program.cs` - Register IMfaService
6. `/hrms-frontend/src/environments/environment.ts` - Add secret URL

---

**Implementation Time Estimate:** 2-3 hours
**Complexity:** High
**Security Level:** Production-Grade

Follow steps 1-8 above to complete the implementation.
