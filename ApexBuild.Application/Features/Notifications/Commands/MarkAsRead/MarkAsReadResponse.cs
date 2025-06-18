namespace ApexBuild.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkAsReadResponse
{
    public Guid NotificationId { get; init; }
    public string Message { get; init; } = string.Empty;
}

