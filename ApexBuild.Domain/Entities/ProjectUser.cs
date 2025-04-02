using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class ProjectUser : BaseAuditableEntity
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public ProjectUserStatus Status { get; set; } = ProjectUserStatus.Active;
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public Guid? AddedBy { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual Project Project { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}