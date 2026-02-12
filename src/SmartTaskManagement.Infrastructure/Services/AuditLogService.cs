using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Infrastructure.Data;
using SmartTaskManagement.Infrastructure.Data.Entities;
using System.Text.Json;

namespace SmartTaskManagement.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AuditLogService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string entityName,
        Guid entityId,
        string action,
        string? oldValues,
        string? newValues,
        string changedBy,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _currentUserService.TenantId;
            
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot log audit entry without tenant ID");
                return;
            }

            ipAddress ??= GetClientIpAddress();
            userAgent ??= GetUserAgent();

            var auditLog = new AuditLog
            {
                TenantId = tenantId.Value,
                EntityName = entityName,
                EntityId = entityId.ToString(),
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedBy = changedBy,
                ChangedAtUtc = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit entry for {EntityName} {EntityId}", 
                entityName, entityId);
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    public async Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid tenantId,
        PaginationRequest pagination,
        AuditLogFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        // Start with base query
        var query = _context.AuditLogs
            .Where(a => a.TenantId == tenantId);

        // Apply filtering
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.EntityName))
                query = query.Where(a => a.EntityName == filter.EntityName);

            if (filter.EntityId.HasValue)
                query = query.Where(a => a.EntityId == filter.EntityId.Value.ToString());

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (!string.IsNullOrEmpty(filter.ChangedBy))
                query = query.Where(a => a.ChangedBy == filter.ChangedBy);

            if (filter.FromDateUtc.HasValue)
                query = query.Where(a => a.ChangedAtUtc >= filter.FromDateUtc.Value);

            if (filter.ToDateUtc.HasValue)
                query = query.Where(a => a.ChangedAtUtc <= filter.ToDateUtc.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var items = await query
            .OrderByDescending(a => a.ChangedAtUtc)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = Guid.Parse(a.EntityId),
                Action = a.Action,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                ChangedBy = a.ChangedBy,
                ChangedAtUtc = a.ChangedAtUtc,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent
            })
            .ToListAsync(cancellationToken);

        return PaginatedResult<AuditLogDto>.Create(
            items, pagination.PageNumber, pagination.PageSize, totalCount);
    }

    /// <summary>
    /// Log entity changes automatically
    /// </summary>
    public async Task LogEntityChangesAsync(
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> changes,
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in changes)
        {
            if (entry.Entity is AuditLog) // Don't audit audit logs
                continue;

            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry.Entity);
            var action = entry.State.ToString();
            var changedBy = _currentUserService.UserId ?? "system";

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified)
            {
                var changedProperties = entry.Properties
                    .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
                    .ToList();

                if (changedProperties.Any())
                {
                    oldValues = JsonSerializer.Serialize(
                        changedProperties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                    
                    newValues = JsonSerializer.Serialize(
                        changedProperties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var properties = entry.Properties
                    .Where(p => !p.Metadata.IsPrimaryKey())
                    .ToList();

                if (properties.Any())
                {
                    newValues = JsonSerializer.Serialize(
                        properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                }
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                await LogAsync(
                    entityName,
                    Guid.Parse(entityId),
                    action,
                    oldValues,
                    newValues,
                    changedBy,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private string? GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            return value?.ToString();
        }
        return null;
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get IP from X-Forwarded-For header (for proxy scenarios)
        var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
            return forwardedHeader.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();

        // Fall back to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
    }
}