using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByOwner;

public class GetProjectsByOwnerQueryHandler : IRequestHandler<GetProjectsByOwnerQuery, GetProjectsByOwnerResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsByOwnerQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetProjectsByOwnerResponse> Handle(GetProjectsByOwnerQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var ownerId = request.OwnerId ?? currentUserId;

        if (!ownerId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated");
        }

        // Check authorization: Users can only view their own projects unless they're admin
        if (ownerId != currentUserId && 
            !_currentUserService.HasRole("SuperAdmin") && 
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to view projects for this user");
        }

        var projects = await _unitOfWork.Projects.GetProjectsByOwnerAsync(ownerId.Value, cancellationToken);

        var projectDtos = projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            Status = p.Status,
            ProjectType = p.ProjectType,
            StartDate = p.StartDate,
            ExpectedEndDate = p.ExpectedEndDate,
            Budget = p.Budget,
            Currency = p.Currency,
            CoverImageUrl = p.CoverImageUrl,
            CreatedAt = p.CreatedAt,
            DepartmentCount = p.Departments?.Count(d => !d.IsDeleted) ?? 0,
            UserCount = p.ProjectUsers?.Count(pu => pu.Status == Domain.Enums.ProjectUserStatus.Active) ?? 0
        }).ToList();

        return new GetProjectsByOwnerResponse
        {
            Projects = projectDtos,
            TotalCount = projectDtos.Count
        };
    }
}

