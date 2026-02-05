using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IContractorRepository : IRepository<Contractor>
    {
        Task<Contractor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Contractor>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Contractor>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Contractor>> GetExpiringSoonAsync(int daysAhead = 14, CancellationToken cancellationToken = default);
        Task<bool> IsUserContractorAdminAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    }
}
