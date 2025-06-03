using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Infrastructure.Repositories;

public class TaskUpdateRepository : BaseRepository<TaskUpdate>, ITaskUpdateRepository
{
    public TaskUpdateRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TaskUpdate>> GetUpdatesByTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.SubmittedByUser)
            .Include(u => u.ReviewedBySupervisor)
            .Include(u => u.ReviewedByAdmin)
            .Where(u => u.TaskId == taskId && !u.IsDeleted)
            .OrderByDescending(u => u.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TaskUpdate>> GetUpdatesByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Task)
                .ThenInclude(t => t.Department)
            .Where(u => u.SubmittedByUserId == userId && !u.IsDeleted)
            .OrderByDescending(u => u.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskUpdate?> GetWithDetailsAsync(
        Guid updateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Task)
                .ThenInclude(t => t.Department)
                    .ThenInclude(d => d.Project)
            .Include(u => u.SubmittedByUser)
            .Include(u => u.ReviewedBySupervisor)
            .Include(u => u.ReviewedByAdmin)
            .FirstOrDefaultAsync(u => u.Id == updateId && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> HasDailyReportForDateAsync(
        Guid taskId,
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _dbSet
            .AnyAsync(u =>
                u.TaskId == taskId &&
                u.SubmittedByUserId == userId &&
                !u.IsDeleted &&
                u.SubmittedAt >= startOfDay &&
                u.SubmittedAt < endOfDay,
                cancellationToken);
    }
}

