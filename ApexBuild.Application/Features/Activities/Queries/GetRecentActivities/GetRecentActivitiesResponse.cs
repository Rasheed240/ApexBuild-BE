using System.Collections.Generic;

namespace ApexBuild.Application.Features.Activities.Queries.GetRecentActivities;

public class GetRecentActivitiesResponse
{
    public List<RecentActivityDto> Items { get; set; } = new List<RecentActivityDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
