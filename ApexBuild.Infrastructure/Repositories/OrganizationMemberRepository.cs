using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class OrganizationMemberRepository : BaseRepository<OrganizationMember>, IOrganizationMemberRepository
    {
        public OrganizationMemberRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrganizationMember>> GetMembersByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.OrganizationId == organizationId && m.IsActive)
                .Include(m => m.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationMember?> GetMemberAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.User)
                .FirstOrDefaultAsync(
                    m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive,
                    cancellationToken);
        }

        public async Task<bool> IsMemberAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(
                m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive,
                cancellationToken);
        }
    }
}
