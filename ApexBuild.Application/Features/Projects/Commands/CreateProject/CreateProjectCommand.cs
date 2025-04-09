using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand : IRequest<CreateProjectResponse>
{
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; } // Optional - will be auto-generated if not provided
    public Guid OrganizationId { get; init; } // Required - project must belong to an organization
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; } = ProjectStatus.Planning;
    public string ProjectType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? ExpectedEndDate { get; init; }
    public decimal? Budget { get; init; }
    public string? Currency { get; init; }
    public Guid? ProjectAdminId { get; init; }
    public string? CoverImageUrl { get; init; }
    public List<string>? ImageUrls { get; init; }
    public Dictionary<string, object>? MetaData { get; init; }
}

