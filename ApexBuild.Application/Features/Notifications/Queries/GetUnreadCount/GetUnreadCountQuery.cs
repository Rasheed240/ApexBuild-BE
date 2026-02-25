using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>
/// This endpoint is polled frequently (health-bar widget in the navbar).
/// A 2-minute cache keeps DB load low while still feeling near-real-time.
/// The cache is invalidated immediately whenever a notification is created or read.
/// The key deliberately does NOT include user-id here because
/// the handler already filters by current user — the CurrentUserService injects
/// user context at handler invocation time.
///
/// IMPORTANT: the CacheKey below must embed the user context so two different
/// users on the same process never see each other's count.
/// We expose a factory method so the handler can supply UserId.
/// </summary>
public record GetUnreadCountQuery : IRequest<GetUnreadCountResponse>, ICacheableQuery
{
    /// <summary>Populated by the handler / controller before dispatching.</summary>
    public Guid? InvokingUserId { get; init; }

    public string CacheKey => $"notifications:unread-count:user:{InvokingUserId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
