using MediatR;

namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskComments;

public record GetTaskCommentsQuery : IRequest<GetTaskCommentsResponse>
{
    public Guid TaskId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
