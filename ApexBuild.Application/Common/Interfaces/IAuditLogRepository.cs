using ApexBuild.Domain.Common;
using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Get queryable for building custom queries
    /// </summary>
    IQueryable<AuditLog> GetQueryable();

    /// <summary>
    /// Get audit logs for an entity
    /// </summary>
    Task<List<AuditLog>> GetEntityAuditLogsAsync(Guid entityId, string entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's action history
    /// </summary>
    Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsByActionAsync(string actionType, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed audit logs
    /// </summary>
    Task<List<AuditLog>> GetFailedAuditLogsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get critical audit logs
    /// </summary>
    Task<List<AuditLog>> GetCriticalAuditLogsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
}
