using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class TaskUpdateConfiguration : IEntityTypeConfiguration<TaskUpdate>
    {
        public void Configure(EntityTypeBuilder<TaskUpdate> builder)
        {
            builder.HasKey(tu => tu.Id);

            builder.Property(tu => tu.TaskId)
                .HasColumnName("task_id");

            builder.Property(tu => tu.SubmittedByUserId)
                .HasColumnName("submitted_by_user_id");

            builder.Property(tu => tu.Description)
                .IsRequired()
                .HasColumnName("description");

            builder.Property(tu => tu.Status)
                .HasColumnName("status");

            builder.Property(tu => tu.ProgressPercentage)
                .HasColumnName("progress_percentage");

            // Explicitly configure MetaData as JSON
            builder.Property(tu => tu.MetaData)
                .HasColumnType("jsonb")
                .HasColumnName("meta_data");

            // Soft delete
            builder.Property(tu => tu.IsDeleted)
                .HasColumnName("is_deleted");

            builder.Property(tu => tu.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(tu => tu.DeletedBy)
                .HasColumnName("deleted_by");

            builder.ToTable("task_updates");
        }
    }
}
