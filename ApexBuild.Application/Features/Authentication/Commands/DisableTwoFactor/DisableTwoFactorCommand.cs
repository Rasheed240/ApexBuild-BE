using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.DisableTwoFactor;

public record DisableTwoFactorCommand : IRequest<DisableTwoFactorResponse>
{
    public string? Password { get; init; } // Required for security verification
}
