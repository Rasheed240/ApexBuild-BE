using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cache;

    public CreateProjectCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<CreateProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to create a project");
        }

        // Validate that the organization exists
        var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);
        if (organization == null)
        {
            throw new NotFoundException("Organization", request.OrganizationId);
        }

        // Validate that the current user is a member of the organization
        var userOrgMembership = await _unitOfWork.OrganizationMembers.FindAsync(
            om => om.UserId == currentUserId.Value && om.OrganizationId == request.OrganizationId && om.IsActive,
            cancellationToken);

        if (!userOrgMembership.Any())
        {
            throw new ForbiddenException("You must be a member of the organization to create projects in it");
        }

        // Generate project code if not provided
        string projectCode = request.Code ?? await GenerateProjectCodeAsync(cancellationToken);

        // Check if code already exists
        if (await _unitOfWork.Projects.GetByCodeAsync(projectCode, cancellationToken) != null)
        {
            throw new BadRequestException($"Project with code '{projectCode}' already exists");
        }

        // Validate ProjectAdminId if provided
        if (request.ProjectAdminId.HasValue)
        {
            var adminUser = await _unitOfWork.Users.GetByIdAsync(request.ProjectAdminId.Value, cancellationToken);
            if (adminUser == null)
            {
                throw new NotFoundException("User", request.ProjectAdminId.Value);
            }

            // Validate that the project admin is also a member of the organization
            var adminOrgMembership = await _unitOfWork.OrganizationMembers.FindAsync(
                om => om.UserId == request.ProjectAdminId.Value && om.OrganizationId == request.OrganizationId && om.IsActive,
                cancellationToken);

            if (!adminOrgMembership.Any())
            {
                throw new BadRequestException("Project admin must be a member of the organization");
            }
        }

        // Create project
        var project = new Project
        {
            Name = request.Name,
            Code = projectCode,
            OrganizationId = request.OrganizationId,
            Description = request.Description,
            Status = request.Status,
            ProjectType = Enum.TryParse<ProjectType>(request.ProjectType, true, out var pt) ? pt : ProjectType.Building,
            Location = request.Location,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartDate = request.StartDate,
            ExpectedEndDate = request.ExpectedEndDate,
            Budget = request.Budget,
            Currency = request.Currency?.ToUpper(),
            ProjectOwnerId = currentUserId.Value,
            ProjectAdminId = request.ProjectAdminId,
            CoverImageUrl = request.CoverImageUrl,
            ImageUrls = request.ImageUrls,
            MetaData = request.MetaData
        };

        await _unitOfWork.Projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Cache Invalidation ─────────────────────────────────────────────
        // A new project must appear on every project listing immediately.
        await Task.WhenAll(
            _cache.RemoveByPrefixAsync("projects:list:", cancellationToken),
            _cache.RemoveByPrefixAsync($"projects:owner:{currentUserId}", cancellationToken)
        );

        return new CreateProjectResponse
        {
            ProjectId = project.Id,
            Name = project.Name,
            Code = project.Code,
            Message = "Project created successfully"
        };
    }

    private async Task<string> GenerateProjectCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = "PROJ";
        
        // Find all projects with codes matching the pattern for this year
        // We need to check all projects to ensure uniqueness across the system
        var allProjects = await _unitOfWork.Projects.FindAsync(
            p => !p.IsDeleted && p.Code.StartsWith($"{prefix}-{year}-", StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        
        int sequence = 1;
        if (allProjects.Any())
        {
            var sequences = allProjects
                .Select(p =>
                {
                    var parts = p.Code.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int seq))
                        return seq;
                    return 0;
                })
                .Where(s => s > 0)
                .ToList();

            if (sequences.Any())
            {
                sequence = sequences.Max() + 1;
            }
        }

        return $"{prefix}-{year}-{sequence:D3}";
    }
}
