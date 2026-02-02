using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Domain.Entities
{
    public class ProjectTask : BaseAuditableEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Human-readable reference e.g. TASK-2025-001</summary>
        public string Code { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }
        public Guid DepartmentId { get; set; }

        /// <summary>Parent task ID for subtasks</summary>
        public Guid? ParentTaskId { get; set; }

        /// <summary>Primary assignee</summary>
        public Guid? AssignedToUserId { get; set; }
        public Guid? AssignedByUserId { get; set; }

        /// <summary>
        /// If this task falls under a contracted company, links to that Contractor.
        /// Drives the review chain: FieldWorker → ContractorAdmin → Supervisor → Admin.
        /// If null, non-contracted chain applies: FieldWorker → Supervisor → Admin.
        /// </summary>
        public Guid? ContractorId { get; set; }

        /// <summary>Optional link to a project milestone this task contributes to</summary>
        public Guid? MilestoneId { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
        public int Priority { get; set; } = 1; // 1=Low, 2=Medium, 3=High, 4=Critical

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int EstimatedHours { get; set; }
        public int? ActualHours { get; set; }

        /// <summary>Overall progress 0-100 (auto-updated from approved task updates)</summary>
        public decimal Progress { get; set; } = 0;

        public string? Location { get; set; }
        public List<string>? Tags { get; set; }

        // Rich media uploads for this task
        public List<string>? ImageUrls { get; set; }
        public List<string>? VideoUrls { get; set; }

        /// <summary>Audio recordings (verbal site notes, instructions, walkthroughs)</summary>
        public List<string>? AudioUrls { get; set; }

        /// <summary>Documents: blueprints, plans, specifications, PDFs</summary>
        public List<string>? AttachmentUrls { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation Properties
        public virtual Department Department { get; set; } = null!;
        public virtual Contractor? Contractor { get; set; }
        public virtual ProjectMilestone? Milestone { get; set; }
        public virtual ProjectTask? ParentTask { get; set; }
        public virtual ICollection<ProjectTask> Subtasks { get; set; } = new List<ProjectTask>();
        public virtual User? AssignedToUser { get; set; }
        public virtual User? AssignedByUser { get; set; }
        public virtual ICollection<TaskUpdate> Updates { get; set; } = new List<TaskUpdate>();
        public virtual ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
