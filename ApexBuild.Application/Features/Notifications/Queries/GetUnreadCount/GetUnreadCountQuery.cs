using MediatR;

namespace ApexBuild.Application.Features.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<GetUnreadCountResponse>
{
}

