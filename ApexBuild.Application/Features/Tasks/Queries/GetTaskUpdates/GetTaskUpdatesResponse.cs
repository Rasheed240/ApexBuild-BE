namespace ApexBuild.Application.Features.Tasks.Queries.GetTaskUpdates;

public class GetTaskUpdatesResponse
{
    public List<TaskUpdateItemDto> Updates { get; set; } = new();
    public int TotalCount { get; set; }
}

public class TaskUpdateItemDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }

    // Submitter
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByUserName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int Status { get; set; }
    public decimal ProgressPercentage { get; set; }

    public List<string> MediaUrls { get; set; } = new();
    public List<string> MediaTypes { get; set; } = new();

    // Contractor Admin review
    public bool? ContractorAdminApproved { get; set; }
    public string? ContractorAdminFeedback { get; set; }
    public string? ReviewedByContractorAdminName { get; set; }
    public DateTime? ContractorAdminReviewedAt { get; set; }

    // Supervisor review
    public bool? SupervisorApproved { get; set; }
    public string? SupervisorFeedback { get; set; }
    public string? ReviewedBySupervisorName { get; set; }
    public DateTime? SupervisorReviewedAt { get; set; }

    // Admin review
    public bool? AdminApproved { get; set; }
    public string? AdminFeedback { get; set; }
    public string? ReviewedByAdminName { get; set; }
    public DateTime? AdminReviewedAt { get; set; }
}
