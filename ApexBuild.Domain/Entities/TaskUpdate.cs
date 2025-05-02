using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;
using Microsoft.Win32;

namespace ApexBuild.Domain.Entities
{
    public class TaskUpdate : BaseAuditableEntity, ISoftDelete
    {
        public Guid TaskId { get; set; }
        public Guid SubmittedByUserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public UpdateStatus Status { get; set; } = UpdateStatus.Submitted;
        public List<string> MediaUrls { get; set; } = new List<string>(); // Photos/Videos
        public List<string> MediaTypes { get; set; } = new List<string>(); // image, video
        public decimal ProgressPercentage { get; set; } = 0;
        public string? Summary { get; set; }
        public DateTime SubmittedAt { get; set; }
        
        // Supervisor Review
        public Guid? ReviewedBySupervisorId { get; set; }
        public DateTime? SupervisorReviewedAt { get; set; }
        public string? SupervisorFeedback { get; set; }
        public bool? SupervisorApproved { get; set; }
        
        // Admin Review
        public Guid? ReviewedByAdminId { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public string? AdminFeedback { get; set; }
        public bool? AdminApproved { get; set; }
        
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual ProjectTask Task { get; set; } = null!;
        public virtual User SubmittedByUser { get; set; } = null!;
        public virtual User? ReviewedBySupervisor { get; set; }
        public virtual User? ReviewedByAdmin { get; set; }
    }
}