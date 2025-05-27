using MediatR;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.ListTasks;

public record ListTasksQuery : IRequest<ListTasksResponse>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public Guid? DepartmentId { get; init; }

    /// <summary>
    /// Filter tasks where the specified user is assigned (supports multiple assignees per task).
    /// Returns all tasks where this user is one of the assignees.
    /// </summary>
    public Guid? AssignedToUserId { get; init; }

    public Guid? ParentTaskId { get; init; }
    public string? SearchTerm { get; init; }
}

