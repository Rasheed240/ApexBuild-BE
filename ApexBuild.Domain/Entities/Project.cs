using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Enums;

namespace ApexBuild.Domain.Entities
{
    public class Project : BaseAuditableEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // e.g., PROJ-2025-001
        public Guid OrganizationId { get; set; }
        public string Description { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
        public string ProjectType { get; set; } = string.Empty; // Building, Bridge, Road, etc.
        public string? Location { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? Currency { get; set; }
        public Guid? ProjectOwnerId { get; set; }
        public Guid? ProjectAdminId { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }
        public Dictionary<string, object>? MetaData { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation Properties
        public virtual Organization? Organization { get; set; }
        public virtual User? ProjectOwner { get; set; }
        public virtual User? ProjectAdmin { get; set; }
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
        public virtual ICollection<WorkInfo> WorkInfos { get; set; } = new List<WorkInfo>();
        public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();


    }
}