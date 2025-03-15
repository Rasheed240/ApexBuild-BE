using MediatR;
using ApexBuild.Contracts.Responses.DTOs;

namespace ApexBuild.Application.Features.Organizations.Queries.GetOrganizationMembers
{
    public record GetOrganizationMembersQuery : IRequest<GetOrganizationMembersResponse>
    {
        public Guid OrganizationId { get; init; }
        public bool? IsActive { get; init; }
        public string? SearchTerm { get; init; }
    }

    public record GetOrganizationMembersResponse
    {
        public List<OrganizationMemberDto> Members { get; init; } = new();
        public int TotalMembers { get; init; }
        public int ActiveMembers { get; init; }
    }
}
