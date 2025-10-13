using System.Text.Json;
using ApexBuild.Application.Common.Interfaces;
using ApexBuild.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexBuild.Infrastructure.Services;

/// <summary>
/// Service for comprehensive audit logging
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogActionAsync(
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
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = description,
                OldValues = SerializeToJson(oldValues),
                NewValues = SerializeToJson(newValues),
                ChangesSummary = GenerateChangesSummary(oldValues, newValues),
                Severity = severity,
                Status = AuditStatus.Success,
                ActionTimestamp = DateTime.UtcNow,
                Metadata = SerializeToJson(metadata),
                IsReversible = IsActionReversible(actionType),
                CreatedBy = userId
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {ActionType} on {EntityType} {EntityId} by user {UserId}",
                actionType, entityType, entityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit action: {ActionType} on {EntityType}", actionType, entityType);
            // Don't throw - audit logging should not fail the main operation
        }
    }

    public async Task LogFailedActionAsync(
        Guid userId,
        string actionType,
        string entityType,
        Guid entityId,
        string description,
        string errorMessage,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = description,
                Status = AuditStatus.Failed,
                ErrorMessage = errorMessage,
                Severity = AuditSeverity.Warning,
                ActionTimestamp = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning(
                "Failed audit log created: {ActionType} on {EntityType} {EntityId} by user {UserId}. Error: {ErrorMessage}",
                actionType, entityType, entityId, userId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging failed audit action");
        }
    }

    public async Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid? entityId = null,
        string? entityType = null,
        string? actionType = null,
        Guid? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        try
        {
            var query = _unitOfWork.AuditLogs.GetQueryable();

            if (entityId.HasValue)
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(a => a.ActionType == actionType);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.ActionTimestamp >= fromDate);

            if (toDate.HasValue)
                query = query.Where(a => a.ActionTimestamp <= toDate);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(a => a.User)
                .OrderByDescending(a => a.ActionTimestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToDto(a))
                .ToListAsync();

            return new PaginatedResult<AuditLogDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return new PaginatedResult<AuditLogDto> { Items = new List<AuditLogDto>() };
        }
    }

    public async Task<PaginatedResult<AuditLogDto>> GetUserActionHistoryAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50)
    {
        return await GetAuditLogsAsync(userId: userId, pageNumber: pageNumber, pageSize: pageSize);
    }

    public async Task<List<AuditLogDto>> GetEntityChangeHistoryAsync(
        Guid entityId,
        string entityType)
    {
        try
        {
            var logs = await _unitOfWork.AuditLogs.GetQueryable()
                .Where(a => a.EntityId == entityId && a.EntityType == entityType)
                .Include(a => a.User)
                .OrderBy(a => a.ActionTimestamp)
                .Select(a => MapToDto(a))
                .ToListAsync();

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity change history for {EntityType} {EntityId}", entityType, entityId);
            return new List<AuditLogDto>();
        }
    }

    private string? SerializeToJson(object? obj)
    {
        if (obj == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(obj);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize object to JSON");
            return null;
        }
    }

    private string? GenerateChangesSummary(object? oldValues, object? newValues)
    {
        if (oldValues == null || newValues == null)
            return null;

        try
        {
            var changes = new List<string>();

            if (oldValues is Dictionary<string, object> oldDict && 
                newValues is Dictionary<string, object> newDict)
            {
                foreach (var key in oldDict.Keys)
                {
                    if (newDict.TryGetValue(key, out var newValue))
                    {
                        if (!Equals(oldDict[key], newValue))
                        {
                            changes.Add($"{key}: {oldDict[key]} → {newValue}");
                        }
                    }
                }
            }

            return changes.Any() ? string.Join("; ", changes) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate changes summary");
            return null;
        }
    }

    private bool IsActionReversible(string actionType)
    {
        return actionType switch
        {
            "Update" => true,
            "StatusChange" => true,
            "Assign" => true,
            "Reassign" => true,
            "Approve" => true,
            "Reject" => true,
            "Create" => false,
            "Delete" => false,
            _ => false
        };
    }

    private AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            ActionType = log.ActionType,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            RelatedEntityId = log.RelatedEntityId,
            RelatedEntityType = log.RelatedEntityType,
            UserId = log.UserId,
            UserName = log.User?.FullName,
            UserEmail = log.User?.Email,
            IpAddress = log.IpAddress,
            Description = log.Description,
            ChangesSummary = log.ChangesSummary,
            SeverityLevel = log.Severity.ToString(),
            Status = log.Status.ToString(),
            ErrorMessage = log.ErrorMessage,
            DurationMs = log.DurationMs,
            ActionTimestamp = log.ActionTimestamp,
            Changes = ParseJson(log.NewValues)
        };
    }

    private Dictionary<string, object>? ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
