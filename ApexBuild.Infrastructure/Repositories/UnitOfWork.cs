

using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Infrastructure.Persistence;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace ApexBuild.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Users = new UserRepository(_context);
            Roles = new RoleRepository(_context);
            Projects = new ProjectRepository(_context);
            Organizations = new OrganizationRepository(_context);
            OrganizationMembers = new OrganizationMemberRepository(_context);
            Departments = new DepartmentRepository(_context);
            Tasks = new TaskRepository(_context);
            TaskUpdates = new TaskUpdateRepository(_context);
            Invitations = new InvitationRepository(_context);
            Notifications = new NotificationRepository(_context);
            AuditLogs = new AuditLogRepository(_context);
            Subscriptions = new SubscriptionRepository(_context);
            OrganizationLicenses = new OrganizationLicenseRepository(_context);
            PaymentTransactions = new PaymentTransactionRepository(_context);
            UserRoles = new BaseRepository<UserRole>(_context);
            TaskComments = new BaseRepository<TaskComment>(_context);
            TaskUsers = new BaseRepository<TaskUser>(_context);
            DepartmentSupervisors = new BaseRepository<DepartmentSupervisor>(_context);
            WorkInfos = new BaseRepository<WorkInfo>(_context);
            ProjectUsers = new BaseRepository<ProjectUser>(_context);
        }

        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IProjectRepository Projects { get; }
        public IOrganizationRepository Organizations { get; }
        public IOrganizationMemberRepository OrganizationMembers { get; }
        public IDepartmentRepository Departments { get; }
        public ITaskRepository Tasks { get; }
        public ITaskUpdateRepository TaskUpdates { get; }
        public IInvitationRepository Invitations { get; }
        public INotificationRepository Notifications { get; }
        public IAuditLogRepository AuditLogs { get; }
        public ISubscriptionRepository Subscriptions { get; }
        public IOrganizationLicenseRepository OrganizationLicenses { get; }
        public IPaymentTransactionRepository PaymentTransactions { get; }
        public IRepository<UserRole> UserRoles { get; }
        public IRepository<TaskComment> TaskComments { get; }
        public IRepository<TaskUser> TaskUsers { get; }
        public IRepository<DepartmentSupervisor> DepartmentSupervisors { get; }
        public IRepository<WorkInfo> WorkInfos { get; }
        public IRepository<ProjectUser> ProjectUsers { get; }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _context.DisposeAsync();
        }
    }
}

