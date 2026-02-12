namespace SmartTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email
    /// </summary>
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send reminder email
    /// </summary>
    Task SendReminderEmailAsync(
        string toEmail,
        string workItemTitle,
        string reminderMessage,
        DateTime dueDate,
        CancellationToken cancellationToken = default);
}