namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationById;

public record OrganizationMemberDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime JoinedAt { get; init; }
}

public record GetOrganizationByIdResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? LogoUrl { get; init; }
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int MemberCount { get; init; }
    public int DepartmentCount { get; init; }
    public List<OrganizationMemberDto> Members { get; init; } = new();
}

