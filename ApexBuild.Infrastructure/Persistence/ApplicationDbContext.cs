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

        // ── Core User & Role ──────────────────────────────────────────────────
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        // ── Organization ──────────────────────────────────────────────────────
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

        // ── Project & Structure ───────────────────────────────────────────────
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Contractor> Contractors => Set<Contractor>();
        public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();

        // ── Tasks & Updates ───────────────────────────────────────────────────
        public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
        public DbSet<TaskUser> TaskUsers => Set<TaskUser>();
        public DbSet<TaskUpdate> TaskUpdates => Set<TaskUpdate>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();

        // ── Work & Invitations ────────────────────────────────────────────────
        public DbSet<WorkInfo> WorkInfos => Set<WorkInfo>();
        public DbSet<Invitation> Invitations => Set<Invitation>();

        // ── Communication ─────────────────────────────────────────────────────
        public DbSet<Notification> Notifications => Set<Notification>();

        // ── Billing & Payments ────────────────────────────────────────────────
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        // ── Audit & Security ──────────────────────────────────────────────────
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
        public DbSet<DepartmentSupervisor> DepartmentSupervisors => Set<DepartmentSupervisor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all IEntityTypeConfiguration classes from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // ── JSON value converters ─────────────────────────────────────────
            var listStringConverter = new ValueConverter<List<string>?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
            );

            // ── Global soft-delete query filters ─────────────────────────────
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Organization>().HasQueryFilter(o => !o.IsDeleted);
            modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
            modelBuilder.Entity<Contractor>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<ProjectMilestone>().HasQueryFilter(m => !m.IsDeleted);
            modelBuilder.Entity<ProjectTask>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<TaskUpdate>().HasQueryFilter(tu => !tu.IsDeleted);
            modelBuilder.Entity<TaskComment>().HasQueryFilter(tc => !tc.IsDeleted);
            modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
            modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<PaymentTransaction>().HasQueryFilter(p => !p.IsDeleted);

            // ── Unique constraints ────────────────────────────────────────────
            // One role per user per project - enforces "can't hold two roles in same project"
            modelBuilder.Entity<ProjectUser>()
                .HasIndex(pu => new { pu.UserId, pu.ProjectId })
                .IsUnique()
                .HasDatabaseName("IX_ProjectUsers_UserId_ProjectId");

            // ── Notification ──────────────────────────────────────────────────
            modelBuilder.Entity<Notification>()
                .Property(n => n.MetaData)
                .HasColumnType("jsonb");

            // ── Project ───────────────────────────────────────────────────────
            modelBuilder.Entity<Project>()
                .Property(p => p.MetaData)
                .HasColumnType("jsonb");
            modelBuilder.Entity<Project>()
                .Property(p => p.ImageUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<Project>()
                .Property(p => p.DocumentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // ── Organization ──────────────────────────────────────────────────
            modelBuilder.Entity<Organization>()
                .Property(o => o.MetaData)
                .HasColumnType("jsonb");

            // ── Department ────────────────────────────────────────────────────
            modelBuilder.Entity<Department>()
                .Property(d => d.MetaData)
                .HasColumnType("jsonb");

            // ── Contractor ────────────────────────────────────────────────────
            modelBuilder.Entity<Contractor>()
                .Property(c => c.MetaData)
                .HasColumnType("jsonb");
            modelBuilder.Entity<Contractor>()
                .Property(c => c.ContractDocumentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // ── ProjectMilestone ──────────────────────────────────────────────
            modelBuilder.Entity<ProjectMilestone>()
                .Property(m => m.MetaData)
                .HasColumnType("jsonb");

            // ── ProjectTask ───────────────────────────────────────────────────
            modelBuilder.Entity<ProjectTask>()
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
                .Property(pt => pt.AudioUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");
            modelBuilder.Entity<ProjectTask>()
                .Property(pt => pt.AttachmentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // ── TaskUpdate ────────────────────────────────────────────────────
            modelBuilder.Entity<TaskUpdate>()
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

            // ── TaskComment ───────────────────────────────────────────────────
            modelBuilder.Entity<TaskComment>()
                .Property(tc => tc.AttachmentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // ── WorkInfo ──────────────────────────────────────────────────────
            modelBuilder.Entity<WorkInfo>()
                .Property(w => w.ContractDocumentUrls)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            // ── Subscription ──────────────────────────────────────────────────
            modelBuilder.Entity<Subscription>()
                .Property(s => s.MetaData)
                .HasColumnType("jsonb");

            // ── PaymentTransaction ────────────────────────────────────────────
            modelBuilder.Entity<PaymentTransaction>()
                .Property(p => p.MetaData)
                .HasColumnType("jsonb");

            // ── Invitation ────────────────────────────────────────────────────
            modelBuilder.Entity<Invitation>()
                .Property(i => i.MetaData)
                .HasColumnType("jsonb");

            // ── AuditLog ──────────────────────────────────────────────────────
            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Metadata)
                .HasColumnType("jsonb");

            // ── Contractor relationships ───────────────────────────────────────
            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Contractors)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.Department)
                .WithOne(d => d.Contractor)
                .HasForeignKey<Contractor>(c => c.DepartmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.ContractorAdmin)
                .WithMany()
                .HasForeignKey(c => c.ContractorAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── ProjectTask contractor/milestone FKs ──────────────────────────
            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Contractor)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.ContractorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Milestone)
                .WithMany()
                .HasForeignKey(t => t.MilestoneId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── WorkInfo contractor FK ─────────────────────────────────────────
            modelBuilder.Entity<WorkInfo>()
                .HasOne(w => w.Contractor)
                .WithMany(c => c.Members)
                .HasForeignKey(w => w.ContractorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── TaskUpdate contractor-admin FK ────────────────────────────────
            modelBuilder.Entity<TaskUpdate>()
                .HasOne(u => u.ReviewedByContractorAdmin)
                .WithMany()
                .HasForeignKey(u => u.ReviewedByContractorAdminId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── ProjectMilestone relationships ─────────────────────────────────
            modelBuilder.Entity<ProjectMilestone>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMilestone>()
                .HasOne(m => m.Department)
                .WithMany(d => d.Milestones)
                .HasForeignKey(m => m.DepartmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Invitation contractor FK ───────────────────────────────────────
            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Contractor)
                .WithMany()
                .HasForeignKey(i => i.ContractorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
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
