namespace ApexBuild.Application.Features.Projects.Commands.DeleteProject;

public record DeleteProjectResponse
{
    public Guid ProjectId { get; init; }
    public string Message { get; init; } = string.Empty;
}

