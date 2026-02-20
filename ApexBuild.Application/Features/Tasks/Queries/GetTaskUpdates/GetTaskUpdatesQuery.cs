using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskUpdates;

public record GetTaskUpdatesQuery : IRequest<GetTaskUpdatesResponse>
{
    public Guid TaskId { get; init; }
}
