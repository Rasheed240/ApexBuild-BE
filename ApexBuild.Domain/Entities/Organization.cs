using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    public class Organization : BaseAuditableEntity, ISoftDelete
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? LogoUrl { get; set; }
        public string? LogoPublicId { get; set; } // Cloudinary public ID for deletion
        public Guid OwnerId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public Dictionary<string, object>? MetaData { get; set; }
        
        // Stripe Integration
        public string? StripeCustomerId { get; set; }
        
        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        
        // Navigation Properties
        public virtual User Owner { get; set; } = null!;
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
        public virtual ICollection<WorkInfo> WorkInfos { get; set; } = new List<WorkInfo>();

    }
}