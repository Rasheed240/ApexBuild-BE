namespace ApexBuild.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsResponse
{
    public List<AuditLogItem> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

public class AuditLogItem
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ChangesSummary { get; set; }
    public string SeverityLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTime ActionTimestamp { get; set; }
}
