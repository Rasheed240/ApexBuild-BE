using MediatR;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByOwner;

public record GetProjectsByOwnerQuery : IRequest<GetProjectsByOwnerResponse>
{
    public Guid? OwnerId { get; init; } // If null, uses current user
}

