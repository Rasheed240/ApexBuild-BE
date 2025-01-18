using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    public class OrganizationMember : BaseAuditableEntity
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string Position { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        
        // Navigation Properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}