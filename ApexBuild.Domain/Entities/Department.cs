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

        /// <summary>
        /// If this department's work is delivered by an external contractor company,
        /// link to the Contractor record (which holds company details and contract terms).
        /// </summary>
        public Guid? ContractorId { get; set; }

        /// <summary>Department supervisor (DepartmentSupervisor role). Reports to ProjectAdmin.</summary>
        public Guid? SupervisorId { get; set; }

        public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// True when this department's work is managed by an external Contractor.
        /// Set automatically when ContractorId is assigned.
        /// </summary>
        public bool IsOutsourced { get; set; } = false;

        /// <summary>e.g., Electrical, Plumbing, HVAC, Structural, Roofing, Interior, Exterior</summary>
        public string? Specialization { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation Properties
        public virtual Project Project { get; set; } = null!;
        public virtual Contractor? Contractor { get; set; }
        public virtual User? Supervisor { get; set; }
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<WorkInfo> WorkInfos { get; set; } = new List<WorkInfo>();
        public virtual ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    }
}
