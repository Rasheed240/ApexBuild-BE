using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateBySupervisor;

public record ApproveTaskUpdateBySupervisorCommand : IRequest<ApproveTaskUpdateBySupervisorResponse>
{
    public Guid UpdateId { get; init; }
    public bool Approved { get; init; } = true;
    public string? Feedback { get; init; }
}

