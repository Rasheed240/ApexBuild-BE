using MediatR;

namespace ApexBuild.Application.Features.Manuals.Queries.GetLatestManual;

public record GetLatestManualQuery : IRequest<GetLatestManualResponse?>;
