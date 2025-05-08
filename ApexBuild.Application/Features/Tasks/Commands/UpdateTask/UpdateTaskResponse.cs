namespace ApexBuild.Application.Features.Tasks.Commands.UpdateTask;

public record UpdateTaskResponse
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

