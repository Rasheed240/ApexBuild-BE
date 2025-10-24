using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Authentication.Commands.DisableTwoFactor;

public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, DisableTwoFactorResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly INotificationService _notificationService;

    public DisableTwoFactorCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        INotificationService notificationService)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _notificationService = notificationService;
    }

    public async Task<DisableTwoFactorResponse> Handle(
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken)
            ?? throw new NotFoundException("User", userId.Value.ToString());

        if (!user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-factor authentication is not enabled for this account.");

        // Require password verification for security
        if (string.IsNullOrEmpty(request.Password))
            throw new InvalidOperationException("Password is required to disable two-factor authentication.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Password verification failed.");

        // Disable 2FA
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send notification
        await _notificationService.NotifyUserAsync(
            user.Id,
            "Two-Factor Authentication Disabled",
            "Two-factor authentication has been disabled on your account. Your account security is reduced. Enable it again if this wasn't intentional."
        );

        return new DisableTwoFactorResponse
        {
            Success = true,
            Message = "Two-factor authentication has been successfully disabled."
        };
    }
}
