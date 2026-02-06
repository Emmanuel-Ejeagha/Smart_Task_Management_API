using Microsoft.Extensions.Primitives;

namespace SmartTaskManagementAPI.API.Middleware;


public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant from various sources (header, claim, query string, subdomain)
        var tenantId = ResolveTenantId(context);
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            // Add tenant ID to the request context for later use
            context.Items["TenantId"] = tenantId;
            
            // Log tenant resolution (without sensitive data)
            _logger.LogDebug("Resolved tenant: {TenantId} for request: {Path}", 
                tenantId, context.Request.Path);
        }
        else
        {
            _logger.LogDebug("No tenant resolved for request: {Path}", context.Request.Path);
        }

        await _next(context);
    }

    private string? ResolveTenantId(HttpContext context)
    {
        // Priority order for tenant resolution:
        // 1. JWT claim (already handled by authentication)
        // 2. X-Tenant-ID header
        // 3. Query string parameter
        // 4. Subdomain
        
        // Check X-Tenant-ID header
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out StringValues tenantHeader))
        {
            return tenantHeader.ToString();
        }

        // Check query string
        if (context.Request.Query.TryGetValue("tenantId", out StringValues tenantQuery))
        {
            return tenantQuery.ToString();
        }

        // Check subdomain (if applicable)
        var host = context.Request.Host.Host;
        if (host.Contains('.'))
        {
            var subdomain = host.Split('.')[0];
            if (!subdomain.Equals("www", StringComparison.OrdinalIgnoreCase) &&
                !subdomain.Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return subdomain;
            }
        }

        return null;
    }
}