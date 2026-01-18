using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Contracts.Responses.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationMembers
{
    public class GetOrganizationMembersQueryHandler : IRequestHandler<GetOrganizationMembersQuery, GetOrganizationMembersResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetOrganizationMembersQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<GetOrganizationMembersResponse> Handle(
            GetOrganizationMembersQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
                throw new UnauthorizedException("User not authenticated");

            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
            if (organization == null || organization.IsDeleted)
                throw new NotFoundException("Organization", request.OrganizationId);

            var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") ||
                                  _currentUserService.HasRole("PlatformAdmin");

            if (!isPlatformAdmin)
            {
                var isMember = await _unitOfWork.OrganizationMembers.IsMemberAsync(
                    request.OrganizationId, currentUserId.Value, cancellationToken);
                if (!isMember && organization.OwnerId != currentUserId.Value)
                    throw new ForbiddenException("You do not have access to this organization");
            }

            // All members
            var members = (await _unitOfWork.OrganizationMembers
                .GetMembersByOrganizationAsync(request.OrganizationId, cancellationToken))
                .ToList();

            if (request.IsActive.HasValue)
                members = members.Where(m => m.IsActive == request.IsActive.Value).ToList();

            // Batch-load users
            var memberIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await _unitOfWork.Users.GetAll()
                .Where(u => memberIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            // Fetch ALL active roles for these members across all scopes:
            // project-level, org-level, and system-level (SuperAdmin/PlatformAdmin).
            // A member can hold different roles in different projects within this org,
            // so we collect every unique role name they have for filtering purposes.
            var allUserRoles = await _unitOfWork.UserRoles.GetAll()
                .Where(ur => memberIds.Contains(ur.UserId) && ur.IsActive)
                .Include(ur => ur.Role)
                .ToListAsync(cancellationToken);

            // Group role names by user, deduplicated
            var rolesByUser = allUserRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur => ur.Role?.Name).Where(n => n != null).Distinct().OrderBy(n => n).ToList()
                );

            // Build DTOs
            var memberDtos = new List<OrganizationMemberDto>();
            foreach (var member in members)
            {
                if (!users.TryGetValue(member.UserId, out var user)) continue;

                var roles = rolesByUser.TryGetValue(member.UserId, out var r) ? r : new List<string?>();
                var roleNames = roles.Where(n => n != null).Cast<string>().ToList();

                var dto = new OrganizationMemberDto
                {
                    UserId       = member.UserId,
                    UserName     = user.FullName,
                    Email        = user.Email,
                    Position     = member.Position ?? "Member",
                    ProfileImageUrl = user.ProfileImageUrl,
                    Roles        = roleNames,
                    IsActive     = member.IsActive,
                    JoinedAt     = member.JoinedAt,
                    PhoneNumber  = user.PhoneNumber,
                    IsOwner      = organization.OwnerId == member.UserId
                };

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var s = request.SearchTerm.ToLower();
                    if (!dto.UserName.ToLower().Contains(s) && !dto.Email.ToLower().Contains(s))
                        continue;
                }

                memberDtos.Add(dto);
            }

            return new GetOrganizationMembersResponse
            {
                Members       = memberDtos.OrderByDescending(m => m.IsActive).ThenBy(m => m.UserName).ToList(),
                TotalMembers  = members.Count,
                ActiveMembers = members.Count(m => m.IsActive)
            };
        }
    }
}
