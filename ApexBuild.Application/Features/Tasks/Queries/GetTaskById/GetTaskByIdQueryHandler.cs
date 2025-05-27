using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Features.Tasks.Common;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, GetTaskByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTaskByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetTaskByIdResponse> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to view tasks");
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

        // Get parent task if exists
        var parentTask = task.ParentTaskId.HasValue
            ? await _unitOfWork.Tasks.GetByIdAsync(task.ParentTaskId.Value, cancellationToken)
            : null;

        // Count subtasks
        var subtasks = await _unitOfWork.Tasks.FindAsync(
            t => t.ParentTaskId == task.Id && !t.IsDeleted,
            cancellationToken);

        // Get task assignees
        var taskUsers = await _unitOfWork.TaskUsers.FindAsync(
            tu => tu.TaskId == task.Id,
            cancellationToken);

        var assignees = new List<TaskAssigneeDto>();
        foreach (var taskUser in taskUsers)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(taskUser.UserId, cancellationToken);
            var assignedByUser = taskUser.AssignedByUserId.HasValue
                ? await _unitOfWork.Users.GetByIdAsync(taskUser.AssignedByUserId.Value, cancellationToken)
                : null;

            if (user != null)
            {
                assignees.Add(new TaskAssigneeDto
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    UserEmail = user.Email,
                    Role = taskUser.Role,
                    AssignedAt = taskUser.AssignedAt,
                    AssignedByUserId = taskUser.AssignedByUserId,
                    AssignedByName = assignedByUser?.FullName,
                    IsActive = taskUser.IsActive
                });
            }
        }

        return new GetTaskByIdResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Code = task.Code,
            DepartmentId = task.DepartmentId,
            DepartmentName = department.Name,
            ParentTaskId = task.ParentTaskId,
            ParentTaskTitle = parentTask?.Title,
            SubtaskCount = subtasks.Count(),
            Assignees = assignees,
            AssignedByUserId = task.AssignedByUserId,
            AssignedByName = task.AssignedByUser?.FullName,
            Status = task.Status,
            Priority = task.Priority,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            Progress = task.Progress,
            Location = task.Location,
            Tags = task.Tags,
            ImageUrls = task.ImageUrls,
            VideoUrls = task.VideoUrls,
            AttachmentUrls = task.AttachmentUrls,
            UpdateCount = task.Updates.Count(u => !u.IsDeleted),
            CommentCount = task.Comments.Count(c => !c.IsDeleted),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}

