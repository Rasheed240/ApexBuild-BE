using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorToken;

public class VerifyTwoFactorTokenCommandHandler : IRequestHandler<VerifyTwoFactorTokenCommand, VerifyTwoFactorTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IJwtTokenService _jwtTokenService;

    public VerifyTwoFactorTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITwoFactorService twoFactorService,
        IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _twoFactorService = twoFactorService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<VerifyTwoFactorTokenResponse> Handle(
        VerifyTwoFactorTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Code))
            throw new InvalidOperationException("Authentication code is required.");

        // Get user from email or current context
        var user = !string.IsNullOrEmpty(request.Email)
            ? await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
            : throw new InvalidOperationException("User email is required for 2FA verification.");

        if (user == null)
            throw new UnauthorizedException("User not found.");

        if (!user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is not enabled for this account.");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("Two-factor authentication configuration is missing.");

        // Extract TOTP secret
        var parts = user.TwoFactorSecret.Split("||");
        var totpSecret = parts[0];

        bool isValid = false;

        if (request.IsBackupCode)
        {
            // Verify backup code
            isValid = await _twoFactorService.VerifyBackupCodeAsync(user.TwoFactorSecret, request.Code);
            if (isValid)
            {
                // Consume the backup code
                await _twoFactorService.ConsumeBackupCodeAsync(user.TwoFactorSecret, request.Code);
            }
        }
        else
        {
            // Verify TOTP code
            isValid = await _twoFactorService.VerifyTwoFactorCodeAsync(totpSecret, request.Code);
        }

        if (!isValid)
            throw new UnauthorizedException("Invalid or expired authentication code.");

        // Fetch user with roles
        var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.Id, cancellationToken);
        if (userWithRoles == null)
            throw new NotFoundException(nameof(user), user.Id.ToString());

        // Update last login
        userWithRoles.LastLoginAt = DateTime.UtcNow;
        userWithRoles.FailedLoginAttempts = 0;
        userWithRoles.LockedOutUntil = null;

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(userWithRoles);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        userWithRoles.RefreshToken = refreshToken;
        userWithRoles.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        _unitOfWork.Users.Update(userWithRoles);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VerifyTwoFactorTokenResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = userWithRoles.Id,
                Email = userWithRoles.Email,
                FullName = userWithRoles.FullName,
                ProfileImageUrl = userWithRoles.ProfileImageUrl
            },
            Message = "Authentication successful."
        };
    }
}
