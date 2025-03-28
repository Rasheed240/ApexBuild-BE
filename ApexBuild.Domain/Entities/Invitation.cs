using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class Invitation : BaseAuditableEntity
    {
        public Guid InvitedByUserId { get; set; }
        public Guid? InvitedUserId { get; set; } // Null until accepted
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string Token { get; set; } = string.Empty;
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        public DateTime ExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string? Message { get; set; }
        public string? Position { get; set; }
        public Dictionary<string, object>? MetaData { get; set; }

        // Navigation Properties
        public virtual User InvitedByUser { get; set; } = null!;
        public virtual User? InvitedUser { get; set; }
        public virtual Role Role { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual Organization? Organization { get; set; }
        public virtual Department? Department { get; set; }
    }
}
