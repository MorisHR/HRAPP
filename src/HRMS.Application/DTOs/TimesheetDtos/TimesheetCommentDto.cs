namespace HRMS.Application.DTOs.TimesheetDtos;

public class TimesheetCommentDto
{
    public Guid Id { get; set; }
    public Guid TimesheetId { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public DateTime CommentedAt { get; set; }
}
