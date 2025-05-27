namespace ApexBuild.Application.Features.Tasks.Common;

public record TaskAssigneeDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? UserEmail { get; init; }
    public string? Role { get; init; }
    public DateTime AssignedAt { get; init; }
    public Guid? AssignedByUserId { get; init; }
    public string? AssignedByName { get; init; }
    public bool IsActive { get; init; }
}
