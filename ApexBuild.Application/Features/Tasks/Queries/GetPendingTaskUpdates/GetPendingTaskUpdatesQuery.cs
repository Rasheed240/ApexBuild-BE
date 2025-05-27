using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingTaskUpdates;

public record GetPendingTaskUpdatesQuery : IRequest<GetPendingTaskUpdatesResponse>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public UpdateStatus? FilterByStatus { get; init; }
    public Guid? FilterByProjectId { get; init; }
    public Guid? FilterByDepartmentId { get; init; }
    public string? SearchTerm { get; init; }
    public bool IncludeMedia { get; init; } = true;
    public bool IncludeTaskDetails { get; init; } = true;
}
