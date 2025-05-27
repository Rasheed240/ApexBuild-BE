using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskById;

public record GetTaskByIdQuery : IRequest<GetTaskByIdResponse>
{
    public Guid TaskId { get; init; }
}

