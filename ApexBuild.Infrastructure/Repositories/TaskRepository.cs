using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Infrastructure.Repositories
{
    public class TaskRepository : BaseRepository<ProjectTask>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ProjectTask>> GetTasksByDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .Where(t => t.DepartmentId == departmentId && !t.IsDeleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectTask>> GetTasksByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Department)
                    .ThenInclude(d => d.Project)
                .Include(t => t.AssignedByUser)
                .Where(t => t.AssignedToUserId == userId && !t.IsDeleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectTask?> GetWithUpdatesAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Department)
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .Include(t => t.Updates.Where(u => !u.IsDeleted))
                    .ThenInclude(u => u.SubmittedByUser)
                .Include(t => t.Updates.Where(u => !u.IsDeleted))
                    .ThenInclude(u => u.ReviewedBySupervisor)
                .Include(t => t.Updates.Where(u => !u.IsDeleted))
                    .ThenInclude(u => u.ReviewedByAdmin)
                .Include(t => t.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<ProjectTask>> GetOverdueTasksAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.AssignedToUser)
                .Include(t => t.Department)
                    .ThenInclude(d => d.Project)
                .Where(t => t.DueDate.HasValue
                            && t.DueDate.Value < DateTime.UtcNow
                            && t.Status != Domain.Enums.TaskStatus.Completed
                            && !t.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectTask>> GetTasksDueInDaysAsync(
            int days,
            CancellationToken cancellationToken = default)
        {
            var targetDate = DateTime.UtcNow.AddDays(days);

            return await _dbSet
                .Include(t => t.AssignedToUser)
                .Include(t => t.Department)
                    .ThenInclude(d => d.Project)
                .Where(t => t.DueDate.HasValue
                            && t.DueDate.Value.Date == targetDate.Date
                            && t.Status != Domain.Enums.TaskStatus.Completed
                            && !t.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ProjectTask>> GetSubtasksAsync(
            Guid parentTaskId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.ParentTaskId == parentTaskId && !t.IsDeleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProjectTask?> GetTaskWithSubtasksAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Subtasks.Where(st => !st.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken);
        }
    }
}