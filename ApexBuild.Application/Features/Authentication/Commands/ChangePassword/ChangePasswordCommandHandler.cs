using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Common.Interfaces;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly ICurrentUserService _currentUserService;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPasswordHistoryService passwordHistoryService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _passwordHistoryService = passwordHistoryService;
        _currentUserService = currentUserService;
    }

    public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User not authenticated");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId.Value);
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect");
        }

        // Validate new password strength
        var passwordValidation = _passwordHasher.ValidatePasswordStrength(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            throw new BadRequestException(passwordValidation.ErrorMessage);
        }

            // Check if password is in history (pass plain password, not hash)
            if (await _passwordHistoryService.IsPasswordInHistoryAsync(userId.Value, request.NewPassword, cancellationToken))
            {
                throw new BadRequestException("You cannot reuse a recently used password. Please choose a different password.");
            }

            // Hash the new password
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // Add current password to history before changing
        await _passwordHistoryService.AddPasswordToHistoryAsync(userId.Value, user.PasswordHash, cancellationToken);

        // Update password
        user.PasswordHash = newPasswordHash;
        user.RefreshToken = null; // Invalidate refresh tokens on password change
        user.RefreshTokenExpiry = null;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse
        {
            Message = "Password changed successfully"
        };
    }
}

