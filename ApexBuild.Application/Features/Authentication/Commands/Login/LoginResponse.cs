using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Authentication.Commands.Login
{
    public record LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public UserDto User { get; init; } = null!;
        public bool RequiresTwoFactorAuthentication { get; init; } = false;
        public string? TwoFactorToken { get; init; } // Temporary token for 2FA verification
    }
}