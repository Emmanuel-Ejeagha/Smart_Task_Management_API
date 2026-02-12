using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;

namespace SmartTaskManagement.Infrastructure.Data;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // ----- TENANT SEEDING -----
        Tenant? defaultTenant = null;
        
        if (!await context.Tenants.AnyAsync())
        {
            defaultTenant = new Tenant(
                "Default Tenant",
                "Default tenant for initial setup",
                "system");
            await context.Tenants.AddAsync(defaultTenant);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Created default tenant");
        }
        else
        {
            // Safely retrieve the first tenant – never assume the query will return data.
            defaultTenant = await context.Tenants.FirstOrDefaultAsync();
            if (defaultTenant == null)
            {
                // This should never happen if the query filter is correct.
                throw new InvalidOperationException(
                    "Tenant exists in database but query returned null. " +
                    "Check global query filter configuration.");
            }
        }

        // ----- WORK ITEM SEEDING -----
        if (!await context.WorkItems.AnyAsync())
        {
            // Guard: Ensure we have a tenant ID to use.
            if (defaultTenant == null)
                throw new InvalidOperationException("Cannot seed work items: no tenant available.");

            var sampleWorkItems = new List<WorkItem>
            {
                new WorkItem(
                    defaultTenant.Id,
                    "Complete project documentation",
                    "Write comprehensive documentation for the Smart Task Management API",
                    WorkItemPriority.High,
                    DateTime.UtcNow.AddDays(7),
                    "system")
                {
                    EstimatedHours = 8,
                    State = WorkItemState.InProgress
                },
                new WorkItem(
                    defaultTenant.Id,
                    "Set up CI/CD pipeline",
                    "Configure GitHub Actions for automated testing and deployment",
                    WorkItemPriority.Medium,
                    DateTime.UtcNow.AddDays(14),
                    "system")
                {
                    EstimatedHours = 12,
                    State = WorkItemState.Draft
                },
                new WorkItem(
                    defaultTenant.Id,
                    "Review pull requests",
                    "Review pending pull requests from the development team",
                    WorkItemPriority.Critical,
                    DateTime.UtcNow.AddDays(1),
                    "system")
                {
                    EstimatedHours = 4,
                    State = WorkItemState.InProgress
                }
            };

            // Add tags
            sampleWorkItems[0].AddTag("documentation", "system");
            sampleWorkItems[0].AddTag("high-priority", "system");
            sampleWorkItems[1].AddTag("devops", "system");
            sampleWorkItems[1].AddTag("automation", "system");
            sampleWorkItems[2].AddTag("code-review", "system");
            sampleWorkItems[2].AddTag("urgent", "system");

            await context.WorkItems.AddRangeAsync(sampleWorkItems);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Created {sampleWorkItems.Count} sample work items");
        }

        // ----- REMINDER SEEDING -----
        if (!await context.Reminders.AnyAsync())
        {
            var workItem = await context.WorkItems.FirstOrDefaultAsync();
            if (workItem != null)
            {
                var sampleReminder = new Reminder(
                    workItem.Id,
                    DateTime.UtcNow.AddHours(2),
                    "Don't forget to update the documentation!",
                    "system");
                await context.Reminders.AddAsync(sampleReminder);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Created sample reminder");
            }
        }

        Console.WriteLine("✅ Database seeding completed successfully");
    }
}