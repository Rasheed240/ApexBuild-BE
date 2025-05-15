using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.SubmitTaskUpdate;

public record SubmitTaskUpdateResponse
{
    public Guid UpdateId { get; init; }
    public Guid TaskId { get; init; }
    public UpdateStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

