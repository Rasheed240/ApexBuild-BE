using MediatR;
using ApexBuild.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskComments;

public class GetTaskCommentsQueryHandler : IRequestHandler<GetTaskCommentsQuery, GetTaskCommentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTaskCommentsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetTaskCommentsResponse> Handle(GetTaskCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.TaskComments.GetAll()
            .Where(c => c.TaskId == request.TaskId && !c.IsDeleted && c.ParentCommentId == null);

        var totalCount = await query.CountAsync();

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(c => c.User)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .ThenInclude(r => r.User)
            .ToListAsync();

        var commentsDto = comments.Select(c => new CommentDto
        {
            Id = c.Id,
            Comment = c.Comment,
            UserId = c.UserId,
            UserName = c.User?.FullName,
            UserProfileImage = c.User?.ProfileImageUrl,
            CreatedAt = c.CreatedAt,
            AttachmentUrls = c.AttachmentUrls,
            Replies = c.Replies
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new CommentDto
                {
                    Id = r.Id,
                    Comment = r.Comment,
                    UserId = r.UserId,
                    UserName = r.User?.FullName,
                    UserProfileImage = r.User?.ProfileImageUrl,
                    CreatedAt = r.CreatedAt,
                    AttachmentUrls = r.AttachmentUrls
                })
                .ToList()
        }).ToList();

        return new GetTaskCommentsResponse
        {
            Comments = commentsDto,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
