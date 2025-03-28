using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    /// <summary>
    /// Represents a user who has supervisor privileges for a specific department
    /// </summary>
    public class DepartmentSupervisor : BaseEntity
    {
        public Guid DepartmentId { get; set; }
        public Guid SupervisorId { get; set; }

        // Navigation Properties
        public virtual Department Department { get; set; } = null!;
        public virtual User Supervisor { get; set; } = null!;
    }
}
