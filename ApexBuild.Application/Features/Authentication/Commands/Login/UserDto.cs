using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Authentication.Commands.Login
{
    public record UserDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? ProfileImageUrl { get; init; }
        public List<RoleDto> Roles { get; init; } = new();
    }
}