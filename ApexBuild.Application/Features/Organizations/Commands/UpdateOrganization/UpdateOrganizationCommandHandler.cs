using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Organizations.Commands.UpdateOrganization;

public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, UpdateOrganizationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateOrganizationResponse> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to update an organization");
        }

        var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Check authorization: Only organization owner, project owners/admins, or platform admins can update
        var canUpdateOrganization = organization.OwnerId == currentUserId.Value ||
                                   _currentUserService.HasRole("SuperAdmin") ||
                                   _currentUserService.HasRole("PlatformAdmin") ||
                                   _currentUserService.HasRole("ProjectOwner") ||
                                   _currentUserService.HasRole("ProjectAdministrator");

        if (!canUpdateOrganization)
        {
            throw new ForbiddenException("You do not have permission to update this organization");
        }

        // Update organization fields (only update provided fields)
        if (!string.IsNullOrWhiteSpace(request.Name))
            organization.Name = request.Name;

        if (request.Description != null)
            organization.Description = request.Description;

        if (request.RegistrationNumber != null)
            organization.RegistrationNumber = request.RegistrationNumber;

        if (request.TaxId != null)
            organization.TaxId = request.TaxId;

        if (!string.IsNullOrWhiteSpace(request.Email))
            organization.Email = request.Email.ToLower();

        if (request.PhoneNumber != null)
            organization.PhoneNumber = request.PhoneNumber;

        if (request.Website != null)
            organization.Website = request.Website;

        if (request.Address != null)
            organization.Address = request.Address;

        if (request.City != null)
            organization.City = request.City;

        if (request.State != null)
            organization.State = request.State;

        if (request.Country != null)
            organization.Country = request.Country;

        if (request.LogoUrl != null)
            organization.LogoUrl = request.LogoUrl;

        if (request.IsActive.HasValue)
            organization.IsActive = request.IsActive.Value;

        if (request.MetaData != null)
            organization.MetaData = request.MetaData;

        await _unitOfWork.Organizations.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateOrganizationResponse
        {
            OrganizationId = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            Message = "Organization updated successfully"
        };
    }
}

