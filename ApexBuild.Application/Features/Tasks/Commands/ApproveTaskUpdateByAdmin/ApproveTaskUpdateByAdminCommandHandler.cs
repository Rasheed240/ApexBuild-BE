using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByAdmin;

public class ApproveTaskUpdateByAdminCommandHandler : IRequestHandler<ApproveTaskUpdateByAdminCommand, ApproveTaskUpdateByAdminResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public ApproveTaskUpdateByAdminCommandHandler(
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

    public async Task<ApproveTaskUpdateByAdminResponse> Handle(ApproveTaskUpdateByAdminCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to review task updates");
        }

        // Get task update with details
        var taskUpdate = await _unitOfWork.TaskUpdates.GetWithDetailsAsync(request.UpdateId, cancellationToken);
        if (taskUpdate == null || taskUpdate.IsDeleted)
        {
            throw new NotFoundException("Task Update", request.UpdateId);
        }

        var task = await _unitOfWork.Tasks.GetByIdAsync(taskUpdate.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            throw new NotFoundException("Task", taskUpdate.TaskId);
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

        // Check authorization: Must be project admin/owner, or platform admin
        var isProjectAdmin = project.ProjectAdminId == currentUserId.Value || project.ProjectOwnerId == currentUserId.Value;
        var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") || _currentUserService.HasRole("PlatformAdmin");

        if (!isProjectAdmin && !isPlatformAdmin)
        {
            throw new ForbiddenException("You do not have permission to review this task update. Only project admins, project owners, or platform admins can perform admin review.");
        }

        // Verify update has been approved by supervisor first (or skip if supervisor approval not required)
        if (taskUpdate.Status != UpdateStatus.UnderAdminReview && 
            taskUpdate.Status != UpdateStatus.SupervisorApproved)
        {
            // Allow admin to review if supervisor already approved, or if there's no supervisor
            if (taskUpdate.Status != UpdateStatus.SupervisorApproved && department.SupervisorId.HasValue)
            {
                throw new BadRequestException($"Task update must be approved by supervisor first before admin review. Current status: {taskUpdate.Status}");
            }
        }

        // Update review information
        taskUpdate.ReviewedByAdminId = currentUserId.Value;
        taskUpdate.AdminReviewedAt = _dateTimeService.UtcNow;
        taskUpdate.AdminFeedback = request.Feedback;
        taskUpdate.AdminApproved = request.Approved;

        if (request.Approved)
        {
            taskUpdate.Status = UpdateStatus.AdminApproved;
            
            // Update task progress
            if (taskUpdate.ProgressPercentage > task.Progress)
            {
                task.Progress = taskUpdate.ProgressPercentage;
                await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
            }

            // If progress is 100% and task is not completed, mark it as ready for completion
            if (taskUpdate.ProgressPercentage >= 100 && task.Status != TaskStatus.Completed)
            {
                task.Status = TaskStatus.Approved; // Ready to be marked complete
                await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
            }
            
            // Notify submitter
            await _notificationService.SendNotificationAsync(
                taskUpdate.SubmittedByUserId,
                "Daily Report Approved",
                $"Your daily report for task '{task.Title}' has been approved by admin.",
                NotificationType.TaskUpdate,
                NotificationChannel.Both,
                taskUpdate.Id,
                "TaskUpdate",
                null,
                $"/tasks/{task.Id}/updates/{taskUpdate.Id}");
        }
        else
        {
            taskUpdate.Status = UpdateStatus.AdminRejected;
            
            // Notify submitter
            await _notificationService.SendNotificationAsync(
                taskUpdate.SubmittedByUserId,
                "Task Update Rejected by Admin",
                $"Your daily report for task '{task.Title}' was rejected by admin. Feedback: {request.Feedback ?? "No feedback provided"}",
                NotificationType.TaskUpdate,
                NotificationChannel.Both,
                taskUpdate.Id,
                "TaskUpdate",
                null,
                $"/tasks/{task.Id}/updates/{taskUpdate.Id}");
        }

        await _unitOfWork.TaskUpdates.UpdateAsync(taskUpdate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var statusMessage = request.Approved
            ? "Task update approved by admin."
            : "Task update rejected by admin. Feedback has been sent to the submitter.";

        return new ApproveTaskUpdateByAdminResponse
        {
            UpdateId = taskUpdate.Id,
            Status = taskUpdate.Status,
            Message = statusMessage
        };
    }
}

