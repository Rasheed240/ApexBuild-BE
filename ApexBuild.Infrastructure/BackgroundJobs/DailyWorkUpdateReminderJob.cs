
using Hangfire;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class DailyWorkUpdateReminderJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IDateTimeService _dateTimeService;

        public DailyWorkUpdateReminderJob(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _dateTimeService = dateTimeService;
        }

        public async Task SendDailyWorkUpdateRemindersAsync()
        {
            var today = _dateTimeService.UtcNow.Date;

            // Only send on workdays (Monday-Friday)
            if (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            {
                return;
            }

            // Get all active tasks with assigned workers
            var allTasks = await _unitOfWork.Tasks.FindAsync(
                t => t.Status == Domain.Enums.TaskStatus.InProgress &&
                     t.AssignedToUserId != null);

            var tasksGroupedByUser = allTasks
                .GroupBy(t => t.AssignedToUserId!.Value)
                .ToList();

            foreach (var userTasks in tasksGroupedByUser)
            {
                var userId = userTasks.Key;
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null) continue;

                // Check if user has submitted any update today
                var hasSubmittedToday = userTasks
                    .SelectMany(t => t.Updates)
                    .Any(u => u.SubmittedAt.Date == today);

                if (!hasSubmittedToday)
                {
                    var taskTitles = string.Join(", ", userTasks.Select(t => t.Title).Take(3));
                    var moreTasksCount = userTasks.Count() - 3;
                    var tasksList = moreTasksCount > 0
                        ? $"{taskTitles} and {moreTasksCount} more"
                        : taskTitles;

                    await _emailService.SendDailyUpdateReminderAsync(
                        user.Email,
                        user.FullName,
                        userTasks.Count(),
                        tasksList);
                }
            }
        }
    }
}