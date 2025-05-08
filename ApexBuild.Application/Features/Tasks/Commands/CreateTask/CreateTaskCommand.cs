using MediatR;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.CreateTask;

public record CreateTaskCommand : IRequest<CreateTaskResponse>
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid DepartmentId { get; init; }
    public Guid? ParentTaskId { get; init; } // For subtasks
    public List<Guid> AssignedUserIds { get; init; } = new List<Guid>(); // Multiple assignees
    public TaskStatus Status { get; init; } = TaskStatus.NotStarted;
    public int Priority { get; init; } = 1; // 1=Low, 2=Medium, 3=High, 4=Critical
    public DateTime? StartDate { get; init; }
    public DateTime? DueDate { get; init; }
    public int EstimatedHours { get; init; } = 0;
    public string? Location { get; init; }
    public List<string>? Tags { get; init; }
}

