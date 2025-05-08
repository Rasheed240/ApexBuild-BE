using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, DeleteTaskResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteTaskResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to delete a task");
        }

        var task = await _unitOfWork.Tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        var department = await _unitOfWork.Departments.GetByIdAsync(task.DepartmentId, cancellationToken);
        if (department == null || department.IsDeleted)
        {
            throw new NotFoundException("Department", task.DepartmentId);
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(department.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException("Project", department.ProjectId);
        }

        // Check authorization: Only project admin/owner or platform admin can delete
        var hasPermission = project.ProjectOwnerId == currentUserId.Value ||
                           project.ProjectAdminId == currentUserId.Value ||
                           _currentUserService.HasRole("SuperAdmin") ||
                           _currentUserService.HasRole("PlatformAdmin");

        if (!hasPermission)
        {
            throw new ForbiddenException("You do not have permission to delete this task");
        }

        // Check if task has subtasks
        var subtasks = await _unitOfWork.Tasks.FindAsync(
            t => t.ParentTaskId == request.TaskId && !t.IsDeleted,
            cancellationToken);

        if (subtasks.Any())
        {
            throw new BadRequestException("Cannot delete a task that has subtasks. Please delete or reassign subtasks first.");
        }

        // Soft delete the task
        await _unitOfWork.Tasks.DeleteAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTaskResponse
        {
            TaskId = task.Id,
            Message = "Task deleted successfully"
        };
    }
}

