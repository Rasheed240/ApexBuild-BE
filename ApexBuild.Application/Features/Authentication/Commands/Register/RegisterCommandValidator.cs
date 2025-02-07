using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace ApexBuild.Application.Features.Authentication.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("First name contains invalid characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Last name contains invalid characters");

            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Middle name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Middle name contains invalid characters")
                .When(x => !string.IsNullOrEmpty(x.MiddleName));

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow.AddYears(-13)).WithMessage("You must be at least 13 years old")
                .GreaterThan(DateTime.UtcNow.AddYears(-120)).WithMessage("Invalid date of birth")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Gender)
                .Must(g => string.IsNullOrEmpty(g) || new[] { "Male", "Female", "Other", "PreferNotToSay" }.Contains(g))
                .WithMessage("Gender must be one of: Male, Female, Other, PreferNotToSay")
                .When(x => !string.IsNullOrEmpty(x.Gender));

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

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            // Organization validation
            RuleFor(x => x.OrganizationName)
                .NotEmpty().WithMessage("Organization name is required")
                .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters");

            RuleFor(x => x.OrganizationDescription)
                .MaximumLength(1000).WithMessage("Organization description must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.OrganizationDescription));

            RuleFor(x => x.OrganizationCode)
                .MaximumLength(50).WithMessage("Organization code must not exceed 50 characters")
                .Matches(@"^[A-Z0-9-]+$").WithMessage("Organization code must contain only uppercase letters, numbers, and hyphens")
                .When(x => !string.IsNullOrEmpty(x.OrganizationCode));
        }
    }
}