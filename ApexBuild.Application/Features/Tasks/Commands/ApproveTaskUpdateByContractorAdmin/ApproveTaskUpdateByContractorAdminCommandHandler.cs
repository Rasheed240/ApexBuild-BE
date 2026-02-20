using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ApproveTaskUpdateByContractorAdmin;

public class ApproveTaskUpdateByContractorAdminCommandHandler
    : IRequestHandler<ApproveTaskUpdateByContractorAdminCommand, ApproveTaskUpdateByContractorAdminResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public ApproveTaskUpdateByContractorAdminCommandHandler(
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

    public async Task<ApproveTaskUpdateByContractorAdminResponse> Handle(
        ApproveTaskUpdateByContractorAdminCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
            throw new UnauthorizedException("User must be authenticated to review task updates");

        var taskUpdate = await _unitOfWork.TaskUpdates.GetWithDetailsAsync(request.UpdateId, cancellationToken);
        if (taskUpdate == null || taskUpdate.IsDeleted)
            throw new NotFoundException("Task Update", request.UpdateId);

        var task = await _unitOfWork.Tasks.GetByIdAsync(taskUpdate.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
            throw new NotFoundException("Task", taskUpdate.TaskId);

        // Must be a contracted task
        if (!task.ContractorId.HasValue)
            throw new BadRequestException("This task does not have a contractor. Contractor Admin review is not applicable.");

        var contractor = await _unitOfWork.Contractors.GetByIdAsync(task.ContractorId.Value, cancellationToken);
        if (contractor == null)
            throw new NotFoundException("Contractor", task.ContractorId.Value);

        var department = await _unitOfWork.Departments.GetByIdAsync(task.DepartmentId, cancellationToken);
        if (department == null || department.IsDeleted)
            throw new NotFoundException("Department", task.DepartmentId);

        var project = await _unitOfWork.Projects.GetByIdAsync(department.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
            throw new NotFoundException("Project", department.ProjectId);

        // Authorization: must be the contractor admin, project admin/owner, or platform admin
        var isContractorAdmin = contractor.ContractorAdminId == currentUserId.Value;
        var isProjectAdmin = project.ProjectAdminId == currentUserId.Value || project.ProjectOwnerId == currentUserId.Value;
        var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") || _currentUserService.HasRole("PlatformAdmin");

        if (!isContractorAdmin && !isProjectAdmin && !isPlatformAdmin)
            throw new ForbiddenException("Only the contractor admin, project admin, or platform admins can review at this stage.");

        if (taskUpdate.Status != UpdateStatus.UnderContractorAdminReview)
            throw new BadRequestException($"Task update is not awaiting contractor admin review. Current status: {taskUpdate.Status}");

        // Record the review
        taskUpdate.ReviewedByContractorAdminId = currentUserId.Value;
        taskUpdate.ContractorAdminReviewedAt = _dateTimeService.UtcNow;
        taskUpdate.ContractorAdminFeedback = request.Feedback;
        taskUpdate.ContractorAdminApproved = request.Approved;

        if (request.Approved)
        {
            // Move to supervisor review if the department has a supervisor
            if (department.SupervisorId.HasValue)
            {
                taskUpdate.Status = UpdateStatus.UnderSupervisorReview;

                await _notificationService.SendNotificationAsync(
                    department.SupervisorId.Value,
                    "Task Update Approved by Contractor Admin",
                    $"A task update for '{task.Title}' has been approved by the contractor admin and is awaiting your review.",
                    NotificationType.PendingApproval,
                    NotificationChannel.Both,
                    taskUpdate.Id,
                    "TaskUpdate",
                    null,
                    $"/tasks/{task.Id}");
            }
            else
            {
                // No supervisor — go straight to admin
                taskUpdate.Status = UpdateStatus.UnderAdminReview;

                var adminId = project.ProjectAdminId ?? project.ProjectOwnerId;
                if (adminId.HasValue)
                {
                    await _notificationService.SendNotificationAsync(
                        adminId.Value,
                        "Task Update Ready for Admin Review",
                        $"A task update for '{task.Title}' has been approved by contractor admin and is awaiting admin review.",
                        NotificationType.PendingApproval,
                        NotificationChannel.Both,
                        taskUpdate.Id,
                        "TaskUpdate",
                        null,
                        $"/tasks/{task.Id}");
                }
            }
        }
        else
        {
            taskUpdate.Status = UpdateStatus.ContractorAdminRejected;

            // Notify submitter
            await _notificationService.SendNotificationAsync(
                taskUpdate.SubmittedByUserId,
                "Task Update Rejected by Contractor Admin",
                $"Your update for task '{task.Title}' was rejected by the contractor admin. Feedback: {request.Feedback ?? "No feedback provided"}",
                NotificationType.TaskUpdate,
                NotificationChannel.Both,
                taskUpdate.Id,
                "TaskUpdate",
                null,
                $"/tasks/{task.Id}");
        }

        await _unitOfWork.TaskUpdates.UpdateAsync(taskUpdate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var statusMessage = request.Approved
            ? (taskUpdate.Status == UpdateStatus.UnderSupervisorReview
                ? "Update approved by contractor admin. Awaiting supervisor review."
                : "Update approved by contractor admin. Awaiting admin review.")
            : "Update rejected by contractor admin. Feedback sent to submitter.";

        return new ApproveTaskUpdateByContractorAdminResponse
        {
            UpdateId = taskUpdate.Id,
            Status = taskUpdate.Status,
            Message = statusMessage,
        };
    }
}
