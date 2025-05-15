using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateBySupervisor;

public class ApproveTaskUpdateBySupervisorCommandHandler : IRequestHandler<ApproveTaskUpdateBySupervisorCommand, ApproveTaskUpdateBySupervisorResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public ApproveTaskUpdateBySupervisorCommandHandler(
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

    public async Task<ApproveTaskUpdateBySupervisorResponse> Handle(ApproveTaskUpdateBySupervisorCommand request, CancellationToken cancellationToken)
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

        // Check authorization: Must be department supervisor, project admin/owner, or platform admin
        var isDepartmentSupervisor = department.SupervisorId == currentUserId.Value;
        var isProjectAdmin = project.ProjectAdminId == currentUserId.Value || project.ProjectOwnerId == currentUserId.Value;
        var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") || _currentUserService.HasRole("PlatformAdmin");

        if (!isDepartmentSupervisor && !isProjectAdmin && !isPlatformAdmin)
        {
            throw new ForbiddenException("You do not have permission to review this task update. Only department supervisors, project admins, or platform admins can review.");
        }

        // Verify update is in correct status for supervisor review
        if (taskUpdate.Status != UpdateStatus.Submitted && taskUpdate.Status != UpdateStatus.UnderSupervisorReview)
        {
            throw new BadRequestException($"Task update cannot be reviewed by supervisor in current status: {taskUpdate.Status}");
        }

        // Update review information
        taskUpdate.ReviewedBySupervisorId = currentUserId.Value;
        taskUpdate.SupervisorReviewedAt = _dateTimeService.UtcNow;
        taskUpdate.SupervisorFeedback = request.Feedback;
        taskUpdate.SupervisorApproved = request.Approved;

        if (request.Approved)
        {
            // Move to admin review or mark as fully approved if no admin review needed
            var needsAdminReview = project.ProjectAdminId.HasValue || project.ProjectOwnerId.HasValue;
            
            if (needsAdminReview)
            {
                taskUpdate.Status = UpdateStatus.UnderAdminReview;
                
                // Notify project admin or owner
                var adminId = project.ProjectAdminId ?? project.ProjectOwnerId;
                if (adminId.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        adminId.Value,
                        "Task Update Approved by Supervisor",
                        $"A task update for '{task.Title}' has been approved by supervisor and is pending your review.",
                        NotificationType.PendingApproval,
                        NotificationChannel.Both,
                        taskUpdate.Id,
                        "TaskUpdate",
                        null,
                        $"/tasks/{task.Id}/updates/{taskUpdate.Id}");
                }
            }
            else
            {
                // No admin review needed, mark as supervisor approved (final approval)
                taskUpdate.Status = UpdateStatus.SupervisorApproved;
            }
        }
        else
        {
            taskUpdate.Status = UpdateStatus.SupervisorRejected;
            
            // Notify submitter
            await _notificationService.SendNotificationAsync(
                taskUpdate.SubmittedByUserId,
                "Task Update Needs Revision",
                $"Your daily report for task '{task.Title}' needs revision. Supervisor feedback: {request.Feedback ?? "No feedback provided"}",
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
            ? (taskUpdate.Status == UpdateStatus.UnderAdminReview
                ? "Task update approved by supervisor. Awaiting admin review."
                : "Task update approved by supervisor.")
            : "Task update rejected by supervisor. Feedback has been sent to the submitter.";

        return new ApproveTaskUpdateBySupervisorResponse
        {
            UpdateId = taskUpdate.Id,
            Status = taskUpdate.Status,
            Message = statusMessage
        };
    }
}

