using MediatR;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetMyTasks;

public record GetMyTasksQuery : IRequest<GetMyTasksResponse>
{
    public Guid? OrganizationId { get; init; }
    public Guid? ProjectId { get; init; }
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
