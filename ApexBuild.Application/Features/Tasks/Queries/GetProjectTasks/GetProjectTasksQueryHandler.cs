using MediatR;
using ApexBuild.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Queries.GetProjectTasks;

public class GetProjectTasksQueryHandler : IRequestHandler<GetProjectTasksQuery, GetProjectTasksResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjectTasksQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetProjectTasksResponse> Handle(GetProjectTasksQuery request, CancellationToken cancellationToken)
    {
        var departmentIds = await _unitOfWork.Departments.GetAll()
            .Where(d => d.ProjectId == request.ProjectId && !d.IsDeleted)
            .Select(d => d.Id)
            .ToListAsync();

        if (!departmentIds.Any())
        {
            return new GetProjectTasksResponse
            {
                Tasks = new(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        var query = _unitOfWork.Tasks.GetAll()
            .Where(t => departmentIds.Contains(t.DepartmentId) && !t.IsDeleted && t.ParentTaskId == null);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.Priority.HasValue)
            query = query.Where(t => t.Priority == request.Priority.Value);

        if (request.DepartmentId.HasValue)
            query = query.Where(t => t.DepartmentId == request.DepartmentId.Value);

        if (request.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == request.AssignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(t => t.Title.Contains(request.SearchTerm) || 
                                     t.Description.Contains(request.SearchTerm) ||
                                     t.Code.Contains(request.SearchTerm));

        var totalCount = await query.CountAsync();

        var tasks = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(t => t.Department)
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .Include(t => t.Subtasks)
            .Include(t => t.Updates)
            .Include(t => t.Comments)
            .ToListAsync();

        var tasksDto = tasks.Select(t => new ProjectTaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Code = t.Code,
            Description = t.Description,
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department?.Name ?? string.Empty,
            Status = t.Status,
            Priority = t.Priority,
            Progress = t.Progress,
            StartDate = t.StartDate,
            DueDate = t.DueDate,
            CompletedAt = t.CompletedAt,
            EstimatedHours = t.EstimatedHours,
            ActualHours = t.ActualHours,
            Location = t.Location,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToUserName = t.AssignedToUser?.FullName ?? string.Empty,
            AssignedByUserId = t.AssignedByUserId,
            AssignedByUserName = t.AssignedByUser?.FullName ?? string.Empty,
            Tags = t.Tags,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            Subtasks = request.IncludeSubtasks ? t.Subtasks
                .Where(st => !st.IsDeleted)
                .Select(st => new SubtaskDto
                {
                    Id = st.Id,
                    Title = st.Title,
                    Code = st.Code,
                    Status = st.Status,
                    Priority = st.Priority,
                    Progress = st.Progress,
                    DueDate = st.DueDate,
                    AssignedToUserName = st.AssignedToUser?.FullName ?? string.Empty,
                    RecentUpdates = request.IncludeUpdates ? st.Updates
                        .Where(u => !u.IsDeleted)
                        .OrderByDescending(u => u.CreatedAt)
                        .Take(2)
                        .Select(u => new TaskUpdateDto
                        {
                            Id = u.Id,
                            Description = u.Description,
                            Status = u.Status,
                            ProgressPercentage = u.ProgressPercentage,
                            SubmittedByUserName = u.SubmittedByUser?.FullName ?? string.Empty,
                            SubmittedAt = u.SubmittedAt,
                            SupervisorApproved = u.SupervisorApproved,
                            AdminApproved = u.AdminApproved,
                            SupervisorFeedback = u.SupervisorFeedback,
                            AdminFeedback = u.AdminFeedback,
                            MediaUrls = u.MediaUrls
                        })
                        .ToList() : new()
                })
                .ToList() : new(),
            Updates = request.IncludeUpdates ? t.Updates
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new TaskUpdateDto
                {
                    Id = u.Id,
                    Description = u.Description,
                    Status = u.Status,
                    ProgressPercentage = u.ProgressPercentage,
                    SubmittedByUserName = u.SubmittedByUser?.FullName ?? string.Empty,
                    SubmittedAt = u.SubmittedAt,
                    SupervisorApproved = u.SupervisorApproved,
                    AdminApproved = u.AdminApproved,
                    SupervisorFeedback = u.SupervisorFeedback,
                    AdminFeedback = u.AdminFeedback,
                    MediaUrls = u.MediaUrls
                })
                .ToList() : new(),
            SubtaskCount = t.Subtasks.Count(st => !st.IsDeleted),
            UpdateCount = t.Updates.Count(u => !u.IsDeleted),
            CommentCount = t.Comments.Count(c => !c.IsDeleted)
        }).ToList();

        return new GetProjectTasksResponse
        {
            Tasks = tasksDto,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
