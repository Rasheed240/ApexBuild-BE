using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Junction table for many-to-many relationship between Tasks and Users (assignees)
    /// </summary>
    public class TaskUser : BaseAuditableEntity
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string? Role { get; set; } // e.g., "Primary", "Support", "Reviewer"
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public Guid? AssignedByUserId { get; set; }

        // Navigation Properties
        public virtual ProjectTask Task { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual User? AssignedByUser { get; set; }
    }
}
