using System;
using System.Security.Claims;

namespace SmartTaskManagementAPI.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Try to get tenant ID from various sources in order of priority
            var tenantId = ResolveTenantId(context);

            if (!string.IsNullOrEmpty(tenantId))
            {
                // Add tenant ID to HttpContext items for later use
                context.Items["TenantId"] = tenantId;

                // Also add to claims if user id authenticated
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var identity = context.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Remove existing tenant claim if present
                        var existingClaim = identity.FindFirst("TenantId");
                        if (existingClaim != null)
                        {
                            identity.RemoveClaim(existingClaim);
                        }

                        identity.AddClaim(new Claim("TenantId", tenantId));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant in middleware");
        }

        await _next(context);
    }

    private string? ResolveTenantId(HttpContext context)
    {
        // 1. Check for X-Tenant-Id header (for API calls)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            return headerTenantId.ToString();
        }

        // 2. Check for tenant in JWT claim (for authenticated users)
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("TenantId");
            if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
            {
                return tenantClaim.Value;
            }

            // Fallback to subdomain for web applications
            var domainClaim = context.User.FindFirst("domain");
            if (domainClaim != null && !string.IsNullOrEmpty(domainClaim.Value))
            {
                return domainClaim.Value;
            }
        }

        // 3. Check for tenant in query string (for specific use cases)
        if (context.Request.Query.TryGetValue("tenantId", out var queryTenantId))
        {
            return queryTenantId.ToString();
        }

        // 4. Check subdomain (for multi-tenant SaaS appliactions)
        var host = context.Request.Host.Host;
        if (host.Contains('.'))
        {
            var subdomain = host.Split('.')[0];
            if (!string.IsNullOrEmpty(subdomain) && subdomain != "www" && subdomain != "api")
            {
                return subdomain;
            }
        }

        return null;
    }
}
