using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskDone;

public record MarkTaskDoneCommand : IRequest<MarkTaskDoneResponse>
{
    public Guid TaskId { get; init; }
    public string? Notes { get; init; }
}
