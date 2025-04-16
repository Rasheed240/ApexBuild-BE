using MediatR;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery : IRequest<GetProjectByIdResponse>
{
    public Guid ProjectId { get; init; }
}

