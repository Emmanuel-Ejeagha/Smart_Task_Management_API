using System;

namespace SmartTaskManagementAPI.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendTaskReminderEmailAsync(string toEmail, string userName, string taskTitle, DateTime dueDate, CancellationToken cancellationToken = default);
    Task SendOverdueTaskNotificationAsync(string toEmail, string userName, string taskTitle, DateTime dueDate, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string toEmail, string userName, string tenantName, CancellationToken cancellationToken = default);
}

