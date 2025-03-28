using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace ApexBuild.Infrastructure.Repositories
{
    public class InvitationRepository : BaseRepository<Invitation>, IInvitationRepository
    {
        public InvitationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(i => i.InvitedByUser)
                .Include(i => i.Role)
                .Include(i => i.Project)
                .Include(i => i.Organization)
                .Include(i => i.Department)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
        }

        public async Task<IEnumerable<Invitation>> GetPendingInvitationsByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(i => i.InvitedByUser)
                .Include(i => i.Role)
                .Include(i => i.Project)
                .Where(i => i.Email.ToLower() == email.ToLower()
                            && i.Status == Domain.Enums.InvitationStatus.Pending
                            && i.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Invitation>> GetInvitationsByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(i => i.InvitedByUser)
                .Include(i => i.Role)
                .Where(i => i.ProjectId == projectId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Invitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(i => i.Status == Domain.Enums.InvitationStatus.Pending && i.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
    }
}

