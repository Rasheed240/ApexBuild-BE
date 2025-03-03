namespace ApexBuild.Application.Features.Organizations.Commands.RemoveMember;

public record RemoveMemberResponse
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string Message { get; init; } = string.Empty;
}

