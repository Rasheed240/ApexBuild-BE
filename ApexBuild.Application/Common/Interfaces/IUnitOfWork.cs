using System.Linq.Expressions;
using ApexBuild.Domain.Common;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IProjectRepository Projects { get; }
        IOrganizationRepository Organizations { get; }
        IOrganizationMemberRepository OrganizationMembers { get; }
        IDepartmentRepository Departments { get; }
        ITaskRepository Tasks { get; }
        ITaskUpdateRepository TaskUpdates { get; }
        IInvitationRepository Invitations { get; }
        INotificationRepository Notifications { get; }
        IAuditLogRepository AuditLogs { get; }
        ISubscriptionRepository Subscriptions { get; }
        IOrganizationLicenseRepository OrganizationLicenses { get; }
        IPaymentTransactionRepository PaymentTransactions { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<TaskComment> TaskComments { get; }
        IRepository<TaskUser> TaskUsers { get; }
        IRepository<DepartmentSupervisor> DepartmentSupervisors { get; }
        IRepository<WorkInfo> WorkInfos { get; }
        IRepository<ProjectUser> ProjectUsers { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}