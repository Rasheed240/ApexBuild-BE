using FluentValidation;

namespace ApexBuild.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Task description is required")
            .MaximumLength(2000).WithMessage("Task description cannot exceed 2000 characters");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 4).WithMessage("Priority must be between 1 (Low) and 4 (Critical)");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated hours must be 0 or greater");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow.Date).When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.DueDate).When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Start date must be before due date");
    }
}

