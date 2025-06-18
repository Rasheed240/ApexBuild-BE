using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, MarkAsReadResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkAsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<MarkAsReadResponse> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to mark notifications as read");
        }

        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null || notification.IsDeleted)
        {
            throw new NotFoundException("Notification", request.NotificationId);
        }

        // Check authorization: User can only mark their own notifications as read
        if (notification.UserId != currentUserId.Value)
        {
            throw new ForbiddenException("You do not have permission to modify this notification");
        }

        // Mark as read if not already read
        if (!notification.IsRead)
        {
            await _unitOfWork.Notifications.MarkAsReadAsync(request.NotificationId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new MarkAsReadResponse
        {
            NotificationId = notification.Id,
            Message = "Notification marked as read"
        };
    }
}

