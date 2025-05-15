using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateBySupervisor;

public record ApproveTaskUpdateBySupervisorResponse
{
    public Guid UpdateId { get; init; }
    public UpdateStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

