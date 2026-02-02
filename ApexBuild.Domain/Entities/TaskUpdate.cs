using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents a progress update submitted on a task by a FieldWorker.
    ///
    /// Approval chain for CONTRACTED tasks (task.ContractorId != null):
    ///   Submitted → UnderContractorAdminReview → ContractorAdmin(Approved/Rejected)
    ///             → UnderSupervisorReview → Supervisor(Approved/Rejected)
    ///             → UnderAdminReview → Admin(Approved/Rejected)
    ///
    /// Approval chain for NON-CONTRACTED tasks:
    ///   Submitted → UnderSupervisorReview → Supervisor(Approved/Rejected)
    ///             → UnderAdminReview → Admin(Approved/Rejected)
    /// </summary>
    public class TaskUpdate : BaseAuditableEntity, ISoftDelete
    {
        public Guid TaskId { get; set; }
        public Guid SubmittedByUserId { get; set; }

        public string Description { get; set; } = string.Empty;
        public string? Summary { get; set; }

        public UpdateStatus Status { get; set; } = UpdateStatus.Submitted;

        // Media submitted with this update (rich media support)
        public List<string> MediaUrls { get; set; } = new List<string>();
        public List<string> MediaTypes { get; set; } = new List<string>(); // image, video, audio, document

        public decimal ProgressPercentage { get; set; } = 0;

        public DateTime SubmittedAt { get; set; }

        // ── Contractor Admin Review ─────────────────────────────────────────
        public Guid? ReviewedByContractorAdminId { get; set; }
        public DateTime? ContractorAdminReviewedAt { get; set; }
        public string? ContractorAdminFeedback { get; set; }
        public bool? ContractorAdminApproved { get; set; }

        // ── Department Supervisor Review ────────────────────────────────────
        public Guid? ReviewedBySupervisorId { get; set; }
        public DateTime? SupervisorReviewedAt { get; set; }
        public string? SupervisorFeedback { get; set; }
        public bool? SupervisorApproved { get; set; }

        // ── Project Admin Final Review ──────────────────────────────────────
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
        public virtual User? ReviewedByContractorAdmin { get; set; }
        public virtual User? ReviewedBySupervisor { get; set; }
        public virtual User? ReviewedByAdmin { get; set; }
    }
}
