using FluentValidation;

namespace ApexBuild.Application.Features.Organizations.Commands.AddMember;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required")
            .MaximumLength(100).WithMessage("Position must not exceed 100 characters");
    }
}

