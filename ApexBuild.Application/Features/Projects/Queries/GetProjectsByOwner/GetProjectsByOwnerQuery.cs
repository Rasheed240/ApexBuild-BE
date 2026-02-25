using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByOwner;

public record GetProjectsByOwnerQuery : IRequest<GetProjectsByOwnerResponse>, ICacheableQuery
{
    public Guid? OwnerId { get; init; } // If null, uses current user

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    public string CacheKey => $"projects:owner:{OwnerId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
