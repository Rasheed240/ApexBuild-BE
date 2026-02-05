using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IProjectMilestoneRepository : IRepository<ProjectMilestone>
    {
        Task<ProjectMilestone?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMilestone>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMilestone>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMilestone>> GetOverdueAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ProjectMilestone>> GetByStatusAsync(Guid projectId, MilestoneStatus status, CancellationToken cancellationToken = default);
    }
}
