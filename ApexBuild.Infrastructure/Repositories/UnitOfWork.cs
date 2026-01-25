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

            // Core
            Users = new UserRepository(_context);
            Roles = new RoleRepository(_context);
            UserRoles = new BaseRepository<UserRole>(_context);

            // Organization
            Organizations = new OrganizationRepository(_context);
            OrganizationMembers = new OrganizationMemberRepository(_context);

            // Project & Structure
            Projects = new ProjectRepository(_context);
            ProjectUsers = new BaseRepository<ProjectUser>(_context);
            Departments = new DepartmentRepository(_context);
            Contractors = new ContractorRepository(_context);
            Milestones = new ProjectMilestoneRepository(_context);

            // Tasks
            Tasks = new TaskRepository(_context);
            TaskUpdates = new TaskUpdateRepository(_context);
            TaskUsers = new BaseRepository<TaskUser>(_context);
            TaskComments = new BaseRepository<TaskComment>(_context);

            // Work & Invitations
            WorkInfos = new BaseRepository<WorkInfo>(_context);
            Invitations = new InvitationRepository(_context);

            // Communication
            Notifications = new NotificationRepository(_context);

            // Billing
            Subscriptions = new SubscriptionRepository(_context);
            PaymentTransactions = new PaymentTransactionRepository(_context);

            // Audit & Security
            AuditLogs = new AuditLogRepository(_context);
            DepartmentSupervisors = new BaseRepository<DepartmentSupervisor>(_context);

            // Manuals
            UserManuals = new BaseRepository<UserManual>(_context);
        }

        // Core
        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IRepository<UserRole> UserRoles { get; }

        // Organization
        public IOrganizationRepository Organizations { get; }
        public IOrganizationMemberRepository OrganizationMembers { get; }

        // Project & Structure
        public IProjectRepository Projects { get; }
        public IRepository<ProjectUser> ProjectUsers { get; }
        public IDepartmentRepository Departments { get; }
        public IContractorRepository Contractors { get; }
        public IProjectMilestoneRepository Milestones { get; }

        // Tasks
        public ITaskRepository Tasks { get; }
        public ITaskUpdateRepository TaskUpdates { get; }
        public IRepository<TaskUser> TaskUsers { get; }
        public IRepository<TaskComment> TaskComments { get; }

        // Work & Invitations
        public IRepository<WorkInfo> WorkInfos { get; }
        public IInvitationRepository Invitations { get; }

        // Communication
        public INotificationRepository Notifications { get; }

        // Billing
        public ISubscriptionRepository Subscriptions { get; }
        public IPaymentTransactionRepository PaymentTransactions { get; }

        // Audit & Security
        public IAuditLogRepository AuditLogs { get; }
        public IRepository<DepartmentSupervisor> DepartmentSupervisors { get; }

        // Manuals
        public IRepository<UserManual> UserManuals { get; }

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
