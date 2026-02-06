using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;
        
        // Check if user has Admin role
        var isAdmin = httpContext.User.IsInRole("Admin");
        
        // For development, also allow if it's localhost
        var isLocal = httpContext.Request.Host.Host == "localhost" || 
                      httpContext.Request.Host.Host == "127.0.0.1";
        
        // Allow access if user is Admin OR if it's local development
        return isAdmin || isLocal;
    }
}