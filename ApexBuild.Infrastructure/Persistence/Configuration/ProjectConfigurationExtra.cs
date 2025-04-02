using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                .IsRequired()
                .HasColumnName("code");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(p => p.Description)
                .HasColumnName("description");

            builder.Property(p => p.Location)
                .HasMaxLength(500)
                .HasColumnName("location");

            builder.Property(p => p.Latitude)
                .HasColumnName("latitude");

            builder.Property(p => p.Longitude)
                .HasColumnName("longitude");

            builder.Property(p => p.OrganizationId)
                .IsRequired()
                .HasColumnName("organization_id");

            builder.Property(p => p.ProjectOwnerId)
                .HasColumnName("owner_id");

            builder.Property(p => p.Status)
                .HasColumnName("status");

            builder.Property(p => p.StartDate)
                .HasColumnName("start_date");

            builder.Property(p => p.ExpectedEndDate)
                .HasColumnName("end_date");

            builder.Property(p => p.IsActive)
                .HasColumnName("is_active");

            // Explicitly configure MetaData as JSON
            builder.Property(p => p.MetaData)
                .HasColumnType("jsonb")
                .HasColumnName("meta_data");

            // Soft delete
            builder.Property(p => p.IsDeleted)
                .HasColumnName("is_deleted");

            builder.Property(p => p.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(p => p.DeletedBy)
                .HasColumnName("deleted_by");

            // Relationships
            builder.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ProjectOwner)
                .WithMany()
                .HasForeignKey(p => p.ProjectOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ProjectAdmin)
                .WithMany()
                .HasForeignKey(p => p.ProjectAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.ToTable("projects");
        }
    }
}
