using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Employee Draft Management API
/// Handles draft save, auto-save, recovery, and finalization
/// Supports shared editing across admins with 30-day auto-deletion
/// </summary>
[ApiController]
[Route("api/employee-drafts")]
[Authorize(Roles = "Admin,HR")]
public class EmployeeDraftsController : ControllerBase
{
    private readonly IEmployeeDraftService _draftService;
    private readonly ILogger<EmployeeDraftsController> _logger;

    public EmployeeDraftsController(
        IEmployeeDraftService draftService,
        ILogger<EmployeeDraftsController> logger)
    {
        _draftService = draftService;
        _logger = logger;
    }

    /// <summary>
    /// Get all drafts for current tenant
    /// Returns drafts ordered by last edit date (most recent first)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeDraftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDrafts()
    {
        try
        {
            var drafts = await _draftService.GetDraftsForTenantAsync();

            return Ok(new
            {
                success = true,
                data = drafts,
                count = drafts.Count,
                message = $"Retrieved {drafts.Count} draft(s)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee drafts");
            return StatusCode(500, new { success = false, message = "Error retrieving drafts" });
        }
    }

    /// <summary>
    /// Get specific draft by ID
    /// Used for draft recovery when user returns to form
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDraftById(Guid id)
    {
        try
        {
            var draft = await _draftService.GetDraftByIdAsync(id);

            if (draft == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Draft {id} not found or has been deleted"
                });
            }

            return Ok(new
            {
                success = true,
                data = draft,
                message = "Draft retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draft {DraftId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving draft" });
        }
    }

    /// <summary>
    /// Save draft (create new or update existing)
    /// Auto-save calls this every 10 seconds
    /// Manual save calls this when user clicks "Save Draft"
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveDraft([FromBody] SaveEmployeeDraftRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid draft data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            // Extract user information from JWT claims
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found"));
            var userName = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? "Unknown User";

            var draft = await _draftService.SaveDraftAsync(request, userId, userName);

            var draftDto = new EmployeeDraftDto
            {
                Id = draft.Id,
                FormDataJson = draft.FormDataJson,
                DraftName = draft.DraftName,
                CompletionPercentage = draft.CompletionPercentage,
                CreatedBy = draft.CreatedBy,
                CreatedByName = draft.CreatedByName,
                CreatedAt = draft.CreatedAt,
                LastEditedBy = draft.LastEditedBy,
                LastEditedByName = draft.LastEditedByName,
                LastEditedAt = draft.LastEditedAt,
                ExpiresAt = draft.ExpiresAt,
                DaysUntilExpiry = draft.DaysUntilExpiry,
                IsExpired = draft.IsExpired
            };

            _logger.LogInformation(
                "Draft saved: {DraftId} by {UserName} ({CompletionPercentage}%)",
                draft.Id, userName, draft.CompletionPercentage);

            return Ok(new
            {
                success = true,
                data = draftDto,
                message = request.Id.HasValue
                    ? "Draft updated successfully"
                    : "Draft created successfully"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized draft save attempt");
            return Unauthorized(new { success = false, message = "User authentication required" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Draft not found for update");
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving draft");
            return StatusCode(500, new { success = false, message = "Error saving draft" });
        }
    }

    /// <summary>
    /// Finalize draft - convert to employee
    /// Validates form data, creates employee record, and soft-deletes draft
    /// </summary>
    [HttpPost("{id}/finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FinalizeDraft(Guid id)
    {
        try
        {
            var employee = await _draftService.FinalizeDraftAsync(id);

            _logger.LogInformation(
                "Draft {DraftId} finalized - Employee {EmployeeCode} created",
                id, employee.EmployeeCode);

            return Ok(new
            {
                success = true,
                message = "Employee created successfully from draft",
                data = new
                {
                    employeeId = employee.Id,
                    employeeCode = employee.EmployeeCode,
                    fullName = $"{employee.FirstName} {employee.LastName}"
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Draft not found for finalization: {DraftId}", id);
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error finalizing draft: {DraftId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing draft {DraftId}", id);
            return StatusCode(500, new { success = false, message = "Error finalizing draft" });
        }
    }

    /// <summary>
    /// Delete draft (soft delete)
    /// User can manually delete drafts they no longer need
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDraft(Guid id)
    {
        try
        {
            await _draftService.DeleteDraftAsync(id);

            _logger.LogInformation("Draft {DraftId} deleted", id);

            return Ok(new
            {
                success = true,
                message = "Draft deleted successfully"
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Draft not found for deletion: {DraftId}", id);
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting draft {DraftId}", id);
            return StatusCode(500, new { success = false, message = "Error deleting draft" });
        }
    }
}
