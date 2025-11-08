using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Interfaces;
using HRMS.Core.Settings;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Authentication service for tenant employees
/// </summary>
public class TenantAuthService
{
    private readonly MasterDbContext _masterContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly string _connectionString;
    private readonly ILogger<TenantAuthService> _logger;

    public TenantAuthService(
        MasterDbContext masterContext,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        IConfiguration configuration,
        ILogger<TenantAuthService> logger)
    {
        _masterContext = masterContext;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task<(string Token, DateTime ExpiresAt, Employee User, Guid TenantId)?> LoginAsync(
        string email,
        string password,
        string subdomain)
    {
        // Step 1: Get tenant by subdomain
        var tenant = await _masterContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLower());

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
            return null;
        }

        if (tenant.Status != Core.Enums.TenantStatus.Active)
        {
            _logger.LogWarning("Tenant is not active: {TenantId}, Status: {Status}", tenant.Id, tenant.Status);
            throw new InvalidOperationException("Tenant account is not active. Please contact support.");
        }

        // Step 2: Create tenant-specific DbContext
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(_connectionString, o =>
        {
            // Use tenant-specific schema for migrations history
            o.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
            o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

        await using var tenantContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName);

        // Step 3: Find employee by email in tenant schema
        var employee = await tenantContext.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email == email);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found in tenant {TenantId}: {Email}", tenant.Id, email);
            return null;
        }

        if (!employee.IsActive)
        {
            _logger.LogWarning("Employee account is not active: {EmployeeId}", employee.Id);
            return null;
        }

        if (employee.IsOffboarded)
        {
            _logger.LogWarning("Employee has been offboarded: {EmployeeId}", employee.Id);
            return null;
        }

        // Step 4: Check password hash exists
        if (string.IsNullOrEmpty(employee.PasswordHash))
        {
            _logger.LogError("Employee has no password hash: {EmployeeId}", employee.Id);
            throw new InvalidOperationException("Account is not configured for login. Please contact your administrator.");
        }

        // Step 5: Check account lockout
        if (employee.LockoutEnabled && employee.LockoutEnd.HasValue)
        {
            if (employee.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Employee account is locked: {EmployeeId} until {LockoutEnd}",
                    employee.Id, employee.LockoutEnd.Value);
                throw new InvalidOperationException(
                    $"Account is locked until {employee.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC. " +
                    $"Please try again later or contact your administrator.");
            }
            else
            {
                // Lockout period expired, reset
                employee.LockoutEnd = null;
                employee.AccessFailedCount = 0;
                await tenantContext.SaveChangesAsync();
            }
        }

        // Step 6: Verify password
        if (!_passwordHasher.VerifyPassword(password, employee.PasswordHash))
        {
            // Increment failed login attempts
            employee.AccessFailedCount++;

            // Lock account after 5 failed attempts (15 minute lockout)
            if (employee.LockoutEnabled && employee.AccessFailedCount >= 5)
            {
                employee.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await tenantContext.SaveChangesAsync();

                _logger.LogWarning("Employee account locked due to failed login attempts: {EmployeeId}", employee.Id);
                throw new InvalidOperationException(
                    "Account has been locked due to multiple failed login attempts. " +
                    $"Please try again after {employee.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC or contact your administrator.");
            }

            await tenantContext.SaveChangesAsync();
            _logger.LogWarning("Invalid password for employee: {Email} in tenant {TenantId}", email, tenant.Id);
            return null;
        }

        // Step 7: Reset failed login count on successful login
        employee.AccessFailedCount = 0;
        employee.LockoutEnd = null;
        employee.UpdatedAt = DateTime.UtcNow;
        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Successful tenant login: Employee {EmployeeId} in Tenant {TenantId}",
            employee.Id, tenant.Id);

        // Step 8: Check if employee is a manager (has subordinates)
        var isManager = await tenantContext.Employees
            .AnyAsync(e => e.ManagerId == employee.Id && !e.IsDeleted);

        // Step 9: Generate JWT token with tenant claims
        var token = GenerateTenantJwtToken(
            employee.Id,
            employee.Email,
            employee.FullName,
            employee.JobTitle,
            isManager,
            tenant.Id,
            tenant.Subdomain,
            tenant.SchemaName
        );
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return (token, expiresAt, employee, tenant.Id);
    }

    private string GenerateTenantJwtToken(
        Guid employeeId,
        string email,
        string fullName,
        string? jobTitle,
        bool isManager,
        Guid tenantId,
        string subdomain,
        string schemaName)
    {
        // Determine employee roles based on job title and manager status
        var roles = new List<string> { "TenantEmployee" };

        if (!string.IsNullOrEmpty(jobTitle))
        {
            var titleLower = jobTitle.ToLower();

            // Check for Admin role
            if (titleLower.Contains("admin"))
            {
                roles.Add("Admin");
            }

            // Check for HR role
            if (titleLower.Contains("hr") || titleLower.Contains("human resource"))
            {
                roles.Add("HR");
            }

            // Check for Manager role based on title
            if (titleLower.Contains("manager") || titleLower.Contains("supervisor") ||
                titleLower.Contains("head") || titleLower.Contains("director") ||
                titleLower.Contains("lead"))
            {
                roles.Add("Manager");
            }
        }

        // Add Manager role if employee has subordinates
        if (isManager && !roles.Contains("Manager"))
        {
            roles.Add("Manager");
        }

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, employeeId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, fullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_subdomain", subdomain),
            new Claim("tenant_schema", schemaName)
        };

        // Add all role claims
        foreach (var role in roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
            claimsList.Add(new Claim("role", role)); // Legacy claim name for compatibility
        }

        var claims = claimsList.ToArray();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
