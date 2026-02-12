using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace SmartTaskManagement.Infrastructure.BackgroundJobs;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// Only allow admins to access the dashboard
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HangfireDashboardAuthorizationFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return false;

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        // Check if user has admin role
        // This assumes you have a role claim in your JWT
        var isAdmin = httpContext.User.IsInRole("Admin");
        
        return isAdmin;
    }
}