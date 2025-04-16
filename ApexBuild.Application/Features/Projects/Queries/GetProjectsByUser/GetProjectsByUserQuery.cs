using MediatR;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectsByUser;

public record GetProjectsByUserQuery : IRequest<GetProjectsByUserResponse>
{
    public Guid? UserId { get; init; } // If null, uses current user
}

