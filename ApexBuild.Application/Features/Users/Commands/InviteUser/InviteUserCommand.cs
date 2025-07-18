using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBuild.Application.Features.Users.Commands.InviteUser
{
    public record InviteUserCommand : IRequest<InviteUserResponse>
    {
        public string Email { get; init; } = string.Empty;
        public Guid RoleId { get; init; }
        public Guid? ProjectId { get; init; }
        public Guid? OrganizationId { get; init; }
        public Guid? DepartmentId { get; init; }
        public string? Position { get; init; }
        public string? Message { get; init; }
        public Guid InvitedByUserId { get; init; }
    }
}