using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.DeleteTask;

public record DeleteTaskCommand : IRequest<DeleteTaskResponse>
{
    public Guid TaskId { get; init; }
}

