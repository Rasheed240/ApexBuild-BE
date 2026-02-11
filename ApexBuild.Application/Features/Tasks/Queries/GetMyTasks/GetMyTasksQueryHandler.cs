using MediatR;
using System.Linq.Expressions;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Features.Tasks.Common;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Tasks.Queries.GetMyTasks;

public class GetMyTasksQueryHandler : IRequestHandler<GetMyTasksQuery, GetMyTasksResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTasksQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyTasksResponse> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated");
        }

        // Get task IDs where current user is assigned via TaskUsers table
        var assignedTaskUsers = await _unitOfWork.TaskUsers.FindAsync(
            tu => tu.UserId == currentUserId.Value && tu.IsActive,
            cancellationToken);
        var assignedTaskIds = assignedTaskUsers.Select(tu => tu.TaskId).ToList();

        // If no tasks found for this user, return empty result
        if (!assignedTaskIds.Any())
        {
            return new GetMyTasksResponse
            {
                Tasks = new List<TaskDto>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // Build predicate for tasks assigned to current user (exclude subtasks)
        Expression<Func<ProjectTask, bool>> predicate = t =>
            !t.IsDeleted &&
            assignedTaskIds.Contains(t.Id) &&
            !t.ParentTaskId.HasValue &&
            (!request.Status.HasValue || t.Status == request.Status.Value) &&
            (!request.Priority.HasValue || t.Priority == request.Priority.Value) &&
            (string.IsNullOrWhiteSpace(request.SearchTerm) ||
             t.Title.ToLower().Contains(request.SearchTerm!.Trim().ToLower()) ||
             (t.Description != null && t.Description.ToLower().Contains(request.SearchTerm.Trim().ToLower())) ||
             t.Code.ToLower().Contains(request.SearchTerm.Trim().ToLower()));

        // Get paginated results
        var (tasksList, totalCount) = await _unitOfWork.Tasks.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate,
            q => q.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate),
            t => t.Department,
            t => t.Department.Project);

        // Filter by organization/project if specified
        if (request.OrganizationId.HasValue)
        {
            tasksList = tasksList.Where(t =>
                t.Department != null &&
                t.Department.Project != null &&
                t.Department.Project.OrganizationId == request.OrganizationId.Value
            ).ToList();
        }

        if (request.ProjectId.HasValue)
        {
            tasksList = tasksList.Where(t => t.Department != null && t.Department.ProjectId == request.ProjectId.Value).ToList();
        }

        // Update total count after filtering
        var filteredCount = tasksList.Count();

        // Load departments for the tasks we found
        var departmentIds = tasksList.Select(t => t.DepartmentId).Distinct().ToList();
        var departments = await _unitOfWork.Departments.FindAsync(
            d => departmentIds.Contains(d.Id),
            cancellationToken);
        var departmentsDict = departments.ToDictionary(d => d.Id, d => d.Name);

        // Get task IDs and load all task users
        var taskIds = tasksList.Select(t => t.Id).ToList();
        var taskUsers = await _unitOfWork.TaskUsers.FindAsync(
            tu => taskIds.Contains(tu.TaskId),
            cancellationToken);

        var taskUsersDict = taskUsers.GroupBy(tu => tu.TaskId).ToDictionary(g => g.Key, g => g.ToList());

        // Load all users involved
        var userIds = taskUsers.Select(tu => tu.UserId)
            .Concat(taskUsers.Where(tu => tu.AssignedByUserId.HasValue).Select(tu => tu.AssignedByUserId!.Value))
            .Distinct()
            .ToList();
        var users = await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id), cancellationToken);
        var usersDict = users.ToDictionary(u => u.Id);

        var taskDtos = tasksList.Select(t =>
        {
            var assignees = new List<TaskAssigneeDto>();
            if (taskUsersDict.TryGetValue(t.Id, out var tuList))
            {
                assignees = tuList.Select(tu => new TaskAssigneeDto
                {
                    UserId = tu.UserId,
                    UserName = usersDict.TryGetValue(tu.UserId, out var user) ? user.FullName : "Unknown",
                    UserEmail = usersDict.TryGetValue(tu.UserId, out var u) ? u.Email : null,
                    Role = tu.Role,
                    AssignedAt = tu.AssignedAt,
                    AssignedByUserId = tu.AssignedByUserId,
                    AssignedByName = tu.AssignedByUserId.HasValue && usersDict.TryGetValue(tu.AssignedByUserId.Value, out var assignedBy) ? assignedBy.FullName : null,
                    IsActive = tu.IsActive
                }).ToList();
            }

            return new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Code = t.Code,
                Description = t.Description,
                DepartmentId = t.DepartmentId,
                DepartmentName = t.Department?.Name ?? departmentsDict.GetValueOrDefault(t.DepartmentId, "Unknown"),
                ParentTaskId = t.ParentTaskId,
                Assignees = assignees,
                Status = t.Status,
                Priority = t.Priority,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                Progress = t.Progress,
                ImageUrls = t.ImageUrls,
                VideoUrls = t.VideoUrls,
                AttachmentUrls = t.AttachmentUrls,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            };
        }).ToList();

        return new GetMyTasksResponse
        {
            Tasks = taskDtos,
            TotalCount = filteredCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
