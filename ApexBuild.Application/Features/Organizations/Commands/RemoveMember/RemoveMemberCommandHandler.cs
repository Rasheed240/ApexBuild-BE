using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Organizations.Commands.RemoveMember;

public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, RemoveMemberResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveMemberCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<RemoveMemberResponse> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to remove members");
        }

        var organization = await _unitOfWork.Organizations.GetWithMembersAsync(request.OrganizationId, cancellationToken);
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Check authorization: Only organization owner, project owners/admins, or platform admins can remove members
        // Users can also remove themselves
        var canRemoveMembers = organization.OwnerId == currentUserId.Value ||
                              request.UserId == currentUserId.Value || // User removing themselves
                              _currentUserService.HasRole("SuperAdmin") ||
                              _currentUserService.HasRole("PlatformAdmin") ||
                              _currentUserService.HasRole("ProjectOwner") ||
                              _currentUserService.HasRole("ProjectAdministrator");

        if (!canRemoveMembers)
        {
            throw new ForbiddenException("You do not have permission to remove members from this organization");
        }

        // Additional check: Regular members cannot remove other members (only themselves)
        if (request.UserId != currentUserId.Value &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin") &&
            !_currentUserService.HasRole("ProjectOwner") &&
            !_currentUserService.HasRole("ProjectAdministrator") &&
            organization.OwnerId != currentUserId.Value)
        {
            throw new ForbiddenException("Regular members can only remove themselves from the organization");
        }

        // Prevent removing the owner
        if (request.UserId == organization.OwnerId)
        {
            throw new BadRequestException("Cannot remove the organization owner");
        }

        var member = organization.Members.FirstOrDefault(m => m.UserId == request.UserId && m.IsActive);
        if (member == null)
        {
            throw new NotFoundException("Active member", request.UserId);
        }

        // Soft remove (deactivate)
        member.IsActive = false;
        member.LeftAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RemoveMemberResponse
        {
            OrganizationId = organization.Id,
            UserId = request.UserId,
            Message = "Member removed successfully"
        };
    }
}

