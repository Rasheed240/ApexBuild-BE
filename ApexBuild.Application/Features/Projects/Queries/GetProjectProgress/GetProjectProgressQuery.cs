using System;
using MediatR;
using ApexBuild.Application.Features.Projects.Queries.GetTopProjectProgress;

namespace ApexBuild.Application.Features.Projects.Queries.GetProjectProgress;

public record GetProjectProgressQuery(Guid ProjectId) : IRequest<ProjectProgressDto>;
