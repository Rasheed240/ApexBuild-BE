using System;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents a user's license within an organization.
    /// Each user in an organization must have a valid, active license.
    /// </summary>
    public class OrganizationLicense : BaseAuditableEntity, ISoftDelete
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        
        // License Information
        public string LicenseKey { get; set; } = string.Empty; // Unique identifier for audit trail
        public LicenseStatus Status { get; set; } = LicenseStatus.Active;
        
        // License Period
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevocationReason { get; set; }
        
        // License Type (for future extensibility)
        public string LicenseType { get; set; } = "Full"; // Full, Limited, Trial, etc.
        
        // Metadata
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Subscription Subscription { get; set; } = null!;
        
        // Computed Properties
        public bool IsActive => Status == LicenseStatus.Active && ValidUntil > DateTime.UtcNow;
        public bool IsExpired => ValidUntil <= DateTime.UtcNow;
        public bool IsExpiringSoon => ValidUntil > DateTime.UtcNow && ValidUntil <= DateTime.UtcNow.AddDays(7);
        public int DaysUntilExpiration => Math.Max(0, (ValidUntil - DateTime.UtcNow).Days);
    }
}
