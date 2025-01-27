using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(o => o.Code)
                .IsRequired()
                .HasColumnName("code");

            builder.Property(o => o.Description)
                .HasColumnName("description");

            builder.Property(o => o.RegistrationNumber)
                .HasColumnName("registration_number");

            builder.Property(o => o.TaxId)
                .HasColumnName("tax_id");

            builder.Property(o => o.Email)
                .HasColumnName("email");

            builder.Property(o => o.PhoneNumber)
                .HasColumnName("phone_number");

            builder.Property(o => o.Website)
                .HasColumnName("website");

            builder.Property(o => o.Address)
                .HasColumnName("address");

            builder.Property(o => o.City)
                .HasColumnName("city");

            builder.Property(o => o.State)
                .HasColumnName("state");

            builder.Property(o => o.Country)
                .HasColumnName("country");

            builder.Property(o => o.LogoUrl)
                .HasColumnName("logo_url");

            builder.Property(o => o.OwnerId)
                .HasColumnName("owner_id");

            builder.Property(o => o.IsActive)
                .HasColumnName("is_active");

            builder.Property(o => o.IsVerified)
                .HasColumnName("is_verified");

            builder.Property(o => o.VerifiedAt)
                .HasColumnName("verified_at");

            // Soft delete
            builder.Property(o => o.IsDeleted)
                .HasColumnName("is_deleted");

            builder.Property(o => o.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(o => o.DeletedBy)
                .HasColumnName("deleted_by");

            // Explicitly configure MetaData as JSON
            builder.Property(o => o.MetaData)
                .HasColumnType("jsonb")
                .HasColumnName("meta_data");

            // Relationships
            builder.HasOne(o => o.Owner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Members)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.Departments)
                .WithOne(d => d.Organization)
                .HasForeignKey(d => d.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.WorkInfos)
                .WithOne(w => w.Organization)
                .HasForeignKey(w => w.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ToTable("organizations");
        }
    }
}
