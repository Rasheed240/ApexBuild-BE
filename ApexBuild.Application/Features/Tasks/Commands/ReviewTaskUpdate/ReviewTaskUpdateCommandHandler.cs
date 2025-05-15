using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.ReviewTaskUpdate;

public class ReviewTaskUpdateCommandHandler : IRequestHandler<ReviewTaskUpdateCommand, ReviewTaskUpdateResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public ReviewTaskUpdateCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public async Task<ReviewTaskUpdateResponse> Handle(ReviewTaskUpdateCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var taskUpdate = await _unitOfWork.TaskUpdates
            .FirstOrDefaultAsync(tu => tu.Id == request.TaskUpdateId, cancellationToken);

        if (taskUpdate == null)
        {
            return new ReviewTaskUpdateResponse
            {
                Success = false,
                Message = "Task update not found"
            };
        }

        var currentUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (currentUser == null)
        {
            return new ReviewTaskUpdateResponse
            {
                Success = false,
                Message = "Current user not found"
            };
        }

        var task = await _unitOfWork.Tasks.FirstOrDefaultAsync(t => t.Id == taskUpdate.TaskId, cancellationToken);
        if (task == null)
        {
            return new ReviewTaskUpdateResponse
            {
                Success = false,
                Message = "Associated task not found"
            };
        }

        var submittedByUser = await _unitOfWork.Users
            .FirstOrDefaultAsync(u => u.Id == taskUpdate.SubmittedByUserId, cancellationToken);

        // Verify user has authority to review
        var userRoles = await _unitOfWork.UserRoles.GetAll()
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var canReview = false;

        if (taskUpdate.Status == UpdateStatus.UnderSupervisorReview)
        {
            var isSupervisor = userRoles.Any(ur => ur.Role.Name == "DepartmentSupervisor") &&
                              await _unitOfWork.DepartmentSupervisors.GetAll()
                                  .AnyAsync(ds => ds.SupervisorId == currentUserId && ds.DepartmentId == task.DepartmentId, 
                                  cancellationToken);
            canReview = isSupervisor || userRoles.Any(ur => ur.Role.Level <= 4);
        }
        else if (taskUpdate.Status == UpdateStatus.UnderAdminReview)
        {
            canReview = userRoles.Any(ur => ur.Role.Level <= 4);
        }

        if (!canReview)
        {
            return new ReviewTaskUpdateResponse
            {
                Success = false,
                Message = "You do not have authority to review this task update"
            };
        }

        // Update task update based on action
        taskUpdate.ReviewedByAdminId = currentUserId;
        taskUpdate.AdminReviewedAt = DateTime.UtcNow;
        taskUpdate.AdminFeedback = request.ReviewNotes;
        taskUpdate.UpdatedAt = DateTime.UtcNow;

        // Apply adjusted progress percentage if provided by reviewer
        if (request.AdjustedProgressPercentage.HasValue)
        {
            var adjustedProgress = Math.Max(0, Math.Min(100, request.AdjustedProgressPercentage.Value));
            taskUpdate.ProgressPercentage = adjustedProgress;
        }

        var notificationMessage = "";
        var notificationTitle = "";

        if (request.Action == ReviewAction.Approve)
        {
            if (taskUpdate.Status == UpdateStatus.UnderSupervisorReview)
            {
                taskUpdate.Status = UpdateStatus.SupervisorApproved;
                notificationMessage = $"Your task update for '{task.Title}' has been approved by supervisor {currentUser.FirstName} {currentUser.LastName}";
                notificationTitle = "Task Update Approved by Supervisor";

                if (taskUpdate.ProgressPercentage >= 100)
                {
                    task.Status = TaskStatus.UnderReview;
                }

                var project = await _unitOfWork.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId, cancellationToken);
                if (project != null && project.CreatedBy != currentUserId)
                {
                    var projectOwnerId = project.CreatedBy ?? Guid.Empty;
                    taskUpdate.Status = UpdateStatus.UnderAdminReview;
                    notificationMessage = $"Task update for '{task.Title}' from {submittedByUser?.FirstName} {submittedByUser?.LastName} is waiting for your approval";
                    notificationTitle = "Task Update Pending Admin Review";

                    await _notificationService.NotifyUserAsync(
                        projectOwnerId, 
                        notificationTitle,
                        notificationMessage,
                        NotificationType.TaskReview,
                        task.Id,
                        "Task"
                    );
                }
            }
            else if (taskUpdate.Status == UpdateStatus.UnderAdminReview)
            {
                taskUpdate.Status = UpdateStatus.AdminApproved;
                notificationMessage = $"Your task update for '{task.Title}' has been fully approved by administrator {currentUser.FirstName} {currentUser.LastName}";
                notificationTitle = "Task Update Fully Approved";

                if (taskUpdate.ProgressPercentage >= 100)
                {
                    task.Status = TaskStatus.Completed;
                }
            }
        }
        else // Reject
        {
            if (taskUpdate.Status == UpdateStatus.UnderSupervisorReview)
            {
                taskUpdate.Status = UpdateStatus.SupervisorRejected;
                notificationMessage = $"Your task update for '{task.Title}' was rejected by supervisor {currentUser.FirstName} {currentUser.LastName}. Reason: {request.ReviewNotes}";
                notificationTitle = "Task Update Rejected by Supervisor";
            }
            else if (taskUpdate.Status == UpdateStatus.UnderAdminReview)
            {
                taskUpdate.Status = UpdateStatus.AdminRejected;
                notificationMessage = $"Your task update for '{task.Title}' was rejected by administrator {currentUser.FirstName} {currentUser.LastName}. Reason: {request.ReviewNotes}";
                notificationTitle = "Task Update Rejected";
            }
        }

        _unitOfWork.TaskUpdates.Update(taskUpdate);
        if (taskUpdate.ProgressPercentage >= 100)
        {
            _unitOfWork.Tasks.Update(task);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            taskUpdate.SubmittedByUserId,
            notificationTitle,
            notificationMessage,
            NotificationType.TaskReview,
            task.Id,
            "Task"
        );

        return new ReviewTaskUpdateResponse
        {
            TaskUpdateId = request.TaskUpdateId,
            Success = true,
            Message = $"Task update {(request.Action == ReviewAction.Approve ? "approved" : "rejected")} successfully",
            ReviewedAt = taskUpdate.AdminReviewedAt.Value
        };
    }
}
