using MediatR;

namespace ApexBuild.Application.Features.Organizations.Commands.AddMember;

public record AddMemberCommand : IRequest<AddMemberResponse>
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string Position { get; init; } = string.Empty;
}

