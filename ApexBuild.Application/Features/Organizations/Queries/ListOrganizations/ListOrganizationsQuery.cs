using MediatR;

namespace ApexBuild.Application.Features.Organizations.Queries.ListOrganizations;

public record ListOrganizationsQuery : IRequest<ListOrganizationsResponse>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool? IsActive { get; init; }
    public bool? IsVerified { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? OwnerId { get; init; }
}

