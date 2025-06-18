using MediatR;

namespace ApexBuild.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkAsReadCommand : IRequest<MarkAsReadResponse>
{
    public Guid NotificationId { get; init; }
}

