using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface ITaskRepository : IRepository<Domain.Entities.ProjectTask>
    {
        Task<IEnumerable<Domain.Entities.ProjectTask>> GetTasksByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.ProjectTask>> GetTasksByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.ProjectTask?> GetWithUpdatesAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.ProjectTask>> GetOverdueTasksAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.ProjectTask>> GetTasksDueInDaysAsync(int days, CancellationToken cancellationToken = default);
    }
}
