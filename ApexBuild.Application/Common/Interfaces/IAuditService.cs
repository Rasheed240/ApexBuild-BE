using ApexBuild.Domain.Entities;

namespace ApexBuild.Application.Common.Interfaces;

/// <summary>
/// Service for logging audit trails of user actions
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an action with detailed information
    /// </summary>
    Task LogActionAsync(
        Guid userId,
        string actionType,
        string entityType,
        Guid entityId,
        string description,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        AuditSeverity severity = AuditSeverity.Info,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        Dictionary<string, object>? metadata = null
    );

    /// <summary>
    /// Log a failed action
    /// </summary>
    Task LogFailedActionAsync(
        Guid userId,
        string actionType,
        string entityType,
        Guid entityId,
        string description,
        string errorMessage,
        string? ipAddress = null,
        string? userAgent = null
    );

    /// <summary>
    /// Get audit logs for an entity with pagination and filtering
    /// </summary>
    Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid? entityId = null,
        string? entityType = null,
        string? actionType = null,
        Guid? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Get user action history
    /// </summary>
    Task<PaginatedResult<AuditLogDto>> GetUserActionHistoryAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Get entity change history (showing all modifications)
    /// </summary>
    Task<List<AuditLogDto>> GetEntityChangeHistoryAsync(
        Guid entityId,
        string entityType
    );
}

/// <summary>
/// DTO for audit log
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ChangesSummary { get; set; }
    public string SeverityLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTime ActionTimestamp { get; set; }
    public Dictionary<string, object>? Changes { get; set; }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
