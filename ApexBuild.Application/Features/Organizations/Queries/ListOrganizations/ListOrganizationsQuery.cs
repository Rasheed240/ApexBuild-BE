using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;

public record ListOrganizationsQuery : IRequest<ListOrganizationsResponse>, ICacheableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool? IsActive { get; init; }
    public bool? IsVerified { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? OwnerId { get; init; }

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Long TTL: orgs change rarely; invalidated on create/update/member events
    public string CacheKey => string.IsNullOrWhiteSpace(SearchTerm)
        ? $"orgs:list:owner:{OwnerId}:active:{IsActive}:verified:{IsVerified}:pg:{PageNumber}:{PageSize}"
        : string.Empty;

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}
