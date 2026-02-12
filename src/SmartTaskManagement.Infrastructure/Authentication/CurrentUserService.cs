using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Infrastructure.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("userId");

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public IReadOnlyList<string> Roles => _httpContextAccessor.HttpContext?.User?
        .FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList()
        ?? new List<string>();

    public Guid? TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenantId")
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("https://smarttaskmanagement.com/tenantId");

            if (Guid.TryParse(tenantIdClaim, out var tenantId))
                return tenantId;

            return null;
        }
    }

    public bool IsInRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAdmin => IsInRole("Admin");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Get all claims
    /// </summary>
    public IEnumerable<Claim> GetClaims()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
    }

    /// <summary>
    /// Get specific claim value
    /// </summary>
    public string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
    }

    /// <summary>
    /// Get user's full name
    /// </summary>
    public string? GetFullName()
    {
        var givenName = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.GivenName);
        var familyName = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Surname);
        
        if (!string.IsNullOrEmpty(givenName) && !string.IsNullOrEmpty(familyName))
            return $"{givenName} {familyName}";
        
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue("name");
    }

    /// <summary>
    /// Check if user has permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        var permissions = _httpContextAccessor.HttpContext?.User?.FindAll("permissions")
            .Select(c => c.Value)
            .ToList()
            ?? new List<string>();

        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}