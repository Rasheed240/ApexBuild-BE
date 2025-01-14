using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    public class UserRole : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? OrganizationId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ActivatedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual Project? Project { get; set; }
        public virtual Organization? Organization { get; set; }
    }
}