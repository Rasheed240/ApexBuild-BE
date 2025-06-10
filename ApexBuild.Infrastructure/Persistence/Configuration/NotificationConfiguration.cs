using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(n => n.Channel)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(n => n.MetaData)
                .HasColumnType("jsonb");

            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}