using HRMS.Core.Exceptions;
using HRMS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Base class for all tenant-aware services
/// Provides automatic tenant context validation and enforcement
/// FORTUNE 500 GRADE: Prevents cross-tenant data leakage through strict tenant validation
/// </summary>
/// <typeparam name="TService">The derived service type (for logging)</typeparam>
public abstract class TenantAwareServiceBase<TService>
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger<TService> Logger;

    protected TenantAwareServiceBase(
        ITenantContext tenantContext,
        ILogger<TService> logger)
    {
        TenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets and validates the current tenant ID
    /// SECURITY: Throws if tenant context is not set (prevents accidental cross-tenant queries)
    /// </summary>
    /// <param name="operationName">Name of the operation (for logging/error messages)</param>
    /// <returns>Validated tenant ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when tenant context is not available</exception>
    protected Guid GetCurrentTenantIdOrThrow(string operationName)
    {
        var tenantId = TenantContext.TenantId;

        if (!tenantId.HasValue)
        {
            Logger.LogError(
                "Attempted to perform tenant operation '{Operation}' without valid tenant context. " +
                "This is a critical security violation.",
                operationName);

            throw new UnauthorizedAccessException(
                "Tenant context is required for this operation. Please ensure you are authenticated " +
                "and accessing the system through a valid tenant subdomain.");
        }

        // Log tenant context for audit trail
        Logger.LogDebug(
            "Executing tenant operation '{Operation}' for tenant {TenantId} ({TenantName})",
            operationName,
            tenantId.Value,
            TenantContext.TenantName ?? "Unknown");

        return tenantId.Value;
    }

    /// <summary>
    /// Validates that tenant context is set
    /// Use this for operations that need tenant context but don't need the ID directly
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when tenant context is not available</exception>
    protected void ValidateTenantContext(string operationName)
    {
        GetCurrentTenantIdOrThrow(operationName);
    }

    /// <summary>
    /// Gets current tenant ID if available (doesn't throw)
    /// Use this for optional tenant context scenarios only
    /// </summary>
    /// <returns>Tenant ID or null</returns>
    protected Guid? GetCurrentTenantIdIfAvailable()
    {
        return TenantContext.TenantId;
    }

    /// <summary>
    /// Creates a scoped logger with tenant context information
    /// All log messages will include tenant ID for audit trail
    /// </summary>
    /// <returns>Logger scope</returns>
    protected IDisposable BeginTenantScope()
    {
        var tenantId = TenantContext.TenantId;
        var tenantName = TenantContext.TenantName;

        return Logger.BeginScope(new Dictionary<string, object>
        {
            ["TenantId"] = tenantId?.ToString() ?? "None",
            ["TenantName"] = tenantName ?? "None",
            ["TenantSchema"] = TenantContext.TenantSchema ?? "None"
        })!;
    }

    /// <summary>
    /// Wraps an async operation with tenant context validation and logging
    /// SECURITY: Ensures tenant context is valid before and after operation
    /// </summary>
    /// <typeparam name="TResult">Return type</typeparam>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    protected async Task<TResult> ExecuteTenantOperationAsync<TResult>(
        string operationName,
        Func<Guid, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantIdOrThrow(operationName);

        using (BeginTenantScope())
        {
            try
            {
                Logger.LogInformation(
                    "Starting tenant operation: {Operation}",
                    operationName);

                var result = await operation(tenantId, cancellationToken);

                Logger.LogInformation(
                    "Completed tenant operation: {Operation}",
                    operationName);

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(
                    ex,
                    "Failed to execute tenant operation: {Operation}",
                    operationName);

                throw;
            }
        }
    }

    /// <summary>
    /// Wraps an async operation with tenant context validation (no return value)
    /// </summary>
    protected async Task ExecuteTenantOperationAsync(
        string operationName,
        Func<Guid, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantIdOrThrow(operationName);

        using (BeginTenantScope())
        {
            try
            {
                Logger.LogInformation(
                    "Starting tenant operation: {Operation}",
                    operationName);

                await operation(tenantId, cancellationToken);

                Logger.LogInformation(
                    "Completed tenant operation: {Operation}",
                    operationName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(
                    ex,
                    "Failed to execute tenant operation: {Operation}",
                    operationName);

                throw;
            }
        }
    }
}
