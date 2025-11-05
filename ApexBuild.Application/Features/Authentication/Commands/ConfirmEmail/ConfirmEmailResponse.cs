namespace ApexBuild.Application.Features.Authentication.Commands.ConfirmEmail;

public record ConfirmEmailResponse
{
    public string Message { get; init; } = string.Empty;
    public bool IsAlreadyConfirmed { get; init; }
}

