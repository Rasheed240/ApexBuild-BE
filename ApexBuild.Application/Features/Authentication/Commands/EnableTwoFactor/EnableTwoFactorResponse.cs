namespace ApexBuild.Application.Features.Authentication.Commands.EnableTwoFactor;

public class EnableTwoFactorResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public List<string> BackupCodes { get; set; } = new List<string>();
    public string Message { get; set; } = "Scan the QR code with your authenticator app and enter a code to verify.";
}
