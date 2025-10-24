using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorSetup;

public class VerifyTwoFactorSetupCommandHandler : IRequestHandler<VerifyTwoFactorSetupCommand, VerifyTwoFactorSetupResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITwoFactorService _twoFactorService;
    private readonly INotificationService _notificationService;

    public VerifyTwoFactorSetupCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ITwoFactorService twoFactorService,
        INotificationService notificationService)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _twoFactorService = twoFactorService;
        _notificationService = notificationService;
    }

    public async Task<VerifyTwoFactorSetupResponse> Handle(
        VerifyTwoFactorSetupCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken)
            ?? throw new NotFoundException("User", userId.Value.ToString());

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new InvalidOperationException("No pending two-factor authentication setup found.");

        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is already enabled.");

        // Extract the TOTP secret (first part before ||)
        var parts = user.TwoFactorSecret.Split("||");
        var totpSecret = parts[0];

        // Verify the code
        var isValid = await _twoFactorService.VerifyTwoFactorCodeAsync(totpSecret, request.Code);
        if (!isValid)
            throw new InvalidOperationException("Invalid or expired authentication code.");

        // Get backup codes (everything after the TOTP secret)
        var backupCodes = parts.Skip(1)
            .Where(p => !p.StartsWith("USED_"))
            .ToList();

        // Enable 2FA
        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = user.TwoFactorSecret; // Keep the full secret with backup codes

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        await _notificationService.NotifyUserAsync(
            user.Id,
            "Two-Factor Authentication Enabled",
            "Your account now has two-factor authentication enabled. You will need to provide an authentication code on your next login."
        );

        return new VerifyTwoFactorSetupResponse
        {
            Success = true,
            Message = "Two-factor authentication has been successfully enabled.",
            RemainingBackupCodes = backupCodes
        };
    }
}
