using MediatR;

namespace ApexBuild.Application.Features.Notifications.Commands.DeleteNotification;

public record DeleteNotificationCommand : IRequest<DeleteNotificationResponse>
{
    public Guid NotificationId { get; init; }
}

