using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendEmailConfirmationAsync(string email, string fullName, string confirmationToken);
        Task SendPasswordResetAsync(string email, string fullName, string resetToken);
        Task SendInvitationAsync(string email, string inviterName, string roleName, string? projectName, string invitationUrl, string? message);
        Task SendTaskAssignedAsync(string email, string fullName, string taskTitle, string projectName);
        Task SendUpdateSubmittedAsync(string email, string fullName, string taskTitle, string submitterName);
        Task SendUpdateReviewedAsync(string email, string fullName, string taskTitle, bool approved, string? feedback);
        Task SendDeadlineReminderAsync(string email, string fullName, string taskTitle, DateTime dueDate);
        Task SendDailyUpdateReminderAsync(string email, string fullName, int taskCount, string tasksList);
        Task SendWeeklyProgressReportAsync(
            string email,
            string fullName,
            string projectName,
            int totalTasks,
            int completedTasks,
            int updatesLastWeek,
            int completedTasksLastWeek,
            double progressPercentage);
            
    }
}