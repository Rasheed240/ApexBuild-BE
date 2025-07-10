using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Activities.Queries.GetRecentActivities;

public class GetRecentActivitiesQueryHandler : IRequestHandler<GetRecentActivitiesQuery, GetRecentActivitiesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetRecentActivitiesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetRecentActivitiesResponse> Handle(GetRecentActivitiesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.PageNumber);
        var pageSize = Math.Max(1, request.PageSize);
        var skip = (page - 1) * pageSize;
        var userId = _currentUserService.UserId;

        var activities = new List<RecentActivityDto>();

        // Recent notifications for the user
        if (userId.HasValue)
        {
            var notifications = await _unitOfWork.Notifications.GetNotificationsByUserAsync(userId.Value, null, null, null, cancellationToken);
            activities.AddRange(notifications.Select(n => new RecentActivityDto
            {
                Id = n.Id,
                Type = "notification",
                Message = n.Message ?? n.Title ?? "Notification",
                Timestamp = n.CreatedAt,
                UserId = n.UserId,
                Link = n.Link
            }));
        }

        // Recent task updates for the user (if authenticated) otherwise global recent updates limited by pageSize * 2
        IEnumerable<Domain.Entities.TaskUpdate> taskUpdates = Array.Empty<Domain.Entities.TaskUpdate>();
        if (userId.HasValue)
        {
            taskUpdates = await _unitOfWork.TaskUpdates.GetUpdatesByUserAsync(userId.Value, cancellationToken);
        }
        else
        {
            // fallback: get all updates via repository GetAll and order by submitted date
            taskUpdates = (await _unitOfWork.TaskUpdates.GetAllAsync(cancellationToken))
                .OrderByDescending(u => u.SubmittedAt)
                .Take(pageSize * 2);
        }

        activities.AddRange(taskUpdates.Select(u => new RecentActivityDto
        {
            Id = u.Id,
            Type = "task_update",
            Message = string.IsNullOrWhiteSpace(u.Summary) ? $"Update on task {u.Task?.Title ?? u.TaskId.ToString()}" : u.Summary,
            Timestamp = u.SubmittedAt,
            RelatedTaskId = u.TaskId,
            RelatedProjectId = u.Task?.Department?.Project?.Id,
            UserId = u.SubmittedByUserId,
            Link = $"/tasks/{u.TaskId}"
        }));

        // Recent projects created (global)
        var recentProjects = (await _unitOfWork.Projects.GetAllAsync(cancellationToken))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new RecentActivityDto
            {
                Id = p.Id,
                Type = "project",
                Message = $"Project '{p.Name}' was created",
                Timestamp = p.CreatedAt,
                RelatedProjectId = p.Id,
                Link = $"/projects/{p.Id}"
            });

        activities.AddRange(recentProjects);

        // Order and page
        var ordered = activities.OrderByDescending(a => a.Timestamp).ToList();
        var total = ordered.Count;
        var paged = ordered.Skip(skip).Take(pageSize).ToList();

        return new GetRecentActivitiesResponse
        {
            Items = paged,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }
}
