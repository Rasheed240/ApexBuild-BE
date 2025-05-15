namespace ApexBuild.Application.Features.Tasks.Commands.ReviewTaskUpdate;

public class ReviewTaskUpdateResponse
{
    public Guid TaskUpdateId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime ReviewedAt { get; set; }
}
