using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetProjectTasks;

public record TaskUpdateDto
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public UpdateStatus Status { get; init; }
    public decimal ProgressPercentage { get; init; }
    public string? SubmittedByUserName { get; init; }
    public DateTime SubmittedAt { get; init; }
    public bool? SupervisorApproved { get; init; }
    public bool? AdminApproved { get; init; }
    public string? SupervisorFeedback { get; init; }
    public string? AdminFeedback { get; init; }
    public List<string> MediaUrls { get; init; } = new();
}

public record SubtaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public TaskStatus Status { get; init; }
    public int Priority { get; init; }
    public decimal Progress { get; init; }
    public DateTime? DueDate { get; init; }
    public string? AssignedToUserName { get; init; }
    public List<TaskUpdateDto> RecentUpdates { get; init; } = new();
}

public record ProjectTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public TaskStatus Status { get; init; }
    public int Priority { get; init; }
    public decimal Progress { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public string? Location { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public Guid? AssignedByUserId { get; init; }
    public string? AssignedByUserName { get; init; }
    public List<string>? Tags { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<SubtaskDto> Subtasks { get; init; } = new();
    public List<TaskUpdateDto> Updates { get; init; } = new();
    public int SubtaskCount { get; init; }
    public int UpdateCount { get; init; }
    public int CommentCount { get; init; }
}

public record GetProjectTasksResponse
{
    public List<ProjectTaskDto> Tasks { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
