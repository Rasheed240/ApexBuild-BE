namespace ApexBuild.Application.Features.Organizations.Commands.AddMember;

public record AddMemberResponse
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

