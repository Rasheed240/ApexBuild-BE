using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using ApexBuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApexBuild.Infrastructure.Repositories;

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<AuditLog> GetQueryable()
    {
        return _dbContext.AuditLogs;
    }

    public async Task<List<AuditLog>> GetEntityAuditLogsAsync(Guid entityId, string entityType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Where(a => a.EntityId == entityId && a.EntityType == entityType)
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Where(a => a.UserId == userId)
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetAuditLogsByActionAsync(string actionType, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Where(a => a.ActionType == actionType)
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionTimestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetFailedAuditLogsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.ActionTimestamp >= fromDate);

        return await query
            .Where(a => a.Status == AuditStatus.Failed)
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetCriticalAuditLogsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.ActionTimestamp >= fromDate);

        return await query
            .Where(a => a.Severity == AuditSeverity.Critical)
            .Include(a => a.User)
            .OrderByDescending(a => a.ActionTimestamp)
            .ToListAsync(cancellationToken);
    }
}
