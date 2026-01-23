using System;
using Microsoft.AspNetCore.Identity;

namespace SmartTaskManagementAPI.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? DomainUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpireTime { get; set; }
    public bool IsActive { get; set; } = true;

    public string GetFullName() => $"{FirstName} {LastName}".Trim();
}
