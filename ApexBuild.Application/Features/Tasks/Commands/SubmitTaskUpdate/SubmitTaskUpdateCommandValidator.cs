using FluentValidation;

namespace ApexBuild.Application.Features.Tasks.Commands.SubmitTaskUpdate;

public class SubmitTaskUpdateCommandValidator : AbstractValidator<SubmitTaskUpdateCommand>
{
    public SubmitTaskUpdateCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.ProgressPercentage)
            .InclusiveBetween(0, 100).WithMessage("Progress percentage must be between 0 and 100");

        RuleFor(x => x.MediaUrls)
            .Must((command, urls) => urls == null || urls.Count == command.MediaTypes.Count)
            .WithMessage("Number of media URLs must match number of media types");

        RuleForEach(x => x.MediaTypes)
            .Must(type => type == "image" || type == "video")
            .WithMessage("Media type must be either 'image' or 'video'");
    }
}

