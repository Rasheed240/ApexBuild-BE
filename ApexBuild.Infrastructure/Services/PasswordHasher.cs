using System.Text.RegularExpressions;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int MinLength = 8;
        private const int MaxLength = 100;
        private const int MinUniqueChars = 4;

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        public bool IsPasswordStrong(string password)
        {
            var validation = ValidatePasswordStrength(password);
            return validation.IsValid;
        }

        public (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            if (password.Length < MinLength)
                return (false, $"Password must be at least {MinLength} characters long");

            if (password.Length > MaxLength)
                return (false, $"Password must not exceed {MaxLength} characters");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return (false, "Password must contain at least one uppercase letter");

            if (!Regex.IsMatch(password, @"[a-z]"))
                return (false, "Password must contain at least one lowercase letter");

            if (!Regex.IsMatch(password, @"[0-9]"))
                return (false, "Password must contain at least one digit");

            if (!Regex.IsMatch(password, @"[@$!%*?&#]"))
                return (false, "Password must contain at least one special character (@$!%*?&#)");

            // Check for minimum unique characters
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars < MinUniqueChars)
                return (false, $"Password must contain at least {MinUniqueChars} unique characters");

            // Check for common patterns
            if (Regex.IsMatch(password, @"(.)\1{2,}"))
                return (false, "Password cannot contain the same character repeated more than twice consecutively");

            // Check for common sequences
            var sequences = new[] { "123", "abc", "qwe", "asd", "zxc" };
            var lowerPassword = password.ToLower();
            if (sequences.Any(s => lowerPassword.Contains(s)))
                return (false, "Password cannot contain common sequences");

            return (true, string.Empty);
        }
    }
}