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
            {
                throw new UnauthorizedException("User not authenticated");
            }

            // Check if organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
            if (organization == null || organization.IsDeleted)
            {
                throw new NotFoundException("Organization", request.OrganizationId);
            }

            // Platform admins and super admins can access any organization
            var isPlatformAdmin = _currentUserService.HasRole("SuperAdmin") ||
                                  _currentUserService.HasRole("PlatformAdmin");

            if (!isPlatformAdmin)
            {
                // Check if current user is a member or owner of the organization
                var isMember = await _unitOfWork.OrganizationMembers.IsMemberAsync(
                    request.OrganizationId,
                    currentUserId.Value,
                    cancellationToken);

                var isOwner = organization.OwnerId == currentUserId.Value;

                if (!isMember && !isOwner)
                {
                    throw new ForbiddenException("You do not have access to this organization");
                }
            }

            // Get all members with user details
            var members = await _unitOfWork.OrganizationMembers
                .GetMembersByOrganizationAsync(request.OrganizationId, cancellationToken);

            var membersList = members.ToList();

            // Apply filtering
            if (request.IsActive.HasValue)
            {
                membersList = membersList.Where(m => m.IsActive == request.IsActive.Value).ToList();
            }

            // Load user details for each member
            var memberDtos = new List<OrganizationMemberDto>();
            foreach (var member in membersList)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(member.UserId, cancellationToken);
                if (user != null)
                {
                    // Get user roles for this organization
                    var userRoles = await _unitOfWork.UserRoles.FindAsync(
                        ur => ur.UserId == member.UserId &&
                              ur.OrganizationId == request.OrganizationId &&
                              ur.IsActive,
                        cancellationToken);

                    var roleNames = new List<string>();
                    foreach (var userRole in userRoles)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(userRole.RoleId, cancellationToken);
                        if (role != null)
                        {
                            roleNames.Add(role.Name);
                        }
                    }

                    var memberDto = new OrganizationMemberDto
                    {
                        UserId = member.UserId,
                        UserName = user.FullName,
                        Email = user.Email,
                        Position = member.Position ?? "Member",
                        ProfileImageUrl = user.ProfileImageUrl,
                        Roles = roleNames,
                        IsActive = member.IsActive,
                        JoinedAt = member.JoinedAt,
                        PhoneNumber = user.PhoneNumber,
                        IsOwner = organization.OwnerId == member.UserId
                    };

                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var searchLower = request.SearchTerm.ToLower();
                        if (memberDto.UserName.ToLower().Contains(searchLower) ||
                            memberDto.Email.ToLower().Contains(searchLower) ||
                            (memberDto.Position?.ToLower().Contains(searchLower) ?? false))
                        {
                            memberDtos.Add(memberDto);
                        }
                    }
                    else
                    {
                        memberDtos.Add(memberDto);
                    }
                }
            }

            return new GetOrganizationMembersResponse
            {
                Members = memberDtos.OrderByDescending(m => m.IsActive).ThenBy(m => m.UserName).ToList(),
                TotalMembers = membersList.Count,
                ActiveMembers = membersList.Count(m => m.IsActive)
            };
        }
    }
}
