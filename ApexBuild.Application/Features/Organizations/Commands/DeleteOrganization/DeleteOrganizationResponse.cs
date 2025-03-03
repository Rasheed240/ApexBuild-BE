namespace ApexBuild.Application.Features.Organizations.Commands.DeleteOrganization;

public record DeleteOrganizationResponse
{
    public Guid OrganizationId { get; init; }
    public string Message { get; init; } = string.Empty;
}

