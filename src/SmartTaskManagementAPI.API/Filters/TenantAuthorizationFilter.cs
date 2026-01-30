
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartTaskManagementAPI.API.Filters;

public class TenantAuthorizationFilter : IAuthorizationFilter
{
    private readonly ILogger<TenantAuthorizationFilter> _logger;
    public TenantAuthorizationFilter(ILogger<TenantAuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip authorization if action is decorated with [AllowAnonymous]
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType().Name == "AllowAnonymousAttribute"))
            return;

        // Get tenant ID from user claims
        var tenantClaim = context.HttpContext.User.FindFirst("TenantId");
        if (tenantClaim == null)
        {
            _logger.LogWarning("TenantId claim not found in user claims");
            context.Result = new ForbidResult();
            return;
        }

        // Get tenant ID from route/query if available (for cross-tenant operations by admins)
        var requestedTenantId = GetRequestedTenantId(context.HttpContext);

        if (!string.IsNullOrEmpty(requestedTenantId))
        {
            // Check if user is trying to access a different tenant
            if (tenantClaim.Value != requestedTenantId)
            {
                // Only allow if user is an admin
                var isAdmin = context.HttpContext.User.IsInRole("Admin");
                if (!isAdmin)
                {
                    _logger.LogWarning(
                        "User from tenant {UserTenantId} attempted to access tenant {RequestedTenantId}",
                        tenantClaim.Value, requestedTenantId);
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        // Store Tenant ID in HttpContext for use in controllers
        context.HttpContext.Items["CurrentTenantId"] = tenantClaim.Value;
    }

    private static string? GetRequestedTenantId(HttpContext httpContext)
    {
        // Check route values first
        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var tenantId))
        {
            return tenantId?.ToString();
        }

        // Check query string
        if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId))
        {
            return queryTenantId.ToString();
        }

        // Check headers
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            return headerTenantId.ToString();
        }

        return null;
    }
}
