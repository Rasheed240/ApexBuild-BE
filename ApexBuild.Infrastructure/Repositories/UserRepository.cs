using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Project)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetWithWorkInfoAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.WorkInfos.Where(w => w.ProjectId == projectId && w.IsActive))
                    .ThenInclude(w => w.Department)
                .Include(u => u.WorkInfos.Where(w => w.ProjectId == projectId && w.IsActive))
                    .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.ProjectUsers)
                .Include(u => u.WorkInfos.Where(w => w.ProjectId == projectId))
                .Where(u => u.ProjectUsers.Any(pu => pu.ProjectId == projectId && pu.Status == Domain.Enums.ProjectUserStatus.Active)
                            && !u.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> GetUsersByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _context.OrganizationMembers
                .Where(om => om.OrganizationId == organizationId && om.IsActive)
                .Select(om => om.User)
                .Where(u => !u.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);
        }
    }

}