using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IOrganizationRepository : IRepository<Domain.Entities.Organization>
    {
        Task<Domain.Entities.Organization?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Organization?> GetWithMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Organization>> GetOrganizationsByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    }
}