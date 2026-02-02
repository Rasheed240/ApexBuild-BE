namespace ApexBuild.Domain.Enums
{
    public enum TaskStatus
    {
        NotStarted = 1,
        InProgress = 2,
        OnHold = 3,
        UnderReview = 4,    // Update submitted, awaiting review
        Approved = 5,       // Approved through review chain
        Rejected = 6,       // Rejected - needs rework
        Completed = 7,      // Final completion confirmed by ProjectAdmin
        Cancelled = 8
    }
}
