using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery : IRequest<GetProjectByIdResponse>, ICacheableQuery
{
    public Guid ProjectId { get; init; }

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Long TTL: project detail rarely changes; invalidated on update/delete
    public string CacheKey => $"project:{ProjectId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}
