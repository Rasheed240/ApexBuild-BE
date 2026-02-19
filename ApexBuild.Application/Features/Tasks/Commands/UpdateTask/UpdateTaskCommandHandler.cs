using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, UpdateTaskResponse>
{
    private const int PriorityMin = 1;
    private const int PriorityMax = 4;
    private const decimal ProgressMin = 0;
    private const decimal ProgressMax = 100;
    private const decimal AutoCompleteProgress = 100m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public UpdateTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<UpdateTaskResponse> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to update a task");
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

        // Check authorization: Assigned user, supervisor, project admin/owner, or platform admin
        var isAssignedUser = await _unitOfWork.TaskUsers.AnyAsync(
            tu => tu.TaskId == task.Id && tu.UserId == currentUserId.Value && tu.IsActive,
            cancellationToken);

        var hasPermission = isAssignedUser ||
                           department.SupervisorId == currentUserId.Value ||
                           project.ProjectOwnerId == currentUserId.Value ||
                           project.ProjectAdminId == currentUserId.Value ||
                           _currentUserService.HasRole("SuperAdmin") ||
                           _currentUserService.HasRole("PlatformAdmin") ||
                           _currentUserService.HasRole("ProjectOwner") ||
                           _currentUserService.HasRole("ProjectAdministrator");

        if (!hasPermission)
        {
            throw new ForbiddenException("You do not have permission to update this task");
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Title))
            task.Title = request.Title;

        if (!string.IsNullOrWhiteSpace(request.Description))
            task.Description = request.Description;

        // Update assignees if provided
        if (request.AssignedUserIds != null)
        {
            // Validate all users are active members of this project
            foreach (var userId in request.AssignedUserIds)
            {
                var isMember = await _unitOfWork.ProjectUsers.AnyAsync(
                    pu => pu.ProjectId == project.Id && pu.UserId == userId && pu.IsActive,
                    cancellationToken);
                if (!isMember)
                    throw new BadRequestException($"User {userId} is not an active member of this project.");
            }

            // Remove all existing assignments
            var existingAssignments = await _unitOfWork.TaskUsers.FindAsync(
                tu => tu.TaskId == task.Id,
                cancellationToken);

            foreach (var assignment in existingAssignments)
            {
                await _unitOfWork.TaskUsers.DeleteAsync(assignment, cancellationToken);
            }

            // Create new assignments
            foreach (var userId in request.AssignedUserIds)
            {
                var taskUser = new TaskUser
                {
                    TaskId = task.Id,
                    UserId = userId,
                    AssignedByUserId = currentUserId.Value,
                    AssignedAt = _dateTimeService.UtcNow,
                    IsActive = true,
                    Role = "Assignee"
                };

                await _unitOfWork.TaskUsers.AddAsync(taskUser, cancellationToken);
            }
        }

        if (request.Status.HasValue)
            task.Status = request.Status.Value;

        if (request.Priority.HasValue)
        {
            if (request.Priority.Value < PriorityMin || request.Priority.Value > PriorityMax)
                throw new BadRequestException("Priority must be between 1 (Low) and 4 (Critical)");
            task.Priority = request.Priority.Value;
        }

        if (request.StartDate.HasValue)
            task.StartDate = request.StartDate.Value;

        if (request.DueDate.HasValue)
        {
            if (task.StartDate.HasValue && request.DueDate.Value < task.StartDate.Value)
                throw new BadRequestException("Due date must be after start date");
            task.DueDate = request.DueDate.Value;
        }

        if (request.EstimatedHours.HasValue)
        {
            if (request.EstimatedHours.Value < 0)
                throw new BadRequestException("Estimated hours must be 0 or greater");
            task.EstimatedHours = request.EstimatedHours.Value;
        }

        if (request.ActualHours.HasValue)
        {
            if (request.ActualHours.Value < 0)
                throw new BadRequestException("Actual hours must be 0 or greater");
            task.ActualHours = request.ActualHours.Value;
        }

        if (request.Progress.HasValue)
        {
            if (request.Progress.Value < ProgressMin || request.Progress.Value > ProgressMax)
                throw new BadRequestException("Progress must be between 0 and 100");
            task.Progress = request.Progress.Value;
        }

        if (request.Location != null)
            task.Location = request.Location;

        if (request.Tags != null)
            task.Tags = request.Tags;

        // Auto-complete task if progress is 100%
        if (task.Progress >= AutoCompleteProgress && task.Status != TaskStatus.Completed)
        {
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (task.Progress < AutoCompleteProgress && task.Status == TaskStatus.Completed)
        {
            task.Status = TaskStatus.InProgress;
            task.CompletedAt = null;
        }

        await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateTaskResponse
        {
            TaskId = task.Id,
            Title = task.Title,
            Message = "Task updated successfully"
        };
    }
}

