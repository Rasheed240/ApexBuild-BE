using MediatR;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetProjectTasks;

public record GetProjectTasksQuery : IRequest<GetProjectTasksResponse>
{
    public Guid ProjectId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? SearchTerm { get; init; }
    public bool IncludeSubtasks { get; init; } = true;
    public bool IncludeUpdates { get; init; } = true;
}
