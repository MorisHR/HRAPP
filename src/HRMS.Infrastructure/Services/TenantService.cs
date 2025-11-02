using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for resolving tenant context from HTTP request
/// </summary>
public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterDbContext _masterDbContext;
    private Guid? _currentTenantId;
    private string? _currentTenantSchema;

    public TenantService(IHttpContextAccessor httpContextAccessor, MasterDbContext masterDbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDbContext = masterDbContext;
    }

    public Guid? GetCurrentTenantId() => _currentTenantId;

    public string? GetCurrentTenantSchema() => _currentTenantSchema;

    public void SetTenantContext(Guid tenantId, string schemaName)
    {
        _currentTenantId = tenantId;
        _currentTenantSchema = schemaName;
    }

    public string? GetSubdomainFromHost(string host)
    {
        if (string.IsNullOrEmpty(host))
            return null;

        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];

        // Split by dots
        var parts = hostWithoutPort.Split('.');

        // If we have at least 3 parts (subdomain.domain.tld), extract subdomain
        if (parts.Length >= 3)
        {
            return parts[0].ToLower();
        }

        // For localhost or single-level domains
        if (parts.Length == 1)
        {
            // For development: localhost/tenant1 pattern or subdomain in header
            return null;
        }

        return null;
    }

    public async Task<(Guid? TenantId, string? SchemaName)> ResolveTenantFromRequestAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return (null, null);

        var host = httpContext.Request.Host.Host;
        var subdomain = GetSubdomainFromHost(host);

        if (string.IsNullOrEmpty(subdomain))
        {
            // SECURITY FIX: Only allow X-Tenant-Subdomain header in Development environment
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development" || environment == "Staging")
            {
                subdomain = httpContext.Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();
            }
        }

        if (string.IsNullOrEmpty(subdomain))
            return (null, null);

        // Special case: "admin" subdomain is for Super Admin panel (no tenant)
        if (subdomain.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        // Look up tenant by subdomain
        var tenant = await _masterDbContext.Tenants
            .Where(t => t.Subdomain == subdomain && t.Status == Core.Enums.TenantStatus.Active)
            .FirstOrDefaultAsync();

        if (tenant == null)
            return (null, null);

        return (tenant.Id, tenant.SchemaName);
    }
}
