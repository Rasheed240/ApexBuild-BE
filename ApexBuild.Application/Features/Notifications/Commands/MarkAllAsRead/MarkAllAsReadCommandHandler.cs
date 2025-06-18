using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, MarkAllAsReadResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkAllAsReadCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<MarkAllAsReadResponse> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to mark notifications as read");
        }

        // Get unread count before marking
        var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(currentUserId.Value, cancellationToken);

        // Mark all as read
        await _unitOfWork.Notifications.MarkAllAsReadAsync(currentUserId.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarkAllAsReadResponse
        {
            MarkedCount = unreadCount,
            Message = $"{unreadCount} notification(s) marked as read"
        };
    }
}

