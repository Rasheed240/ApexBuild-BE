using MediatR;

namespace ApexBuild.Application.Features.Audit.Queries.GetAuditLogs;

public record GetAuditLogsQuery : IRequest<GetAuditLogsResponse>
{
    public Guid? EntityId { get; init; }
    public string? EntityType { get; init; }
    public string? ActionType { get; init; }
    public Guid? UserId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
