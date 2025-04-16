using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByUser;

public class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, GetProjectsByUserResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsByUserQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetProjectsByUserResponse> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var userId = request.UserId ?? currentUserId;

        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated");
        }

        // Check authorization: Users can only view their own projects unless they're admin
        if (userId != currentUserId &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to view projects for this user");
        }

        var projects = await _unitOfWork.Projects.GetProjectsByUserAsync(userId.Value, cancellationToken);

        var projectDtos = projects.Select(p =>
        {
            var userRole = p.ProjectUsers?.FirstOrDefault(pu => pu.UserId == userId.Value && pu.IsActive);
            return new UserProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                Status = p.Status,
                ProjectType = p.ProjectType,
                StartDate = p.StartDate,
                ExpectedEndDate = p.ExpectedEndDate,
                CoverImageUrl = p.CoverImageUrl,
                CreatedAt = p.CreatedAt,
                RoleName = userRole?.Role?.Name,
                RoleType = userRole?.Role?.RoleType
            };
        }).ToList();

        return new GetProjectsByUserResponse
        {
            Projects = projectDtos,
            TotalCount = projectDtos.Count
        };
    }
}

