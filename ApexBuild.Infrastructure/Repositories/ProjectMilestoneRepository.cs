using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class ProjectMilestoneRepository : BaseRepository<ProjectMilestone>, IProjectMilestoneRepository
    {
        public ProjectMilestoneRepository(ApplicationDbContext context) : base(context) { }

        public async Task<ProjectMilestone?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Project)
                .Include(m => m.Department)
                .Include(m => m.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<ProjectMilestone>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Department)
                .Include(m => m.CreatedByUser)
                .Where(m => m.ProjectId == projectId)
                .OrderBy(m => m.OrderIndex)
                .ThenBy(m => m.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMilestone>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Project)
                .Where(m => m.DepartmentId == departmentId)
                .OrderBy(m => m.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMilestone>> GetOverdueAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(m => m.Project)
                .Where(m => m.DueDate < now
                         && m.Status != MilestoneStatus.Completed
                         && m.Status != MilestoneStatus.Cancelled)
                .OrderBy(m => m.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectMilestone>> GetByStatusAsync(Guid projectId, MilestoneStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(m => m.Department)
                .Where(m => m.ProjectId == projectId && m.Status == status)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync(cancellationToken);
        }
    }
}
