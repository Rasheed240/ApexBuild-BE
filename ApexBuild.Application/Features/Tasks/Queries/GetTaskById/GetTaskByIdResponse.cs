using ApexBuild.Application.Features.Tasks.Common;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskById;

public record GetTaskByIdResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid? ParentTaskId { get; init; }
    public string? ParentTaskTitle { get; init; }
    public int SubtaskCount { get; init; }
    public List<TaskAssigneeDto> Assignees { get; init; } = new List<TaskAssigneeDto>();
    public Guid? AssignedByUserId { get; init; }
    public string? AssignedByName { get; init; }
    public TaskStatus Status { get; init; }
    public int Priority { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public decimal Progress { get; init; }
    public string? Location { get; init; }
    public List<string>? Tags { get; init; }
    public List<string>? ImageUrls { get; init; }
    public List<string>? VideoUrls { get; init; }
    public List<string>? AttachmentUrls { get; init; }
    public int UpdateCount { get; init; }
    public int CommentCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

