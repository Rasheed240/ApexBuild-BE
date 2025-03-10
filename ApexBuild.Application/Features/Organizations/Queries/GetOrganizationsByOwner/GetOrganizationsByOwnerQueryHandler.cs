using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationsByOwner;

public class GetOrganizationsByOwnerQueryHandler : IRequestHandler<GetOrganizationsByOwnerQuery, GetOrganizationsByOwnerResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizationsByOwnerQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetOrganizationsByOwnerResponse> Handle(GetOrganizationsByOwnerQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var ownerId = request.OwnerId ?? currentUserId;

        if (!ownerId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated");
        }

        // Check authorization: Users can only view their own organizations unless they're admin
        if (ownerId != currentUserId && 
            !_currentUserService.HasRole("SuperAdmin") && 
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to view organizations for this user");
        }

        var organizations = await _unitOfWork.Organizations.GetOrganizationsByOwnerAsync(ownerId.Value, cancellationToken);

        var organizationDtos = organizations.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            Code = o.Code,
            Description = o.Description,
            LogoUrl = o.LogoUrl,
            IsActive = o.IsActive,
            IsVerified = o.IsVerified,
            CreatedAt = o.CreatedAt,
            MemberCount = o.Members?.Count(m => m.IsActive) ?? 0,
            DepartmentCount = o.Departments?.Count(d => !d.IsDeleted) ?? 0
        }).ToList();

        return new GetOrganizationsByOwnerResponse
        {
            Organizations = organizationDtos,
            TotalCount = organizationDtos.Count
        };
    }
}

