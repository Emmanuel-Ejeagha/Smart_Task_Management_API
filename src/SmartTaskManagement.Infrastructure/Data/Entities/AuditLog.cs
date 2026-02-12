namespace SmartTaskManagement.Infrastructure.Data.Entities;

/// <summary>
/// Entity for audit logging (not a domain entity, infrastructure concern)
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}