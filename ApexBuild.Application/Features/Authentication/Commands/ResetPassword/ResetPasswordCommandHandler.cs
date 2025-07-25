using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordHistoryService _passwordHistoryService;

        public ResetPasswordCommandHandler(
            IUnitOfWork unitOfWork, 
            IPasswordHasher passwordHasher,
            IPasswordHistoryService passwordHistoryService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _passwordHistoryService = passwordHistoryService;
        }

        public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                u => u.PasswordResetToken == request.Token &&
                     u.PasswordResetTokenExpiry > DateTime.UtcNow,
                cancellationToken);

            if (user == null)
            {
                throw new BadRequestException("Invalid or expired reset token");
            }

            // Validate new password strength
            var passwordValidation = _passwordHasher.ValidatePasswordStrength(request.NewPassword);
            if (!passwordValidation.IsValid)
            {
                throw new BadRequestException(passwordValidation.ErrorMessage);
            }

            // Check if password is in history (pass plain password, not hash)
            if (await _passwordHistoryService.IsPasswordInHistoryAsync(user.Id, request.NewPassword, cancellationToken))
            {
                throw new BadRequestException("You cannot reuse a recently used password. Please choose a different password.");
            }

            // Hash the new password
            var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

            // Add current password to history before changing
            await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash, cancellationToken);

            // Update password
            user.PasswordHash = newPasswordHash;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.RefreshToken = null; // Invalidate refresh tokens on password reset
            user.RefreshTokenExpiry = null;
            user.FailedLoginAttempts = 0; // Reset failed attempts
            user.LockedOutUntil = null; // Unlock account

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResetPasswordResponse
            {
                Message = "Password has been reset successfully"
            };
        }
    }
}