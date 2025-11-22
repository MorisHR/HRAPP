using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using HRMS.Core.Entities;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Persistence;
using HRMS.Infrastructure.Data;

namespace HRMS.API.Services;

/// <summary>
/// Fortune 500-grade tenant impersonation service
/// Allows SuperAdmins to temporarily assume tenant user identity for support/troubleshooting
/// </summary>
public interface IImpersonationService
{
    Task<ImpersonationSession> StartImpersonationAsync(string adminUserId, Guid targetUserId, string reason);
    Task StopImpersonationAsync(string sessionId);
    Task<ImpersonationSession?> GetCurrentSessionAsync(string adminUserId);
    Task<(List<ImpersonationSession> sessions, int totalCount)> GetImpersonationHistoryAsync(
        int pageIndex = 0, int pageSize = 50, string? adminUserId = null, DateTime? fromDate = null, DateTime? toDate = null);
    bool IsImpersonating(ClaimsPrincipal user);
    string? GetActualAdminUserId(ClaimsPrincipal user);
}

public class ImpersonationService : IImpersonationService
{
    private readonly MasterDbContext _masterDb;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ImpersonationService> _logger;
    private static readonly List<ImpersonationSession> _sessions = new(); // In-memory for now

    public ImpersonationService(
        MasterDbContext masterDb,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogService auditLogService,
        ILogger<ImpersonationService> logger)
    {
        _masterDb = masterDb;
        _httpContextAccessor = httpContextAccessor;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ImpersonationSession> StartImpersonationAsync(string adminUserId, Guid targetUserId, string reason)
    {
        _logger.LogInformation("Admin {AdminId} starting impersonation of user {TargetUserId}", adminUserId, targetUserId);

        // TODO: Fix - Tenant users are not in MasterDbContext, they're in tenant-specific schemas
        // This impersonation feature needs to be redesigned to work with multi-tenant architecture

        // Check if admin already has an active session
        var existingSession = await GetCurrentSessionAsync(adminUserId);
        if (existingSession != null)
        {
            throw new InvalidOperationException("Admin already has an active impersonation session. Please stop it first.");
        }

        // Create impersonation session (temporary implementation)
        var session = new ImpersonationSession
        {
            Id = Guid.NewGuid().ToString(),
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            TargetUserEmail = "unknown@temp.com", // TODO: Get from tenant database
            TargetUserName = "Unknown User", // TODO: Get from tenant database
            TenantId = null, // TODO: Get from tenant database
            Reason = reason,
            StartedAt = DateTime.UtcNow,
            IsActive = true,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
        };

        _sessions.Add(session);

        // Audit log
        await _auditLogService.LogSecurityEventAsync(
            AuditActionType.TENANT_IMPERSONATION_STARTED,
            AuditSeverity.CRITICAL,
            Guid.Parse(adminUserId),
            $"Admin started impersonating user (ID: {targetUserId}). Reason: {reason}",
            $"SessionId={session.Id}"
        );

        _logger.LogWarning("SECURITY: Admin {AdminId} started impersonating user {TargetUserId}. Reason: {Reason}",
            adminUserId, targetUserId, reason);

        return await Task.FromResult(session);
    }

    public async Task StopImpersonationAsync(string sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Impersonation session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException("Session is already inactive");
        }

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        session.DurationMinutes = (int)(session.EndedAt.Value - session.StartedAt).TotalMinutes;

        // Audit log
        await _auditLogService.LogSecurityEventAsync(
            AuditActionType.TENANT_IMPERSONATION_ENDED,
            AuditSeverity.WARNING,
            Guid.Parse(session.AdminUserId),
            $"Admin stopped impersonating {session.TargetUserEmail}. Duration: {session.DurationMinutes} minutes",
            $"SessionId={session.Id}, TenantId={session.TenantId}"
        );

        _logger.LogWarning("SECURITY: Admin {AdminId} stopped impersonating user {TargetEmail}. Duration: {Duration} min",
            session.AdminUserId, session.TargetUserEmail, session.DurationMinutes);
    }

    public Task<ImpersonationSession?> GetCurrentSessionAsync(string adminUserId)
    {
        var session = _sessions.FirstOrDefault(s => s.AdminUserId == adminUserId && s.IsActive);
        return Task.FromResult(session);
    }

    public Task<(List<ImpersonationSession> sessions, int totalCount)> GetImpersonationHistoryAsync(
        int pageIndex = 0, int pageSize = 50, string? adminUserId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _sessions.AsQueryable();

        if (!string.IsNullOrEmpty(adminUserId))
            query = query.Where(s => s.AdminUserId == adminUserId);

        if (fromDate.HasValue)
            query = query.Where(s => s.StartedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.StartedAt <= toDate.Value);

        var totalCount = query.Count();
        var sessions = query.OrderByDescending(s => s.StartedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((sessions, totalCount));
    }

    public bool IsImpersonating(ClaimsPrincipal user)
    {
        return user.HasClaim(c => c.Type == "impersonating" && c.Value == "true");
    }

    public string? GetActualAdminUserId(ClaimsPrincipal user)
    {
        if (!IsImpersonating(user))
            return null;

        return user.FindFirst("actual_admin_id")?.Value;
    }
}
