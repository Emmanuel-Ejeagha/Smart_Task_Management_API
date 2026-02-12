using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartTaskManagement.Infrastructure.Authentication;

/// <summary>
/// Custom claims transformation for Auth0
/// Maps Auth0 claims to application claims
/// </summary>
public class Auth0ClaimsPrincipalFactory
{
    private readonly IOptions<JwtBearerOptions> _jwtBearerOptions;
    private readonly ILogger<Auth0ClaimsPrincipalFactory> _logger;

    public Auth0ClaimsPrincipalFactory(
        IOptions<JwtBearerOptions> jwtBearerOptions,
        ILogger<Auth0ClaimsPrincipalFactory> logger)
    {
        _jwtBearerOptions = jwtBearerOptions;
        _logger = logger;
    }

    /// <summary>
    /// Transform claims principal after Auth0 authentication
    /// </summary>
    public ClaimsPrincipal Transform(ClaimsPrincipal principal)
    {
        var claims = principal.Claims.ToList();
        var identity = principal.Identity as ClaimsIdentity;

        if (identity == null)
            return principal;

        // Extract user ID from Auth0 sub claim
        var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
        if (subClaim != null)
        {
            // Auth0 sub format: "auth0|1234567890" or "google-oauth2|1234567890"
            var parts = subClaim.Value.Split('|');
            if (parts.Length == 2)
            {
                // Add simpler user ID claim
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, parts[1]));
            }
        }

        // Map Auth0 permissions to claims
        var permissions = claims.Where(c => c.Type == "permissions").ToList();
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permissions", permission.Value));
        }

        // Extract tenant ID from custom claim
        var tenantIdClaim = claims.FirstOrDefault(c => 
            c.Type == "https://smarttaskmanagement.com/tenantId" ||
            c.Type == "tenantId");

        if (tenantIdClaim != null)
        {
            identity.AddClaim(new Claim("tenantId", tenantIdClaim.Value));
        }

        // Map Auth0 roles
        var roles = claims.Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role).ToList();
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));
        }

        _logger.LogInformation("Transformed claims for user {UserId}", 
            identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return principal;
    }
}