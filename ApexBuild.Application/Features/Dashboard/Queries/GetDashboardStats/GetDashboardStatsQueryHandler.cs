using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, GetDashboardStatsResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetDashboardStatsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<GetDashboardStatsResponse> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var currentUserId = _currentUserService.UserId;
            var orgIds = _currentUserService.GetOrganizationIds();

            var activeProjects = await _unitOfWork.Projects.CountAsync(p => p.Status == ProjectStatus.Active, cancellationToken);
            var teamMembers = await _unitOfWork.Users.CountAsync(u => true, cancellationToken);
            var completedTasks = await _unitOfWork.Tasks.CountAsync(t => t.Status == TaskStatus.Completed, cancellationToken);
            var upcomingDeadlines = await _unitOfWork.Tasks.CountAsync(t => t.DueDate != null && t.DueDate >= now && t.DueDate <= now.AddDays(7), cancellationToken);
            
            // Count TaskUpdates that the current user can review (role-based filtering)
            int pendingReviews = 0;
            
            if (currentUserId.HasValue)
            {
                var userRoles = await _unitOfWork.UserRoles.GetAll()
                    .Where(ur => ur.UserId == currentUserId)
                    .Include(ur => ur.Role)
                    .ToListAsync(cancellationToken);

                var query = _unitOfWork.TaskUpdates.GetAll()
                    .Include(tu => tu.Task)
                        .ThenInclude(t => t.Department)
                    .Where(tu => !tu.IsDeleted);

                // Filter by organization context
                if (orgIds.Any())
                {
                    var orgProjectIds = await _unitOfWork.Projects.GetAll()
                        .Where(p => orgIds.Contains(p.OrganizationId))
                        .Select(p => p.Id)
                        .ToListAsync(cancellationToken);
                    
                    query = query.Where(tu => orgProjectIds.Contains(tu.Task.ProjectId));
                }

                // Apply role-based filtering
                if (userRoles.Any(ur => ur.Role.Name == "SuperAdmin"))
                {
                    // SuperAdmins see ALL pending reviews across all organizations
                    pendingReviews = await query
                        .Where(tu => tu.Status == UpdateStatus.UnderSupervisorReview ||
                                    tu.Status == UpdateStatus.UnderAdminReview)
                        .CountAsync(cancellationToken);
                }
                else if (userRoles.Any(ur => ur.Role.Name == "PlatformAdmin"))
                {
                    // PlatformAdmins (OrganizationOwners) see all pending reviews in their organization(s)
                    pendingReviews = await query
                        .Where(tu => tu.Status == UpdateStatus.UnderSupervisorReview ||
                                    tu.Status == UpdateStatus.UnderAdminReview)
                        .CountAsync(cancellationToken);
                }
                else if (userRoles.Any(ur => ur.Role.Name == "DepartmentSupervisor"))
                {
                    // Supervisors see reviews waiting under their supervision
                    var supervisedDepartments = await _unitOfWork.DepartmentSupervisors.GetAll()
                        .Where(ds => ds.SupervisorId == currentUserId)
                        .Select(ds => ds.DepartmentId)
                        .ToListAsync(cancellationToken);

                    pendingReviews = await query
                        .Where(tu => tu.Status == UpdateStatus.UnderSupervisorReview &&
                                    supervisedDepartments.Contains(tu.Task.DepartmentId))
                        .CountAsync(cancellationToken);
                }
                else if (userRoles.Any(ur => ur.Role.Level <= 4)) // ProjectOwner, ProjectAdmin level
                {
                    // Project admins/owners see reviews waiting for admin approval in their projects
                    var projectIds = await _unitOfWork.Projects.GetAll()
                        .Where(p => !p.IsDeleted && (p.CreatedBy == currentUserId ||
                                    p.ProjectOwnerId == currentUserId ||
                                    p.ProjectAdminId == currentUserId))
                        .Select(p => p.Id)
                        .ToListAsync(cancellationToken);

                    if (projectIds.Any())
                    {
                        pendingReviews = await query
                            .Where(tu => tu.Status == UpdateStatus.UnderAdminReview &&
                                        projectIds.Contains(tu.Task.ProjectId))
                            .CountAsync(cancellationToken);
                    }
                }
            }
            
            var totalTasks = await _unitOfWork.Tasks.CountAsync(t => true, cancellationToken);

            return new GetDashboardStatsResponse
            {
                ActiveProjects = activeProjects,
                TeamMembers = teamMembers,
                CompletedTasks = completedTasks,
                UpcomingDeadlines = upcomingDeadlines,
                PendingReviews = pendingReviews,
                TotalTasks = totalTasks
            };
        }
    }
}
