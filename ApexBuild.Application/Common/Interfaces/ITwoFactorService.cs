namespace ApexBuild.Application.Common.Interfaces;

public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new TOTP secret and returns the secret key and QR code URL
    /// </summary>
    Task<(string Secret, string QrCodeUrl)> GenerateTwoFactorSecretAsync(string userEmail);

    /// <summary>
    /// Verifies the TOTP code entered by the user
    /// </summary>
    Task<bool> VerifyTwoFactorCodeAsync(string secret, string code);

    /// <summary>
    /// Generates backup codes for account recovery
    /// </summary>
    Task<List<string>> GenerateBackupCodesAsync();

    /// <summary>
    /// Verifies if a backup code is valid and not used
    /// </summary>
    Task<bool> VerifyBackupCodeAsync(string secret, string code);

    /// <summary>
    /// Consumes a backup code (marks it as used)
    /// </summary>
    Task ConsumeBackupCodeAsync(string secret, string code);
}
