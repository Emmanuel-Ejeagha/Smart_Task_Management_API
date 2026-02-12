using System.Security.Claims;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
    {
        // Skip tenant validation for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!currentUserService.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "User is not authenticated"
            });
            return;
        }

        // Get tenant ID from claims
        var tenantId = currentUserService.TenantId;
        
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("User {UserId} is authenticated but has no tenant claim", 
                currentUserService.UserId);
            
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "User does not belong to any tenant"
            });
            return;
        }

        // Add tenant ID to response headers for debugging
        context.Response.Headers.Append("X-Tenant-Id", tenantId.Value.ToString());

        await _next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/swagger",
            "/favicon.ico",
            "/robots.txt",
            "/.well-known"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p));
    }

    /// <summary>
    /// Validate tenant access for a specific resource
    /// </summary>
    public static bool ValidateTenantAccess(Guid resourceTenantId, Guid userTenantId, ILogger logger)
    {
        if (resourceTenantId != userTenantId)
        {
            logger.LogWarning(
                "Tenant validation failed: User tenant {UserTenantId} tried to access resource from tenant {ResourceTenantId}",
                userTenantId, resourceTenantId);
            
            return false;
        }

        return true;
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}