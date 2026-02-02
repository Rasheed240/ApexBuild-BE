using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Captures a user's work-related details for a specific project assignment.
    /// Each invitation to a project creates a WorkInfo record upon acceptance.
    /// A user can have multiple WorkInfo records across different projects.
    ///
    /// If the user belongs to a contracted company on this project, ContractorId is set.
    /// </summary>
    public class WorkInfo : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? DepartmentId { get; set; }

        /// <summary>If user belongs to a contractor team on this project</summary>
        public Guid? ContractorId { get; set; }

        /// <summary>Job title / position for this project e.g. "Senior Electrician"</summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>Internal or contractor-assigned employee ID</summary>
        public string? EmployeeId { get; set; }

        // Contract period
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ProjectUserStatus Status { get; set; } = ProjectUserStatus.Active;
        public bool IsActive { get; set; } = true;

        public string? Responsibilities { get; set; }
        public decimal? HourlyRate { get; set; }

        public ContractType ContractType { get; set; } = ContractType.Contract;

        /// <summary>Name or UserID of the person this user reports to on this project</summary>
        public string? ReportingTo { get; set; }

        /// <summary>Cloudinary URLs for signed contract / work agreement documents</summary>
        public List<string>? ContractDocumentUrls { get; set; }

        /// <summary>Contract reference / agreement number</summary>
        public string? ContractNumber { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Project Project { get; set; } = null!;
        public virtual Organization? Organization { get; set; }
        public virtual Department? Department { get; set; }
        public virtual Contractor? Contractor { get; set; }
    }
}
