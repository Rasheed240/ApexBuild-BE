using MediatR;

namespace ApexBuild.Application.Features.Tasks.Commands.SubmitTaskUpdate;

public record SubmitTaskUpdateCommand : IRequest<SubmitTaskUpdateResponse>
{
    public Guid TaskId { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<string> MediaUrls { get; init; } = new();
    public List<string> MediaTypes { get; init; } = new(); // "image" or "video"
    public decimal ProgressPercentage { get; init; } = 0;
    public DateTime? SubmittedAt { get; init; } // Optional, defaults to now
    public Dictionary<string, object>? MetaData { get; init; }
}

