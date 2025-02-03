using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
        List<string> Roles { get; }
        string? GetBaseUrl();
        bool HasRole(string role);
        bool HasRole(string role, Guid? organizationId, Guid? projectId);
        bool HasPermission(string permission);
        List<Guid> GetOrganizationIds();
        List<Guid> GetProjectIds();
    }
}