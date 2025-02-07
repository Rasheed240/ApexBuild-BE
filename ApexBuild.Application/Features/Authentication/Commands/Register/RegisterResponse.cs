using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Authentication.Commands.Register
{
    public record RegisterResponse
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public Guid OrganizationId { get; init; }
        public string OrganizationName { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}