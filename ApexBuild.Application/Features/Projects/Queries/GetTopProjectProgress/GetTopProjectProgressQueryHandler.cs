using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

public class GetTopProjectProgressQueryHandler : IRequestHandler<GetTopProjectProgressQuery, GetTopProjectProgressResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTopProjectProgressQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetTopProjectProgressResponse> Handle(GetTopProjectProgressQuery request, CancellationToken cancellationToken)
    {
        var count = request.Count <= 0 ? 3 : request.Count;
        var currentUserId = _currentUserService.UserId;

        // Determine user's roles (org-scoped when organisationId provided)
        IEnumerable<Domain.Entities.UserRole> allUserRoles = await _unitOfWork.UserRoles.FindAsync(
            ur => ur.UserId == currentUserId!.Value && ur.IsActive, cancellationToken);

        if (request.OrganizationId.HasValue)
            allUserRoles = allUserRoles.Where(ur =>
                ur.OrganizationId == request.OrganizationId.Value ||
                ur.Project?.OrganizationId == request.OrganizationId.Value);

        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ur in allUserRoles)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(ur.RoleId, cancellationToken);
            if (role != null) roleNames.Add(role.Name);
        }

        bool isAdminOrAbove = roleNames.Any(r =>
            r is "ProjectAdministrator" or "ProjectOwner" or "PlatformAdmin" or "SuperAdmin");

        // Decide which projects to show
        IEnumerable<Domain.Entities.Project> projects;

        if (isAdminOrAbove)
        {
            // Admins see all projects within the org (or all if no org filter)
            var all = await _unitOfWork.Projects.GetAllAsync(cancellationToken);
            projects = request.OrganizationId.HasValue
                ? all.Where(p => p.OrganizationId == request.OrganizationId.Value)
                : all;
        }
        else if (currentUserId.HasValue)
        {
            // Lower roles see only projects they are assigned to
            var userProjects = await _unitOfWork.Projects.GetProjectsByUserAsync(currentUserId.Value, cancellationToken);
            projects = request.OrganizationId.HasValue
                ? userProjects.Where(p => p.OrganizationId == request.OrganizationId.Value)
                : userProjects;
        }
        else
        {
            projects = Enumerable.Empty<Domain.Entities.Project>();
        }

        projects = projects.OrderByDescending(p => p.CreatedAt).Take(count);

        var result = new GetTopProjectProgressResponse();

        foreach (var p in projects)
        {
            var total = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == p.Id, cancellationToken);
            var completed = await _unitOfWork.Tasks.CountAsync(t => t.Department.ProjectId == p.Id && t.Status == Domain.Enums.TaskStatus.Completed, cancellationToken);
            var progress = total == 0 ? 0 : (int)System.Math.Round((completed * 100.0M) / total);

            result.Items.Add(new ProjectProgressDto
            {
                Id = p.Id,
                Name = p.Name,
                TotalTasks = total,
                CompletedTasks = completed,
                Progress = progress
            });
        }

        return result;
    }
}
