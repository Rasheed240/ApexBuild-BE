using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Guid userId, string title, string message, Domain.Enums.NotificationType type, Domain.Enums.NotificationChannel channel, Guid? relatedEntityId = null, string? relatedEntityType = null, Dictionary<string, object>? metaData = null, string? actionUrl = null);
        Task SendBulkNotificationAsync(List<Guid> userIds, string title, string message, Domain.Enums.NotificationType type, Domain.Enums.NotificationChannel channel);
        Task NotifyUserAsync(Guid userId, string title, string message, Domain.Enums.NotificationType type = Domain.Enums.NotificationType.TaskUpdate, Guid? relatedEntityId = null, string? relatedEntityType = null, Dictionary<string, object>? metaData = null, string? actionUrl = null);
    }
}