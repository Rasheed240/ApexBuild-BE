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
    public class OrganizationLicenseRepository : BaseRepository<OrganizationLicense>, IOrganizationLicenseRepository
    {
        public OrganizationLicenseRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<OrganizationLicense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(l => l.Organization)
                .Include(l => l.User)
                .Include(l => l.Subscription)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        }

        public async Task<OrganizationLicense?> GetByUserAndOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(l => l.Organization)
                .Include(l => l.User)
                .Include(l => l.Subscription)
                .FirstOrDefaultAsync(l => l.OrganizationId == organizationId &&
                                        l.UserId == userId &&
                                        !l.IsDeleted, cancellationToken);
        }

        public async Task<List<OrganizationLicense>> GetByOrganizationIdAsync(
            Guid organizationId,
            LicenseStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<OrganizationLicense> query = _dbSet
                .Where(l => l.OrganizationId == organizationId && !l.IsDeleted)
                .Include(l => l.Organization)
                .Include(l => l.User)
                .Include(l => l.Subscription);

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<OrganizationLicense>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .Include(l => l.Organization)
                .Include(l => l.User)
                .Include(l => l.Subscription)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<OrganizationLicense>> GetActiveLicensesByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => l.OrganizationId == organizationId &&
                       !l.IsDeleted &&
                       l.Status == LicenseStatus.Active &&
                       l.ValidUntil > DateTime.UtcNow)
                .Include(l => l.Organization)
                .Include(l => l.User)
                .Include(l => l.Subscription)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<OrganizationLicense>> GetExpiredLicensesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(l => !l.IsDeleted && l.ValidUntil <= DateTime.UtcNow)
                .Include(l => l.Organization)
                .Include(l => l.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<OrganizationLicense>> GetExpiringLicensesAsync(int daysUntilExpiration = 7, CancellationToken cancellationToken = default)
        {
            var expirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration);
            return await _dbSet
                .Where(l => !l.IsDeleted &&
                       l.ValidUntil > DateTime.UtcNow &&
                       l.ValidUntil <= expirationDate)
                .Include(l => l.Organization)
                .Include(l => l.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetActiveLicenseCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(l => l.OrganizationId == organizationId &&
                           !l.IsDeleted &&
                           l.Status == LicenseStatus.Active &&
                           l.ValidUntil > DateTime.UtcNow, cancellationToken);
        }

        public async Task<bool> HasActiveLicenseAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(l => l.OrganizationId == organizationId &&
                          l.UserId == userId &&
                          !l.IsDeleted &&
                          l.Status == LicenseStatus.Active &&
                          l.ValidUntil > DateTime.UtcNow, cancellationToken);
        }

        public async Task<List<(User User, OrganizationLicense License)>> GetOrganizationUsersWithLicensesAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var licenses = await _dbSet
                .Where(l => l.OrganizationId == organizationId &&
                       !l.IsDeleted &&
                       l.Status == LicenseStatus.Active)
                .Include(l => l.User)
                .ToListAsync(cancellationToken);

            return licenses.Select(l => (l.User, l)).ToList();
        }
    }
}
