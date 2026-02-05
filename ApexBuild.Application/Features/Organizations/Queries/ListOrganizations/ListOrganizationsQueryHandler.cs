using System.Linq.Expressions;
using MediatR;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Application.Common.Exceptions;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;

public class ListOrganizationsQueryHandler : IRequestHandler<ListOrganizationsQuery, ListOrganizationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ListOrganizationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ListOrganizationsResponse> Handle(ListOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        // Only SuperAdmin sees all orgs. PlatformAdmin sees only the orgs they own or are a member of.
        var isAdmin = _currentUserService.HasRole("SuperAdmin");

        // Build predicate with all conditions in one expression
        Expression<Func<Organization, bool>> predicate = o => !o.IsDeleted &&
            (!request.IsActive.HasValue || o.IsActive == request.IsActive.Value) &&
            (!request.IsVerified.HasValue || o.IsVerified == request.IsVerified.Value) &&
            (string.IsNullOrWhiteSpace(request.SearchTerm) || 
             o.Name.ToLower().Contains(request.SearchTerm.Trim().ToLower()) ||
             o.Code.ToLower().Contains(request.SearchTerm.Trim().ToLower()) ||
             (o.Description != null && o.Description.ToLower().Contains(request.SearchTerm.Trim().ToLower()))) &&
            (!request.OwnerId.HasValue || o.OwnerId == request.OwnerId.Value) &&
            (isAdmin || !currentUserId.HasValue || 
             request.OwnerId == currentUserId ||
             o.OwnerId == currentUserId.Value ||
             o.Members.Any(m => m.UserId == currentUserId.Value && m.IsActive));

        // Check authorization for owner filter
        if (request.OwnerId.HasValue && request.OwnerId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("You do not have permission to view organizations for this owner");
        }

        // Get paginated results
        var (items, totalCount) = await _unitOfWork.Organizations.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            predicate,
            q => q.OrderByDescending(o => o.CreatedAt),
            o => o.Owner,
            o => o.Members,
            o => o.Departments);

        var organizationDtos = items.Select(o => new OrganizationListItemDto
        {
            Id = o.Id,
            Name = o.Name,
            Code = o.Code,
            Description = o.Description,
            LogoUrl = o.LogoUrl,
            OwnerName = o.Owner?.FullName,
            IsActive = o.IsActive,
            IsVerified = o.IsVerified,
            CreatedAt = o.CreatedAt,
            MemberCount = o.Members?.Count(m => m.IsActive) ?? 0,
            DepartmentCount = o.Departments?.Count(d => !d.IsDeleted) ?? 0
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new ListOrganizationsResponse
        {
            Organizations = organizationDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1,
            HasNextPage = request.PageNumber < totalPages
        };
    }
}

