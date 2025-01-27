using Microsoft.EntityFrameworkCore;
using ApexBuild.Domain.Entities;
using ApexBuild.Domain.Common;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;


namespace ApexBuild.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
        public DbSet<WorkInfo> WorkInfos => Set<WorkInfo>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<TaskUser> TaskUsers => Set<TaskUser>();
        public DbSet<TaskUpdate> TaskUpdates => Set<TaskUpdate>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<OrganizationLicense> OrganizationLicenses => Set<OrganizationLicense>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // JSON value converter for List<string>
            var listStringConverter = new ValueConverter<List<string>?, string>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
            );

            // Global query filters for soft delete
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Organization>().HasQueryFilter(o => !o.IsDeleted);
            modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<ProjectTask>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<TaskUpdate>().HasQueryFilter(tu => !tu.IsDeleted);
            modelBuilder.Entity<TaskComment>().HasQueryFilter(tc => !tc.IsDeleted);

            // Notification - MetaData
            modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted)
                .Property(no => no.MetaData)
                .HasColumnType("jsonb");

            // Project - MetaData and ImageUrls
            modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted)
               .Property(p => p.MetaData)
               .HasColumnType("jsonb");
            modelBuilder.Entity<Project>()
               .Property(p => p.ImageUrls)
               .HasConversion(listStringConverter)
               .HasColumnType("jsonb");

            // Organization - MetaData
            modelBuilder.Entity<Organization>().HasQueryFilter(o => !o.IsDeleted)
                .Property(o => o.MetaData)
                .HasColumnType("jsonb");

            // OrganizationLicense - MetaData
            modelBuilder.Entity<OrganizationLicense>().HasQueryFilter(o => !o.IsDeleted)
                .Property(o => o.MetaData)
                .HasColumnType("jsonb");

            // PaymentTransaction - MetaData
            modelBuilder.Entity<PaymentTransaction>().HasQueryFilter(o => !o.IsDeleted)
                .Property(o => o.MetaData)
                .HasColumnType("jsonb");

            // Subscription - MetaData
            modelBuilder.Entity<Subscription>().HasQueryFilter(o => !o.IsDeleted)
                .Property(o => o.MetaData)
                .HasColumnType("jsonb");

            // Department - MetaData
            modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted)
                .Property(d => d.MetaData)
                .HasColumnType("jsonb");

            // ProjectTask - MetaData, Tags, ImageUrls, VideoUrls, AttachmentUrls
            modelBuilder.Entity<ProjectTask>().HasQueryFilter(t => !t.IsDeleted)
                .Property(pt => pt.MetaData)
                .HasColumnType("jsonb");
            modelBuilder.Entity<ProjectTask>()
                .Property(pt => pt.Tags)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<ProjectTask>()
                .Property(pt => pt.ImageUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<ProjectTask>()
                .Property(pt => pt.VideoUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<ProjectTask>()
                .Property(pt => pt.AttachmentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // TaskUpdate - MetaData, MediaUrls, MediaTypes
            modelBuilder.Entity<TaskUpdate>().HasQueryFilter(tu => !tu.IsDeleted)
                .Property(tu => tu.MetaData)
                .HasColumnType("jsonb");
            modelBuilder.Entity<TaskUpdate>()
                .Property(tu => tu.MediaUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<TaskUpdate>()
                .Property(tu => tu.MediaTypes)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // TaskComment - AttachmentUrls
            modelBuilder.Entity<TaskComment>()
                .Property(tc => tc.AttachmentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps before saving
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}