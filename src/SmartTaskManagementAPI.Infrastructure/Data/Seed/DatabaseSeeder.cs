using System;
using Microsoft.Extensions.DependencyInjection;
using UserEntity = SmartTaskManagementAPI.Domain.Entities.User;
using TenantEntity = SmartTaskManagementAPI.Domain.Entities.Tenant;
using TaskEntity = SmartTaskManagementAPI.Domain.Entities.Task;
using Microsoft.EntityFrameworkCore;

namespace SmartTaskManagementAPI.Infrastructure.Data.Seed;

public class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        // Check if we already have data
        if (await context.Tenants.AnyAsync())
        {
            return; // Database has been seeded
        }
        // Create a default tenant
        var tenant = TenantEntity.Create(
            "Default Tenant",
            "default",
            Guid.Empty); // System user

        await context.Tenants.AddAsync(tenant);
        await context.SaveChangesAsync();

        // Create an admin user for the default tenant
        // Note: In a real application, you would hash the password
        var adminUser = UserEntity.Create(
            "Admin",
            "User",
            "admin@smarttask.com",
            "hashed-password-placeholder", // Will be hashed by ASP.NET Identity
            tenant.Id,
            Guid.Empty,
            "Admin");

        adminUser.ConfirmEmail();

        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();

        // Create some sample tasks
        var tasks = new List<TaskEntity>
        {
            TaskEntity.Create("Review project requirements", tenant.Id, adminUser.Id),
            TaskEntity.Create("Design database schema", tenant.Id, adminUser.Id),
            TaskEntity.Create("Implement authentication", tenant.Id, adminUser.Id),
            TaskEntity.Create("Write unit tests", tenant.Id, adminUser.Id)
        };

        // Update some tasks with additional details
        tasks[0].Update(
            "Review project requirements",
            "Go through all project requirements with the team",
            Domain.Enums.TaskPriority.High,
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(2),
            adminUser.Id);
        tasks[0].MoveToInProgress(adminUser.Id);

        tasks[1].Update(
            "Design database schema",
            "Design normalized database schema for multi-tenancy",
            Domain.Enums.TaskPriority.Critical,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(1),
            adminUser.Id);

        tasks[2].Update(
            "implement authentication",
            "Implement JWT authentication with refresh tokens",
            Domain.Enums.TaskPriority.High,
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(4),
            adminUser.Id);

        tasks[3].Update(
            "Write unit tests",
            "Achieve 80% code coverage",
            Domain.Enums.TaskPriority.Medium,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(6),
            adminUser.Id);

        await context.Tasks.AddRangeAsync(tasks);
        await context.SaveChangesAsync();
    }
}
