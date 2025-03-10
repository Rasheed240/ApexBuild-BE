using MediatR;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationById;

public record GetOrganizationByIdQuery : IRequest<GetOrganizationByIdResponse>
{
    public Guid OrganizationId { get; init; }
}

