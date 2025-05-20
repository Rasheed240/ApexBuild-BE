namespace ApexBuild.Application.Features.Tasks.Commands.MarkTaskDone;

public class MarkTaskDoneResponse
{
    public Guid TaskId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
