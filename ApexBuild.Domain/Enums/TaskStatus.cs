namespace ApexBuild.Domain.Enums
{
    public enum TaskStatus
    {
        NotStarted = 1,
        InProgress = 2,
        Done = 3,              // Assignee marks as done (awaiting review/submission)
        UnderReview = 4,       // Submission under review
        Approved = 5,          // Approved by reviewer
        Rejected = 6,          // Rejected by reviewer
        Completed = 7,         // Project admin marks as completed
        Pending = 8,           // Waiting for something
        Cancelled = 9          // Task cancelled
    }
}
