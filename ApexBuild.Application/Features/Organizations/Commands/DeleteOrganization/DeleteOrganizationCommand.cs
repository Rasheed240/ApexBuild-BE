using MediatR;

namespace ApexBuild.Application.Features.Organizations.Commands.DeleteOrganization;

public record DeleteOrganizationCommand : IRequest<DeleteOrganizationResponse>
{
    public Guid OrganizationId { get; init; }
}

