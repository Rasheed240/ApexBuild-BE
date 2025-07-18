using MediatR;

namespace ApexBuild.Application.Features.Users.Queries.GetProfileCompletion;

public record GetProfileCompletionQuery : IRequest<GetProfileCompletionResponse>
{
}

