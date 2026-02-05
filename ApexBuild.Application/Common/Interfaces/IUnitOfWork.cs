using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Core
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IRepository<UserRole> UserRoles { get; }

        // Organization
        IOrganizationRepository Organizations { get; }
        IOrganizationMemberRepository OrganizationMembers { get; }

        // Project & Structure
        IProjectRepository Projects { get; }
        IRepository<ProjectUser> ProjectUsers { get; }
        IDepartmentRepository Departments { get; }
        IContractorRepository Contractors { get; }
        IProjectMilestoneRepository Milestones { get; }

        // Tasks
        ITaskRepository Tasks { get; }
        ITaskUpdateRepository TaskUpdates { get; }
        IRepository<TaskUser> TaskUsers { get; }
        IRepository<TaskComment> TaskComments { get; }

        // Work & Invitations
        IRepository<WorkInfo> WorkInfos { get; }
        IInvitationRepository Invitations { get; }

        // Communication
        INotificationRepository Notifications { get; }

        // Billing
        ISubscriptionRepository Subscriptions { get; }
        IPaymentTransactionRepository PaymentTransactions { get; }

        // Audit & Security
        IAuditLogRepository AuditLogs { get; }
        IRepository<DepartmentSupervisor> DepartmentSupervisors { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
