namespace ApexBuild.Application.Features.Projects.Commands.UpdateProject;

public record UpdateProjectResponse
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

