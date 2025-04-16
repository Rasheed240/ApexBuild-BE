using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Projects.Queries.ListProjects;

public record ListProjectsQuery : IRequest<ListProjectsResponse>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public ProjectStatus? Status { get; init; }
    public string? ProjectType { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? OwnerId { get; init; }
}

