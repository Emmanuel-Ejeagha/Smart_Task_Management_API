namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Service to get current user information from Auth0 claims
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Get current user ID from Auth0 claims
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Get current user email from Auth0 claims
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Get current user roles from Auth0 claims
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Get tenant ID from Auth0 claims
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Check if current user has a specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Check if current user is admin
    /// </summary>
    bool IsAdmin { get; }

    /// <summary>
    /// Check if current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}