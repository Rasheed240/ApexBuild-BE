using MediatR;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationsByOwner;

public record GetOrganizationsByOwnerQuery : IRequest<GetOrganizationsByOwnerResponse>
{
    public Guid? OwnerId { get; init; } // If null, uses current user
}

