using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities
{
    public class PasswordHistory : BaseEntity
    {
        public Guid UserId { get; set; }
        public string PasswordHash { get; set; } = string.Empty;

        // Navigation Property
        public virtual User User { get; set; } = null!;
    }
}

