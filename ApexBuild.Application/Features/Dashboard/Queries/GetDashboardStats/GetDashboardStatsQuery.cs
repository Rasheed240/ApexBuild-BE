using MediatR;

namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery : IRequest<GetDashboardStatsResponse>
    {
        public Guid? OrganizationId { get; init; }
    }
}
