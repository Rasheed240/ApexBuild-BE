using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskComplete;

public record MarkTaskCompleteCommand : IRequest<MarkTaskCompleteResponse>
{
    public Guid TaskId { get; init; }
}

