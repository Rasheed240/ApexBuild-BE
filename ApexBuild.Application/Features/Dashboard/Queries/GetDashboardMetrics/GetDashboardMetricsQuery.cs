using MediatR;

namespace ApexBuild.Application.Features.Dashboard.Queries.GetDashboardMetrics;

public record GetDashboardMetricsQuery : IRequest<GetDashboardMetricsResponse>
{
}
