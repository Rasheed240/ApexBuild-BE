using MediatR;

namespace ApexBuild.Application.Features.Activities.Queries.GetRecentActivities;

public record GetRecentActivitiesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<GetRecentActivitiesResponse>;
