using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, CreateTaskResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICacheService _cache;

    public CreateTaskCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _cache = cache;
    }

    public async Task<CreateTaskResponse> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to create a task");
        }

        // Validate department exists
        var department = await _unitOfWork.Departments.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department == null || department.IsDeleted)
        {
            throw new NotFoundException("Department", request.DepartmentId);
        }

        // Check authorization: User must be ProjectOwner, ProjectAdministrator, or DepartmentSupervisor
        var project = await _unitOfWork.Projects.GetByIdAsync(department.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException("Project", department.ProjectId);
        }

        var hasPermission = project.ProjectOwnerId == currentUserId.Value ||
                           project.ProjectAdminId == currentUserId.Value ||
                           department.SupervisorId == currentUserId.Value ||
                           _currentUserService.HasRole("SuperAdmin") ||
                           _currentUserService.HasRole("PlatformAdmin") ||
                           _currentUserService.HasRole("ProjectOwner") ||
                           _currentUserService.HasRole("ProjectAdministrator");

        if (!hasPermission)
        {
            throw new ForbiddenException("You do not have permission to create tasks in this department");
        }

        // Validate parent task if provided (for subtasks)
        if (request.ParentTaskId.HasValue)
        {
            var parentTask = await _unitOfWork.Tasks.GetByIdAsync(request.ParentTaskId.Value, cancellationToken);
            if (parentTask == null || parentTask.IsDeleted)
            {
                throw new NotFoundException("Parent Task", request.ParentTaskId.Value);
            }

            // Ensure parent task is in the same department
            if (parentTask.DepartmentId != request.DepartmentId)
            {
                throw new BadRequestException(
                    $"Parent task '{parentTask.Code}' belongs to department {parentTask.DepartmentId} " +
                    $"but the new task targets department {request.DepartmentId}. " +
                    "Parent and child tasks must be in the same department.");
            }

            // Prevent circular references (parent cannot be a subtask itself)
            if (parentTask.ParentTaskId.HasValue)
            {
                throw new BadRequestException(
                    $"Task '{parentTask.Code}' is already a subtask of {parentTask.ParentTaskId}. " +
                    "Only one level of nesting is supported.");
            }
        }

        // Validate assigned users are active members of this project
        if (request.AssignedUserIds != null && request.AssignedUserIds.Any())
        {
            foreach (var userId in request.AssignedUserIds)
            {
                var isMember = await _unitOfWork.ProjectUsers.AnyAsync(
                    pu => pu.ProjectId == project.Id && pu.UserId == userId && pu.IsActive,
                    cancellationToken);
                if (!isMember)
                    throw new BadRequestException($"User {userId} is not an active member of this project.");
            }
        }

        // Generate task code
        string taskCode = await GenerateTaskCodeAsync(department, cancellationToken);

        // Create task
        var task = new ProjectTask
        {
            Title = request.Title,
            Description = request.Description,
            Code = taskCode,
            DepartmentId = request.DepartmentId,
            ParentTaskId = request.ParentTaskId,
            AssignedByUserId = currentUserId.Value,
            Status = request.Status,
            Priority = request.Priority,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours,
            Location = request.Location,
            Tags = request.Tags,
            Progress = 0
        };

        await _unitOfWork.Tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create task-user assignments
        if (request.AssignedUserIds != null && request.AssignedUserIds.Any())
        {
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // ── Cache Invalidation ─────────────────────────────────────────────
        // Evict project-task lists and progress so assignees see new task,
        // and the progress bar reflects any status recalculation.
        await Task.WhenAll(
            _cache.RemoveByPrefixAsync($"tasks:project:{project.Id}:", cancellationToken),
            _cache.RemoveByPrefixAsync("tasks:my:", cancellationToken),
            _cache.RemoveAsync($"project-progress:{project.Id}", cancellationToken),
            _cache.RemoveAsync($"dashboard:stats:org:{project.OrganizationId}", cancellationToken)
        );

        return new CreateTaskResponse
        {
            TaskId = task.Id,
            Title = task.Title,
            Code = task.Code,
            Message = "Task created successfully"
        };
    }

    private async Task<string> GenerateTaskCodeAsync(Department department, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(department.ProjectId, cancellationToken);
        if (project == null) throw new NotFoundException("Project", department.ProjectId);

        var year = _dateTimeService.UtcNow.Year;
        var prefix = department.Code.ToUpper().Replace(" ", "-");
        var projectCode = project.Code;
        
        // Format: DEPT-PROJ-2025-001
        var allTasks = await _unitOfWork.Tasks.FindAsync(
            t => !t.IsDeleted && 
                 t.DepartmentId == department.Id &&
                 t.Code.StartsWith($"{prefix}-{projectCode}-{year}-", StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        
        int sequence = 1;
        if (allTasks.Any())
        {
            var sequences = allTasks
                .Select(t =>
                {
                    var parts = t.Code.Split('-');
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int seq))
                        return seq;
                    return 0;
                })
                .Where(s => s > 0)
                .ToList();

            if (sequences.Any())
            {
                sequence = sequences.Max() + 1;
            }
        }

        return $"{prefix}-{projectCode}-{year}-{sequence:D3}";
    }
}

