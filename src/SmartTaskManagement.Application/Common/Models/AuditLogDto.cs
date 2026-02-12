namespace SmartTaskManagement.Application.Common.Models;

/// <summary>
/// DTO for audit log entries
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Request for filtering audit logs
/// </summary>
public class AuditLogFilter
{
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? Action { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? FromDateUtc { get; set; }
    public DateTime? ToDateUtc { get; set; }
}