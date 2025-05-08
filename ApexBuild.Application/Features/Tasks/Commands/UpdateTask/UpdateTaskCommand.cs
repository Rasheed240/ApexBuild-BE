using MediatR;
using ApexBuild.Domain.Enums;
using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Application.Features.Tasks.Commands.UpdateTask;

public record UpdateTaskCommand : IRequest<UpdateTaskResponse>
{
    public Guid TaskId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<Guid>? AssignedUserIds { get; init; } // Multiple assignees - null means don't update
    public TaskStatus? Status { get; init; }
    public int? Priority { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? DueDate { get; init; }
    public int? EstimatedHours { get; init; }
    public int? ActualHours { get; init; }
    public decimal? Progress { get; init; }
    public string? Location { get; init; }
    public List<string>? Tags { get; init; }
}

