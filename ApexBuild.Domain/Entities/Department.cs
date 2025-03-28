using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class Department : BaseAuditableEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? OrganizationId { get; set; } // If outsourced
        public Guid? SupervisorId { get; set; }
        public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsOutsourced { get; set; } = false;
        public string? Specialization { get; set; } // Electrical, Plumbing, etc.
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual Project Project { get; set; } = null!;
        public virtual Organization? Organization { get; set; }
        public virtual User? Supervisor { get; set; }
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<WorkInfo> WorkInfos { get; set; } = new List<WorkInfo>();
    }
}