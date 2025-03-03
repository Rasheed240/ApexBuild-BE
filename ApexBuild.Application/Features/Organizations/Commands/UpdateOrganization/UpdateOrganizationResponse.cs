namespace ApexBuild.Application.Features.Organizations.Commands.UpdateOrganization;

public record UpdateOrganizationResponse
{
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

