using System.Linq.Expressions;
using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Projects.Queries.ListProjects;

public class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, ListProjectsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListProjectsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ListProjectsResponse> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.HasRole("SuperAdmin") || _currentUserService.HasRole("PlatformAdmin");

        // Build predicate with all conditions in one expression
        Expression<Func<Project, bool>> predicate = p => !p.IsDeleted &&
            (!request.Status.HasValue || p.Status == request.Status.Value) &&
            (string.IsNullOrWhiteSpace(request.ProjectType) || p.ProjectType.ToString().ToLowerInvariant().Contains(request.ProjectType.Trim().ToLowerInvariant())) &&
            (string.IsNullOrWhiteSpace(request.SearchTerm) || 
             p.Name.ToLower().Contains(request.SearchTerm.Trim().ToLower()) ||
             p.Code.ToLower().Contains(request.SearchTerm.Trim().ToLower()) ||
             (p.Description != null && p.Description.ToLower().Contains(request.SearchTerm.Trim().ToLower()))) &&
            (!request.OwnerId.HasValue || p.ProjectOwnerId == request.OwnerId.Value) &&
            (isAdmin || !currentUserId.HasValue || 
             request.OwnerId == currentUserId ||
             p.ProjectOwnerId == currentUserId.Value ||
             p.ProjectAdminId == currentUserId.Value ||
             p.ProjectUsers.Any(pu => pu.UserId == currentUserId.Value && pu.Status == Domain.Enums.ProjectUserStatus.Active));

        // Check authorization for owner filter
        if (request.OwnerId.HasValue && request.OwnerId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("You do not have permission to view projects for this owner");
        }

        // Get paginated results
        var (items, totalCount) = await _unitOfWork.Projects.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate,
            q => q.OrderByDescending(p => p.CreatedAt),
            p => p.ProjectOwner,
            p => p.Departments,
            p => p.ProjectUsers);

        var projectDtos = items.Select(p => new ProjectListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            Status = p.Status,
            ProjectType = p.ProjectType.ToString(),
            StartDate = p.StartDate,
            ExpectedEndDate = p.ExpectedEndDate,
            Budget = p.Budget,
            Currency = p.Currency,
            CoverImageUrl = p.CoverImageUrl,
            ProjectOwnerName = p.ProjectOwner?.FullName,
            CreatedAt = p.CreatedAt,
            DepartmentCount = p.Departments?.Count(d => !d.IsDeleted) ?? 0,
            UserCount = p.ProjectUsers?.Count(pu => pu.Status == Domain.Enums.ProjectUserStatus.Active) ?? 0
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new ListProjectsResponse
        {
            Projects = projectDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1,
            HasNextPage = request.PageNumber < totalPages
        };
    }
}
