using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartTaskManagement.Application.Common.Interfaces;
using SmartTaskManagement.Application.Features.Reminders.Commands.TriggerReminder;
using SmartTaskManagement.Infrastructure.Data;

namespace SmartTaskManagement.Infrastructure.BackgroundJobs;

public class ReminderJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReminderJob> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEmailService _emailService;

    public ReminderJob(
        IMediator mediator,
        ILogger<ReminderJob> logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IEmailService emailService)
    {
        _mediator = mediator;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _emailService = emailService;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessReminderAsync(Guid reminderId, IJobCancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing reminder {ReminderId}", reminderId);

            using var context = _dbContextFactory.CreateDbContext();
            
            // Get reminder with work item details
            var reminder = await context.Reminders
                .Include(r => r.WorkItem)
                .FirstOrDefaultAsync(r => r.Id == reminderId);

            if (reminder == null)
            {
                _logger.LogWarning("Reminder {ReminderId} not found", reminderId);
                return;
            }

            // Check if reminder is still scheduled
            if (reminder.Status != Domain.Enums.ReminderStatus.Scheduled)
            {
                _logger.LogInformation("Reminder {ReminderId} is already processed (status: {Status})", 
                    reminderId, reminder.Status);
                return;
            }

            // Try to trigger the reminder via command
            var command = new TriggerReminderCommand
            {
                ReminderId = reminderId
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to trigger reminder {ReminderId}: {Error}", 
                    reminderId, result.Error);
                
                // Mark as failed
                var failCommand = new TriggerReminderCommand
                {
                    ReminderId = reminderId,
                    ErrorMessage = result.Error
                };
                
                await _mediator.Send(failCommand);
            }
            else
            {
                _logger.LogInformation("Successfully triggered reminder {ReminderId}", reminderId);
                
                // Optionally send email notification
                await SendReminderNotificationAsync(reminder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reminder {ReminderId}", reminderId);
            throw; // Hangfire will retry based on retry policy
        }
    }

    private async Task SendReminderNotificationAsync(Domain.Entities.Reminder reminder)
    {
        try
        {
            if (reminder.WorkItem == null)
                return;

            // Get user email (would come from Auth0 or user profile)
            // For now, we'll use a placeholder
            var userEmail = "user@example.com"; // This should come from user service
            
            await _emailService.SendReminderEmailAsync(
                userEmail,
                reminder.WorkItem.Title,
                reminder.Message,
                reminder.ReminderDateUtc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder notification for reminder {ReminderId}", 
                reminder.Id);
            // Don't throw - email failure shouldn't fail the job
        }
    }

    public async Task LogReminderScheduledAsync(Guid reminderId, string jobId)
    {
        _logger.LogInformation("Reminder {ReminderId} scheduled with job {JobId}", 
            reminderId, jobId);
        
        // Could store job ID in database for tracking
        await Task.CompletedTask;
    }

    [AutomaticRetry(Attempts = 0)] // Don't retry batch processing failures
    public async Task ProcessMultipleRemindersAsync(List<Guid> reminderIds, IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing batch of {Count} reminders", reminderIds.Count);

        foreach (var reminderId in reminderIds)
        {
            if (cancellationToken.ShutdownToken.IsCancellationRequested)
                break;

            await ProcessReminderAsync(reminderId, cancellationToken);
        }
    }
}