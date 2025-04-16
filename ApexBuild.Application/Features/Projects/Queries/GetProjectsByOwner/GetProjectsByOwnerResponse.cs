using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByOwner;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public string ProjectType { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? ExpectedEndDate { get; init; }
    public decimal? Budget { get; init; }
    public string? Currency { get; init; }
    public string? CoverImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public int DepartmentCount { get; init; }
    public int UserCount { get; init; }
}

public record GetProjectsByOwnerResponse
{
    public List<ProjectDto> Projects { get; init; } = new();
    public int TotalCount { get; init; }
}

