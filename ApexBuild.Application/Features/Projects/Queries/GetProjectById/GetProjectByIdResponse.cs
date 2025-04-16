using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public string ProjectType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? ExpectedEndDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public decimal? Budget { get; init; }
    public string? Currency { get; init; }
    public Guid? ProjectOwnerId { get; init; }
    public string? ProjectOwnerName { get; init; }
    public Guid? ProjectAdminId { get; init; }
    public string? ProjectAdminName { get; init; }
    public string? CoverImageUrl { get; init; }
    public List<string>? ImageUrls { get; init; }
    public Dictionary<string, object>? MetaData { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int DepartmentCount { get; init; }
    public int UserCount { get; init; }
}

