using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;

namespace ApexBuild.Application.Features.Projects.Commands.DeleteProject;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cache;

    public DeleteProjectCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cache)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedException("User must be authenticated to delete a project");
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null || project.IsDeleted)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        // Check authorization: Only project owner or super admin can delete
        if (project.ProjectOwnerId != currentUserId.Value &&
            !_currentUserService.HasRole("SuperAdmin") &&
            !_currentUserService.HasRole("PlatformAdmin"))
        {
            throw new ForbiddenException("You do not have permission to delete this project");
        }

        // Soft delete
        project.IsDeleted = true;
        project.DeletedAt = DateTime.UtcNow;
        project.DeletedBy = currentUserId.Value;

        await _unitOfWork.Projects.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Cache Invalidation ─────────────────────────────────────────────
        await Task.WhenAll(
            _cache.RemoveAsync($"project:{request.ProjectId}", cancellationToken),
            _cache.RemoveAsync($"project-progress:{request.ProjectId}", cancellationToken),
            _cache.RemoveByPrefixAsync("projects:list:", cancellationToken),
            _cache.RemoveByPrefixAsync($"projects:owner:{project.ProjectOwnerId}", cancellationToken)
        );

        return new DeleteProjectResponse
        {
            ProjectId = project.Id,
            Message = "Project deleted successfully"
        };
    }
}

