using System.Linq.Expressions;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public NotificationService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task SendNotificationAsync(
            Guid userId,
            string title,
            string message,
            Domain.Enums.NotificationType type,
            Domain.Enums.NotificationChannel channel,
            Guid? relatedEntityId = null,
            string? relatedEntityType = null,
            Dictionary<string, object>? metaData = null,
            string? actionUrl = null)
        {
            // Create in-app notification
            if (channel == Domain.Enums.NotificationChannel.InApp ||
                channel == Domain.Enums.NotificationChannel.Both)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    Channel = channel,
                    RelatedEntityId = relatedEntityId,
                    RelatedEntityType = relatedEntityType,
                    ActionUrl = actionUrl,
                    MetaData = metaData
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }

            // Send email notification
            if (channel == Domain.Enums.NotificationChannel.Email ||
                channel == Domain.Enums.NotificationChannel.Both)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null)
                {
                    await _emailService.SendEmailAsync(user.Email, title, message);
                }
            }
        }

        public async Task SendBulkNotificationAsync(
            List<Guid> userIds,
            string title,
            string message,
            Domain.Enums.NotificationType type,
            Domain.Enums.NotificationChannel channel)
        {
            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Channel = channel
            }).ToList();

            await _unitOfWork.Notifications.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            // Send emails if needed
            if (channel == Domain.Enums.NotificationChannel.Email ||
                channel == Domain.Enums.NotificationChannel.Both)
            {
                var users = await Task.WhenAll(userIds.Select(id => _unitOfWork.Users.GetByIdAsync(id)));

                foreach (var user in users.Where(u => u != null))
                {
                    await _emailService.SendEmailAsync(user!.Email, title, message);
                }
            }
        }

        public async Task NotifyUserAsync(
            Guid userId,
            string title,
            string message,
            Domain.Enums.NotificationType type = Domain.Enums.NotificationType.TaskUpdate,
            Guid? relatedEntityId = null,
            string? relatedEntityType = null,
            Dictionary<string, object>? metaData = null,
            string? actionUrl = null)
        {
            // Default to InApp notification
            await SendNotificationAsync(userId, title, message, type, Domain.Enums.NotificationChannel.InApp, relatedEntityId, relatedEntityType, metaData, actionUrl);
        }
    }
}