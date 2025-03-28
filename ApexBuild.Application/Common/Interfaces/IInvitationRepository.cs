using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IInvitationRepository : IRepository<Domain.Entities.Invitation>
    {
        Task<Domain.Entities.Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Invitation>> GetPendingInvitationsByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Invitation>> GetInvitationsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Invitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);
    }
}