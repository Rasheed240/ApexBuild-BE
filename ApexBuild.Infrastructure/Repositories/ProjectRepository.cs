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
    public class ProjectRepository : BaseRepository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Project?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower() && !p.IsDeleted, cancellationToken);
        }

        public async Task<Project?> GetWithDetailsAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.ProjectOwner)
                .Include(p => p.ProjectAdmin)
                .Include(p => p.Departments)
                    .ThenInclude(d => d.Supervisor)
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetProjectsByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.ProjectOwnerId == ownerId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Project>> GetProjectsByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.ProjectUsers.Any(pu => pu.UserId == userId
                            && pu.Status == Domain.Enums.ProjectUserStatus.Active)
                            && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}