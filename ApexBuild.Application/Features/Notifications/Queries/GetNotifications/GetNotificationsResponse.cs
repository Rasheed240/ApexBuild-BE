using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Notifications.Queries.GetNotifications;

public record NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record GetNotificationsResponse
{
    public List<NotificationDto> Notifications { get; init; } = new();
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

