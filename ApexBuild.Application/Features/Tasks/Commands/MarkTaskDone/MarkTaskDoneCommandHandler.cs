using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskDone;

public class MarkTaskDoneCommandHandler : IRequestHandler<MarkTaskDoneCommand, MarkTaskDoneResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MarkTaskDoneCommandHandler(
        IUnitOfWork _unitOfWork,
        ICurrentUserService currentUserService)
    {
        this._unitOfWork = _unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<MarkTaskDoneResponse> Handle(MarkTaskDoneCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated");
        }

        var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        // Check if user is assigned to this task
        var isAssigned = task.AssignedToUserId == currentUserId.Value ||
                        task.TaskUsers.Any(tu => tu.UserId == currentUserId.Value && tu.IsActive);

        if (!isAssigned)
        {
            throw new ForbiddenException("You are not assigned to this task");
        }

        // Mark task as done
        task.Status = TaskStatus.Done;
        task.Progress = 100;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarkTaskDoneResponse
        {
            TaskId = task.Id,
            Status = "Done",
            Message = "Task marked as done. Please submit your work for review."
        };
    }
}
