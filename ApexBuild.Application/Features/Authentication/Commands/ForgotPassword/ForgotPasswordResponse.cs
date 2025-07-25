using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Features.Authentication.Commands.ForgotPassword
{
    public record ForgotPasswordResponse
    {
        public string Message { get; init; } = string.Empty;
    }
}