using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Features.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, UpdateProjectResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProjectCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateProjectResponse> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to update a project");
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        // Check authorization: Only project owner or admin can update
        if (project.ProjectOwnerId != currentUserId.Value && 
            project.ProjectAdminId != currentUserId.Value &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to update this project");
        }

        // Validate ProjectAdminId if provided
        if (request.ProjectAdminId.HasValue)
        {
            var adminUser = await _unitOfWork.Users.GetByIdAsync(request.ProjectAdminId.Value, cancellationToken);
            if (adminUser == null)
            {
                throw new NotFoundException("User", request.ProjectAdminId.Value);
            }
        }

        // Update project fields (only update provided fields)
        if (!string.IsNullOrWhiteSpace(request.Name))
            project.Name = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Description))
            project.Description = request.Description;

        if (request.Status.HasValue)
            project.Status = request.Status.Value;

        if (!string.IsNullOrWhiteSpace(request.ProjectType))
            project.ProjectType = request.ProjectType;

        if (request.Location != null)
            project.Location = request.Location;

        if (request.Address != null)
            project.Address = request.Address;

        if (request.Latitude.HasValue)
            project.Latitude = request.Latitude;

        if (request.Longitude.HasValue)
            project.Longitude = request.Longitude;

        if (request.StartDate.HasValue)
            project.StartDate = request.StartDate;

        if (request.ExpectedEndDate.HasValue)
            project.ExpectedEndDate = request.ExpectedEndDate;

        if (request.ActualEndDate.HasValue)
            project.ActualEndDate = request.ActualEndDate;

        if (request.Budget.HasValue)
            project.Budget = request.Budget;

        if (!string.IsNullOrWhiteSpace(request.Currency))
            project.Currency = request.Currency.ToUpper();

        if (request.ProjectAdminId.HasValue)
            project.ProjectAdminId = request.ProjectAdminId;

        if (request.CoverImageUrl != null)
            project.CoverImageUrl = request.CoverImageUrl;

        if (request.ImageUrls != null)
            project.ImageUrls = request.ImageUrls;

        if (request.MetaData != null)
            project.MetaData = request.MetaData;

        // Auto-update status based on dates
        if (project.ActualEndDate.HasValue && project.Status != Domain.Enums.ProjectStatus.Completed)
        {
            project.Status = Domain.Enums.ProjectStatus.Completed;
        }
        else if (project.ExpectedEndDate.HasValue && 
                 project.ExpectedEndDate.Value < DateTime.UtcNow && 
                 !project.ActualEndDate.HasValue &&
                 project.Status == Domain.Enums.ProjectStatus.Active)
        {
            // Project is overdue but not completed - could set to OnHold or leave as Active
            // Leaving as Active for now, but this could be configurable
        }

        await _unitOfWork.Projects.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProjectResponse
        {
            ProjectId = project.Id,
            Name = project.Name,
            Code = project.Code,
            Message = "Project updated successfully"
        };
    }
}

