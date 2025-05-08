namespace ApexBuild.Application.Features.Tasks.Commands.CreateTask;

public record CreateTaskResponse
{
    public Guid TaskId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

