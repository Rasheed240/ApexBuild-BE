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
        public int OverdueTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int NotStartedTasks { get; set; }
        public int UnderReviewTasks { get; set; }
        public int RejectedTasks { get; set; }
    }
}
