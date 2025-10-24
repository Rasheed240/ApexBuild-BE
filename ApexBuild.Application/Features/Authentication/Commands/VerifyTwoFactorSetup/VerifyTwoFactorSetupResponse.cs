namespace ApexBuild.Application.Features.Authentication.Commands.VerifyTwoFactorSetup;

public class VerifyTwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? RemainingBackupCodes { get; set; }
}
