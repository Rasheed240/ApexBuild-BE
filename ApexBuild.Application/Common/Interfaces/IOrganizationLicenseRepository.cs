using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Common.Interfaces
{
    /// <summary>
    /// Repository interface for OrganizationLicense entities.
    /// </summary>
    public interface IOrganizationLicenseRepository : IRepository<OrganizationLicense>
    {
        Task<OrganizationLicense?> GetByUserAndOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
        Task<List<OrganizationLicense>> GetByOrganizationIdAsync(Guid organizationId, LicenseStatus? status = null, CancellationToken cancellationToken = default);
        Task<List<OrganizationLicense>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<OrganizationLicense>> GetActiveLicensesByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<List<OrganizationLicense>> GetExpiredLicensesAsync(CancellationToken cancellationToken = default);
        Task<List<OrganizationLicense>> GetExpiringLicensesAsync(int daysUntilExpiration = 7, CancellationToken cancellationToken = default);
        Task<int> GetActiveLicenseCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<bool> HasActiveLicenseAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
        Task<List<(User User, OrganizationLicense License)>> GetOrganizationUsersWithLicensesAsync(Guid organizationId, CancellationToken cancellationToken = default);
    }
}
