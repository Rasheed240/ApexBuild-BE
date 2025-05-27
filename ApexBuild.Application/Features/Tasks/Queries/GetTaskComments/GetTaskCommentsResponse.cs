namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskComments;

public record CommentDto
{
    public Guid Id { get; init; }
    public string Comment { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserProfileImage { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<string>? AttachmentUrls { get; init; }
    public List<CommentDto> Replies { get; init; } = new();
}

public record GetTaskCommentsResponse
{
    public List<CommentDto> Comments { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
