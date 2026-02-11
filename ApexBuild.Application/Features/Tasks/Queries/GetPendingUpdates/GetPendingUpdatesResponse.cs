namespace ApexBuild.Application.Features.Tasks.Queries.GetPendingUpdates;

public class GetPendingUpdatesResponse
{
    public List<PendingUpdateDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class PendingUpdateDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? ContractorName { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public Guid SubmittedByUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<string> MediaUrls { get; set; } = new();
    public List<string> MediaTypes { get; set; } = new();
    public bool? ContractorAdminApproved { get; set; }
    public string? ContractorAdminFeedback { get; set; }
    public bool? SupervisorApproved { get; set; }
    public string? SupervisorFeedback { get; set; }
}
