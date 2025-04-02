using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IProjectRepository : IRepository<Domain.Entities.Project>
    {
        Task<Domain.Entities.Project?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Project?> GetWithDetailsAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Project>> GetProjectsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Project>> GetProjectsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}