using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IOrganizationMemberRepository : IRepository<Domain.Entities.OrganizationMember>
    {
        Task<IEnumerable<Domain.Entities.OrganizationMember>> GetMembersByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.OrganizationMember?> GetMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> IsMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    }
}
