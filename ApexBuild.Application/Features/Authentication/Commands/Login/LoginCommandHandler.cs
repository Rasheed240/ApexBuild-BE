using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;

        public LoginCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
        }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetWithRolesAsync(
            (await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken))?.Id ?? Guid.Empty,
            cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Check if account is locked
        if (user.IsLocked)
        {
            throw new UnauthorizedException($"Account is locked until {user.LockedOutUntil}");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedOutUntil = DateTime.UtcNow.AddMinutes(30);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;

        // Check if 2FA is enabled - if so, return pending status
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            // Generate a temporary token for 2FA verification (expires in 5 minutes)
            var tempClaims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("user_id", user.Id.ToString()),
                new System.Security.Claims.Claim("email", user.Email),
                new System.Security.Claims.Claim("2fa_pending", "true")
            };

            // You'll need to create a temp token service or use JWT with short expiry
            // For now, we'll return an empty temp token and handle this in the controller
            var tempToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.Id.ToString()));

            return new LoginResponse
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.UtcNow,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    ProfileImageUrl = user.ProfileImageUrl,
                    Roles = new List<RoleDto>()
                },
                RequiresTwoFactorAuthentication = true,
                TwoFactorToken = tempToken
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = request.IpAddress;

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles
            .Where(ur => ur.IsActive)
            .Select(ur => new RoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                RoleType = ur.Role.RoleType,
                ProjectId = ur.ProjectId,
                ProjectName = ur.Project?.Name,
                OrganizationId = ur.OrganizationId,
                OrganizationName = ur.Organization?.Name
            })
            .ToList();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles
            },
            RequiresTwoFactorAuthentication = false
        };
    }
    }
}
