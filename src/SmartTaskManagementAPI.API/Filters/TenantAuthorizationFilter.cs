using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartTaskManagementAPI.Application.Interfaces;
using System.Security.Claims;

namespace SmartTaskManagementAPI.API.Filters;

public class TenantAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<TenantAuthorizationFilter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TenantAuthorizationFilter(
        ILogger<TenantAuthorizationFilter> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip authorization for anonymous endpoints
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            return;

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            return;

        // Get tenant ID from user claims
        var tenantIdClaim = context.HttpContext.User.FindFirstValue("TenantId");
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            _logger.LogWarning("User does not have TenantId claim");
            context.Result = new ForbidResult();
            return;
        }

        // Parse tenant ID
        if (!Guid.TryParse(tenantIdClaim, out var userTenantId))
        {
            _logger.LogWarning("Invalid TenantId claim format: {TenantIdClaim}", tenantIdClaim);
            context.Result = new ForbidResult();
            return;
        }

        // Check if the action requires tenant ID in route
        var routeData = context.RouteData;
        if (routeData.Values.TryGetValue("tenantId", out var routeTenantId))
        {
            if (routeTenantId is string routeTenantIdStr && Guid.TryParse(routeTenantIdStr, out var routeTenantIdGuid))
            {
                if (routeTenantIdGuid != userTenantId)
                {
                    _logger.LogWarning(
                        "Tenant mismatch: User from tenant {UserTenantId} attempted to access tenant {RouteTenantId}",
                        userTenantId, routeTenantIdGuid);
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }

        // For actions that operate on resources, check if resource belongs to user's tenant
        await CheckResourceTenantAccess(context, userTenantId);
    }

    private async Task CheckResourceTenantAccess(AuthorizationFilterContext context, Guid userTenantId)
    {
        try
        {
            // Get resource ID from route if present
            var routeData = context.RouteData;
            
            // Check for task ID
            if (routeData.Values.TryGetValue("id", out var resourceIdValue) && 
                Guid.TryParse(resourceIdValue?.ToString(), out var resourceId))
            {
                var httpContext = context.HttpContext;
                var path = httpContext.Request.Path.ToString();

                using var scope = _serviceProvider.CreateScope();
                
                if (path.Contains("/tasks/", StringComparison.OrdinalIgnoreCase))
                {
                    var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    var task = await taskRepository.GetByIdAsync(resourceId);
                    
                    if (task != null && task.TenantId != userTenantId)
                    {
                        _logger.LogWarning(
                            "Tenant access violation: User from tenant {UserTenantId} attempted to access task {TaskId} from tenant {TaskTenantId}",
                            userTenantId, resourceId, task.TenantId);
                        context.Result = new ForbidResult();
                    }
                }
                else if (path.Contains("/users/", StringComparison.OrdinalIgnoreCase) && 
                         !path.EndsWith("/me", StringComparison.OrdinalIgnoreCase))
                {
                    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    var user = await userRepository.GetByIdAsync(resourceId);
                    
                    if (user != null && user.TenantId != userTenantId)
                    {
                        _logger.LogWarning(
                            "Tenant access violation: User from tenant {UserTenantId} attempted to access user {TargetUserId} from tenant {TargetUserTenantId}",
                            userTenantId, resourceId, user.TenantId);
                        context.Result = new ForbidResult();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource tenant access");
            // Don't block on error - let other filters handle it
        }
    }
}