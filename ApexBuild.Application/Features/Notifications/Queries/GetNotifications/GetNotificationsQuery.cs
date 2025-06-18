using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery : IRequest<GetNotificationsResponse>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsRead { get; init; }
    public NotificationType? Type { get; init; }
}

