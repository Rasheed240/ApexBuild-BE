using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingUpdates;

public record GetPendingUpdatesQuery : IRequest<GetPendingUpdatesResponse>
{
    public Guid OrganizationId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
