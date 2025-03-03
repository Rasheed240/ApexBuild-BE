using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Organizations.Commands.DeleteOrganization;

public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, DeleteOrganizationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteOrganizationResponse> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to delete an organization");
        }

        var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Check authorization: Only organization owner or super admin can delete
        if (organization.OwnerId != currentUserId.Value &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to delete this organization");
        }

        // Soft delete
        organization.IsDeleted = true;
        organization.DeletedAt = DateTime.UtcNow;
        organization.DeletedBy = currentUserId.Value;
        organization.IsActive = false; // Also deactivate

        await _unitOfWork.Organizations.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteOrganizationResponse
        {
            OrganizationId = organization.Id,
            Message = "Organization deleted successfully"
        };
    }
}

