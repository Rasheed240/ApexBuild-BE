using ApexBuild.Application.Features.Tasks.Common;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.ListTasks;

public record TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid? ParentTaskId { get; init; }
    public List<TaskAssigneeDto> Assignees { get; init; } = new List<TaskAssigneeDto>();
    public TaskStatus Status { get; init; }
    public int Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal Progress { get; init; }
    public List<string>? ImageUrls { get; init; }
    public List<string>? VideoUrls { get; init; }
    public List<string>? AttachmentUrls { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ListTasksResponse
{
    public List<TaskDto> Tasks { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

