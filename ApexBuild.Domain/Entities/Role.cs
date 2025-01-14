using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public RoleType RoleType { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; } = false;
        public int Level { get; set; } // Hierarchy level for permission checks

        // Navigation Properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}