using ApexBuild.Domain.Common;

namespace ApexBuild.Domain.Entities;

/// <summary>
/// Represents a comprehensive audit log entry for tracking user actions
/// </summary>
public class AuditLog : BaseAuditableEntity
{
    /// <summary>
    /// Type of action performed (Create, Update, Delete, Review, Approve, Reject, etc.)
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected (User, Project, Task, TaskUpdate, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Primary entity ID that was affected
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Related entity ID (e.g., ProjectId for a Task)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Related entity type
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Organization context for multi-tenant data isolation (nullable for platform-level actions)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent (browser info)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Brief description of what was changed
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized old values (before change)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON serialized new values (after change)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Changes summary (e.g., "Status: Pending → In Progress")
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// Severity level: Info, Warning, Critical
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    /// <summary>
    /// Status of the action: Success, Failed, Partial
    /// </summary>
    public AuditStatus Status { get; set; } = AuditStatus.Success;

    /// <summary>
    /// Any error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration in milliseconds to complete the action
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Timestamp when the action was performed
    /// </summary>
    public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Is this action reversible (can be undone)
    /// </summary>
    public bool IsReversible { get; set; }

    /// <summary>
    /// ID of the related undo action if this was undone
    /// </summary>
    public Guid? UndoAuditLogId { get; set; }

    // Navigation Properties
    public virtual User? User { get; set; }
    public virtual Organization? Organization { get; set; }
}

/// <summary>
/// Severity levels for audit log entries
/// </summary>
public enum AuditSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

/// <summary>
/// Status of audit log entry
/// </summary>
public enum AuditStatus
{
    Success = 1,
    Failed = 2,
    Partial = 3
}
