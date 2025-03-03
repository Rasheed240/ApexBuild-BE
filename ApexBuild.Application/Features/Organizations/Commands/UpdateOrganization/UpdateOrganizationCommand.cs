using MediatR;

namespace ApexBuild.Application.Features.Organizations.Commands.UpdateOrganization;

public record UpdateOrganizationCommand : IRequest<UpdateOrganizationResponse>
{
    public Guid OrganizationId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? LogoUrl { get; init; }
    public bool? IsActive { get; init; }
    public Dictionary<string, object>? MetaData { get; init; }
}

