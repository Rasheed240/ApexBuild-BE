using System;

namespace ApexBuild.Application.Features.Activities.Queries.GetRecentActivities;

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // e.g., "task_update", "notification", "project"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid? RelatedProjectId { get; set; }
    public Guid? RelatedTaskId { get; set; }
    public Guid? UserId { get; set; }
    public string? Link { get; set; }
}
