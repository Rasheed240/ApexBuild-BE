namespace ApexBuild.Application.Features.Tasks.Commands.DeleteTask;

public record DeleteTaskResponse
{
    public Guid TaskId { get; init; }
    public string Message { get; init; } = string.Empty;
}

