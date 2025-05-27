using ApexBuild.Domain.Enums;

namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingTaskUpdates;

public class GetPendingTaskUpdatesResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public ICollection<PendingTaskUpdateDto> Updates { get; set; } = new List<PendingTaskUpdateDto>();
}

public class PendingTaskUpdateDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskCode { get; set; }
    public string TaskDescription { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public UpdateStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string Description { get; set; }
    public SubmittedByDto SubmittedBy { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public ReviewerDto LastReviewedBy { get; set; }
    public ICollection<MediaDto> Media { get; set; } = new List<MediaDto>();
    public int CommentCount { get; set; }
    public TimeSpan TimeAwaitingReview => DateTime.UtcNow - SubmittedAt;
}

public class SubmittedByDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string ProfileImageUrl { get; set; }
    public string RoleName { get; set; }
    public string DepartmentName { get; set; }
}

public class ReviewerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string RoleName { get; set; }
    public string ReviewNotes { get; set; }
}

public class MediaDto
{
    public Guid Id { get; set; }
    public string Url { get; set; }
    public string MediaType { get; set; }
    public string FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

