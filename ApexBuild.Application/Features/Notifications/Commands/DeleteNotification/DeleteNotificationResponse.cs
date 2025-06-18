namespace ApexBuild.Application.Features.Notifications.Commands.DeleteNotification;

public record DeleteNotificationResponse
{
    public Guid NotificationId { get; init; }
    public string Message { get; init; } = string.Empty;
}

