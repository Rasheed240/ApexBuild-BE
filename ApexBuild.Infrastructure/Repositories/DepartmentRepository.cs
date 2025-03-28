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
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Supervisor)
                .Include(d => d.Organization)
                .Where(d => d.ProjectId == projectId && !d.IsDeleted)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Department>> GetDepartmentsByOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Project)
                .Include(d => d.Supervisor)
                .Where(d => d.OrganizationId == organizationId && !d.IsDeleted)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Department?> GetWithTasksAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(d => d.Supervisor)
                .Include(d => d.Organization)
                .Include(d => d.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefaultAsync(d => d.Id == departmentId && !d.IsDeleted, cancellationToken);
        }
    }
}