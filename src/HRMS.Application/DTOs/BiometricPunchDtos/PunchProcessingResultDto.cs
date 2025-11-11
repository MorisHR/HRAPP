namespace HRMS.Application.DTOs.BiometricPunchDtos;

/// <summary>
/// DTO for punch processing operation results
/// Provides detailed feedback on punch processing success/failure
/// </summary>
public class PunchProcessingResultDto
{
    /// <summary>
    /// Indicates if the processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Main message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID of the created/updated punch record (if applicable)
    /// </summary>
    public Guid? PunchRecordId { get; set; }

    /// <summary>
    /// ID of the attendance record created/updated (if applicable)
    /// </summary>
    public Guid? AttendanceId { get; set; }

    /// <summary>
    /// Warning messages (non-critical issues)
    /// Example: "Employee device access not configured", "Low verification quality"
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();

    /// <summary>
    /// Error messages (critical issues that prevented processing)
    /// Example: "Device not found", "Employee mapping failed"
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>
    /// Helper method to add a warning
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    /// <summary>
    /// Helper method to add an error
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        Success = false;
    }

    /// <summary>
    /// Check if there are any warnings
    /// </summary>
    public bool HasWarnings => Warnings.Any();

    /// <summary>
    /// Check if there are any errors
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static PunchProcessingResultDto CreateSuccess(string message, Guid? punchRecordId = null, Guid? attendanceId = null)
    {
        return new PunchProcessingResultDto
        {
            Success = true,
            Message = message,
            PunchRecordId = punchRecordId,
            AttendanceId = attendanceId
        };
    }

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static PunchProcessingResultDto CreateFailure(string message, params string[] errors)
    {
        var result = new PunchProcessingResultDto
        {
            Success = false,
            Message = message
        };

        foreach (var error in errors)
        {
            result.AddError(error);
        }

        return result;
    }
}
