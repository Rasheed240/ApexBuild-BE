using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Organization)
                .Include(s => s.User)
                .Include(s => s.OrganizationLicenses)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
        }

        public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Organization)
                .Include(s => s.User)
                .Include(s => s.OrganizationLicenses)
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && !s.IsDeleted, cancellationToken);
        }

        public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Organization)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId && !s.IsDeleted, cancellationToken);
        }

        public async Task<List<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.BillingEndDate <= DateTime.UtcNow)
                .Include(s => s.Organization)
                .Include(s => s.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetExpiringSubscriptionsAsync(int daysUntilExpiration, CancellationToken cancellationToken = default)
        {
            var expirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration);
            return await _dbSet
                .Where(s => !s.IsDeleted &&
                       s.BillingEndDate > DateTime.UtcNow &&
                       s.BillingEndDate <= expirationDate)
                .Include(s => s.Organization)
                .Include(s => s.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.Status == SubscriptionStatus.Active)
                .Include(s => s.Organization)
                .Include(s => s.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(s => s.OrganizationId == organizationId && !s.IsDeleted, cancellationToken);
        }
    }
}
