using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface INotificationRepository : IRepository<Domain.Entities.Notification>
    {
        Task<IEnumerable<Domain.Entities.Notification>> GetUnreadNotificationsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Notification>> GetNotificationsByUserAsync(
            Guid userId, 
            bool? isRead = null,
            Domain.Enums.NotificationType? type = null,
            int? limit = null,
            CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}