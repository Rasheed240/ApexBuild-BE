using MediatR;
using Microsoft.EntityFrameworkCore;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectMembers
{
    public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, GetProjectMembersResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetProjectMembersQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<GetProjectMembersResponse> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedException("User not authenticated");

            var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null || project.IsDeleted)
                throw new NotFoundException("Project", request.ProjectId);

            // Base: all ProjectUser records for this project (authoritative membership)
            IQueryable<ProjectUser> puQuery = _unitOfWork.ProjectUsers.GetAll()
                .Where(pu => pu.ProjectId == request.ProjectId)
                .Include(pu => pu.User);

            if (request.IsActive.HasValue)
                puQuery = puQuery.Where(pu => pu.IsActive == request.IsActive.Value);

            var projectUsers = await puQuery.ToListAsync(cancellationToken);

            // WorkInfo for dept/contractor/position details (left-join in memory)
            var workInfos = await _unitOfWork.WorkInfos.GetAll()
                .Where(w => w.ProjectId == request.ProjectId)
                .Include(w => w.Department)
                .Include(w => w.Contractor)
                .ToListAsync(cancellationToken);

            // UserRoles for project-level role names
            var projectUserRoles = await _unitOfWork.UserRoles.GetAll()
                .Where(ur => ur.ProjectId == request.ProjectId && ur.IsActive)
                .Include(ur => ur.Role)
                .ToListAsync(cancellationToken);

            var roleNameByUser = projectUserRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(g => g.Key, g => g.First().Role?.Name);

            var workInfoByUser = workInfos
                .GroupBy(w => w.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Apply department filter: keep users who have at least one WorkInfo in that dept
            IEnumerable<ProjectUser> filtered = projectUsers;
            if (request.DepartmentId.HasValue)
            {
                filtered = projectUsers.Where(pu =>
                    workInfoByUser.TryGetValue(pu.UserId, out var wis) &&
                    wis.Any(w => w.DepartmentId == request.DepartmentId.Value));
            }

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                filtered = filtered.Where(pu =>
                    (pu.User?.FullName?.ToLower().Contains(term) ?? false) ||
                    (pu.User?.Email?.ToLower().Contains(term) ?? false));
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var activeCount = filteredList.Count(pu => pu.IsActive);

            var paged = filteredList
                .OrderByDescending(pu => pu.IsActive)
                .ThenBy(pu => pu.User?.FullName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var members = paged.Select(pu =>
            {
                workInfoByUser.TryGetValue(pu.UserId, out var wiList);
                // Prefer WorkInfo matching the requested department; else take first
                var wi = request.DepartmentId.HasValue
                    ? wiList?.FirstOrDefault(w => w.DepartmentId == request.DepartmentId.Value)
                    : wiList?.FirstOrDefault();

                return new ProjectMemberDto
                {
                    UserId       = pu.UserId,
                    WorkInfoId   = wi?.Id,
                    FullName     = pu.User?.FullName ?? string.Empty,
                    Email        = pu.User?.Email ?? string.Empty,
                    ProfileImageUrl = pu.User?.ProfileImageUrl,
                    Position     = wi?.Position,
                    EmployeeId   = wi?.EmployeeId,
                    DepartmentName = wi?.Department?.Name,
                    DepartmentId = wi?.DepartmentId,
                    ContractorName = wi?.Contractor?.CompanyName,
                    ContractorId = wi?.ContractorId,
                    ContractType = wi?.ContractType.ToString() ?? "FullTime",
                    Status       = pu.Status.ToString(),
                    IsActive     = pu.IsActive,
                    StartDate    = wi?.StartDate ?? pu.JoinedAt,
                    EndDate      = wi?.EndDate,
                    Responsibilities = wi?.Responsibilities,
                    ReportingTo  = wi?.ReportingTo,
                    PhoneNumber  = pu.User?.PhoneNumber,
                    RoleName     = roleNameByUser.TryGetValue(pu.UserId, out var rn) ? rn : null,
                };
            }).ToList();

            return new GetProjectMembersResponse
            {
                Members      = members,
                TotalMembers = totalCount,
                ActiveMembers = activeCount,
                PageNumber   = request.PageNumber,
                PageSize     = request.PageSize,
            };
        }
    }
}
