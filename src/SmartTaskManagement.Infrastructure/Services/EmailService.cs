using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartTaskManagement.Application.Common.Interfaces;

namespace SmartTaskManagement.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly ISendGridClient _sendGridClient;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings,
        ISendGridClient sendGridClient)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _sendGridClient = sendGridClient;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
            var to = new EmailAddress(toEmail);
            
            var message = MailHelper.CreateSingleEmail(from, to, subject, 
                isHtml ? null : body, 
                isHtml ? body : null);

            var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send email to {ToEmail}. Status: {StatusCode}, Body: {ErrorBody}",
                    toEmail, response.StatusCode, errorBody);
                throw new Exception($"Failed to send email: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            throw;
        }
    }

    public async Task SendReminderEmailAsync(
        string toEmail,
        string workItemTitle,
        string reminderMessage,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Reminder: {workItemTitle}";
        
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; background-color: #f9f9f9; }}
                    .footer {{ text-align: center; padding: 10px; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Task Reminder</h1>
                    </div>
                    <div class='content'>
                        <h2>{workItemTitle}</h2>
                        <p><strong>Reminder:</strong> {reminderMessage}</p>
                        <p><strong>Due:</strong> {dueDate:MMMM dd, yyyy hh:mm tt} UTC</p>
                        <p>Please log in to your Smart Task Management account to view and update this task.</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated reminder from Smart Task Management.</p>
                        <p>© {DateTime.UtcNow.Year} Smart Task Management. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
    }

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to Smart Task Management!";
        
        var body = $@"
            <h1>Welcome, {userName}!</h1>
            <p>Thank you for joining Smart Task Management.</p>
            <p>Get started by creating your first task and setting up reminders.</p>
            <p>If you have any questions, please contact our support team.</p>";

        await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password";
        
        var body = $@"
            <h1>Password Reset Request</h1>
            <p>You requested to reset your password. Click the link below to proceed:</p>
            <p><a href='{resetLink}'>Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p><strong>Note:</strong> This link will expire in 24 hours.</p>";

        await SendEmailAsync(toEmail, subject, body, true, cancellationToken);
    }
}

public class EmailSettings
{
    public string FromEmail { get; set; } = "noreply@smarttaskmanagement.com";
    public string FromName { get; set; } = "Smart Task Management";
    public string SendGridApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}