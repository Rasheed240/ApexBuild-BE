using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, GetProjectByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<GetProjectByIdResponse> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Projects.GetWithDetailsAsync(request.ProjectId, cancellationToken);
        
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        // Check authorization: User must be project owner, admin, or a member
        var currentUserId = _currentUserService.UserId;
        var isAuthorized = currentUserId.HasValue && (
            project.ProjectOwnerId == currentUserId.Value ||
            project.ProjectAdminId == currentUserId.Value ||
            project.ProjectUsers.Any(pu => pu.UserId == currentUserId.Value && pu.Status == Domain.Enums.ProjectUserStatus.Active) ||
            _currentUserService.HasRole("SuperAdmin") ||
            _currentUserService.HasRole("PlatformAdmin")
        );

        if (!isAuthorized)
        {
            throw new ForbiddenException("You do not have permission to view this project");
        }

        return new GetProjectByIdResponse
        {
            Id = project.Id,
            Name = project.Name,
            Code = project.Code,
            Description = project.Description,
            Status = project.Status,
            ProjectType = project.ProjectType,
            Location = project.Location,
            Address = project.Address,
            Latitude = project.Latitude,
            Longitude = project.Longitude,
            StartDate = project.StartDate,
            ExpectedEndDate = project.ExpectedEndDate,
            ActualEndDate = project.ActualEndDate,
            Budget = project.Budget,
            Currency = project.Currency,
            ProjectOwnerId = project.ProjectOwnerId,
            ProjectOwnerName = project.ProjectOwner?.FullName,
            ProjectAdminId = project.ProjectAdminId,
            ProjectAdminName = project.ProjectAdmin?.FullName,
            CoverImageUrl = project.CoverImageUrl,
            ImageUrls = project.ImageUrls,
            MetaData = project.MetaData,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            DepartmentCount = project.Departments?.Count(d => !d.IsDeleted) ?? 0,
            UserCount = project.ProjectUsers?.Count(pu => pu.Status == Domain.Enums.ProjectUserStatus.Active) ?? 0
        };
    }
}

