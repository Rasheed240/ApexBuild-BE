using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Queries.ListProjects;

public record ListProjectsQuery : IRequest<ListProjectsResponse>, ICacheableQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public ProjectStatus? Status { get; init; }
    public string? ProjectType { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? OwnerId { get; init; }

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Key encodes every discriminator so different filter combinations never
    // share the same cache entry. SearchTerm purposely bypasses cache to avoid
    // caching the full cartesian product of user-typed strings.
    public string CacheKey => string.IsNullOrWhiteSpace(SearchTerm)
        ? $"projects:list:owner:{OwnerId}:status:{Status}:type:{ProjectType}:pg:{PageNumber}:{PageSize}"
        : string.Empty; // empty key = skip cache for search requests

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
