namespace ApexBuild.Application.Features.Notifications.Commands.MarkAllAsRead;

public record MarkAllAsReadResponse
{
    public int MarkedCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

