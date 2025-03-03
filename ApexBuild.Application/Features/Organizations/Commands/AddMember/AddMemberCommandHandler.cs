using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Organizations.Commands.AddMember;

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, AddMemberResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AddMemberResponse> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to add members");
        }

        var organization = await _unitOfWork.Organizations.GetWithMembersAsync(request.OrganizationId, cancellationToken);
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Check authorization: Only organization owner or admin can add members
        if (organization.OwnerId != currentUserId.Value &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to add members to this organization");
        }

        // Check if user exists
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // Check if user is already a member
        var existingMember = organization.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (existingMember != null)
        {
            if (existingMember.IsActive)
            {
                throw new BadRequestException("User is already a member of this organization");
            }
            else
            {
                // Reactivate existing member
                existingMember.IsActive = true;
                existingMember.JoinedAt = DateTime.UtcNow;
                existingMember.LeftAt = null;
                existingMember.Position = request.Position;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new AddMemberResponse
                {
                    OrganizationId = organization.Id,
                    UserId = user.Id,
                    UserName = user.FullName,
                    Message = "Member reactivated successfully"
                };
            }
        }

        // Add new member
        var member = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = request.UserId,
            Position = request.Position,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        organization.Members.Add(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddMemberResponse
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            UserName = user.FullName,
            Message = "Member added successfully"
        };
    }
}

