using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// A milestone is a significant checkpoint or goal within a project.
    /// Milestones can be linked to specific departments and can have tasks associated with them.
    /// Tracking milestones gives a world-class overview of project trajectory.
    /// </summary>
    public class ProjectMilestone : BaseAuditableEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Guid ProjectId { get; set; }

        /// <summary>Optional: milestone scoped to a specific department</summary>
        public Guid? DepartmentId { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        public MilestoneStatus Status { get; set; } = MilestoneStatus.Upcoming;

        /// <summary>Overall progress 0-100 (can be auto-computed from linked tasks)</summary>
        public decimal Progress { get; set; } = 0;

        /// <summary>Order/sequence within the project (1 = first milestone)</summary>
        public int OrderIndex { get; set; } = 1;

        public string? Notes { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }
}
