using MediatR;
using ApexBuild.Application.Common.Interfaces;

namespace ApexBuild.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResponse>
{
    private readonly IAuditService _auditService;

    public GetAuditLogsQueryHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<GetAuditLogsResponse> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var result = await _auditService.GetAuditLogsAsync(
            entityId: request.EntityId,
            entityType: request.EntityType,
            actionType: request.ActionType,
            userId: request.UserId,
            fromDate: request.FromDate,
            toDate: request.ToDate,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize
        );

        var response = new GetAuditLogsResponse
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(x => new AuditLogItem
            {
                Id = x.Id,
                ActionType = x.ActionType,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                RelatedEntityId = x.RelatedEntityId,
                RelatedEntityType = x.RelatedEntityType,
                UserId = x.UserId,
                UserName = x.UserName ?? "Unknown",
                UserEmail = x.UserEmail ?? "Unknown",
                IpAddress = x.IpAddress,
                Description = x.Description,
                ChangesSummary = x.ChangesSummary,
                SeverityLevel = x.SeverityLevel,
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                DurationMs = x.DurationMs,
                ActionTimestamp = x.ActionTimestamp
            }).ToList()
        };

        return response;
    }
}
