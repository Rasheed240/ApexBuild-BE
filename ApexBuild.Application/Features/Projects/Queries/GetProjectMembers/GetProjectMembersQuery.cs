using MediatR;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectMembers
{
    public record GetProjectMembersQuery : IRequest<GetProjectMembersResponse>
    {
        public Guid ProjectId { get; init; }
        public Guid? DepartmentId { get; init; }
        public string? SearchTerm { get; init; }
        public bool? IsActive { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }
}
