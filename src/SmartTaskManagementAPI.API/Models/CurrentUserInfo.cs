using System;

namespace SmartTaskManagementAPI.API.Models;

public class CurrentUserInfo
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsAdmin { get; set; }
    public List<ClaimInfo> Claims { get; set; } = new();
}

public class ClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

