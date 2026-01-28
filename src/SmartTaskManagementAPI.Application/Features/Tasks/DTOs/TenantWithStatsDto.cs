
namespace SmartTaskManagementAPI.Application.Features.Tasks.DTOs;

public class TenantWithStatsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int TaskCount { get; set; }
    public int ActiveUserCount { get; set; }
}