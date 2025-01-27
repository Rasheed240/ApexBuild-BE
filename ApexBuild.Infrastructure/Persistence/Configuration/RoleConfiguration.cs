using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBuild.Infrastructure.Persistence.Configuration
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(r => r.RoleType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(r => r.Description)
                .HasMaxLength(500);

            builder.HasIndex(r => r.Name)
                .IsUnique();

            builder.HasIndex(r => r.RoleType)
                .IsUnique();

            // Seed data
            // builder.HasData(
            //     new Role
            //     {
            //         Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            //         Name = "Super Administrator",
            //         RoleType = RoleType.SuperAdmin,
            //         Description = "Platform super administrator with full access",
            //         IsSystemRole = true,
            //         Level = 1,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            //         Name = "Platform Admin",
            //         RoleType = RoleType.PlatformAdmin,
            //         Description = "Platform administrator",
            //         IsSystemRole = true,
            //         Level = 2,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            //         Name = "Project Owner",
            //         RoleType = RoleType.ProjectOwner,
            //         Description = "Owner of a construction project",
            //         IsSystemRole = true,
            //         Level = 3,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            //         Name = "Project Administrator",
            //         RoleType = RoleType.ProjectAdministrator,
            //         Description = "Administrator overseeing a project",
            //         IsSystemRole = true,
            //         Level = 4,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            //         Name = "Contractor Admin",
            //         RoleType = RoleType.ContractorAdmin,
            //         Description = "Administrator of contractor organization",
            //         IsSystemRole = true,
            //         Level = 5,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            //         Name = "Department Supervisor",
            //         RoleType = RoleType.DepartmentSupervisor,
            //         Description = "Supervisor of a department",
            //         IsSystemRole = true,
            //         Level = 6,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            //         Name = "Field Worker",
            //         RoleType = RoleType.FieldWorker,
            //         Description = "Field worker executing tasks",
            //         IsSystemRole = true,
            //         Level = 7,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     },
            //     new Role
            //     {
            //         Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            //         Name = "Observer",
            //         RoleType = RoleType.Observer,
            //         Description = "Read-only observer",
            //         IsSystemRole = true,
            //         Level = 8,
            //         CreatedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            //     }
            // );
        }
    }
}