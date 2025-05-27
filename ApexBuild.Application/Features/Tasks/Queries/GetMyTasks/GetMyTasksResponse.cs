using ApexBuild.Application.Features.Tasks.Common;

namespace ApexBuild.Application.Features.Tasks.Queries.GetMyTasks;

public class GetMyTasksResponse
{
    public List<TaskDto> Tasks { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
