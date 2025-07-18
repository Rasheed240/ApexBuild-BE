using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace ApexBuild.Application.Features.Users.Commands.InviteUser
{
    public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
    {
        public InviteUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role is required");

            RuleFor(x => x.InvitedByUserId)
                .NotEmpty().WithMessage("Inviter information is required");

            RuleFor(x => x.Position)
                .MaximumLength(100).WithMessage("Position must not exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Position));

            RuleFor(x => x.Message)
                .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Message));
        }
    }
}