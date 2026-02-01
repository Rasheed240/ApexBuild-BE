using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents a contracted company/firm brought into a project to deliver specific work.
    /// e.g., "ABC Plumbing Co.", "XYZ Electrical Ltd." onboarded for interior wiring.
    ///
    /// The ContractorAdmin is the user who heads the contractor's team on the project.
    /// Field workers supplied by the contractor are linked via WorkInfo.ContractorId.
    /// </summary>
    public class Contractor : BaseAuditableEntity, ISoftDelete
    {
        /// <summary>Company/firm name e.g. "ABC Plumbing Inc."</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Short reference code e.g. "CONTR-2025-001"</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Company registration / business number</summary>
        public string? RegistrationNumber { get; set; }

        /// <summary>Internal contract reference number</summary>
        public string? ContractNumber { get; set; }

        /// <summary>Project this contractor is serving</summary>
        public Guid ProjectId { get; set; }

        /// <summary>Primary department they are contracted to deliver</summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>The user who heads this contractor's team on the project (ContractorAdmin role)</summary>
        public Guid ContractorAdminId { get; set; }

        /// <summary>Area of specialization e.g. Plumbing, Electrical, HVAC, Roofing</summary>
        public string Specialization { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Contract terms
        public DateTime ContractStartDate { get; set; }
        public DateTime ContractEndDate { get; set; }
        public decimal? ContractValue { get; set; }
        public string Currency { get; set; } = "USD";

        /// <summary>Cloudinary URLs (or any CDN) for uploaded contract documents (PDF, DOCX, etc.)</summary>
        public List<string>? ContractDocumentUrls { get; set; }

        public ContractorStatus Status { get; set; } = ContractorStatus.PendingStart;

        public string? Notes { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation Properties
        public virtual Project Project { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual User ContractorAdmin { get; set; } = null!;

        /// <summary>WorkInfo records of all contractor team members on this project</summary>
        public virtual ICollection<WorkInfo> Members { get; set; } = new List<WorkInfo>();

        /// <summary>Tasks assigned to this contractor</summary>
        public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        // Computed
        public bool IsActive => Status == ContractorStatus.Active;
        public bool HasExpired => ContractEndDate < DateTime.UtcNow;
        public bool IsExpiringSoon => ContractEndDate < DateTime.UtcNow.AddDays(14) && ContractEndDate > DateTime.UtcNow;
    }
}
