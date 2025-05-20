using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.AddTaskComment;

public record AddTaskCommentCommand : IRequest<AddTaskCommentResponse>
{
    public Guid TaskId { get; init; }
    public string Comment { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
    public List<string>? AttachmentUrls { get; init; }
}
