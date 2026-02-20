using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByContractorAdmin;

public record ApproveTaskUpdateByContractorAdminResponse
{
    public Guid UpdateId { get; init; }
    public UpdateStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}
