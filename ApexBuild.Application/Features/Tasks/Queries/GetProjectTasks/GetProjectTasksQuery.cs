using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Queries.GetProjectTasks;

public record GetProjectTasksQuery : IRequest<GetProjectTasksResponse>, ICacheableQuery
{
    public Guid ProjectId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? SearchTerm { get; init; }
    public bool IncludeSubtasks { get; init; } = true;
    public bool IncludeUpdates { get; init; } = true;

    // ── ICacheableQuery ───────────────────────────────────────────────────────
    // Bypass cache for search; cache filtered task-list pages per project
    public string CacheKey => string.IsNullOrWhiteSpace(SearchTerm)
        ? $"tasks:project:{ProjectId}:status:{Status}:priority:{Priority}:dept:{DepartmentId}:user:{AssignedToUserId}:sub:{IncludeSubtasks}:upd:{IncludeUpdates}:pg:{PageNumber}:{PageSize}"
        : string.Empty;

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
