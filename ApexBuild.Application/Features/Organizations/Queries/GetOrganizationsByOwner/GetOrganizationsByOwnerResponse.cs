namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationsByOwner;

public record OrganizationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public int MemberCount { get; init; }
    public int DepartmentCount { get; init; }
}

public record GetOrganizationsByOwnerResponse
{
    public List<OrganizationDto> Organizations { get; init; } = new();
    public int TotalCount { get; init; }
}

