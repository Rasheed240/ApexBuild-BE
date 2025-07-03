namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardMetrics;

public class GetDashboardMetricsResponse
{
    public ProductivityMetric Productivity { get; set; } = null!;
    public OnTimeDeliveryMetric OnTimeDelivery { get; set; } = null!;
    public TaskCompletionMetric TaskCompletion { get; set; } = null!;
}

public class ProductivityMetric
{
    public int CurrentMonthCompletion { get; set; }
    public int PreviousMonthCompletion { get; set; }
    public decimal PercentageChange { get; set; }
    public string ChangeDirection { get; set; } = "unchanged"; // "increased", "decreased", "unchanged"
}

public class OnTimeDeliveryMetric
{
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public int TotalCompleted { get; set; }
    public decimal PercentageOnTime { get; set; }
}

public class TaskCompletionMetric
{
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    public decimal CompletionRate { get; set; }
}
