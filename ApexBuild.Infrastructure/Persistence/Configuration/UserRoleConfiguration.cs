using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("user_roles");

            builder.HasKey(ur => ur.Id);

            builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.ProjectId, ur.OrganizationId })
                .IsUnique()
                .HasFilter("project_id IS NOT NULL OR organization_id IS NOT NULL");

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            builder.HasOne(ur => ur.Project)
                .WithMany()
                .HasForeignKey(ur => ur.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Organization)
                .WithMany()
                .HasForeignKey(ur => ur.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
