using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskComplete;

public class MarkTaskCompleteCommandHandler : IRequestHandler<MarkTaskCompleteCommand, MarkTaskCompleteResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public MarkTaskCompleteCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _notificationService = notificationService;
    }

    public async Task<MarkTaskCompleteResponse> Handle(MarkTaskCompleteCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to mark a task as complete");
        }

        var task = await _unitOfWork.Tasks.GetWithUpdatesAsync(request.TaskId, cancellationToken);
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
        var isAssignedUser = task.AssignedToUserId == currentUserId.Value;
        var isSupervisor = department.SupervisorId == currentUserId.Value;
        var isProjectAdmin = project.ProjectAdminId == currentUserId.Value || project.ProjectOwnerId == currentUserId.Value;
        var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") || _currentUserService.HasRole("PlatformAdmin");

        if (!isAssignedUser && !isSupervisor && !isProjectAdmin && !isPlatformAdmin)
        {
            throw new ForbiddenException("You do not have permission to mark this task as complete");
        }

        // Check if task can be marked complete
        if (task.Status == TaskStatus.Completed)
        {
            throw new BadRequestException("Task is already marked as completed");
        }

        // Verify task has at least one approved update (for daily reports)
        var hasApprovedUpdate = task.Updates.Any(u => 
            !u.IsDeleted && 
            (u.Status == UpdateStatus.AdminApproved || 
             u.Status == UpdateStatus.SupervisorApproved));

        if (!hasApprovedUpdate && task.Progress < 100)
        {
            throw new BadRequestException("Task must have at least one approved update or reach 100% progress before it can be marked as complete");
        }

        // Check if all subtasks are completed
        var subtasks = await _unitOfWork.Tasks.FindAsync(
            t => t.ParentTaskId == request.TaskId && !t.IsDeleted,
            cancellationToken);

        var incompleteSubtasks = subtasks.Where(t => t.Status != TaskStatus.Completed).ToList();
        if (incompleteSubtasks.Any())
        {
            throw new BadRequestException($"Cannot mark task as complete. {incompleteSubtasks.Count} subtask(s) are not yet completed.");
        }

        // Mark task as complete
        task.Status = TaskStatus.Completed;
        task.CompletedAt = _dateTimeService.UtcNow;
        task.Progress = 100;

        await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify relevant users
        var notificationRecipients = new List<Guid>();

        if (project.ProjectAdminId.HasValue)
            notificationRecipients.Add(project.ProjectAdminId.Value);

        if (project.ProjectOwnerId.HasValue && project.ProjectOwnerId != project.ProjectAdminId)
            notificationRecipients.Add(project.ProjectOwnerId.Value);

        if (department.SupervisorId.HasValue && !notificationRecipients.Contains(department.SupervisorId.Value))
            notificationRecipients.Add(department.SupervisorId.Value);

        foreach (var recipientId in notificationRecipients)
        {
            await _notificationService.SendNotificationAsync(
                recipientId,
                "Task Completed",
                $"Task '{task.Title}' has been marked as completed by {task.AssignedToUser?.FullName ?? "Unknown"}.",
                NotificationType.TaskUpdate,
                NotificationChannel.Both,
                task.Id,
                "Task",
                null,
                $"/tasks/{task.Id}");
        }

        // Notify assigned user
        if (task.AssignedToUserId.HasValue && task.AssignedToUserId != currentUserId.Value)
        {
            await _notificationService.SendNotificationAsync(
                task.AssignedToUserId.Value,
                "Task Marked Complete",
                $"Task '{task.Title}' has been marked as complete.",
                NotificationType.TaskUpdate,
                NotificationChannel.Both,
                task.Id,
                "Task",
                null,
                $"/tasks/{task.Id}");
        }

        return new MarkTaskCompleteResponse
        {
            TaskId = task.Id,
            Message = "Task marked as complete successfully"
        };
    }
}

