using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IRoleRepository : IRepository<Domain.Entities.Role>
    {
        Task<Domain.Entities.Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Role?> GetByRoleTypeAsync(Domain.Enums.RoleType roleType, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    }
}