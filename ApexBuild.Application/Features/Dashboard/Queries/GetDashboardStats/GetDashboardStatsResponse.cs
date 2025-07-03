namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public class GetDashboardStatsResponse
    {
        public int ActiveProjects { get; set; }
        public int TeamMembers { get; set; }
        public int CompletedTasks { get; set; }
        public int UpcomingDeadlines { get; set; }
        public int PendingReviews { get; set; }
        public int TotalTasks { get; set; }
    }
}
