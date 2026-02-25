using MediatR;
using ApexBuild.Application.Common.Interfaces;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetMyTasks;

public record GetMyTasksQuery : IRequest<GetMyTasksResponse>, ICacheableQuery
{
    public Guid? OrganizationId { get; init; }
    public Guid? ProjectId { get; init; }
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public string? SearchTerm { get; init; }
    public bool? IsOverdue { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Short TTL: "My Tasks" is a primary work surface — must feel near-real-time.
    // Cache key includes org+project+status so multi-org users get isolated entries.
    public string CacheKey => string.IsNullOrWhiteSpace(SearchTerm)
        ? $"tasks:my:org:{OrganizationId}:proj:{ProjectId}:status:{Status}:priority:{Priority}:overdue:{IsOverdue}:pg:{PageNumber}:{PageSize}"
        : string.Empty;

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
