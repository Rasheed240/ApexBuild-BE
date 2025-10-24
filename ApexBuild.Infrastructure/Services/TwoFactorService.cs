using System.Security.Cryptography;
using System.Text;
using OtpNet;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private const int SecretLength = 32; // 256 bits
    private const int BackupCodesCount = 10;
    private const int BackupCodeLength = 8;

    public async Task<(string Secret, string QrCodeUrl)> GenerateTwoFactorSecretAsync(string userEmail)
    {
        // Generate a random secret key
        var secret = GenerateRandomSecret();
        
        // Create TOTP provisioning URI for QR code
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        
        // Format: otpauth://totp/{label}?secret={secret}&issuer={issuer}
        var qrCodeUrl = GenerateQrCodeUrl(userEmail, secret);

        return await Task.FromResult((secret, qrCodeUrl));
    }

    public async Task<bool> VerifyTwoFactorCodeAsync(string secret, string code)
    {
        try
        {
            // Remove any spaces and hyphens
            var cleanCode = code.Replace(" ", "").Replace("-", "");
            
            if (!int.TryParse(cleanCode, out var codeInt))
                return false;

            // Verify the TOTP code with a time window of ±1 interval
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);

            // Check current interval and adjacent intervals for clock skew tolerance
            var result = totp.VerifyTotp(
                cleanCode,
                out long timeStepMatched
            );

            return await Task.FromResult(result);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GenerateBackupCodesAsync()
    {
        var backupCodes = new List<string>();

        for (int i = 0; i < BackupCodesCount; i++)
        {
            var code = GenerateRandomBackupCode();
            backupCodes.Add(code);
        }

        return await Task.FromResult(backupCodes);
    }

    public async Task<bool> VerifyBackupCodeAsync(string secret, string code)
    {
        // Backup codes are stored in the secret field with a specific format
        // Format: "totp_secret||backup_code1||backup_code2||..."
        try
        {
            if (string.IsNullOrEmpty(secret))
                return false;

            var parts = secret.Split("||");
            if (parts.Length < 2)
                return false;

            // Search for the code in the backup codes (skip first element which is TOTP secret)
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("USED_"))
                    continue;

                if (parts[i] == code)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task ConsumeBackupCodeAsync(string secret, string code)
    {
        // Mark backup code as used
        if (string.IsNullOrEmpty(secret))
            return;

        var parts = secret.Split("||").ToList();
        
        for (int i = 1; i < parts.Count; i++)
        {
            if (parts[i] == code)
            {
                parts[i] = $"USED_{code}_{DateTime.UtcNow:yyyyMMddHHmmss}";
                break;
            }
        }

        await Task.CompletedTask;
    }

    private string GenerateRandomSecret()
    {
        var randomBytes = new byte[SecretLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Base32Encoding.ToString(randomBytes);
    }

    private string GenerateRandomBackupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new StringBuilder();

        for (int i = 0; i < BackupCodeLength; i++)
        {
            code.Append(chars[random.Next(chars.Length)]);
        }

        return code.ToString();
    }

    private string GenerateQrCodeUrl(string userEmail, string secret)
    {
        // Format for authenticator apps
        var issuer = "ApexBuild";
        var label = $"{issuer} ({userEmail})";
        
        // URL encode the label
        var encodedLabel = Uri.EscapeDataString(label);
        var encodedIssuer = Uri.EscapeDataString(issuer);

        // Generate QR code provisioning URI
        var qrUri = $"otpauth://totp/{encodedLabel}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";

        return qrUri;
    }
}
