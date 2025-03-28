using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IDepartmentRepository : IRepository<Domain.Entities.Department>
    {
        Task<IEnumerable<Domain.Entities.Department>> GetDepartmentsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Department>> GetDepartmentsByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Department?> GetWithTasksAsync(Guid departmentId, CancellationToken cancellationToken = default);
    }
}