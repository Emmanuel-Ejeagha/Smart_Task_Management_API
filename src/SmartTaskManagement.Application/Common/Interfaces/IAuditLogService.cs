using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Service for logging audit trails
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task LogAsync(
        string entityName,
        Guid entityId,
        string action,
        string? oldValues,
        string? newValues,
        string changedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid tenantId,
        PaginationRequest pagination,
        AuditLogFilter? filter = null,
        CancellationToken cancellationToken = default);
}