using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingTaskUpdates;

public class GetPendingTaskUpdatesQueryHandler : IRequestHandler<GetPendingTaskUpdatesQuery, GetPendingTaskUpdatesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingTaskUpdatesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetPendingTaskUpdatesResponse> Handle(GetPendingTaskUpdatesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var currentUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        
        if (currentUser == null)
            return new GetPendingTaskUpdatesResponse { Updates = new List<PendingTaskUpdateDto>(), TotalCount = 0 };

        var userRoles = await _unitOfWork.UserRoles.GetAll().Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .ToListAsync(cancellationToken);

        var query = _unitOfWork.TaskUpdates.GetAll()
            .Include(tu => tu.Task)
            .Include(tu => tu.SubmittedByUser)
            .Where(tu => !tu.IsDeleted);

        // Filter by organization context
        var orgIds = _currentUserService.GetOrganizationIds();
        if (orgIds.Any())
        {
            var orgProjectIds = await _unitOfWork.Projects.GetAll()
                .Where(p => orgIds.Contains(p.OrganizationId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            
            query = query.Where(tu => orgProjectIds.Contains(tu.Task.ProjectId));
        }

        // Filter by user's role and responsibilities (CHECK HIGHER ROLES FIRST!)
        if (userRoles.Any(ur => ur.Role.Name == "SuperAdmin" || ur.Role.Name == "PlatformAdmin"))
        {
            // SuperAdmin and PlatformAdmin (OrganizationOwner) see all pending reviews in their organization(s)
            query = query.Where(tu => tu.Status == UpdateStatus.UnderSupervisorReview ||
                                     tu.Status == UpdateStatus.UnderAdminReview);
        }
        else if (userRoles.Any(ur => ur.Role.Name == "DepartmentSupervisor"))
        {
            // Supervisors see reviews waiting under their supervision (UnderSupervisorReview status)
            var supervisedDepartments = await _unitOfWork.DepartmentSupervisors.GetAll()
                .Where(ds => ds.SupervisorId == currentUserId)
                .Select(ds => ds.DepartmentId)
                .ToListAsync(cancellationToken);

            query = query.Where(tu => tu.Status == UpdateStatus.UnderSupervisorReview &&
                                     supervisedDepartments.Contains(tu.Task.DepartmentId));
        }
        else if (userRoles.Any(ur => ur.Role.Level <= 4)) // ProjectOwner, ProjectAdmin level
        {
            // Project admins/owners see reviews waiting for admin approval (UnderAdminReview status)
            var projectIds = await _unitOfWork.Projects.GetAll()
                .Where(p => !p.IsDeleted && (p.CreatedBy == currentUserId ||
                            p.ProjectOwnerId == currentUserId ||
                            p.ProjectAdminId == currentUserId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (projectIds.Any())
            {
                query = query.Where(tu => tu.Status == UpdateStatus.UnderAdminReview &&
                                         projectIds.Contains(tu.Task.ProjectId));
            }
            else
            {
                return new GetPendingTaskUpdatesResponse { Updates = new List<PendingTaskUpdateDto>(), TotalCount = 0 };
            }
        }
        else
        {
            // Other roles don't have review responsibilities
            return new GetPendingTaskUpdatesResponse { Updates = new List<PendingTaskUpdateDto>(), TotalCount = 0 };
        }
        // Apply filters
        if (request.FilterByStatus.HasValue)
            query = query.Where(tu => tu.Status == request.FilterByStatus.Value);

        if (request.FilterByProjectId.HasValue)
            query = query.Where(tu => tu.Task.ProjectId == request.FilterByProjectId.Value);

        if (request.FilterByDepartmentId.HasValue)
            query = query.Where(tu => tu.Task.DepartmentId == request.FilterByDepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(tu => tu.Task.Title.ToLower().Contains(searchTerm) ||
                                     tu.Task.Code.ToLower().Contains(searchTerm) ||
                                     tu.Description.ToLower().Contains(searchTerm) ||
                                     tu.SubmittedByUser.FirstName.ToLower().Contains(searchTerm) ||
                                     tu.SubmittedByUser.LastName.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var updates = await query
            .OrderByDescending(tu => tu.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var commentCounts = await _unitOfWork.TaskComments.GetAll()
            .Where(tc => updates.Select(u => u.TaskId).Contains(tc.TaskId) && !tc.IsDeleted)
            .GroupBy(tc => tc.TaskId)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dtos = updates.Select(tu => new PendingTaskUpdateDto
        {
            Id = tu.Id,
            TaskId = tu.TaskId,
            TaskTitle = tu.Task.Title,
            TaskCode = tu.Task.Code,
            TaskDescription = tu.Task.Description,
            ProjectId = tu.Task.ProjectId,
            ProjectName = _unitOfWork.Projects.FirstOrDefault(p => p.Id == tu.Task.ProjectId)?.Name ?? "Unknown",
            DepartmentId = tu.Task.DepartmentId,
            DepartmentName = _unitOfWork.Departments.FirstOrDefault(d => d.Id == tu.Task.DepartmentId)?.Name ?? "Unknown",
            Status = tu.Status,
            ProgressPercentage = (int)tu.ProgressPercentage,
            Description = tu.Description,
            SubmittedBy = new SubmittedByDto
            {
                Id = tu.SubmittedByUser.Id,
                Name = $"{tu.SubmittedByUser.FirstName} {tu.SubmittedByUser.LastName}",
                Email = tu.SubmittedByUser.Email,
                ProfileImageUrl = tu.SubmittedByUser.ProfileImageUrl,
                RoleName = userRoles.FirstOrDefault(ur => ur.UserId == tu.SubmittedByUser.Id)?.Role.Name ?? "Field Worker",
                DepartmentName = _unitOfWork.Departments.FirstOrDefault(d => d.Id == tu.Task.DepartmentId)?.Name ?? "Unknown"
            },
            SubmittedAt = tu.CreatedAt,
            LastReviewedAt = tu.UpdatedAt,
            LastReviewedBy = tu.ReviewedByAdminId.HasValue ? new ReviewerDto
            {
                Id = tu.ReviewedByAdminId.Value,
                Name = $"{tu.ReviewedBySupervisor?.FirstName ?? "Unknown"} {tu.ReviewedBySupervisor?.LastName ?? ""}".Trim(),
                Email = tu.ReviewedBySupervisor?.Email ?? "Unknown",
                RoleName = "Administrator",
                ReviewNotes = tu.AdminFeedback
            } : null,
            Media = tu.MediaUrls.Select((url, index) => new MediaDto
            {
                Id = Guid.NewGuid(), // Generate a temporary ID for frontend
                Url = url,
                MediaType = index < tu.MediaTypes.Count ? tu.MediaTypes[index] : "unknown",
                FileName = url.Split('/').Last(),
                FileSizeBytes = 0, // Size not stored in current schema
                UploadedAt = tu.CreatedAt
            }).ToList(),
            CommentCount = commentCounts.FirstOrDefault(cc => cc.TaskId == tu.TaskId)?.Count ?? 0
        }).ToList();

        return new GetPendingTaskUpdatesResponse
        {
            Updates = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
