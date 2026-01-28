using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Common.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendTaskReminderEmailAsync(string toEmail, string userName, string taskTitle, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "SENDING REMINDER EMAIL - To: {Email}, User: {UserName}, Task: {TaskTitle}, Due: {DueDate}",
                toEmail, userName, taskTitle, dueDate.ToString("yyyy-MM-dd HH:mm"));
            
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation(
                "REMINDER EMAIL SENT - To: {Email}, User: {UserName}",
                toEmail, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendOverdueTaskNotificationAsync(string toEmail, string userName, string taskTitle, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "SENDING OVERDUE NOTIFICATION - To: {Email}, User: {UserName}, Task: {TaskTitle}, Due: {DueDate}",
                toEmail, userName, taskTitle, dueDate.ToString("yyyy-MM-dd HH:mm"));
            
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation(
                "OVERDUE NOTIFICATION SENT - To: {Email}, User: {UserName}",
                toEmail, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send overdue notification to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName, string tenantName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "SENDING WELCOME EMAIL - To: {Email}, User: {UserName}, Tenant: {TenantName}",
                toEmail, userName, tenantName);
            
            await Task.Delay(100, cancellationToken);
            
            _logger.LogInformation(
                "WELCOME EMAIL SENT - To: {Email}, User: {UserName}",
                toEmail, userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }
}