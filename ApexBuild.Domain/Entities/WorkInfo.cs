using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class WorkInfo : BaseAuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectUserStatus Status { get; set; } = ProjectUserStatus.Active;
        public bool IsActive { get; set; } = true;
        public string? Responsibilities { get; set; }
        public decimal? HourlyRate { get; set; }
        public string? ContractType { get; set; } // FullTime, PartTime, Contract
        public string? ReportingTo { get; set; }
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Project Project { get; set; } = null!;
        public virtual Organization? Organization { get; set; }
        public virtual Department? Department { get; set; }
    }
}