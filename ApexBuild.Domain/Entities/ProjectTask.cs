using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

using TaskStatus = ApexBuild.Domain.Enums.TaskStatus;

namespace ApexBuild.Domain.Entities
{
    public class ProjectTask : BaseAuditableEntity, ISoftDelete
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // e.g., TASK-2025-001
        public Guid ProjectId { get; set; }
        public Guid DepartmentId { get; set; }
        public Guid? ParentTaskId { get; set; } // For subtasks
        public Guid? AssignedToUserId { get; set; }
        public Guid? AssignedByUserId { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
        public int Priority { get; set; } = 1; // 1=Low, 2=Medium, 3=High, 4=Critical
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int EstimatedHours { get; set; }
        public int? ActualHours { get; set; }
        public decimal Progress { get; set; } = 0; // 0-100
        public string? Location { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? ImageUrls { get; set; } // Task-related images
        public List<string>? VideoUrls { get; set; } // Task-related videos
        public List<string>? AttachmentUrls { get; set; } // Documents, files
        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual Department Department { get; set; } = null!;
        public virtual ProjectTask? ParentTask { get; set; }
        public virtual ICollection<ProjectTask> Subtasks { get; set; } = new List<ProjectTask>();
        public virtual User? AssignedToUser { get; set; }
        public virtual User? AssignedByUser { get; set; }
        public virtual ICollection<TaskUpdate> Updates { get; set; } = new List<TaskUpdate>();
        public virtual ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>(); // Multiple assignees
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}