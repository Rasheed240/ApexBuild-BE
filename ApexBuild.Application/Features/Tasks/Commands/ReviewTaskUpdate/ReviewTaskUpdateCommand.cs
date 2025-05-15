using MediatR;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Commands.ReviewTaskUpdate;

public record ReviewTaskUpdateCommand : IRequest<ReviewTaskUpdateResponse>
{
    public Guid TaskUpdateId { get; init; }
    public ReviewAction Action { get; init; }
    public string ReviewNotes { get; init; }
    public decimal? AdjustedProgressPercentage { get; init; } // Optional: Allow reviewer to adjust progress
}

public enum ReviewAction
{
    Approve,
    Reject
}
