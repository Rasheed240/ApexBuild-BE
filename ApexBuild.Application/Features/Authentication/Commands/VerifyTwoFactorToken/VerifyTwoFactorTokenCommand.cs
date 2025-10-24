using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorToken;

public record VerifyTwoFactorTokenCommand : IRequest<VerifyTwoFactorTokenResponse>
{
    public string Code { get; init; } = string.Empty; // TOTP code or backup code
    public string? Email { get; init; } // For login flow
    public bool IsBackupCode { get; init; } = false;
}
