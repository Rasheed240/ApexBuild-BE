using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    public class TaskComment : BaseAuditableEntity, ISoftDelete
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public Guid? ParentCommentId { get; set; }
        public List<string>? AttachmentUrls { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual ProjectTask Task { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual TaskComment? ParentComment { get; set; }
        public virtual ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();
    }
}