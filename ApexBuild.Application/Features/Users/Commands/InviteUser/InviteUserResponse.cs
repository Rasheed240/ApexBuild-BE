using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Users.Commands.InviteUser
{
    public record InviteUserResponse
    {
        public Guid InvitationId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string InvitationUrl { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}