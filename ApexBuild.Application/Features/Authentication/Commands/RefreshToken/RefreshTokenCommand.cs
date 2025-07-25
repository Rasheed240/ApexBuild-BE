using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.RefreshToken
{
    public record RefreshTokenCommand : IRequest<RefreshTokenResponse>
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}