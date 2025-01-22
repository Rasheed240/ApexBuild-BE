using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IUserRepository : IRepository<Domain.Entities.User>
    {
        Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Domain.Entities.User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task<Domain.Entities.User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.User?> GetWithWorkInfoAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.User>> GetUsersByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.User>> GetUsersByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    }
        
}