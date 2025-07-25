using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.ForgotPassword
{
    public record ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
    {
        public string Email { get; init; } = string.Empty;
    }
}