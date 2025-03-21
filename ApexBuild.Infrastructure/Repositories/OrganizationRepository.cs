using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Infrastructure.Repositories
{
    public class OrganizationRepository : BaseRepository<Organization>, IOrganizationRepository
    {
        public OrganizationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Organization?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(o => o.Code.ToLower() == code.ToLower() && !o.IsDeleted, cancellationToken);
        }

        public async Task<Organization?> GetWithMembersAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Owner)
                .Include(o => o.Members)
                    .ThenInclude(m => m.User)
                .Include(o => o.Departments)
                .FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Organization>> GetOrganizationsByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(o => o.OwnerId == ownerId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}