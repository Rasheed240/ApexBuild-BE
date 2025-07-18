using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace ApexBuild.Application.Features.Users.Commands.AcceptInvite
{
    public class AcceptInviteCommandValidator : AbstractValidator<AcceptInviteCommand>
    {
        public AcceptInviteCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Invitation token is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("First name contains invalid characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Last name contains invalid characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character");
        }
    }
}