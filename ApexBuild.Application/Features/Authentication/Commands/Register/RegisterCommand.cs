using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBuild.Application.Features.Authentication.Commands.Register
{
    public record RegisterCommand : IRequest<RegisterResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string Password { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? Country { get; init; }
        public string? Bio { get; init; }
        
        // Organization fields (required)
        public string OrganizationName { get; init; } = string.Empty;
        public string? OrganizationDescription { get; init; }
        public string? OrganizationCode { get; init; }
    }
}