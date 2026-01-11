using MediatR;

namespace ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

public record GetTopProjectProgressQuery(int Count = 3, Guid? OrganizationId = null) : IRequest<GetTopProjectProgressResponse>;
