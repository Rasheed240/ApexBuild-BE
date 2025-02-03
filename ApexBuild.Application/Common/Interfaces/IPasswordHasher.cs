using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        bool IsPasswordStrong(string password);
        (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password);
    }
}