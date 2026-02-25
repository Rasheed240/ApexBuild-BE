using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery : IRequest<GetDashboardStatsResponse>, ICacheableQuery
{
    public Guid? OrganizationId { get; init; }

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Short TTL: dashboard aggregates task/project counts that change often
    public string CacheKey => $"dashboard:stats:org:{OrganizationId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}
