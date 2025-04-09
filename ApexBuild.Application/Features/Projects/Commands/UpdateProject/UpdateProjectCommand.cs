using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Commands.UpdateProject;

public record UpdateProjectCommand : IRequest<UpdateProjectResponse>
{
    public Guid ProjectId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public ProjectStatus? Status { get; init; }
    public string? ProjectType { get; init; }
    public string? Location { get; init; }
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? ExpectedEndDate { get; init; }
    public DateTime? ActualEndDate { get; init; }
    public decimal? Budget { get; init; }
    public string? Currency { get; init; }
    public Guid? ProjectAdminId { get; init; }
    public string? CoverImageUrl { get; init; }
    public List<string>? ImageUrls { get; init; }
    public Dictionary<string, object>? MetaData { get; init; }
}

