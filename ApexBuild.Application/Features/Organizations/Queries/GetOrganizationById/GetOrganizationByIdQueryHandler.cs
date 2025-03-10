using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationById;

public class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, GetOrganizationByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetOrganizationByIdResponse> Handle(GetOrganizationByIdQuery request, CancellationToken cancellationToken)
    {
        var organization = await _unitOfWork.Organizations.GetWithMembersAsync(request.OrganizationId, cancellationToken);
        
        if (organization == null || organization.IsDeleted)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Check authorization: User must be owner, member, or admin
        var currentUserId = _currentUserService.UserId;
        var isAuthorized = currentUserId.HasValue && (
            organization.OwnerId == currentUserId.Value ||
            organization.Members.Any(m => m.UserId == currentUserId.Value && m.IsActive) ||
            _currentUserService.HasRole("SuperAdmin") ||
            _currentUserService.HasRole("PlatformAdmin")
        );

        if (!isAuthorized)
        {
            throw new ForbiddenException("You do not have permission to view this organization");
        }

        var memberDtos = organization.Members
            .Where(m => m.IsActive)
            .Select(m => new OrganizationMemberDto
            {
                UserId = m.UserId,
                UserName = m.User?.FullName ?? "Unknown",
                UserEmail = m.User?.Email ?? "",
                Position = m.Position,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt
            })
            .ToList();

        return new GetOrganizationByIdResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Code = organization.Code,
            Description = organization.Description,
            RegistrationNumber = organization.RegistrationNumber,
            TaxId = organization.TaxId,
            Email = organization.Email,
            PhoneNumber = organization.PhoneNumber,
            Website = organization.Website,
            Address = organization.Address,
            City = organization.City,
            State = organization.State,
            Country = organization.Country,
            LogoUrl = organization.LogoUrl,
            OwnerId = organization.OwnerId,
            OwnerName = organization.Owner?.FullName ?? "Unknown",
            IsActive = organization.IsActive,
            IsVerified = organization.IsVerified,
            VerifiedAt = organization.VerifiedAt,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt,
            MemberCount = organization.Members?.Count(m => m.IsActive) ?? 0,
            DepartmentCount = organization.Departments?.Count(d => !d.IsDeleted) ?? 0,
            Members = memberDtos
        };
    }
}

