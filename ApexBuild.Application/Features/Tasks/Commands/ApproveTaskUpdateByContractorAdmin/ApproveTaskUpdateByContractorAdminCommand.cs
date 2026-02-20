using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByContractorAdmin;

public record ApproveTaskUpdateByContractorAdminCommand : IRequest<ApproveTaskUpdateByContractorAdminResponse>
{
    public Guid UpdateId { get; init; }
    public bool Approved { get; init; } = true;
    public string? Feedback { get; init; }
}
