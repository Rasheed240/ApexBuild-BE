namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskComplete;

public record MarkTaskCompleteResponse
{
    public Guid TaskId { get; init; }
    public string Message { get; init; } = string.Empty;
}

