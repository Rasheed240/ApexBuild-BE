using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.SubmitTaskUpdate;

public class SubmitTaskUpdateCommandHandler : IRequestHandler<SubmitTaskUpdateCommand, SubmitTaskUpdateResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public SubmitTaskUpdateCommandHandler(
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

    public async Task<SubmitTaskUpdateResponse> Handle(SubmitTaskUpdateCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to submit a task update");
        }

        // Get task with department
        var task = await _unitOfWork.Tasks.GetByIdWithIncludesAsync(
            request.TaskId,
            cancellationToken,
            t => t.Department,
            t => t.AssignedToUser);

        if (task == null || task.IsDeleted)
        {
            throw new NotFoundException("Task", request.TaskId);
        }

        // Verify user is assigned to the task
        if (task.AssignedToUserId != currentUserId.Value)
        {
            throw new ForbiddenException("Only the assigned user can submit updates for this task");
        }

        // Get department and project
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

        // Check if it's a workday (Monday-Friday)
        var submittedAt = request.SubmittedAt ?? _dateTimeService.UtcNow;
        var submittedDate = submittedAt.Date;
        var dayOfWeek = submittedDate.DayOfWeek;

        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            throw new BadRequestException("Daily reports can only be submitted on workdays (Monday-Friday)");
        }

        // Check if user has already submitted a report for this task today
        var hasReportToday = await _unitOfWork.TaskUpdates.HasDailyReportForDateAsync(
            request.TaskId,
            currentUserId.Value,
            submittedDate,
            cancellationToken);

        if (hasReportToday)
        {
            throw new BadRequestException("You have already submitted a daily report for this task today");
        }

        // Validate media URLs and types match
        if (request.MediaUrls.Count != request.MediaTypes.Count)
        {
            throw new BadRequestException("Number of media URLs must match number of media types");
        }

        // Create task update (daily report)
        var taskUpdate = new TaskUpdate
        {
            TaskId = request.TaskId,
            SubmittedByUserId = currentUserId.Value,
            Description = request.Description,
            Status = UpdateStatus.Submitted,
            MediaUrls = request.MediaUrls,
            MediaTypes = request.MediaTypes,
            ProgressPercentage = request.ProgressPercentage,
            SubmittedAt = submittedAt,
            MetaData = request.MetaData
        };

        await _unitOfWork.TaskUpdates.AddAsync(taskUpdate, cancellationToken);

        // Update task progress
        if (request.ProgressPercentage > task.Progress)
        {
            task.Progress = request.ProgressPercentage;
            await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
        }

        // If progress is 100%, update status to UnderReview
        if (request.ProgressPercentage >= 100 && task.Status != TaskStatus.Completed)
        {
            task.Status = TaskStatus.UnderReview;
            await _unitOfWork.Tasks.UpdateAsync(task, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify department supervisor if exists
        if (department.SupervisorId.HasValue)
        {
            var supervisor = await _unitOfWork.Users.GetByIdAsync(department.SupervisorId.Value, cancellationToken);
            if (supervisor != null)
            {
                taskUpdate.Status = UpdateStatus.UnderSupervisorReview;
                await _unitOfWork.TaskUpdates.UpdateAsync(taskUpdate, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _notificationService.SendNotificationAsync(
                    department.SupervisorId.Value,
                    "New Daily Report Submitted",
                    $"A daily report has been submitted for task '{task.Title}' by {task.AssignedToUser?.FullName ?? "Unknown"}. Please review.",
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
            // If no supervisor, move directly to admin review
            taskUpdate.Status = UpdateStatus.UnderAdminReview;
            await _unitOfWork.TaskUpdates.UpdateAsync(taskUpdate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify project admin or owner
            var adminId = project.ProjectAdminId ?? project.ProjectOwnerId;
            if (adminId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    adminId.Value,
                    "New Daily Report Submitted",
                    $"A daily report has been submitted for task '{task.Title}' by {task.AssignedToUser?.FullName ?? "Unknown"}. Please review.",
                    NotificationType.PendingApproval,
                    NotificationChannel.Both,
                    taskUpdate.Id,
                    "TaskUpdate",
                    null,
                    $"/tasks/{task.Id}/updates/{taskUpdate.Id}");
            }
        }

        return new SubmitTaskUpdateResponse
        {
            UpdateId = taskUpdate.Id,
            TaskId = taskUpdate.TaskId,
            Status = taskUpdate.Status,
            Message = "Daily report submitted successfully. Awaiting supervisor review."
        };
    }
}

