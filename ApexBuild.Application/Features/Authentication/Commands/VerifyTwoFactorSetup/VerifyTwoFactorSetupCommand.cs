using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorSetup;

public record VerifyTwoFactorSetupCommand : IRequest<VerifyTwoFactorSetupResponse>
{
    public string Code { get; init; } = string.Empty; // The TOTP code from authenticator app
}
