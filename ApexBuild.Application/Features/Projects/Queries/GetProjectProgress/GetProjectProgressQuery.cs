using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectProgress;

public record GetProjectProgressQuery(Guid ProjectId) : IRequest<ProjectProgressDto>, ICacheableQuery
{
    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Short TTL: progress % changes frequently as tasks are updated
    public string CacheKey => $"project-progress:{ProjectId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);
}
