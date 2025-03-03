using MediatR;

namespace ApexBuild.Application.Features.Organizations.Commands.RemoveMember;

public record RemoveMemberCommand : IRequest<RemoveMemberResponse>
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
}

