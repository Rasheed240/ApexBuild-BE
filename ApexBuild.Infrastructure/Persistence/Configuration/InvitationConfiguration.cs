using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
    {
        public void Configure(EntityTypeBuilder<Invitation> builder)
        {
            builder.ToTable("invitations");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(i => i.Token)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(i => i.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(i => i.Message)
                .HasMaxLength(1000);

            builder.Property(i => i.Position)
                .HasMaxLength(100);

            builder.Property(i => i.MetaData)
                .HasColumnType("jsonb");

            builder.HasIndex(i => i.Token)
                .IsUnique();

            builder.HasIndex(i => i.Email);

            builder.HasOne(i => i.InvitedByUser)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.InvitedUser)
                .WithMany(u => u.ReceivedInvitations)
                .HasForeignKey(i => i.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}