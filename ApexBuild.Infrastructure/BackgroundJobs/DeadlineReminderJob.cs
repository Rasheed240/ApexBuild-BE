using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class DeadlineReminderJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public DeadlineReminderJob(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task SendDeadlineRemindersAsync()
        {
            // Get tasks due in 3 days
            var tasksDueIn3Days = await _unitOfWork.Tasks.GetTasksDueInDaysAsync(3);

            foreach (var task in tasksDueIn3Days)
            {
                if (task.AssignedToUser != null)
                {
                    await _emailService.SendDeadlineReminderAsync(
                        task.AssignedToUser.Email,
                        task.AssignedToUser.FullName,
                        task.Title,
                        task.DueDate!.Value);

                    await _notificationService.SendNotificationAsync(
                        task.AssignedToUserId!.Value,
                        "Task Deadline Approaching",
                        $"Your task '{task.Title}' is due in 3 days.",
                        NotificationType.DeadlineReminder,
                        NotificationChannel.InApp,
                        task.Id,
                        "Task");
                }
            }

            // Get tasks due tomorrow
            var tasksDueTomorrow = await _unitOfWork.Tasks.GetTasksDueInDaysAsync(1);

            foreach (var task in tasksDueTomorrow)
            {
                if (task.AssignedToUser != null)
                {
                    await _emailService.SendDeadlineReminderAsync(
                        task.AssignedToUser.Email,
                        task.AssignedToUser.FullName,
                        task.Title,
                        task.DueDate!.Value);

                    await _notificationService.SendNotificationAsync(
                        task.AssignedToUserId!.Value,
                        "Task Due Tomorrow",
                        $"Your task '{task.Title}' is due tomorrow!",
                        NotificationType.DeadlineReminder,
                        NotificationChannel.Both,
                        task.Id,
                        "Task");
                }
            }

            // Get overdue tasks
            var overdueTasks = await _unitOfWork.Tasks.GetOverdueTasksAsync();

            foreach (var task in overdueTasks)
            {
                if (task.AssignedToUser != null)
                {
                    await _notificationService.SendNotificationAsync(
                        task.AssignedToUserId!.Value,
                        "Task Overdue",
                        $"Your task '{task.Title}' is overdue!",
                        NotificationType.DeadlineReminder,
                        NotificationChannel.Both,
                        task.Id,
                        "Task");
                }
            }
        }
    }
}