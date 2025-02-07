using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Authentication.Commands.Login
{
    public record RoleDto
    {
        public Guid RoleId { get; init; }
        public string RoleName { get; init; } = string.Empty;
        public RoleType RoleType { get; init; }
        public Guid? ProjectId { get; init; }
        public string? ProjectName { get; init; }
        public Guid? OrganizationId { get; init; }
        public string? OrganizationName { get; init; }
    }
}