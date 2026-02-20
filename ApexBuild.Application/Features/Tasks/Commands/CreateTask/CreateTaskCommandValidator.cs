using FluentValidation;
using ApexBuild.Domain.Common;

namespace ApexBuild.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required")
            .MaximumLength(TaskConstants.MaxTitleLength)
            .WithMessage($"Task title cannot exceed {TaskConstants.MaxTitleLength} characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Task description is required")
            .MaximumLength(TaskConstants.MaxDescriptionLength)
            .WithMessage($"Task description cannot exceed {TaskConstants.MaxDescriptionLength} characters");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.Priority)
            .InclusiveBetween(TaskConstants.MinPriority, TaskConstants.MaxPriority)
            .WithMessage($"Priority must be between {TaskConstants.MinPriority} ({TaskConstants.PriorityLow}) and {TaskConstants.MaxPriority} ({TaskConstants.PriorityCritical})");

        RuleFor(x => x.EstimatedHours)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated hours must be 0 or greater");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow.Date).When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.DueDate).When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Start date must be before due date");

        RuleFor(x => x.AssignedUserIds)
            .NotEmpty().WithMessage("At least one assignee is required.");
    }
}
