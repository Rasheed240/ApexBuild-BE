using MediatR;

namespace ApexBuild.Application.Features.Projects.Commands.DeleteProject;

public record DeleteProjectCommand : IRequest<DeleteProjectResponse>
{
    public Guid ProjectId { get; init; }
}

