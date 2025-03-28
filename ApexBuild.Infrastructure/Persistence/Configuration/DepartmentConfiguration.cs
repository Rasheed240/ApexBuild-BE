using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(d => d.Code)
                .IsRequired()
                .HasColumnName("code");

            builder.Property(d => d.Description)
                .HasColumnName("description");

            //builder.Property(d => d.Location)
            //    .HasMaxLength(500)
            //    .HasColumnName("location");

            //builder.Property(d => d.Latitude)
            //    .HasColumnName("latitude");

            //builder.Property(d => d.Longitude)
            //    .HasColumnName("longitude");

            //builder.Property(d => d.ProjectAdminId)
            //    .HasColumnName("project_admin_id");

            builder.Property(d => d.OrganizationId)
                .HasColumnName("organization_id");

            builder.Property(d => d.IsActive)
                .HasColumnName("is_active");

            // Explicitly configure MetaData as JSON
            builder.Property(d => d.MetaData)
                .HasColumnType("jsonb")
                .HasColumnName("meta_data");

            // Soft delete
            builder.Property(d => d.IsDeleted)
                .HasColumnName("is_deleted");

            builder.Property(d => d.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(d => d.DeletedBy)
                .HasColumnName("deleted_by");

            builder.ToTable("departments");
        }
    }
}
