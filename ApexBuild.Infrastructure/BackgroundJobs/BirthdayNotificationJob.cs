
using System.Linq.Expressions;
using Hangfire;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Infrastructure.BackgroundJobs
{
    public class BirthdayNotificationJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeService _dateTimeService;

        public BirthdayNotificationJob(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _dateTimeService = dateTimeService;
        }

        public async Task SendBirthdayNotificationsAsync()
        {
            var today = _dateTimeService.UtcNow.Date;

            // Get all active users
            var allUsers = await _unitOfWork.Users.FindAsync(u => u.Status == UserStatus.Active);

            foreach (var user in allUsers)
            {
                // Check if today is user's birthday (assuming DateOfBirth field exists)
                // Since we don't have DateOfBirth in current model, this would need to be added
                // For now, this is a placeholder implementation

                // You would need to add DateOfBirth field to User entity:
                // public DateTime? DateOfBirth { get; set; }

                // Then check:
                // if (user.DateOfBirth.HasValue && 
                //     user.DateOfBirth.Value.Month == today.Month && 
                //     user.DateOfBirth.Value.Day == today.Day)
                // {
                //     // Send birthday notification to all users in same projects
                //     var projectUsers = await GetProjectColleaguesAsync(user.Id);
                //     
                //     foreach (var colleague in projectUsers)
                //     {
                //         await _notificationService.SendNotificationAsync(
                //             colleague.Id,
                //             $"🎉 Birthday Celebration",
                //             $"Today is {user.FullName}'s birthday! Wish them a happy birthday!",
                //             NotificationType.BirthdayNotification,
                //             NotificationChannel.InApp,
                //             user.Id,
                //             "User");
                //     }
                // }
            }
        }

        private async Task<List<Domain.Entities.User>> GetProjectColleaguesAsync(Guid userId)
        {
            var userProjects = await _unitOfWork.Projects.GetProjectsByUserAsync(userId);
            var colleagues = new HashSet<Domain.Entities.User>();

            foreach (var project in userProjects)
            {
                var projectUsers = await _unitOfWork.Users.GetUsersByProjectAsync(project.Id);
                foreach (var projectUser in projectUsers.Where(u => u.Id != userId))
                {
                    colleagues.Add(projectUser);
                }
            }

            return colleagues.ToList();
        }
    }
}