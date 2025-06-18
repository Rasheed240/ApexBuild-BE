using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Notifications.Queries.GetUnreadNotifications;

public record UnreadNotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record GetUnreadNotificationsResponse
{
    public List<UnreadNotificationDto> Notifications { get; init; } = new();
    public int Count { get; init; }
}

