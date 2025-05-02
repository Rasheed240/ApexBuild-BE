using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
    {
        public void Configure(EntityTypeBuilder<ProjectTask> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasColumnName("title");

            builder.Property(t => t.Description)
                .HasColumnName("description");

            builder.Property(t => t.Status)
                .HasColumnName("status");

            builder.Property(t => t.ProjectId)
                .HasColumnName("project_id");

            builder.Property(t => t.DepartmentId)
                .HasColumnName("department_id");

            builder.Property(t => t.AssignedToUserId)
                .HasColumnName("assigned_to_user_id");

            builder.Property(t => t.StartDate)
                .HasColumnName("start_date");

            builder.Property(t => t.DueDate)
                .HasColumnName("due_date");

            builder.Property(t => t.CompletedAt)
                .HasColumnName("completed_date");

            builder.Property(t => t.Progress)
                .HasColumnName("progress");

            // Explicitly configure MetaData as JSON
            builder.Property(t => t.MetaData)
                .HasColumnType("jsonb")
                .HasColumnName("meta_data");

            // Soft delete
            builder.Property(t => t.IsDeleted)
                .HasColumnName("is_deleted");

            builder.Property(t => t.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(t => t.DeletedBy)
                .HasColumnName("deleted_by");

            builder.ToTable("project_tasks");
        }
    }
}
