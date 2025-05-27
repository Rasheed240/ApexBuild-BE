using MediatR;
using System.Linq.Expressions;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Application.Features.Tasks.Common;
using ApexBuild.Domain.Enums;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Tasks.Queries.ListTasks;

public class ListTasksQueryHandler : IRequestHandler<ListTasksQuery, ListTasksResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListTasksQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ListTasksResponse> Handle(ListTasksQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to list tasks");
        }

        // If filtering by assigned user, get task IDs from TaskUsers table first
        List<Guid>? assignedTaskIds = null;
        if (request.AssignedToUserId.HasValue)
        {
            var assignedTaskUsers = await _unitOfWork.TaskUsers.FindAsync(
                tu => tu.UserId == request.AssignedToUserId.Value && tu.IsActive,
                cancellationToken);
            assignedTaskIds = assignedTaskUsers.Select(tu => tu.TaskId).ToList();

            // If no tasks found for this user, return empty result
            if (!assignedTaskIds.Any())
            {
                return new ListTasksResponse
                {
                    Tasks = new List<TaskDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
        }

        // Build predicate with all conditions in one expression
        Expression<Func<ProjectTask, bool>> finalPredicate = t =>
            (!request.Status.HasValue || t.Status == request.Status.Value) &&
            (!request.Priority.HasValue || t.Priority == request.Priority.Value) &&
            (!request.DepartmentId.HasValue || t.DepartmentId == request.DepartmentId.Value) &&
            (assignedTaskIds == null || assignedTaskIds.Contains(t.Id)) &&
            (request.ParentTaskId.HasValue ? t.ParentTaskId == request.ParentTaskId.Value : !t.ParentTaskId.HasValue) &&
            (string.IsNullOrWhiteSpace(request.SearchTerm) ||
             t.Title.ToLower().Contains(request.SearchTerm!.Trim().ToLowerInvariant()) ||
             (t.Description != null && t.Description.ToLower().Contains(request.SearchTerm.Trim().ToLowerInvariant())) ||
             t.Code.ToLower().Contains(request.SearchTerm.Trim().ToLowerInvariant()));

        // Get paginated results using GetPagedAsync
        var (tasksList, totalCount) = await _unitOfWork.Tasks.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            finalPredicate,
            q => q.OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate),
            t => t.Department);

        // Load additional related entities if needed
        var departmentIds = tasksList.Select(t => t.DepartmentId).Distinct().ToList();
        var departments = await _unitOfWork.Departments.FindAsync(
            d => departmentIds.Contains(d.Id),
            cancellationToken);
        var departmentsDict = departments.ToDictionary(d => d.Id, d => d.Name);

        // Get task IDs and load assignees
        var taskIds = tasksList.Select(t => t.Id).ToList();
        var taskUsers = await _unitOfWork.TaskUsers.FindAsync(
            tu => taskIds.Contains(tu.TaskId),
            cancellationToken);

        // Group by task ID
        var taskUsersDict = taskUsers.GroupBy(tu => tu.TaskId).ToDictionary(g => g.Key, g => g.ToList());

        // Load all users involved
        var userIds = taskUsers.Select(tu => tu.UserId).Concat(taskUsers.Where(tu => tu.AssignedByUserId.HasValue).Select(tu => tu.AssignedByUserId!.Value)).Distinct().ToList();
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
                DueDate = t.DueDate,
                Progress = t.Progress,
                ImageUrls = t.ImageUrls,
                VideoUrls = t.VideoUrls,
                AttachmentUrls = t.AttachmentUrls,
                CreatedAt = t.CreatedAt
            };
        }).ToList();

        return new ListTasksResponse
        {
            Tasks = taskDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

