using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ConfirmEmail;

public record ConfirmEmailCommand : IRequest<ConfirmEmailResponse>
{
    public string Token { get; init; } = string.Empty;
}

