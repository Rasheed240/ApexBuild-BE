using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using ApexBuild.Infrastructure.Persistence;

namespace ApexBuild.Infrastructure.Repositories
{
    public class ContractorRepository : BaseRepository<Contractor>, IContractorRepository
    {
        public ContractorRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Contractor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Project)
                .Include(c => c.Department)
                .Include(c => c.ContractorAdmin)
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Contractor>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Department)
                .Include(c => c.ContractorAdmin)
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Where(c => c.ProjectId == projectId)
                .OrderBy(c => c.CompanyName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Contractor>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.ContractorAdmin)
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Where(c => c.DepartmentId == departmentId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Contractor>> GetExpiringSoonAsync(int daysAhead = 14, CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddDays(daysAhead);
            return await _dbSet
                .Include(c => c.Project)
                .Include(c => c.ContractorAdmin)
                .Where(c => c.Status == ContractorStatus.Active
                         && c.ContractEndDate <= cutoff
                         && c.ContractEndDate > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsUserContractorAdminAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(c => c.ContractorAdminId == userId
                            && c.ProjectId == projectId
                            && c.Status == ContractorStatus.Active, cancellationToken);
        }
    }
}
