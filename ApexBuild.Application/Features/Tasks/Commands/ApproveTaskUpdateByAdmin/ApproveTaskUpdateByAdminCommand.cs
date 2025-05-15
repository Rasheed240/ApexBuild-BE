using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByAdmin;

public record ApproveTaskUpdateByAdminCommand : IRequest<ApproveTaskUpdateByAdminResponse>
{
    public Guid UpdateId { get; init; }
    public bool Approved { get; init; } = true;
    public string? Feedback { get; init; }
}

