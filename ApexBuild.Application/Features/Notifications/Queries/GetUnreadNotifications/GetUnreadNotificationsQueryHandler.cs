using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Notifications.Queries.GetUnreadNotifications;

public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, GetUnreadNotificationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetUnreadNotificationsResponse> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to view notifications");
        }

        var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsByUserAsync(
            currentUserId.Value, 
            cancellationToken);

        var notificationDtos = notifications.Select(n => new UnreadNotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            RelatedEntityId = n.RelatedEntityId,
            RelatedEntityType = n.RelatedEntityType,
            ActionUrl = n.ActionUrl,
            CreatedAt = n.CreatedAt
        }).ToList();

        return new GetUnreadNotificationsResponse
        {
            Notifications = notificationDtos,
            Count = notificationDtos.Count
        };
    }
}

