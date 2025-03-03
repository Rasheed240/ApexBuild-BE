using FluentValidation;

namespace ApexBuild.Application.Features.Organizations.Commands.UpdateOrganization;

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Website)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Invalid website URL format")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(100).WithMessage("Registration number must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.RegistrationNumber));

        RuleFor(x => x.TaxId)
            .MaximumLength(50).WithMessage("Tax ID must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.TaxId));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .MaximumLength(100).WithMessage("State must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Country));
    }
}

