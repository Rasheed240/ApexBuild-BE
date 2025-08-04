using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for Subscription entities.
    /// </summary>
    public interface ISubscriptionRepository : IRepository<Subscription>
    {
        Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default); // For webhook lookups
        Task<List<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
        Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration, CancellationToken cancellationToken = default);
        Task<List<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}
