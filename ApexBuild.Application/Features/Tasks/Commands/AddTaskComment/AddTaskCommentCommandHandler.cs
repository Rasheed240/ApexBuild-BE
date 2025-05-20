using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Tasks.Commands.AddTaskComment;

public class AddTaskCommentCommandHandler : IRequestHandler<AddTaskCommentCommand, AddTaskCommentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddTaskCommentCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AddTaskCommentResponse> Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId);
        if (task == null)
            throw new Exception($"Task with ID {request.TaskId} not found");

        var comment = new TaskComment
        {
            TaskId = request.TaskId,
            UserId = _currentUserService.UserId ?? Guid.Empty,
            Comment = request.Comment,
            ParentCommentId = request.ParentCommentId,
            AttachmentUrls = request.AttachmentUrls
        };

        await _unitOfWork.TaskComments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddTaskCommentResponse
        {
            CommentId = comment.Id,
            Message = "Comment added successfully"
        };
    }
}
