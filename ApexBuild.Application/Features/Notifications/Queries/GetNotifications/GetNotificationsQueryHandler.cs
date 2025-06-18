using System.Linq.Expressions;
using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, GetNotificationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetNotificationsResponse> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to view notifications");
        }

        // Build predicate for filtering
        Expression<Func<Notification, bool>> predicate = n => n.UserId == currentUserId.Value && !n.IsDeleted &&
            (!request.IsRead.HasValue || n.IsRead == request.IsRead.Value) &&
            (!request.Type.HasValue || n.Type == request.Type.Value);

        // Get paginated results
        var (items, totalCount) = await _unitOfWork.Notifications.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate,
            q => q.OrderByDescending(n => n.CreatedAt));

        // Get unread count
        var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(currentUserId.Value, cancellationToken);

        var notificationDtos = items.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            RelatedEntityId = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            ActionUrl = n.ActionUrl,
            CreatedAt = n.CreatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new GetNotificationsResponse
        {
            Notifications = notificationDtos,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1,
            HasNextPage = request.PageNumber < totalPages
        };
    }
}

