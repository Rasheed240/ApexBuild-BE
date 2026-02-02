using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents an invitation to join a project (or organization/department).
    ///
    /// Smart invite flow:
    /// - If the invitee email already belongs to a registered user (IsExistingUser = true),
    ///   only work-specific details are collected — no re-registration needed.
    ///   On acceptance, a WorkInfo record is created automatically.
    /// - If the email is new, the invitee registers with both personal and work info.
    ///
    /// A user in multiple projects will have multiple Invitation records (one per project).
    /// </summary>
    public class Invitation : BaseAuditableEntity
    {
        public Guid InvitedByUserId { get; set; }

        /// <summary>Set when the invitee is already a registered user</summary>
        public Guid? InvitedUserId { get; set; }

        public string Email { get; set; } = string.Empty;

        /// <summary>True if the invitee email matches an existing registered user</summary>
        public bool IsExistingUser { get; set; } = false;

        public Guid RoleId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? DepartmentId { get; set; }

        /// <summary>If inviting into a contractor team, link to the Contractor entity</summary>
        public Guid? ContractorId { get; set; }

        public string Token { get; set; } = string.Empty;
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }

        /// <summary>Optional personal message from the inviter</summary>
        public string? Message { get; set; }

        // Work-specific details (used to create WorkInfo on acceptance)
        public string? Position { get; set; }
        public ContractType ContractType { get; set; } = ContractType.Contract;
        public DateTime? WorkStartDate { get; set; }
        public DateTime? WorkEndDate { get; set; }
        public string? ContractNumber { get; set; }
        public string? ContractDocumentUrl { get; set; }
        public decimal? HourlyRate { get; set; }

        public Dictionary<string, object>? MetaData { get; set; }

        // Navigation Properties
        public virtual User InvitedByUser { get; set; } = null!;
        public virtual User? InvitedUser { get; set; }
        public virtual Role Role { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual Organization? Organization { get; set; }
        public virtual Department? Department { get; set; }
        public virtual Contractor? Contractor { get; set; }
    }
}
