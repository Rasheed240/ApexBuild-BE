using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Authentication.Commands.Register;
using ApexBuild.Application.Features.Authentication.Commands.Login;
using ApexBuild.Application.Features.Authentication.Commands.ForgotPassword;
using ApexBuild.Application.Features.Authentication.Commands.ResetPassword;
using ApexBuild.Application.Features.Authentication.Commands.RefreshToken;
using ApexBuild.Application.Features.Authentication.Commands.ChangePassword;
using ApexBuild.Application.Features.Authentication.Commands.ConfirmEmail;
using ApexBuild.Application.Features.Authentication.Commands.EnableTwoFactor;
using ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorSetup;
using ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorToken;
using ApexBuild.Application.Features.Authentication.Commands.DisableTwoFactor;
using ApexBuild.Contracts.Responses;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationController(IMediator mediator, IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Register a new user (automatically becomes Platform Admin)
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "User registered successfully"));
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginCommand command)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var commandWithIp = command with { IpAddress = ipAddress };

            var response = await _mediator.Send(commandWithIp);
            return Ok(ApiResponse.Success(response, "Login successful"));
        }

        /// <summary>
        /// Request password reset link
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword(
            [FromBody] ForgotPasswordCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Password reset link sent successfully"));
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ResetPasswordResponse>>> ResetPassword(
            [FromBody] ResetPasswordCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Password reset successfully"));
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken(
            [FromBody] RefreshTokenCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Token refreshed successfully"));
        }

        /// <summary>
        /// Logout (client should discard tokens)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public ActionResult<ApiResponse<object>> Logout()
        {
            // Token invalidation is handled on client side
            // Optionally, you can implement token blacklisting here
            return Ok(ApiResponse.Success(new { }, "Logged out successfully"));
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Failure<object>("User not authenticated"));
            }

            // Get full user details from database
            var userGuid = Guid.Parse(userId);
            var user = await _unitOfWork.Users.GetByIdAsync(userGuid);

            if (user == null)
            {
                return NotFound(ApiResponse.Failure<object>("User not found"));
            }

            var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var userData = new
            {
                id = user.Id,
                userId = user.Id.ToString(),
                email = user.Email,
                fullName = user.FullName,
                firstName = user.FirstName,
                lastName = user.LastName,
                middleName = user.MiddleName,
                phoneNumber = user.PhoneNumber,
                bio = user.Bio,
                profileImageUrl = user.ProfileImageUrl,
                address = user.Address,
                city = user.City,
                state = user.State,
                country = user.Country,
                dateOfBirth = user.DateOfBirth,
                gender = user.Gender,
                status = user.Status,
                emailConfirmed = user.EmailConfirmed,
                twoFactorEnabled = user.TwoFactorEnabled,
                createdAt = user.CreatedAt,
                joinedAt = user.CreatedAt,
                roles
            };

            return Ok(ApiResponse.Success(userData, "User profile retrieved successfully"));
        }

        /// <summary>
        /// Change password (requires authentication)
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<ChangePasswordResponse>>> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Password changed successfully"));
        }

        /// <summary>
        /// Confirm email address using confirmation token
        /// </summary>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ConfirmEmailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ConfirmEmailResponse>>> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, response.IsAlreadyConfirmed ? "Email already confirmed" : "Email confirmed successfully"));
        }

        #region Two-Factor Authentication (2FA)

        /// <summary>
        /// Enable two-factor authentication for the current user
        /// </summary>
        [HttpPost("2fa/enable")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<EnableTwoFactorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<EnableTwoFactorResponse>>> EnableTwoFactor([FromBody] EnableTwoFactorCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Two-factor authentication setup initiated. Please verify with the code from your authenticator app."));
        }

        /// <summary>
        /// Verify the TOTP code and complete 2FA setup
        /// </summary>
        [HttpPost("2fa/verify-setup")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<VerifyTwoFactorSetupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<VerifyTwoFactorSetupResponse>>> VerifyTwoFactorSetup([FromBody] VerifyTwoFactorSetupCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Two-factor authentication has been successfully enabled!"));
        }

        /// <summary>
        /// Verify 2FA code during login
        /// </summary>
        [HttpPost("2fa/verify-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<VerifyTwoFactorTokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<VerifyTwoFactorTokenResponse>>> VerifyTwoFactorToken([FromBody] VerifyTwoFactorTokenCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Authentication successful!"));
        }

        /// <summary>
        /// Disable two-factor authentication for the current user
        /// </summary>
        [HttpPost("2fa/disable")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<DisableTwoFactorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<DisableTwoFactorResponse>>> DisableTwoFactor([FromBody] DisableTwoFactorCommand command)
        {
            var response = await _mediator.Send(command);
            return Ok(ApiResponse.Success(response, "Two-factor authentication has been disabled."));
        }

        #endregion
    }
}