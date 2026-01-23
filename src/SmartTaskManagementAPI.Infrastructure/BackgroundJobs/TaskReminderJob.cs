using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManagementAPI.Application.Interfaces;

namespace SmartTaskManagementAPI.Infrastructure.BackgroundJobs;

public class TaskReminderJob
{
    private readonly ILogger<TaskReminderJob> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaskReminderJob(ILogger<TaskReminderJob> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task SendReminderAsync()
    {
        _logger.LogInformation("Starting task reminder job at {UtcNow}", DateTime.UtcNow);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            var tasksDueForReminder = await taskRepository.GetTasksDueForReminderAsync();

            _logger.LogInformation("Found {Count} tasks due for reminder", tasksDueForReminder.Count());

            foreach (var task in tasksDueForReminder)
            {
                // I will implement send email notification here later
                _logger.LogInformation(
                    "Reminder for task: {TaskTitle} (ID: {TaskId}), Due: {DueDate}, User: {UserId}",
                    task.Title, task.Id, task.DueDate, task.CreatedBy);

                // I will add logic for sending email later
                // await _emailService.SendTaskReminderAsync(task);
            }

            _logger.LogInformation("Completed task reminder job at {UtcNow}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending task reminders");
        }
    }
}
