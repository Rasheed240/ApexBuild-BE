namespace ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;

public record OrganizationListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? OwnerName { get; init; }
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public int MemberCount { get; init; }
    public int DepartmentCount { get; init; }
}

public record ListOrganizationsResponse
{
    public List<OrganizationListItemDto> Organizations { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

