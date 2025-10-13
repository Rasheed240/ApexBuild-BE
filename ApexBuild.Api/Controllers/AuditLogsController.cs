using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBuild.Application.Features.Audit.Queries.GetAuditLogs;
using ApexBuild.Contracts.Responses;

namespace ApexBuild.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    /// <remarks>
    /// Retrieve comprehensive audit trail of system actions with filtering by:
    /// - Entity type and ID
    /// - Action type
    /// - User
    /// - Date range
    /// 
    /// Admin role required for full access.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetAuditLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetAuditLogsResponse>>> GetAuditLogs(
        [FromQuery] Guid? entityId,
        [FromQuery] string? entityType,
        [FromQuery] string? actionType,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            EntityId = entityId,
            EntityType = entityType,
            ActionType = actionType,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Audit logs retrieved successfully"));
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    /// <remarks>
    /// Retrieve all changes made to a specific entity (task, project, etc.)
    /// </remarks>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogItem>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<AuditLogItem>>>> GetEntityAuditLog(
        [FromRoute] string entityType,
        [FromRoute] Guid entityId)
    {
        var query = new GetAuditLogsQuery
        {
            EntityId = entityId,
            EntityType = entityType,
            PageSize = 1000 // Get all records for entity
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response.Items, "Entity audit log retrieved successfully"));
    }

    /// <summary>
    /// Get user action history
    /// </summary>
    /// <remarks>
    /// Retrieve all actions performed by a specific user
    /// </remarks>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<GetAuditLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetAuditLogsResponse>>> GetUserAuditLog(
        [FromRoute] Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            UserId = userId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "User audit log retrieved successfully"));
    }

    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    /// <remarks>
    /// Filter audit logs by specific action type (Create, Update, Delete, Approve, Reject, etc.)
    /// </remarks>
    [HttpGet("action/{actionType}")]
    [ProducesResponseType(typeof(ApiResponse<GetAuditLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetAuditLogsResponse>>> GetAuditLogsByAction(
        [FromRoute] string actionType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetAuditLogsQuery
        {
            ActionType = actionType,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await _mediator.Send(query);
        return Ok(ApiResponse.Success(response, "Audit logs retrieved successfully"));
    }
}
