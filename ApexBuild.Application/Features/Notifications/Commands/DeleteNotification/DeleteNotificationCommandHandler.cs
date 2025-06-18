using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Notifications.Commands.DeleteNotification;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, DeleteNotificationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteNotificationResponse> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to delete notifications");
        }

        var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null || notification.IsDeleted)
        {
            throw new NotFoundException("Notification", request.NotificationId);
        }

        // Check authorization: User can only delete their own notifications
        if (notification.UserId != currentUserId.Value)
        {
            throw new ForbiddenException("You do not have permission to delete this notification");
        }

        // Soft delete
        notification.IsDeleted = true;
        notification.DeletedAt = DateTime.UtcNow;
        notification.DeletedBy = currentUserId.Value;

        await _unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteNotificationResponse
        {
            NotificationId = notification.Id,
            Message = "Notification deleted successfully"
        };
    }
}

