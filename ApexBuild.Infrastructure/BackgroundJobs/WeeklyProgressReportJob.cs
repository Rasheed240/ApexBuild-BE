
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
// using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;


namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class WeeklyProgressReportJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IDateTimeService _dateTimeService;

        public WeeklyProgressReportJob(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _dateTimeService = dateTimeService;
        }

        public async Task SendWeeklyProgressReportsAsync()
        {
            var today = _dateTimeService.UtcNow.Date;
            var lastWeek = today.AddDays(-7);

            var projects = await _unitOfWork.Projects.FindAsync(p => p.Status == ProjectStatus.Active);

            foreach (var project in projects)
            {
                // Get project admin and owner
                var recipients = new List<Guid>();
                if (project.ProjectAdminId.HasValue) recipients.Add(project.ProjectAdminId.Value);
                if (project.ProjectOwnerId.HasValue) recipients.Add(project.ProjectOwnerId.Value);

                if (!recipients.Any()) continue;

                // Get tasks updated in the last week
                var projectTasks = await _unitOfWork.Tasks.FindAsync(
                    t => t.Department.ProjectId == project.Id);

                var updatesLastWeek = projectTasks
                    .SelectMany(t => t.Updates)
                    .Where(u => u.SubmittedAt >= lastWeek && u.SubmittedAt <= today)
                    .Count();

                var completedTasksLastWeek = projectTasks
                    .Count(t => t.CompletedAt >= lastWeek && t.CompletedAt <= today);

                var totalTasks = projectTasks.Count();
                var completedTasks = projectTasks.Count(t => t.Status == Domain.Enums.TaskStatus.Completed);
                var progressPercentage = totalTasks > 0 ? (completedTasks * 100.0 / totalTasks) : 0;

                // Send report to each recipient
                foreach (var recipientId in recipients)
                {
                    var recipient = await _unitOfWork.Users.GetByIdAsync(recipientId);
                    if (recipient == null) continue;

                    await _emailService.SendWeeklyProgressReportAsync(
                        recipient.Email,
                        recipient.FullName,
                        project.Name,
                        totalTasks,
                        completedTasks,
                        updatesLastWeek,
                        completedTasksLastWeek,
                        progressPercentage);
                }
            }
        }
    }
}