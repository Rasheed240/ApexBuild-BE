using FluentValidation;

namespace ApexBuild.Application.Features.Projects.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ProjectType)
            .MaximumLength(100).WithMessage("Project type must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ProjectType));

        RuleFor(x => x.Location)
            .MaximumLength(500).WithMessage("Location must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.Currency)
            .Length(3).WithMessage("Currency must be a 3-letter ISO code (e.g., USD, EUR)")
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be a valid 3-letter uppercase ISO code")
            .When(x => !string.IsNullOrEmpty(x.Currency));

        RuleFor(x => x.Budget)
            .GreaterThan(0).WithMessage("Budget must be greater than zero")
            .When(x => x.Budget.HasValue);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.StartDate)
            .LessThan(x => x.ExpectedEndDate).WithMessage("Start date must be before expected end date")
            .When(x => x.StartDate.HasValue && x.ExpectedEndDate.HasValue);

        RuleFor(x => x.ExpectedEndDate)
            .GreaterThan(x => x.StartDate).WithMessage("Expected end date must be after start date")
            .When(x => x.ExpectedEndDate.HasValue && x.StartDate.HasValue);

        RuleFor(x => x.ActualEndDate)
            .GreaterThan(x => x.StartDate).WithMessage("Actual end date must be after start date")
            .When(x => x.ActualEndDate.HasValue && x.StartDate.HasValue);
    }
}

