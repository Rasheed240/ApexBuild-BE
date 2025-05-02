using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("project_tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(5000);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Location)
            .HasMaxLength(500);

        builder.Property(t => t.Progress)
            .HasPrecision(5, 2);

        builder.Property(t => t.Tags)
            .HasColumnType("jsonb");

        builder.Property(t => t.MetaData)
            .HasColumnType("jsonb");

        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(t => t.DepartmentId);
        builder.HasIndex(t => t.ParentTaskId);
        builder.HasIndex(t => t.AssignedToUserId);

        // Self-referencing relationship for subtasks
        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.Subtasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of parent task

        builder.HasOne(t => t.Department)
            .WithMany(d => d.Tasks)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedByUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

