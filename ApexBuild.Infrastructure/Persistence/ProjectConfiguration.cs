using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.ProjectType)
                .HasMaxLength(100);

            builder.Property(p => p.Location)
                .HasMaxLength(500);

            builder.Property(p => p.Budget)
                .HasPrecision(18, 2);

            builder.Property(p => p.Currency)
                .HasMaxLength(3);

            builder.Property(p => p.ImageUrls)
                .HasColumnType("jsonb");

            builder.Property(p => p.MetaData)
                .HasColumnType("jsonb");

            builder.HasIndex(p => p.Code)
                .IsUnique()
                .HasFilter("is_deleted = false");

            builder.HasOne(p => p.ProjectOwner)
                .WithMany()
                .HasForeignKey(p => p.ProjectOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ProjectAdmin)
                .WithMany()
                .HasForeignKey(p => p.ProjectAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
