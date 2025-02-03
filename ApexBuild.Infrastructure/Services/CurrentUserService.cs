using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ApexBuild.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
            }
        }

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public List<string> Roles
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?
                    .FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList() ?? new List<string>();
            }
        }

        public string? GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return null;

            return $"{request.Scheme}://{request.Host}";
        }

        public bool HasRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasRole(string role, Guid? organizationId, Guid? projectId)
        {
            // Check if user has the role globally (no org/project context)
            var hasGlobalRole = Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
            
            // For global roles (SuperAdmin, PlatformAdmin), they apply everywhere
            if (hasGlobalRole && (role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) || 
                                  role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // If no context specified, check global role
            if (!organizationId.HasValue && !projectId.HasValue)
            {
                return hasGlobalRole;
            }

            // Check for context-specific roles using role_scope claims
            var roleScopeClaims = _httpContextAccessor.HttpContext?.User?
                .FindAll("role_scope")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

            // Check for exact match in role scopes
            if (organizationId.HasValue && projectId.HasValue)
            {
                // Check for role in specific project within organization
                var scope = $"{role}:org:{organizationId}:proj:{projectId}";
                if (roleScopeClaims.Any(s => s.Equals(scope, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (organizationId.HasValue)
            {
                // Check for role in organization (any project or no project)
                var orgScope = $"{role}:org:{organizationId}";
                var orgProjScope = $"{role}:org:{organizationId}:proj:";
                if (roleScopeClaims.Any(s => s.Equals(orgScope, StringComparison.OrdinalIgnoreCase) ||
                                            s.StartsWith(orgProjScope, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            if (projectId.HasValue)
            {
                // Check for role in project
                var projScope = $"{role}:proj:{projectId}";
                var orgProjScope = $"{role}:org:";
                if (roleScopeClaims.Any(s => s.Equals(projScope, StringComparison.OrdinalIgnoreCase) ||
                                            (s.Contains($":proj:{projectId}", StringComparison.OrdinalIgnoreCase) &&
                                             s.StartsWith($"{role}:", StringComparison.OrdinalIgnoreCase))))
                {
                    return true;
                }
            }

            // Fallback: if user has the role globally and no specific context is required
            return hasGlobalRole;
        }

        public List<Guid> GetOrganizationIds()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindAll("organization_id")
                .Select(c => Guid.Parse(c.Value))
                .ToList() ?? new List<Guid>();
        }

        public List<Guid> GetProjectIds()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindAll("project_id")
                .Select(c => Guid.Parse(c.Value))
                .ToList() ?? new List<Guid>();
        }

        public bool HasPermission(string permission)
        {
            // Implement permission logic based on your requirements
            return _httpContextAccessor.HttpContext?.User?.HasClaim("permission", permission) ?? false;
        }
    }
}