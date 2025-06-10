using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace ApexBuild.Infrastructure.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await GetByIdAsync(notificationId, cancellationToken);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await UpdateAsync(notification, cancellationToken);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            if (notifications.Any())
            {
                await UpdateRangeAsync(notifications, cancellationToken);
            }
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserAsync(
            Guid userId,
            bool? isRead = null,
            Domain.Enums.NotificationType? type = null,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}