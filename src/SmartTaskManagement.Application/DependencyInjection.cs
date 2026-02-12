using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SmartTaskManagement.Application.Common.Behaviors;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Services;

namespace SmartTaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            
            // Register pipeline behaviors (order matters)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Register domain services
        services.AddScoped<IWorkItemService, WorkItemDomainService>();

        // Register application services (for interfaces defined in Application layer)
        // These will be implemented in Infrastructure layer
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}

// Stub implementations (will be implemented in Infrastructure layer)
public class CurrentUserService : ICurrentUserService
{
    public string? UserId => throw new NotImplementedException();
    public string? Email => throw new NotImplementedException();
    public IReadOnlyList<string> Roles => throw new NotImplementedException();
    public Guid? TenantId => throw new NotImplementedException();
    public bool IsInRole(string role) => throw new NotImplementedException();
    public bool IsAdmin => throw new NotImplementedException();
    public bool IsAuthenticated => throw new NotImplementedException();
}

public class BackgroundJobService : IBackgroundJobService
{
    public string ScheduleReminder(Guid reminderId, DateTime reminderDate) => throw new NotImplementedException();
    public string ScheduleDueRemindersCheck(TimeSpan interval) => throw new NotImplementedException();
    public bool DeleteJob(string jobId) => throw new NotImplementedException();
    public bool RescheduleJob(string jobId, DateTime newDate) => throw new NotImplementedException();
    public bool TriggerJob(string jobId) => throw new NotImplementedException();
}

public class AuditLogService : IAuditLogService
{
    public Task LogAsync(string entityName, Guid entityId, string action, string? oldValues, string? newValues, string changedBy, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    
    public Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(Guid tenantId, PaginationRequest pagination, AuditLogFilter? filter = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

public class EmailService : IEmailService
{
    public Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
    
    public Task SendReminderEmailAsync(string toEmail, string workItemTitle, string reminderMessage, DateTime dueDate, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}