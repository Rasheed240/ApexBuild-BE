using System;

namespace ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

public class ProjectProgressDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int Progress { get; set; } // 0-100
}
