using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartTaskManagement.Application.Common.Interfaces;
using System.Text.Json;

namespace SmartTaskManagement.API.Filters;

public class AuditLogActionFilter : IAsyncActionFilter
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditLogActionFilter> _logger;

    public AuditLogActionFilter(
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        ILogger<AuditLogActionFilter> logger)
    {
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip audit logging for GET requests and public endpoints
        if (context.HttpContext.Request.Method == HttpMethods.Get ||
            IsPublicEndpoint(context.HttpContext.Request.Path))
        {
            await next();
            return;
        }

        var actionName = context.ActionDescriptor.DisplayName;
        var entityName = GetEntityNameFromAction(actionName ?? "Unknown");
        var actionType = GetActionTypeFromHttpMethod(context.HttpContext.Request.Method);
        
        // Store the original request body for audit logging
        string? requestBody = null;
        
        if (context.HttpContext.Request.Body.CanRead)
        {
            context.HttpContext.Request.EnableBuffering();
            using var reader = new StreamReader(context.HttpContext.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.HttpContext.Request.Body.Position = 0;
        }

        var executedContext = await next();

        // Log the audit entry
        if (executedContext.Result is ObjectResult result && result.Value != null)
        {
            try
            {
                var entityId = ExtractEntityIdFromResult(result.Value);
                var responseBody = JsonSerializer.Serialize(result.Value);
                
                if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var entityIdGuid))
                {
                    await _auditLogService.LogAsync(
                        entityName,
                        entityIdGuid,
                        actionType,
                        null, // Old values - would need before/after comparison
                        responseBody,
                        _currentUserService.UserId ?? "system");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entry for action {ActionName}", actionName);
                // Don't throw - audit logging failure shouldn't break the main operation
            }
        }
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/swagger",
            "/favicon.ico"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p));
    }

    private static string GetEntityNameFromAction(string actionName)
    {
        if (actionName.Contains("WorkItem", StringComparison.OrdinalIgnoreCase))
            return "WorkItem";
        if (actionName.Contains("Reminder", StringComparison.OrdinalIgnoreCase))
            return "Reminder";
        if (actionName.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            return "Tenant";
        
        return "Unknown";
    }

    private static string GetActionTypeFromHttpMethod(string method)
    {
        return method.ToUpper() switch
        {
            "POST" => "Create",
            "PUT" => "Update",
            "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => "Unknown"
        };
    }

    private static string? ExtractEntityIdFromResult(object result)
    {
        // Extract ID from result object using reflection
        var idProperty = result.GetType().GetProperty("Id") 
            ?? result.GetType().GetProperty("WorkItemId")
            ?? result.GetType().GetProperty("TenantId")
            ?? result.GetType().GetProperty("ReminderId");
        
        return idProperty?.GetValue(result)?.ToString();
    }
}