using Hangfire;
using System;

namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class BackgroundJobScheduler
    {
        public static void ScheduleRecurringJobs()
        {
            // Run deadline reminders every day at 8 AM
            RecurringJob.AddOrUpdate<DeadlineReminderJob>(
                "deadline-reminders",
                job => job.SendDeadlineRemindersAsync(),
                "0 8 * * *", // Cron: Every day at 8 AM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Run pending approval reminders every day at 9 AM and 3 PM
            RecurringJob.AddOrUpdate<PendingApprovalReminderJob>(
                "pending-approval-reminders-morning",
                job => job.SendPendingApprovalRemindersAsync(),
                "0 9 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            RecurringJob.AddOrUpdate<PendingApprovalReminderJob>(
                "pending-approval-reminders-afternoon",
                job => job.SendPendingApprovalRemindersAsync(),
                "0 15 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Clean up expired invitations every day at midnight
            RecurringJob.AddOrUpdate<ExpiredInvitationCleanupJob>(
                "cleanup-expired-invitations",
                job => job.CleanupExpiredInvitationsAsync(),
                "0 0 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Send daily work update reminders at 6 PM on workdays
            RecurringJob.AddOrUpdate<DailyWorkUpdateReminderJob>(
                "daily-work-update-reminders",
                job => job.SendDailyWorkUpdateRemindersAsync(),
                "0 18 * * 1-5", // Monday to Friday at 6 PM
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Send birthday notifications every day at 9 AM
            RecurringJob.AddOrUpdate<BirthdayNotificationJob>(
                "birthday-notifications",
                job => job.SendBirthdayNotificationsAsync(),
                "0 9 * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });

            // Send weekly progress reports every Monday at 8 AM
            // RecurringJob.AddOrUpdate<WeeklyProgressReportJob>(
            //     "weekly-progress-reports",
            //     job => job.SendWeeklyProgressReportsAsync(),
            //     "0 8 * * 1", // Every Monday at 8 AM
            //     new RecurringJobOptions
            //     {
            //         TimeZone = TimeZoneInfo.Utc
            //     });
        }
    }
}
