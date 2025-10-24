using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.EnableTwoFactor;

public record EnableTwoFactorCommand : IRequest<EnableTwoFactorResponse>
{
    public string? Password { get; init; } // Required for security verification
}
