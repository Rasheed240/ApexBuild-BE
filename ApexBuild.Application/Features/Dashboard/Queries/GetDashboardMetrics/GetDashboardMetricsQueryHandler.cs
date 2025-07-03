using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using MediatR;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardMetrics
{
    public class GetDashboardMetricsQueryHandler : IRequestHandler<GetDashboardMetricsQuery, GetDashboardMetricsResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetDashboardMetricsQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<GetDashboardMetricsResponse> Handle(
            GetDashboardMetricsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            var orgIds = _currentUserService.GetOrganizationIds();

            var now = DateTime.UtcNow;
            var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
            var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);

            // Get all tasks and projects for filtering
            var allTasks = await _unitOfWork.Tasks.GetAllAsync(cancellationToken);
            var allProjects = await _unitOfWork.Projects.GetAllAsync(cancellationToken);
            
            // Filter tasks by organization through project relationship
            var orgProjectIds = allProjects.Where(p => orgIds.Contains(p.OrganizationId)).Select(p => p.Id).ToList();
            var tasks = allTasks.Where(t => orgProjectIds.Contains(t.ProjectId)).ToList();

            // 1. PRODUCTIVITY METRIC
            var currentMonthCompleted = tasks.Count(t =>
                t.Status == TaskStatus.Completed &&
                t.CompletedAt.HasValue &&
                t.CompletedAt >= startOfCurrentMonth &&
                t.CompletedAt <= now
            );

            var previousMonthCompleted = tasks.Count(t =>
                t.Status == TaskStatus.Completed &&
                t.CompletedAt.HasValue &&
                t.CompletedAt >= startOfPreviousMonth &&
                t.CompletedAt < startOfCurrentMonth
            );

            decimal percentageChange = 0;
            string changeDirection = "unchanged";

            if (previousMonthCompleted > 0)
            {
                percentageChange = ((currentMonthCompleted - previousMonthCompleted) * 100.0m / previousMonthCompleted);
                changeDirection = percentageChange > 0 ? "increased" : percentageChange < 0 ? "decreased" : "unchanged";
            }
            else if (currentMonthCompleted > 0)
            {
                percentageChange = 100;
                changeDirection = "increased";
            }

            // 2. ON-TIME DELIVERY METRIC
            var completedTasks = tasks.Where(t =>
                t.Status == TaskStatus.Completed &&
                t.CompletedAt.HasValue
            );

            var onTimeTasks = completedTasks.Count(t =>
                t.DueDate.HasValue && t.CompletedAt <= t.DueDate
            );

            var lateTasks = completedTasks.Count(t =>
                t.DueDate.HasValue && t.CompletedAt > t.DueDate
            );

            var totalCompleted = completedTasks.Count();
            var percentageOnTime = totalCompleted > 0 ? (onTimeTasks * 100.0m / totalCompleted) : 0;

            // 3. TASK COMPLETION METRIC
            var allTasksCount = tasks.Count;
            var completedTasksCount = tasks.Count(t => t.Status == TaskStatus.Completed);
            var completionRate = allTasksCount > 0 ? (completedTasksCount * 100.0m / allTasksCount) : 0;

            return new GetDashboardMetricsResponse
            {
                Productivity = new ProductivityMetric
                {
                    CurrentMonthCompletion = currentMonthCompleted,
                    PreviousMonthCompletion = previousMonthCompleted,
                    PercentageChange = Math.Round(percentageChange, 1),
                    ChangeDirection = changeDirection
                },
                OnTimeDelivery = new OnTimeDeliveryMetric
                {
                    OnTimeCount = onTimeTasks,
                    LateCount = lateTasks,
                    TotalCompleted = totalCompleted,
                    PercentageOnTime = Math.Round(percentageOnTime, 1)
                },
                TaskCompletion = new TaskCompletionMetric
                {
                    CompletedTasks = completedTasksCount,
                    TotalTasks = allTasksCount,
                    CompletionRate = Math.Round(completionRate, 1)
                }
            };
        }
    }
}
