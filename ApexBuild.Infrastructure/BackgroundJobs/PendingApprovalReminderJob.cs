using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class PendingApprovalReminderJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public PendingApprovalReminderJob(
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task SendPendingApprovalRemindersAsync()
        {
            var departments = await _unitOfWork.Departments.GetAllAsync();

            foreach (var department in departments)
            {
                if (department.SupervisorId == null) continue;

                var departmentWithTasks = await _unitOfWork.Departments.GetWithTasksAsync(department.Id);

                if (departmentWithTasks == null) continue;

                var pendingUpdatesCount = departmentWithTasks.Tasks
                    .SelectMany(t => t.Updates)
                    .Count(u => u.Status == UpdateStatus.Submitted ||
                               u.Status == UpdateStatus.UnderSupervisorReview);

                if (pendingUpdatesCount > 0)
                {
                    await _notificationService.SendNotificationAsync(
                        department.SupervisorId.Value,
                        "Pending Updates for Review",
                        $"You have {pendingUpdatesCount} update(s) pending review in {department.Name}.",
                        NotificationType.PendingApproval,
                        NotificationChannel.Both,
                        department.Id,
                        "Department");
                }
            }
        }
    }
}
