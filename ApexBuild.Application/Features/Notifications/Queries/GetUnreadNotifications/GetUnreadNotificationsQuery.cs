using MediatR;

namespace ApexBuild.Application.Features.Notifications.Queries.GetUnreadNotifications;

public record GetUnreadNotificationsQuery : IRequest<GetUnreadNotificationsResponse>
{
}

