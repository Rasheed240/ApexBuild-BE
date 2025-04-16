using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByUser;

public record UserProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public string ProjectType { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? ExpectedEndDate { get; init; }
    public string? CoverImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RoleName { get; init; }
    public Domain.Enums.RoleType? RoleType { get; init; }
}

public record GetProjectsByUserResponse
{
    public List<UserProjectDto> Projects { get; init; } = new();
    public int TotalCount { get; init; }
}

