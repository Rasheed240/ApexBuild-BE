using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("password_histories");

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.UserId)
            .IsRequired();

        builder.Property(ph => ph.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ph => ph.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ph => ph.UserId);
        builder.HasIndex(ph => new { ph.UserId, ph.CreatedAt });

        // Relationships
        builder.HasOne(ph => ph.User)
            .WithMany()
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

