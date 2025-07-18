using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Users.Commands.AcceptInvite
{
    public record AcceptInviteResponse
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}