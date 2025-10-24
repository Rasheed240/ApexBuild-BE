using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Authentication.Commands.EnableTwoFactor;

public class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly INotificationService _notificationService;

    public EnableTwoFactorCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ITwoFactorService twoFactorService,
        IPasswordHasher passwordHasher,
        INotificationService notificationService)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _twoFactorService = twoFactorService;
        _passwordHasher = passwordHasher;
        _notificationService = notificationService;
    }

    public async Task<EnableTwoFactorResponse> Handle(
        EnableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId.Value);

        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already enabled for this account.");

        // Require password verification for security
        if (string.IsNullOrEmpty(request.Password))
            throw new InvalidOperationException("Password is required to enable two-factor authentication.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Password verification failed.");

        // Generate 2FA secret
        var (secret, qrCodeUrl) = await _twoFactorService.GenerateTwoFactorSecretAsync(user.Email);
        var backupCodes = await _twoFactorService.GenerateBackupCodesAsync();

        // Store secret temporarily (not yet confirmed)
        // We'll use a temporary format: secret||backup_code1||backup_code2||...
        var secretWithBackupCodes = $"{secret}||{string.Join("||", backupCodes)}";
        user.TwoFactorSecret = secretWithBackupCodes;
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        await _notificationService.NotifyUserAsync(
            user.Id,
            "Two-Factor Authentication Setup Started",
            "You have started the two-factor authentication setup process. If this wasn't you, please secure your account immediately."
        );

        return new EnableTwoFactorResponse
        {
            Secret = secret,
            QrCodeUrl = qrCodeUrl,
            BackupCodes = backupCodes,
            Message = "Scan the QR code with your authenticator app (e.g., Google Authenticator, Microsoft Authenticator, Authy) and save your backup codes in a secure location."
        };
    }
}
