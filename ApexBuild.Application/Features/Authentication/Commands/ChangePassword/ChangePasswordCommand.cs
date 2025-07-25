using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ChangePassword;

public record ChangePasswordCommand : IRequest<ChangePasswordResponse>
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

