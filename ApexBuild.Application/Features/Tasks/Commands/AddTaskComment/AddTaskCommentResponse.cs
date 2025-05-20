namespace ApexBuild.Application.Features.Tasks.Commands.AddTaskComment;

public record AddTaskCommentResponse
{
    public Guid CommentId { get; init; }
    public string Message { get; init; } = string.Empty;
}
