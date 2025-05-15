using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByAdmin;

public record ApproveTaskUpdateByAdminResponse
{
    public Guid UpdateId { get; init; }
    public UpdateStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

