using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Services;

namespace HRMS.API.Controllers;

/// <summary>
/// PRODUCTION-GRADE: Tenant billing and subscription management API
/// SECURITY: Admin and HR only access, tenant-scoped data
/// PURPOSE: Frontend billing portal access to subscription and payment history
/// </summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Roles = "Admin,HR")]
public class TenantBillingController : ControllerBase
{
    private readonly ISubscriptionManagementService _subscriptionService;
    private readonly TenantManagementService _tenantManagementService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantBillingController> _logger;

    public TenantBillingController(
        ISubscriptionManagementService subscriptionService,
        TenantManagementService tenantManagementService,
        ITenantService tenantService,
        ILogger<TenantBillingController> logger)
    {
        _subscriptionService = subscriptionService;
        _tenantManagementService = tenantManagementService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get subscription details and payment history for current tenant
    /// SECURITY: Returns only current tenant's data (tenant-scoped)
    /// USAGE: Tenant billing portal displays subscription status and payment history
    /// </summary>
    /// <returns>Subscription details with payment history</returns>
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(TenantSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubscription()
    {
        try
        {
            // Get current tenant ID from context (set by TenantResolutionMiddleware)
            var tenantId = _tenantService.GetCurrentTenantId();

            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Subscription request without tenant context");
                return Unauthorized(new
                {
                    success = false,
                    message = "Tenant context not found. Please ensure you are accessing from a valid tenant subdomain."
                });
            }

            _logger.LogInformation("Fetching subscription details for tenant: {TenantId}", tenantId.Value);

            // Get tenant details with subscription info from Master database
            var tenantDto = await _tenantManagementService.GetTenantByIdAsync(tenantId.Value);

            if (tenantDto == null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", tenantId.Value);
                return NotFound(new
                {
                    success = false,
                    message = "Subscription not found"
                });
            }

            // Get payment history for tenant
            var payments = await _subscriptionService.GetPaymentsByTenantIdAsync(tenantId.Value);

            // Calculate grace period end date if subscription is expired
            DateTime? gracePeriodEndDate = null;
            if (IsSubscriptionExpired(tenantDto.SubscriptionEndDate) && tenantDto.SubscriptionEndDate.HasValue)
            {
                gracePeriodEndDate = tenantDto.SubscriptionEndDate.Value.AddDays(14); // 14-day grace period
            }

            // Build response DTO
            var response = new TenantSubscriptionDto
            {
                Subscription = new SubscriptionDetailsDto
                {
                    Id = tenantDto.Id,
                    TenantId = tenantDto.Id,
                    Tier = tenantDto.EmployeeTier.ToString(),
                    MonthlyPrice = Math.Round(tenantDto.YearlyPriceMUR / 12, 2),
                    Status = tenantDto.Status.ToString(),
                    CurrentPeriodStart = tenantDto.SubscriptionStartDate,
                    CurrentPeriodEnd = tenantDto.SubscriptionEndDate,
                    AutoRenew = true, // Default behavior - can be made configurable
                    GracePeriodEndDate = gracePeriodEndDate,
                    DaysUntilExpiry = CalculateDaysUntilExpiry(tenantDto.SubscriptionEndDate),
                    IsExpired = IsSubscriptionExpired(tenantDto.SubscriptionEndDate)
                },
                Payments = payments.Select(p => new PaymentHistoryDto
                {
                    Id = p.Id,
                    SubscriptionId = tenantDto.Id,
                    Amount = p.TotalMUR,
                    DueDate = p.DueDate,
                    PaidDate = p.PaidDate,
                    Status = p.Status.ToString(),
                    PaymentMethod = p.PaymentMethod ?? "Manual",
                    InvoiceNumber = p.PaymentReference,
                    PeriodStartDate = p.PeriodStartDate,
                    PeriodEndDate = p.PeriodEndDate,
                    IsOverdue = p.IsOverdue
                }).OrderByDescending(p => p.DueDate).ToList()
            };

            _logger.LogInformation(
                "Subscription details retrieved for tenant {TenantId}: Tier={Tier}, Status={Status}, Payments={PaymentCount}",
                tenantId.Value, response.Subscription.Tier, response.Subscription.Status, response.Payments.Count);

            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription details");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while fetching subscription details"
            });
        }
    }

    /// <summary>
    /// Helper method to calculate days until subscription expiry
    /// </summary>
    private int? CalculateDaysUntilExpiry(DateTime? subscriptionEndDate)
    {
        if (!subscriptionEndDate.HasValue)
            return null;

        return (subscriptionEndDate.Value - DateTime.UtcNow).Days;
    }

    /// <summary>
    /// Helper method to check if subscription is expired
    /// </summary>
    private bool IsSubscriptionExpired(DateTime? subscriptionEndDate)
    {
        if (!subscriptionEndDate.HasValue)
            return false;

        return DateTime.UtcNow > subscriptionEndDate.Value;
    }
}
