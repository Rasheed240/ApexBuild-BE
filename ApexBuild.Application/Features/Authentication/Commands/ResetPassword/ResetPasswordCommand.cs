using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ResetPassword
{
    public record ResetPasswordCommand : IRequest<ResetPasswordResponse>
    {
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}